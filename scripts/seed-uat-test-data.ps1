param(
    [string]$BaseUrl = "http://127.0.0.1:5257"
)

$ErrorActionPreference = "Stop"

function Invoke-Api {
    param(
        [ValidateSet("GET", "POST", "PUT", "DELETE")]
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [switch]$NoContent
    )

    $headers = @{}
    if ($script:Token) {
        $headers.Authorization = "Bearer $script:Token"
    }

    $uri = "$BaseUrl$Path"
    $params = @{
        Method = $Method
        Uri = $uri
        Headers = $headers
        TimeoutSec = 60
    }

    if ($null -ne $Body) {
        $params.ContentType = "application/json"
        $params.Body = ($Body | ConvertTo-Json -Depth 20)
    }

    try {
        if ($NoContent) {
            Invoke-RestMethod @params | Out-Null
            return $null
        }

        return Invoke-RestMethod @params
    }
    catch {
        $detail = $_.ErrorDetails.Message
        if ([string]::IsNullOrWhiteSpace($detail) -and $_.Exception.Response) {
            try {
                $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
                $detail = $reader.ReadToEnd()
                $reader.Dispose()
            }
            catch {
                $detail = $null
            }
        }
        if ([string]::IsNullOrWhiteSpace($detail)) {
            $detail = $_.Exception.Message
        }
        throw "$Method $Path failed: $detail"
    }
}

function Get-OrCreate {
    param(
        [string]$ListPath,
        [scriptblock]$Match,
        [string]$CreatePath,
        [object]$Body
    )

    $existing = @(Invoke-Api GET $ListPath)
    $found = @($existing | Where-Object $Match)[0]
    if ($found) {
        return $found
    }

    return Invoke-Api POST $CreatePath $Body
}

function Post-NoContent([string]$Path, [object]$Body = @{}) {
    Invoke-Api POST $Path $Body -NoContent
}

function Assert-Equal {
    param([string]$Name, [decimal]$Actual, [decimal]$Expected)
    if ([decimal]::Round($Actual, 4) -ne [decimal]::Round($Expected, 4)) {
        throw "$Name expected $Expected but got $Actual"
    }
}

$script:Token = $null

Write-Host "Checking API health..."
Invoke-RestMethod -Uri "$BaseUrl/health" -TimeoutSec 30 | Out-Null

Write-Host "Signing in or bootstrapping admin..."
$email = "admin@local"
$password = "Passw0rd1"
try {
    $auth = Invoke-RestMethod -Method POST -Uri "$BaseUrl/api/auth/login" -ContentType "application/json" -Body (@{ email = $email; password = $password } | ConvertTo-Json)
}
catch {
    $auth = Invoke-RestMethod -Method POST -Uri "$BaseUrl/api/auth/register" -ContentType "application/json" -Body (@{ email = $email; password = $password; displayName = "Admin" } | ConvertTo-Json)
}
$script:Token = $auth.token

Write-Host "Clearing transaction test data..."
foreach ($path in @(
    "/api/admin/test-data/clear-service",
    "/api/admin/test-data/clear-purchase-orders",
    "/api/admin/test-data/zero-stock"
)) {
    try {
        Post-NoContent $path @{}
    }
    catch {
        Write-Host "Cleanup skipped/failed for ${path}: $($_.Exception.Message)"
    }
}

Write-Host "Creating master data from checklist..."
$mainWarehouse = @(Get-OrCreate "/api/warehouses" { $_.code -eq "MAIN" } "/api/warehouses" @{ code = "MAIN"; name = "Main Warehouse"; address = $null })[0]
$secWarehouse = @(Get-OrCreate "/api/warehouses" { $_.code -eq "SEC" } "/api/warehouses" @{ code = "SEC"; name = "Secondary Warehouse"; address = $null })[0]

$mainBins = @(Invoke-Api GET "/api/warehouses/$($mainWarehouse.id)/bins")
if (-not ($mainBins | Where-Object { $_.code -eq "A1-R1-S1" })) {
    Invoke-Api POST "/api/warehouses/$($mainWarehouse.id)/bins" @{ code = "A1-R1-S1"; name = "Aisle 1 Rack 1 Shelf 1"; zone = "A1"; rack = "R1"; shelf = "S1" } | Out-Null
}
$secBins = @(Invoke-Api GET "/api/warehouses/$($secWarehouse.id)/bins")
if (-not ($secBins | Where-Object { $_.code -eq "B1-R1-S1" })) {
    Invoke-Api POST "/api/warehouses/$($secWarehouse.id)/bins" @{ code = "B1-R1-S1"; name = "Secondary Rack 1 Shelf 1"; zone = "B1"; rack = "R1"; shelf = "S1" } | Out-Null
}

$supplier = @(Get-OrCreate "/api/suppliers" { $_.code -eq "SUP1" } "/api/suppliers" @{ companyId = $null; code = "SUP1"; name = "Test Supplier"; phone = $null; email = $null; address = $null; isAuthorized = $true })[0]
$customer = @(Get-OrCreate "/api/customers" { $_.code -eq "CUS1" } "/api/customers" @{ code = "CUS1"; name = "Test Customer"; phone = "0770000000"; email = $null; address = $null })[0]

$skuCore = @(Get-OrCreate "/api/items" { $_.sku -eq "SKU-CORE" } "/api/items" @{ companyId = $null; sku = "SKU-CORE"; name = "Hydraulic Filter"; type = 2; trackingType = 0; unitOfMeasure = "PCS"; brandId = $null; categoryId = $null; subcategoryId = $null; barcode = $null; defaultUnitCost = 5; revenueAccountId = $null; expenseAccountId = $null })[0]
$skuBatch = @(Get-OrCreate "/api/items" { $_.sku -eq "SKU-BATCH" } "/api/items" @{ companyId = $null; sku = "SKU-BATCH"; name = "Engine Oil Lot Item"; type = 2; trackingType = 2; unitOfMeasure = "PCS"; brandId = $null; categoryId = $null; subcategoryId = $null; barcode = $null; defaultUnitCost = 8; revenueAccountId = $null; expenseAccountId = $null })[0]
$skuSerial = @(Get-OrCreate "/api/items" { $_.sku -eq "SKU-SERIAL" } "/api/items" @{ companyId = $null; sku = "SKU-SERIAL"; name = "Control Board Serialized"; type = 2; trackingType = 1; unitOfMeasure = "PCS"; brandId = $null; categoryId = $null; subcategoryId = $null; barcode = $null; defaultUnitCost = 25; revenueAccountId = $null; expenseAccountId = $null })[0]
$eqpGen = @(Get-OrCreate "/api/items" { $_.sku -eq "EQP-GEN" } "/api/items" @{ companyId = $null; sku = "EQP-GEN"; name = "Generator Model A"; type = 1; trackingType = 1; unitOfMeasure = "PCS"; brandId = $null; categoryId = $null; subcategoryId = $null; barcode = $null; defaultUnitCost = 100; revenueAccountId = $null; expenseAccountId = $null })[0]
$laborItem = @(Get-OrCreate "/api/items" { $_.sku -eq "LAB-SVC" } "/api/items" @{ companyId = $null; sku = "LAB-SVC"; name = "Service Labor"; type = 3; trackingType = 0; unitOfMeasure = "HOUR"; brandId = $null; categoryId = $null; subcategoryId = $null; barcode = $null; defaultUnitCost = 0; revenueAccountId = $null; expenseAccountId = $null })[0]

Write-Host "Creating PO and two partial GRNs..."
$po = Invoke-Api POST "/api/procurement/purchase-orders" @{ supplierId = $supplier.id }
Post-NoContent "/api/procurement/purchase-orders/$($po.id)/lines" @{ itemId = $skuCore.id; quantity = 20; unitPrice = 5 }
Post-NoContent "/api/procurement/purchase-orders/$($po.id)/lines" @{ itemId = $skuBatch.id; quantity = 10; unitPrice = 8 }
Post-NoContent "/api/procurement/purchase-orders/$($po.id)/lines" @{ itemId = $skuSerial.id; quantity = 2; unitPrice = 25 }
Post-NoContent "/api/procurement/purchase-orders/$($po.id)/approve"
$po = Invoke-Api GET "/api/procurement/purchase-orders/$($po.id)"

$grn1 = Invoke-Api POST "/api/procurement/goods-receipts" @{ purchaseOrderId = $po.id; warehouseId = $mainWarehouse.id }
$plan1 = Invoke-Api GET "/api/procurement/goods-receipts/$($grn1.id)/receipt-plan"
$plan1Core = @($plan1.lines | Where-Object { $_.itemId -eq $skuCore.id })[0]
$plan1Batch = @($plan1.lines | Where-Object { $_.itemId -eq $skuBatch.id })[0]
$plan1Serial = @($plan1.lines | Where-Object { $_.itemId -eq $skuSerial.id })[0]
Invoke-Api PUT "/api/procurement/goods-receipts/$($grn1.id)/receipt-plan" @{
    lines = @(
        @{ purchaseOrderLineId = $plan1Core.purchaseOrderLineId; quantity = 8; unitCost = 5; batchNumber = $null; serials = $null },
        @{ purchaseOrderLineId = $plan1Batch.purchaseOrderLineId; quantity = 4; unitCost = 8; batchNumber = "LOT-A"; serials = $null },
        @{ purchaseOrderLineId = $plan1Serial.purchaseOrderLineId; quantity = 1; unitCost = 25; batchNumber = $null; serials = @("SER-001") }
    )
} | Out-Null
Post-NoContent "/api/procurement/goods-receipts/$($grn1.id)/post"

$grn2 = Invoke-Api POST "/api/procurement/goods-receipts" @{ purchaseOrderId = $po.id; warehouseId = $mainWarehouse.id }
$plan2 = Invoke-Api GET "/api/procurement/goods-receipts/$($grn2.id)/receipt-plan"
$plan2Core = @($plan2.lines | Where-Object { $_.itemId -eq $skuCore.id })[0]
$plan2Batch = @($plan2.lines | Where-Object { $_.itemId -eq $skuBatch.id })[0]
$plan2Serial = @($plan2.lines | Where-Object { $_.itemId -eq $skuSerial.id })[0]
Invoke-Api PUT "/api/procurement/goods-receipts/$($grn2.id)/receipt-plan" @{
    lines = @(
        @{ purchaseOrderLineId = $plan2Core.purchaseOrderLineId; quantity = 12; unitCost = 5; batchNumber = $null; serials = $null },
        @{ purchaseOrderLineId = $plan2Batch.purchaseOrderLineId; quantity = 6; unitCost = 8; batchNumber = "LOT-A"; serials = $null },
        @{ purchaseOrderLineId = $plan2Serial.purchaseOrderLineId; quantity = 1; unitCost = 25; batchNumber = $null; serials = @("SER-002") }
    )
} | Out-Null
Post-NoContent "/api/procurement/goods-receipts/$($grn2.id)/post"

Write-Host "Creating inventory and sales transactions..."
$transfer = Invoke-Api POST "/api/inventory/stock-transfers" @{ fromWarehouseId = $mainWarehouse.id; toWarehouseId = $secWarehouse.id; notes = "Test transfer" }
Post-NoContent "/api/inventory/stock-transfers/$($transfer.id)/lines" @{ itemId = $skuCore.id; quantity = 5; batchNumber = $null; serials = $null }
Post-NoContent "/api/inventory/stock-transfers/$($transfer.id)/post"

$dispatch = Invoke-Api POST "/api/sales/direct-dispatches" @{ customerId = $customer.id; warehouseId = $mainWarehouse.id; serviceJobId = $null; reason = "Test direct dispatch"; warrantyUntil = $null; warrantyCoverage = 0; serviceIntervalDays = $null; nextServiceDueAt = $null }
Post-NoContent "/api/sales/direct-dispatches/$($dispatch.id)/lines" @{ itemId = $skuCore.id; quantity = 6; batchNumber = $null; serials = $null }
Post-NoContent "/api/sales/direct-dispatches/$($dispatch.id)/post"

$invoice = Invoke-Api POST "/api/sales/invoices" @{ customerId = $customer.id; dueDate = $null }
Post-NoContent "/api/sales/invoices/$($invoice.id)/lines" @{ itemId = $skuCore.id; quantity = 6; unitPrice = 7; discountPercent = 0; taxPercent = 0 }
Post-NoContent "/api/sales/invoices/$($invoice.id)/post"

$customerReturn = Invoke-Api POST "/api/sales/customer-returns" @{ customerId = $customer.id; warehouseId = $mainWarehouse.id; salesInvoiceId = $invoice.id; dispatchNoteId = $null; reason = "Test return" }
Post-NoContent "/api/sales/customer-returns/$($customerReturn.id)/lines" @{ itemId = $skuCore.id; quantity = 1; unitPrice = 7; batchNumber = $null; serials = $null }
Post-NoContent "/api/sales/customer-returns/$($customerReturn.id)/post"

$supplierReturn = Invoke-Api POST "/api/procurement/supplier-returns" @{ supplierId = $supplier.id; warehouseId = $mainWarehouse.id; reason = "Test supplier return" }
Post-NoContent "/api/procurement/supplier-returns/$($supplierReturn.id)/lines" @{ itemId = $skuCore.id; quantity = 2; unitCost = 5; batchNumber = $null; serials = $null }
Post-NoContent "/api/procurement/supplier-returns/$($supplierReturn.id)/post"

$adjustment = Invoke-Api POST "/api/inventory/stock-adjustments" @{ warehouseId = $mainWarehouse.id; reason = "Checklist counted quantity" }
Post-NoContent "/api/inventory/stock-adjustments/$($adjustment.id)/lines" @{ itemId = $skuCore.id; countedQuantity = 9; unitCost = 5; batchNumber = $null; serials = $null }
Post-NoContent "/api/inventory/stock-adjustments/$($adjustment.id)/post"

Write-Host "Creating service job workflow data..."
$equipment = Get-OrCreate "/api/service/equipment-units" { $_.serialNumber -eq "GEN-SN-001" } "/api/service/equipment-units" @{
    itemId = $eqpGen.id
    serialNumber = "GEN-SN-001"
    customerId = $customer.id
    purchasedAt = "2026-05-01T00:00:00Z"
    warrantyUntil = "2026-05-28T00:00:00Z"
    warrantyCoverage = 3
    serviceIntervalDays = $null
    nextServiceDueAt = $null
    nextRepairDueAt = $null
}

$job = Invoke-Api POST "/api/service/jobs" @{
    equipmentUnitId = $equipment.id
    customerId = $customer.id
    kind = 1
    responsibleOfficerName = "Service Supervisor"
    customerComplaint = "Customer reports generator does not start"
    jobDescription = "Repair and test generator starting system"
    problemDescription = "Generator does not start"
    expectedCompletionAt = $null
    siteLocation = $null
    estimatedStartAt = $null
    internalRemarks = $null
}
Post-NoContent "/api/service/jobs/$($job.id)/start"

$operation = Invoke-Api POST "/api/service/jobs/$($job.id)/operations" @{
    sequence = 10
    name = "Diagnose starting system"
    description = "Check battery, starter circuit, and control board"
    plannedItemId = $skuCore.id
    plannedQuantity = 2
    estimatedLaborHours = 2
    requiredAt = $null
    notes = "Replace filter if required during repair"
}
Post-NoContent "/api/service/jobs/$($job.id)/operations/$($operation.id)/start"

$dailySheet = Invoke-Api POST "/api/service/jobs/$($job.id)/daily-sheets" @{
    sheetDate = "2026-05-18T08:30:00Z"
    preparedByName = "Service Supervisor"
    siteLocation = "Workshop"
    shiftName = "Day"
    weatherOrSiteCondition = "Workshop bench test"
    workPlanned = "Diagnose starting system and replace failed parts"
    workCompleted = "Starting system diagnosed and replacement parts installed"
    workPending = "Final customer confirmation"
    problemsFound = "Weak control board output"
    customerInstructions = "Call before delivery"
    technicianNotes = "Test run completed"
    supervisorNotes = "Ready for handover after invoice review"
}

$mrn = Invoke-Api POST "/api/service/material-requisitions" @{ serviceJobId = $job.id; warehouseId = $mainWarehouse.id; purpose = "Repair and test generator starting system"; serviceJobDailySheetId = $dailySheet.id }
Post-NoContent "/api/service/material-requisitions/$($mrn.id)/lines" @{ itemId = $skuCore.id; quantity = 2; batchNumber = $null; serials = $null }
Post-NoContent "/api/service/material-requisitions/$($mrn.id)/lines" @{ itemId = $skuSerial.id; quantity = 1; batchNumber = $null; serials = @("SER-001") }
Post-NoContent "/api/service/material-requisitions/$($mrn.id)/post"
$mrn = Invoke-Api GET "/api/service/material-requisitions/$($mrn.id)"
$coreMrnLine = $mrn.lines | Where-Object itemId -eq $skuCore.id | Select-Object -First 1
$serialMrnLine = $mrn.lines | Where-Object itemId -eq $skuSerial.id | Select-Object -First 1

Invoke-Api POST "/api/service/jobs/$($job.id)/material-dispositions" @{ materialRequisitionLineId = $coreMrnLine.id; kind = 0; quantity = 1; condition = "Installed"; chargeTo = 0; reason = "Installed filter during repair"; supplierReturnId = $null; responsiblePerson = $null; serials = $null; serviceJobDailySheetId = $dailySheet.id } | Out-Null
Invoke-Api POST "/api/service/jobs/$($job.id)/material-dispositions" @{ materialRequisitionLineId = $coreMrnLine.id; kind = 1; quantity = 1; condition = "Good"; chargeTo = 1; reason = "Extra filter not used"; supplierReturnId = $null; responsiblePerson = $null; serials = $null; serviceJobDailySheetId = $dailySheet.id } | Out-Null
Invoke-Api POST "/api/service/jobs/$($job.id)/material-dispositions" @{ materialRequisitionLineId = $serialMrnLine.id; kind = 0; quantity = 1; condition = "Installed"; chargeTo = 4; reason = "Installed replacement board"; supplierReturnId = $null; responsiblePerson = $null; serials = @("SER-001"); serviceJobDailySheetId = $dailySheet.id } | Out-Null

$tech = Get-OrCreate "/api/service/technicians" { $_.code -eq "TECH1" } "/api/service/technicians" @{ code = "TECH1"; name = "Workshop Technician"; defaultCostRate = 10; defaultBillingRate = 25 }
$assignment = Invoke-Api POST "/api/service/jobs/$($job.id)/assignments" @{
    serviceJobDailySheetId = $dailySheet.id
    technicianId = $tech.id
    employeeName = $null
    role = "Technician"
    assignedTask = "Diagnose starting system and replace required parts"
    assignedDate = "2026-05-18T08:30:00Z"
    workStartAt = "2026-05-18T08:30:00Z"
    workEndAt = "2026-05-18T10:30:00Z"
    normalHours = 2
    overtimeHours = 0
    dailyWorkDescription = "Diagnosis and replacement completed"
}
Post-NoContent "/api/service/jobs/$($job.id)/assignments/$($assignment.id)/approve"

$workOrder = Invoke-Api POST "/api/service/work-orders" @{ serviceJobId = $job.id; description = "Diagnose starting system and replace required parts"; assignedToUserId = $null }
Post-NoContent "/api/service/work-orders/$($workOrder.id)/start"
$workOrder = Invoke-Api POST "/api/service/work-orders/$($workOrder.id)/time-entries" @{
    technicianUserId = $null
    technicianName = "Workshop Technician"
    workDate = "2026-05-18T00:00:00Z"
    workDescription = "Diagnosis labor"
    hoursWorked = 2
    costRate = 10
    billableToCustomer = $true
    billableHours = 2
    billingRate = 25
    taxPercent = 0
    notes = $null
}
$timeEntryId = $workOrder.timeEntries[0].id
Post-NoContent "/api/service/work-orders/$($workOrder.id)/time-entries/$timeEntryId/submit"
Post-NoContent "/api/service/work-orders/$($workOrder.id)/time-entries/$timeEntryId/approve"
Post-NoContent "/api/service/work-orders/$($workOrder.id)/done"

Invoke-Api POST "/api/service/jobs/$($job.id)/progress-updates" @{
    serviceJobDailySheetId = $dailySheet.id
    progressDate = "2026-05-18T10:45:00Z"
    workCompleted = "Starting system diagnosed and replacement parts installed"
    workPending = "Final customer confirmation"
    problemsFound = "Weak control board output"
    additionalPartsRequired = "None"
    additionalLaborRequired = "None"
    customerInstructions = "Call before delivery"
    siteIssues = $null
    technicianNotes = "Test run completed"
    supervisorNotes = "Ready for handover after invoice review"
} | Out-Null
Post-NoContent "/api/service/jobs/$($job.id)/operations/$($operation.id)/complete"

$estimate = Invoke-Api POST "/api/service/estimates" @{ serviceJobId = $job.id; validUntil = $null; terms = "Checklist test estimate" }
Post-NoContent "/api/service/estimates/$($estimate.id)/lines" @{ kind = 1; itemId = $skuCore.id; description = "Filter replacement"; quantity = 2; unitPrice = 7; taxPercent = 0 }
Post-NoContent "/api/service/estimates/$($estimate.id)/lines" @{ kind = 2; itemId = $laborItem.id; description = "Diagnosis labor"; quantity = 2; unitPrice = 25; taxPercent = 0 }
Post-NoContent "/api/service/estimates/$($estimate.id)/lines" @{ kind = 3; itemId = $null; description = "Travel cost"; quantity = 1; unitPrice = 15; taxPercent = 0 }
Post-NoContent "/api/service/estimates/$($estimate.id)/send" @{ appBaseUrl = "http://localhost:3000" }
Post-NoContent "/api/service/estimates/$($estimate.id)/approve"

$fund = Get-OrCreate "/api/finance/petty-cash-funds" { $_.code -eq "PC-UAT" } "/api/finance/petty-cash-funds" @{ code = "PC-UAT"; name = "UAT Petty Cash"; currencyCode = "USD"; custodianName = "Finance"; notes = "Checklist fund"; openingBalance = 100; openedAt = "2026-05-18T00:00:00Z"; openingReferenceNumber = "OPEN-UAT" }
$iou = Invoke-Api POST "/api/finance/petty-cash-ious" @{ serviceJobId = $job.id; serviceJobDailySheetId = $dailySheet.id; requestedByName = "TECH1"; amount = 20; expectedSettlementAt = "2026-05-19T00:00:00Z"; purpose = "Travel and parking advance for generator repair" }
Post-NoContent "/api/finance/petty-cash-ious/$($iou.id)/submit"
Post-NoContent "/api/finance/petty-cash-ious/$($iou.id)/approve"
Post-NoContent "/api/finance/petty-cash-ious/$($iou.id)/release" @{ pettyCashFundId = $fund.id; releaseReference = "REL-UAT" }
Post-NoContent "/api/finance/petty-cash-ious/$($iou.id)/settle" @{ settledAmount = 20; settlementReference = "SET-UAT" }

$claim = Invoke-Api POST "/api/service/expense-claims" @{ serviceJobId = $job.id; serviceJobDailySheetId = $dailySheet.id; claimedByName = "TECH1"; fundingSource = 1; expenseDate = "2026-05-18T11:00:00Z"; merchantName = "Test Vendor"; receiptReference = "RCPT-UAT"; notes = "Parking fee" }
Post-NoContent "/api/service/expense-claims/$($claim.id)/lines" @{ itemId = $null; description = "Parking fee"; quantity = 1; unitCost = 5; billableToCustomer = $true }
Post-NoContent "/api/service/expense-claims/$($claim.id)/submit"
Post-NoContent "/api/service/expense-claims/$($claim.id)/approve"
Post-NoContent "/api/service/expense-claims/$($claim.id)/settle" @{ settlementPaymentTypeId = $null; settlementPettyCashFundId = $null; settlementReference = "REIMB-UAT" }

$handover = Invoke-Api POST "/api/service/handovers" @{ serviceJobId = $job.id; itemsReturned = "Generator returned after repair"; postServiceWarrantyMonths = $null; customerAcknowledgement = "Customer accepted"; notes = "Test handover" }
Post-NoContent "/api/service/handovers/$($handover.id)/complete"
Post-NoContent "/api/service/jobs/$($job.id)/complete"
Invoke-Api POST "/api/service/handovers/$($handover.id)/convert-to-sales-invoice" @{ serviceEstimateId = $estimate.id; laborItemId = $laborItem.id; expenseItemId = $laborItem.id; laborBillingSource = 0; dueDate = $null } | Out-Null
Post-NoContent "/api/service/jobs/$($job.id)/daily-sheets/$($dailySheet.id)/submit"
Post-NoContent "/api/service/jobs/$($job.id)/daily-sheets/$($dailySheet.id)/approve"
Post-NoContent "/api/service/jobs/$($job.id)/close"

Write-Host "Verifying checklist balances..."
$availability = @(Invoke-Api GET "/api/inventory/availability")
$coreMain = ($availability | Where-Object { $_.itemId -eq $skuCore.id -and $_.warehouseId -eq $mainWarehouse.id } | Measure-Object -Property onHand -Sum).Sum
$coreSec = ($availability | Where-Object { $_.itemId -eq $skuCore.id -and $_.warehouseId -eq $secWarehouse.id } | Measure-Object -Property onHand -Sum).Sum
$batchMain = ($availability | Where-Object { $_.itemId -eq $skuBatch.id -and $_.warehouseId -eq $mainWarehouse.id -and $_.batchNumber -eq "LOT-A" } | Measure-Object -Property onHand -Sum).Sum
$serial2 = ($availability | Where-Object { $_.itemId -eq $skuSerial.id -and $_.serialNumber -eq "SER-002" } | Measure-Object -Property onHand -Sum).Sum
Assert-Equal "SKU-CORE MAIN" $coreMain 8
Assert-Equal "SKU-CORE SEC" $coreSec 5
Assert-Equal "SKU-BATCH LOT-A MAIN" $batchMain 10
Assert-Equal "SKU-SERIAL SER-002" $serial2 1

$checks = @(Invoke-Api GET "/api/service/jobs/$($job.id)/closeout-checks")
$blocking = @($checks | Where-Object { -not $_.isClear })
if ($blocking.Count -gt 0) {
    throw "Service job closeout still has blockers: $($blocking | ForEach-Object { "$($_.label)=$($_.pendingCount)" } | Join-String -Separator ', ')"
}

$result = [ordered]@{
    BaseUrl = $BaseUrl
    AdminEmail = $email
    AdminPassword = $password
    PurchaseOrder = $po.number
    GoodsReceipt1 = (Invoke-Api GET "/api/procurement/goods-receipts/$($grn1.id)").number
    GoodsReceipt2 = (Invoke-Api GET "/api/procurement/goods-receipts/$($grn2.id)").number
    SalesInvoice = (Invoke-Api GET "/api/sales/invoices/$($invoice.id)").number
    ServiceJob = (Invoke-Api GET "/api/service/jobs/$($job.id)").number
    DailySheet = $dailySheet.number
    MaterialRequisition = $mrn.number
    ServiceEstimate = (Invoke-Api GET "/api/service/estimates/$($estimate.id)").number
    ServiceHandover = (Invoke-Api GET "/api/service/handovers/$($handover.id)").number
    CoreMain = $coreMain
    CoreSecondary = $coreSec
    BatchLotA = $batchMain
    Serial002 = $serial2
}

$result | ConvertTo-Json -Depth 10
