# ISS Testing Input / Output Checklist

This document gives testers exact values to enter, expected outputs, and where to verify each result.

Use it for manual UAT, demo database testing, and regression checks after deployments. Run the scenario as an `Admin` user first. Role-by-role testing can be done later using `docs/role-based-test-checklists.md`.

## 1. Reset Test Data

Use this only on a test database.

Go to `Admin -> Testing Cleanup`.

Type:

| Field | Input |
| --- | --- |
| Confirmation | `CLEAR` |

Recommended reset before this full test:

1. Click `Clear PO`
2. Type `CLEAR` again
3. Click `Clear Jobs / Service`
4. Type `CLEAR` again
5. Click `Zero Stock`

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Cleanup response | Same page | Success message after each button |
| Inventory reset | `Inventory -> Inventory Availability`, click `Load inventory` | No inventory rows |
| PO/GRN reset | `Procurement -> Purchase Orders`, `Procurement -> Goods Receipts` | No old test PO/GRN documents, unless the database has unrelated retained data |
| Service reset | `Service -> Jobs`, `Finance -> Petty Cash IOUs` | No old test jobs, daily sheets, job-linked IOUs, service expenses, MRNs, material dispositions, work orders, QC, or handovers |

Do not use cleanup buttons on real production data.

## 2. Master Data Inputs

Create these values before transaction testing.

### Finance And Reference Master Data

Most fresh local/demo databases already seed these records. Open each page and confirm the values exist. If a value is missing, create it before continuing.

| Page | Record | Field | Input |
| --- | --- | --- | --- |
| `Master Data -> Currencies` | Base currency | Code | `LKR` |
| `Master Data -> Currencies` | Base currency | Name | `Sri Lankan Rupee` |
| `Master Data -> Currencies` | Base currency | Symbol | `Rs` |
| `Master Data -> Currency Rates` | USD to LKR | From | `USD` |
| `Master Data -> Currency Rates` | USD to LKR | To | `LKR` |
| `Master Data -> Currency Rates` | USD to LKR | Rate | `300` |
| `Master Data -> Payment Types` | Cash | Code | `CASH` |
| `Master Data -> Payment Types` | Cash | Name | `Cash` |
| `Master Data -> Payment Types` | Bank transfer | Code | `BANK` |
| `Master Data -> Payment Types` | Bank transfer | Name | `Bank Transfer` |
| `Master Data -> Tax Codes` | Standard VAT | Code | `VAT18` |
| `Master Data -> Tax Codes` | Standard VAT | Name | `VAT 18%` |
| `Master Data -> Tax Codes` | Standard VAT | Rate % | `18` |
| `Master Data -> Tax Codes` | Zero-rated | Code | `ZERO` |
| `Master Data -> Tax Codes` | Zero-rated | Name | `Zero Rated` |
| `Master Data -> Tax Codes` | Zero-rated | Rate % | `0` |
| `Master Data -> Tax Conversions` | VAT conversion | Source tax | `VAT18` |
| `Master Data -> Tax Conversions` | VAT conversion | Target tax | `ZERO` |
| `Master Data -> Reference Forms` | Customer PO | Code | `CPO` |
| `Master Data -> Reference Forms` | Customer PO | Name | `Customer Purchase Order` |
| `Master Data -> Reference Forms` | Delivery note | Code | `DN` |
| `Master Data -> Reference Forms` | Delivery note | Name | `Delivery Note` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Finance setup | Currency, payment, and tax master pages | `LKR`, `CASH`, `BANK`, `VAT18`, and `ZERO` are active |
| Reference setup | `Master Data -> Reference Forms` | `CPO` and `DN` are selectable on transaction forms that ask for references |

### Item Supporting Master Data

Create these records before creating items.

| Page | Record | Field | Input |
| --- | --- | --- | --- |
| `Master Data -> Brands` | Test brand | Code | `BR-TEST` |
| `Master Data -> Brands` | Test brand | Name | `Test Brand` |
| `Master Data -> UoMs` | Pieces | Code | `PCS` |
| `Master Data -> UoMs` | Pieces | Name | `Pieces` |
| `Master Data -> UoMs` | Hours | Code | `HOUR` |
| `Master Data -> UoMs` | Hours | Name | `Hour` |
| `Master Data -> UoM Conversions` | Box to pieces | From UoM | `BOX` |
| `Master Data -> UoM Conversions` | Box to pieces | To UoM | `PCS` |
| `Master Data -> UoM Conversions` | Box to pieces | Factor | `12` |
| `Master Data -> Item Categories` | Spare parts | Code | `SPARES` |
| `Master Data -> Item Categories` | Spare parts | Name | `Spare Parts` |
| `Master Data -> Item Categories` | Equipment | Code | `EQUIP` |
| `Master Data -> Item Categories` | Equipment | Name | `Equipment` |
| `Master Data -> Item Subcategories` | Filters | Category | `SPARES` |
| `Master Data -> Item Subcategories` | Filters | Code | `FILTERS` |
| `Master Data -> Item Subcategories` | Filters | Name | `Filters` |
| `Master Data -> Item Subcategories` | Control boards | Category | `SPARES` |
| `Master Data -> Item Subcategories` | Control boards | Code | `BOARDS` |
| `Master Data -> Item Subcategories` | Control boards | Name | `Control Boards` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Item setup lists | Brand, UoM, category, and subcategory pages | Codes above are visible and active |
| Item create dropdowns | `Master Data -> Items -> Create` | `BR-TEST`, `PCS`, `HOUR`, `SPARES`, `EQUIP`, `FILTERS`, and `BOARDS` are selectable |

### Warehouses And Bins

Go to `Master Data -> Warehouses`.

| Record | Field | Input |
| --- | --- | --- |
| Warehouse 1 | Code | `MAIN` |
| Warehouse 1 | Name | `Main Warehouse` |
| Warehouse 2 | Code | `SEC` |
| Warehouse 2 | Name | `Secondary Warehouse` |
| MAIN bin 1 | Bin Code | `A1-R1-S1` |
| MAIN bin 1 | Name | `Aisle 1 Rack 1 Shelf 1` |
| MAIN bin 1 | Zone | `A1` |
| MAIN bin 1 | Rack | `R1` |
| MAIN bin 1 | Shelf | `S1` |
| SEC bin 1 | Bin Code | `B1-R1-S1` |
| SEC bin 1 | Name | `Secondary Rack 1 Shelf 1` |
| SEC bin 1 | Zone | `B1` |
| SEC bin 1 | Rack | `R1` |
| SEC bin 1 | Shelf | `S1` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Warehouse list | `Master Data -> Warehouses` | `MAIN` and `SEC` appear |
| Bin list | `Bins / Racks` section | `A1-R1-S1` under `MAIN`, `B1-R1-S1` under `SEC` |

### Supplier And Customer

| Page | Field | Input |
| --- | --- | --- |
| `Master Data -> Suppliers` | Code | `SUP1` |
| `Master Data -> Suppliers` | Name | `Test Supplier` |
| `Master Data -> Customers` | Code | `CUS1` |
| `Master Data -> Customers` | Name | `Test Customer` |
| `Master Data -> Customers` | Phone | `0770000000` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Supplier exists | Supplier list | `SUP1 - Test Supplier` |
| Customer exists | Customer list | `CUS1 - Test Customer` |

### Items

Go to `Master Data -> Items`.

| Item | Field | Input |
| --- | --- | --- |
| Normal stock item | SKU | `SKU-CORE` |
| Normal stock item | Name | `Hydraulic Filter` |
| Normal stock item | Type | `Spare Part` |
| Normal stock item | Brand | `BR-TEST` |
| Normal stock item | Category | `SPARES` |
| Normal stock item | Subcategory | `FILTERS` |
| Normal stock item | Tracking | `None` |
| Normal stock item | UoM | `PCS` |
| Normal stock item | Default Unit Cost | `5` |
| Batch item | SKU | `SKU-BATCH` |
| Batch item | Name | `Engine Oil Lot Item` |
| Batch item | Type | `Spare Part` |
| Batch item | Brand | `BR-TEST` |
| Batch item | Category | `SPARES` |
| Batch item | Tracking | `Batch` |
| Batch item | UoM | `PCS` |
| Batch item | Default Unit Cost | `8` |
| Serial item | SKU | `SKU-SERIAL` |
| Serial item | Name | `Control Board Serialized` |
| Serial item | Type | `Spare Part` |
| Serial item | Brand | `BR-TEST` |
| Serial item | Category | `SPARES` |
| Serial item | Subcategory | `BOARDS` |
| Serial item | Tracking | `Serial` |
| Serial item | UoM | `PCS` |
| Serial item | Default Unit Cost | `25` |
| Equipment item | SKU | `EQP-GEN` |
| Equipment item | Name | `Generator Model A` |
| Equipment item | Type | `Equipment` |
| Equipment item | Brand | `BR-TEST` |
| Equipment item | Category | `EQUIP` |
| Equipment item | Tracking | `Serial` |
| Equipment item | UoM | `PCS` |
| Equipment item | Default Unit Cost | `100` |
| Labor item | SKU | `LAB-SVC` |
| Labor item | Name | `Service Labor` |
| Labor item | Type | `Service` |
| Labor item | Tracking | `None` |
| Labor item | UoM | `HOUR` |
| Labor item | Default Unit Cost | `0` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Item search | `Master Data -> Items` | All SKUs can be searched and opened |
| Tracking setup | Item detail/edit page | `SKU-BATCH` is batch tracked; `SKU-SERIAL` and `EQP-GEN` are serial tracked |
| Classification setup | Item detail/edit page | Item brand/category/subcategory values match the supporting master data |

### Reorder Settings

Go to `Master Data -> Reorder Settings`.

| Item | Warehouse | Reorder Level | Reorder Quantity |
| --- | --- | ---: | ---: |
| `SKU-CORE` | `MAIN` | `5` | `20` |
| `SKU-BATCH` | `MAIN` | `3` | `10` |
| `SKU-SERIAL` | `MAIN` | `1` | `2` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Reorder settings list | `Master Data -> Reorder Settings` | Rows exist for the three stock items in `MAIN` |
| Reorder alert readiness | `Inventory -> Reorder Alerts` | Items can appear when on-hand stock falls below the configured level |

## 3. Procurement: PO And Partial GRNs

This phase proves partial receiving. One PO line for `20` units will be received in two GRNs: first `8`, then `12`.

### 3.1 Create And Approve PO

Go to `Procurement -> Purchase Orders`.

| Field | Input |
| --- | --- |
| Supplier | `SUP1` |

Add PO lines:

| Line | Item | Qty | Unit Price |
| --- | --- | ---: | ---: |
| 1 | `SKU-CORE` | `20` | `5` |
| 2 | `SKU-BATCH` | `10` | `8` |
| 3 | `SKU-SERIAL` | `2` | `25` |

Click `Approve`.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| PO status | PO detail | `Approved` |
| PO total | PO detail | `20 x 5 + 10 x 8 + 2 x 25 = 230` |
| Open receipt quantity | GRN create/detail from PO | PO lines are available to receive |

### 3.2 GRN 1 - Partial Receipt

Go to `Procurement -> Goods Receipts`.

Create GRN:

| Field | Input |
| --- | --- |
| Purchase Order | The approved PO |
| Warehouse | `MAIN` |

In `Receive From PO`, enter:

| PO Line | Receive Qty | Unit Cost | Batch | Serials |
| --- | ---: | ---: | --- | --- |
| `SKU-CORE` | `8` | `5` | blank | blank |
| `SKU-BATCH` | `4` | `8` | `LOT-A` | blank |
| `SKU-SERIAL` | `1` | `25` | blank | `SER-001` |

Click `Save receipt plan`, then `Post`.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| GRN 1 status | GRN detail | `Posted` |
| GRN 1 stock movement | `Inventory -> Inventory Availability`, filter item `SKU-CORE` | `SKU-CORE` on hand `8` in `MAIN`, bin `Unassigned` |
| Batch stock | `Inventory -> Inventory Availability`, search `LOT-A` | `SKU-BATCH`, batch `LOT-A`, on hand `4` |
| Serial stock | `Inventory -> Inventory Availability`, search `SER-001` | `SKU-SERIAL`, serial `SER-001`, on hand `1` |
| AP entry | `Finance -> AP` | GRN/AP outstanding includes `8 x 5 + 4 x 8 + 1 x 25 = 97` |
| Costing | `Reporting -> Costing`, item `SKU-CORE` | On hand `8`, weighted avg cost `5`, value `40` |

### 3.3 GRN 2 - Remaining Receipt

Create another GRN from the same approved PO.

| Field | Input |
| --- | --- |
| Purchase Order | Same approved PO |
| Warehouse | `MAIN` |

In `Receive From PO`, enter only the remaining quantities:

| PO Line | Receive Qty | Unit Cost | Batch | Serials |
| --- | ---: | ---: | --- | --- |
| `SKU-CORE` | `12` | `5` | blank | blank |
| `SKU-BATCH` | `6` | `8` | `LOT-A` | blank |
| `SKU-SERIAL` | `1` | `25` | blank | `SER-002` |

Click `Save receipt plan`, then `Post`.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| GRN 2 status | GRN detail | `Posted` |
| Total `SKU-CORE` stock | `Inventory -> On Hand`, item `SKU-CORE`, warehouse `MAIN` | `20` |
| Total batch stock | `Inventory -> Inventory Availability`, search `LOT-A` | `SKU-BATCH`, batch `LOT-A`, on hand `10` |
| Serial stock | `Inventory -> Inventory Availability`, search `SER-001` and `SER-002` | both serials available with on hand `1` each |
| AP total from two GRNs | `Finance -> AP` | GRN entries total `230` before payment/invoice allocation |
| PO remaining receipt | Create another GRN from same PO | no remaining quantity should be available for the fully received lines |

### 3.4 Negative GRN Validation

Try creating a third GRN from the same PO and receiving:

| Item | Qty |
| --- | ---: |
| `SKU-CORE` | `1` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Over receipt prevention | GRN receipt plan save/post | Error that quantity exceeds remaining PO quantity, or no open PO line available |

## 4. Inventory Visibility Checks

### 4.1 Full Inventory Browser

Go to `Inventory -> Inventory Availability`.

Click `Load inventory`.

Expected rows:

| Item | Warehouse | Bin/Rack | Batch/Lot | Serial | On Hand | Unit Cost | Value |
| --- | --- | --- | --- | --- | ---: | ---: | ---: |
| `SKU-CORE` | `MAIN` | `Unassigned` | `-` | `-` | `20` | `5` | `100` |
| `SKU-BATCH` | `MAIN` | `Unassigned` | `LOT-A` | `-` | `10` | `8` | `80` |
| `SKU-SERIAL` | `MAIN` | `Unassigned` | `-` | `SER-001` | `1` | `25` | `25` |
| `SKU-SERIAL` | `MAIN` | `Unassigned` | `-` | `SER-002` | `1` | `25` | `25` |

Expected totals:

| Total | Expected |
| --- | ---: |
| On hand quantity | `32` |
| Inventory value | `230` |

Search tests:

| Search | Expected |
| --- | --- |
| `SKU-CORE` | Only core item row |
| `LOT-A` | Batch item row |
| `SER-001` | Serial row for `SER-001` |
| `MAIN` | All rows in main warehouse |

### 4.2 On Hand Query

Go to `Inventory -> On Hand`.

| Filter | Input | Expected |
| --- | --- | --- |
| Warehouse | `MAIN` | Results are limited to `MAIN` |
| Item | `SKU-CORE` | On hand `20` |
| View | `Warehouse wise` | `MAIN = 20` |
| View | `Warehouse + batch` | `MAIN / No batch = 20` |

Batch item:

| Filter | Input | Expected |
| --- | --- | --- |
| Warehouse | `MAIN` | Results are limited to `MAIN` |
| Item | `SKU-BATCH` | On hand `10` |
| Batch | `LOT-A` | On hand `10` |

## 5. Stock Transfer

Go to `Inventory -> Stock Transfers`.

Create transfer:

| Field | Input |
| --- | --- |
| From Warehouse | `MAIN` |
| To Warehouse | `SEC` |

Add line:

| Item | Qty | Batch | Serials |
| --- | ---: | --- | --- |
| `SKU-CORE` | `5` | blank | blank |

Post transfer.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Transfer status | Transfer detail | `Posted` |
| MAIN stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `15` |
| SEC stock | `Inventory -> On Hand`, `SEC` + `SKU-CORE` | `5` |
| Total company stock | `Inventory -> Inventory Availability`, search `SKU-CORE` | `20` total remains unchanged |

## 6. Sales Direct Dispatch And Invoice

### 6.1 Direct Dispatch

Go to `Sales -> Direct Dispatches`.

Create:

| Field | Input |
| --- | --- |
| Customer | `CUS1` |
| Warehouse | `MAIN` |
| Reason | `Test direct dispatch` |

Add line:

| Item | Qty | Batch | Serials |
| --- | ---: | --- | --- |
| `SKU-CORE` | `6` | blank | blank |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Direct dispatch status | Detail page | `Posted` |
| MAIN stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `9` |
| SEC stock | `Inventory -> On Hand`, `SEC` + `SKU-CORE` | `5` |
| Total stock | `Inventory -> Inventory Availability`, search `SKU-CORE` | `14` |

### 6.2 Sales Invoice

Go to `Sales -> Invoices`.

Create invoice:

| Field | Input |
| --- | --- |
| Customer | `CUS1` |

Add line:

| Item | Qty | Unit Price | Discount % | Tax % |
| --- | ---: | ---: | ---: | ---: |
| `SKU-CORE` | `6` | `7` | `0` | `0` |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Invoice total | Invoice detail | `42` |
| AR entry | `Finance -> AR` | Customer `CUS1`, amount `42`, outstanding `42` |
| Inventory | `Inventory -> On Hand` | unchanged from dispatch; invoice should not issue stock again |

## 7. Customer Return

Go to `Sales -> Customer Returns`.

Create:

| Field | Input |
| --- | --- |
| Customer | `CUS1` |
| Warehouse | `MAIN` |
| Sales Invoice | Invoice from section 6.2 |
| Reason | `Test return` |

Add line:

| Item | Qty | Unit Price |
| --- | ---: | ---: |
| `SKU-CORE` | `1` | `7` |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Return status | Customer return detail | `Posted` |
| MAIN stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `10` |
| Customer credit note | `Finance -> Credit Notes` | Credit note for `CUS1`, amount `7` |

## 8. Supplier Return

Go to `Procurement -> Supplier Returns`.

Create:

| Field | Input |
| --- | --- |
| Supplier | `SUP1` |
| Warehouse | `MAIN` |
| Reason | `Test supplier return` |

Add line:

| Item | Qty | Unit Cost |
| --- | ---: | ---: |
| `SKU-CORE` | `2` | `5` |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Supplier return status | Detail page | `Posted` |
| MAIN stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `8` |
| Supplier credit note | `Finance -> Credit Notes` | Credit note for `SUP1`, amount `10` |

## 9. Stock Adjustment

Go to `Inventory -> Stock Adjustments`.

Create:

| Field | Input |
| --- | --- |
| Warehouse | `MAIN` |

Add counted line:

| Item | Counted Qty | Unit Cost |
| --- | ---: | ---: |
| `SKU-CORE` | `9` | `5` |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| System quantity before count | Stock adjustment stock widget | `8` |
| Expected variance | Stock adjustment line/widget | `+1` |
| Posted stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `9` |
| Stock ledger | `Reporting -> Stock Ledger`, item `SKU-CORE` | Adjustment movement `+1` |

## 10. Service Job / Servicing Full Workflow

Start this section after completing the partial GRN tests in sections 3.2 and 3.3. At that point stock should already contain `SKU-CORE = 20`, `SKU-BATCH = 10` in batch `LOT-A`, and serials `SER-001` / `SER-002` in `MAIN`. If you also completed sections 5 through 9, use the lower balances shown there.

This section is the full service-job test. It must prove that the job can be opened, planned, started, executed through daily work records, issued with materials, updated by technicians/supervisors, costed, billed or marked not billable, closed, and audited afterward.

Use this interpretation while testing:

| Job area | Meaning | Who normally owns it |
| --- | --- | --- |
| `Overview` | Job header, intake, customer/equipment, entitlement, closeout readiness links | service supervisor / service coordinator |
| `Plan` | Intended work stages before or during repair. `Sequence` means execution order, such as `10`, `20`, `30`. `Operation / subassembly` means the work step or equipment area, such as `Diagnose starting system` or `Fuel pump assembly`. Planned parts here are only expected parts; they do not reduce stock. | service supervisor / workshop lead |
| `Daily Work -> Daily Sheets` | The daily job card. Planned work is what the team expects to do that day; completed/pending/problems are the daily field record. | supervisor / job card preparer |
| `Daily Work -> Staff / Labor` | People assigned to the job for that day. Link to a daily sheet when the work belongs to a specific day. | supervisor / timekeeper |
| `Daily Work -> Progress` | Actual progress notes from technicians or supervisors: work done, pending work, problems, customer instructions, and site issues. | technician / supervisor |
| `Materials` | MRN creation, material issue, and used/unused/damaged/rejected material disposition. | stores / service supervisor |
| `Expenses` | IOUs, petty-cash spending, and out-of-pocket claims linked to the job and optionally to a daily sheet. | technician / finance / supervisor |
| `Billing` | Estimate/invoice trail, closeout readiness, and final invoice decision. | service coordinator / finance |
| `Costs` | Actual cost, quoted revenue, invoice revenue, margin view, and source tables. | supervisor / finance |
| `Files & Notes` | Comments, attachments, and supporting evidence. | all authorized job users |

Professional service systems normally avoid one long job form. In ISS, testers should confirm the job detail behaves as a workspace with tabs and sub-tabs, and that pending closeout tiles open the related data list or workflow area instead of leaving the user to search manually.

### 10.1 Create Equipment And Job

Go to `Service -> Equipment Units`.

Create equipment unit:

| Field | Input |
| --- | --- |
| Mode | `Existing item` |
| Equipment item | `EQP-GEN` |
| Serial number | `GEN-SN-001` |
| Customer | `CUS1` |
| Warranty coverage | `Labor and Parts` |

Go to `Service -> Jobs`.

Create job:

| Field | Input |
| --- | --- |
| Equipment unit | `GEN-SN-001` |
| Customer | `CUS1` |
| Job type | `Repair` |
| Responsible officer / supervisor | `Service Supervisor` |
| Customer requirement | `Customer reports generator does not start` |
| More intake details -> Job description | `Repair and test generator starting system` |
| More intake details -> Problem description / intake note | `Generator does not start` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Equipment unit | `Service -> Equipment Units` | `GEN-SN-001` exists |
| Job status | Job detail | `Open` |
| Job tabs | Job detail | tabs appear: `Overview`, `Plan`, `Daily Work`, `Materials`, `Expenses`, `Billing`, `Costs`, `Files & Notes` |
| Job intake | Job detail, `Overview` tab -> `Job Intake` | job description, complaint, supervisor, and intake note are visible |

Start the job from the service job detail before recording execution activity.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Job status | Job detail | status moves from `Open` to `In Progress` |
| Header lock | Job detail, `Overview` tab -> `Edit Job` | core intake/header fields are no longer editable after work starts |

### 10.1.1 Service Job Tab Navigation Smoke Test

Open the job detail and verify each tab loads without leaving the job context:

| Tab | Expected content |
| --- | --- |
| `Overview` | edit/intake summary, warranty/billing entitlement, closeout readiness, related workflow links |
| `Plan` | `Job Operations / Sub-Parts Plan` |
| `Daily Work` | sub-tabs for `Daily Sheets`, `Staff / Labor`, and `Progress` |
| `Materials` | materials/lubricants issue and material returns/damage/rejection |
| `Expenses` | IOU / employee advance, petty cash expense, employee out-of-pocket claim |
| `Billing` | closeout readiness, final invoice decision, quotations and final invoices |
| `Costs` | actual cost cards, profitability report, cost sources |
| `Files & Notes` | comments and attachments |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Persistent header | every tab | job number, equipment, customer, type, status, and action buttons remain visible |
| Active tab | tab bar | selected tab is visually highlighted |
| Deep links | browser URL | non-overview tabs use `?tab=plan`, `?tab=daily-work`, `?tab=materials`, `?tab=expenses`, `?tab=billing`, `?tab=costs`, or `?tab=files` |
| Invalid tab fallback | manually open `?tab=wrong` | page falls back to `Overview` |
| Closeout tile links | `Overview` or `Billing` tab -> `Closeout Readiness` | pending tiles are clickable and open the relevant tab/sub-tab |

Closeout readiness click-through test:

| Tile | Expected navigation or focus |
| --- | --- |
| `Daily field sheets` | opens `Daily Work` with the daily sheet list visible |
| `Expense claims` | opens `Expenses` or the related expense claim workflow/list |
| `Petty cash IOUs` | opens `Expenses` or the related IOU workflow/list |
| `Draft material requisitions` | opens `Materials` or the related MRN list |
| `Technician assignments` | opens `Daily Work -> Staff / Labor` |
| `Labor entries` | opens the labor/work-order area where pending labor can be submitted or approved |
| `Job detail work orders` | opens the related work order list/detail, not a useless link back to the same job page |
| `Material disposition` | opens `Materials` with disposition/returns visible |
| `Final invoice decision` | opens `Billing` with invoice trail and `Mark Not Billable` visible |

### 10.1.2 Plan Operations And Sub-Parts

Open the job detail, then open the `Plan` tab.

In `Job Operations / Sub-Parts Plan`, expand `Add operation / sub-part` and add:

| Field | Input |
| --- | --- |
| Step No. | `10` |
| Work step / subassembly | `Diagnose starting system` |
| Required by | tomorrow |
| Planned part | `SKU-SERIAL - Control Board Serialized` |
| Planned qty | `1` |
| Labor hours | `2` |
| Description | `Check starter control circuit and replace weak control board if required` |
| Notes | `Warranty coverage expected` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Operation row | `Plan` tab | new operation appears with planned part, quantity, labor hours, and status `Planned` |
| Operation actions | `Plan` tab | action buttons allow starting and completing the operation while job is active |
| Sub-part planning | `Plan` tab | planned sub-part does not reduce inventory until an MRN is posted |

Start the operation. Complete it later after the MRN and labor steps below.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Operation start | `Plan` tab | status moves to `In Progress` and started timestamp appears |

### 10.1.3 Create Daily Field Sheet

Open the service job detail.

Open the `Daily Work` tab, then the `Daily Sheets` sub-tab. Expand `Create daily field sheet` and create:

| Field | Input |
| --- | --- |
| Date / time | today, current time |
| Prepared by | `Service Supervisor` |
| Site / location | `Customer workshop` |
| Shift | `Day` |
| Site condition | `Equipment received at workshop` |
| Planned work | `Diagnose generator starting system and request required materials` |
| Completed work | `Initial inspection completed` |
| Pending work | `Issue parts and complete repair` |
| Problems found | `Control board output weak` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Daily sheet | Job detail, `Daily Work` tab -> `Daily Field Sheets` | new `JDS...` row appears with status `Draft` |
| Counts | Daily sheet row | staff, progress, MRN, returns, expenses, and IOU counts start at `0` |
| Running job tracking | Job detail | users can continue daily work, cash, expenses, materials, and returns from the system instead of paper notes |
| Sheet action links | daily sheet row | `Labor` and `Progress` links open the selected sheet in the corresponding sub-tab |

Daily sheet relationship test:

| Action | Expected output |
| --- | --- |
| Open `Daily Work -> Staff / Labor` without selecting a sheet | form still allows unlinked job assignment if the business allows it |
| Select the `JDS...` daily sheet in staff/labor | assignment is counted on that sheet row |
| Open `Daily Work -> Progress` without selecting a sheet | form still allows unlinked progress if the update is general job progress |
| Select the `JDS...` daily sheet in progress | progress is counted on that sheet row |
| Submit the daily sheet before all daily entries are ready | system should either allow supervisor submission or show clear validation; record the observed behavior |

### 10.2 MRN Available Stock Validation

Go to `Service -> Material Requisitions`.

Create MRN:

| Field | Input |
| --- | --- |
| Job | The job from 10.1 |
| Warehouse | `MAIN` |

Preferred daily workflow:

From the job detail, open the `Materials` tab and use `Materials / Lubricants Issue`. Select the `JDS...` daily sheet, warehouse `MAIN`, and create the MRN. Then open the MRN and add the lines below.

Add valid line:

| Item | Qty |
| --- | ---: |
| `SKU-CORE` | `2` |

Expected:

| Check | Where | Expected output |
| --- | --- |
| Line save | MRN detail | Line saves successfully because `MAIN` has `9` |

Try invalid line:

| Item | Qty |
| --- | ---: |
| `SKU-CORE` | `999` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Over-request validation | MRN add line | Error showing insufficient stock / available quantity |

### 10.3 MRN Serial Picker Validation

Add serial item line:

| Item | Qty | Serial |
| --- | ---: | --- |
| `SKU-SERIAL` | `1` | Select `SER-001` from available serial picker |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Serial picker | MRN detail | `SER-001` appears as available |
| Line save | MRN detail | Saves with selected serial |

Post the MRN.

Expected after posting:

| Check | Where | Expected output |
| --- | --- | --- |
| MRN status | MRN detail | `Posted` |
| MAIN `SKU-CORE` | `Inventory -> On Hand` | `7` because `9 - 2 = 7` |
| Serial `SER-001` | `Inventory -> Inventory Availability`, search `SER-001` | no available row, or on hand removed |
| Serial `SER-002` | `Inventory -> Inventory Availability`, search `SER-002` | still available with on hand `1` |
| Job costing | Service job detail, `Costs` tab | Material cost includes `2 x 5 + 1 x 25 = 35` |

### 10.4 Job Material Disposition And Return

Open the service job detail and open the `Materials` tab.

In `Material Returns / Damage / Rejection`, add material dispositions for the posted MRN lines:

| MRN Line | Disposition | Qty | Condition | Charge To | Reason | Serials |
| --- | --- | ---: | --- | --- | --- | --- |
| `SKU-CORE` line | `Used` | `1` | `Installed` | `Customer` | `Installed filter during repair` | blank |
| `SKU-CORE` line | `Unused returned` | `1` | `Good` | `Company` | `Extra filter not used` | blank |
| `SKU-SERIAL` line | `Used` | `1` | `Installed` | `Warranty` | `Installed replacement board` | `SER-001` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Disposition rows | Job detail, `Costs` tab -> `Cost Sources` | each MRN line shows disposition instead of `Pending` |
| Returned stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | increases by `1` because one unused filter was returned |
| Job closeout readiness | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Material disposition` is clear after every posted MRN line quantity is fully disposed |
| Job costing | Job detail, `Costs` tab | material consumed cost still shows the original MRN issue cost; returned/used/damaged status is shown in disposition trail |
| Daily sheet | Job detail, `Daily Work` tab -> `Daily Field Sheets` | MRN and return counts increase for the selected `JDS...` |

## 11. Service Estimate, Work Order, Expense, Handover

This section completes the same job from section 10. Do not create a second job unless you are testing parallel service scenarios.

### 11.1 Technician Assignment And Work Order

Go to `Service -> Technicians`.

Create:

| Field | Input |
| --- | --- |
| Code | `TECH1` |
| Name | `Workshop Technician` |
| Default Cost Rate | `10` |
| Default Billing Rate | `25` |

Go to `Service -> Work Orders`.

Create work order for the job.

Go back to the job detail, open the `Daily Work` tab, click the `Labor` link on the `JDS...` daily sheet row or open the `Staff / Labor` sub-tab, and add technician assignment:

| Field | Input |
| --- | --- |
| Daily sheet | `JDS...` from section 10.1.3 |
| Technician | `TECH1` |
| Role | `Technician` |
| Assigned task | `Diagnose starting system and replace required parts` |
| Normal hours | `2` |
| Overtime hours | `0` |
| Daily work description | `Diagnosis and replacement completed` |

Approve the assignment.

Add time entry:

| Field | Input |
| --- | --- |
| Technician | `TECH1` |
| Hours worked | `2` |
| Cost rate | `10` |
| Billing rate | `25` |
| Billable | `Yes` |

Start the work order, submit and approve the time entry, then mark the work order done.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Assignment | Job detail, `Daily Work` tab -> `Staff / Labor` sub-tab | assignment status is `Approved` |
| Daily sheet count | Job detail, `Daily Work` tab -> `Daily Field Sheets` | staff count increases |
| Work order | Work order detail | status moves `Open -> In Progress -> Done` |
| Labor cost | Work order/job detail, `Costs` tab | `2 x 10 = 20` |
| Billable labor before entitlement | Work order | `2 x 25 = 50` |
| If warranty/contract covers labor | Estimate/invoice conversion | effective customer billing can be `0` for covered labor |

### 11.1.1 Daily Job Progress

Open the service job detail, open the `Daily Work` tab, click the `Progress` link on the `JDS...` daily sheet row or open the `Progress` sub-tab, and add progress update:

| Field | Input |
| --- | --- |
| Daily sheet | `JDS...` from section 10.1.3 |
| Work completed | `Starting system diagnosed and replacement parts installed` |
| Work pending | `Final customer confirmation` |
| Problems found | `Weak control board output` |
| Additional parts required | `None` |
| Additional labor required | `None` |
| Customer instructions | `Call before delivery` |
| Technician notes | `Test run completed` |
| Supervisor notes | `Ready for handover after invoice review` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Daily progress | Job detail, `Daily Work` tab -> `Progress` sub-tab | progress update appears with completed/pending/problem notes |
| Daily sheet count | Job detail, `Daily Work` tab -> `Daily Field Sheets` | progress count increases |
| Closeout link | `Overview` or `Billing` -> `Closeout Readiness`, click `Daily field sheets` if pending | navigates back to the daily sheet list that contains the draft/pending `JDS...` |

Return to the `Plan` tab and complete the planned operation from section 10.1.2.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Operation status | Job detail, `Plan` tab | operation status becomes `Completed` |
| Actuals shown | Job detail, `Plan` tab | actual material quantity/cost and approved labor hours/cost reflect posted MRN and approved labor where linked by job |

### 11.2 Estimate

Go to `Service -> Estimates`.

Create estimate for the job.

Add lines:

| Kind | Item | Description | Qty | Unit Price | Tax % |
| --- | --- | --- | ---: | ---: | ---: |
| Part | `SKU-CORE` | `Filter replacement` | `2` | `7` | `0` |
| Labor | `LAB-SVC` | `Diagnosis labor` | `2` | `25` | `0` |
| Expense | blank or expense item | `Travel cost` | `1` | `15` | `0` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Description display | Estimate detail | Descriptions show in expandable detail rows, not squeezed into the main grid |
| Estimate total before entitlement | Estimate detail | `14 + 50 + 15 = 79` |
| Covered lines | Estimate detail | Covered parts/labor may show unit price `0` depending on job entitlement |

Send estimate, mark customer approved.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Approval state | Estimate detail | `Customer Approval = Approved` |
| Change order | Approved estimate detail | `Create Change Order` creates a new draft revision |
| Estimate link | Job detail, `Billing` tab -> `Quotations & Final Invoices` | approved estimate appears in the estimate table |

### 11.3 Expense Claim

Before the expense claim, create an IOU advance to test petty-cash advance handling while the job is still running.

Preferred daily workflow:

Open the job detail, open the `Expenses` tab, and use `IOU / Employee Advance`.

Create IOU:

| Field | Input |
| --- | --- |
| Daily sheet | `JDS...` from section 10.1.3 |
| Person | `TECH1` |
| Amount | `20` |
| Purpose | `Travel and parking advance for generator repair` |

Submit, approve, release from petty cash fund, then settle the IOU.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| IOU status | `Finance -> Petty Cash IOUs` | status reaches `Settled` |
| Daily sheet count | Job detail, `Daily Work` tab -> `Daily Field Sheets` | IOU count increases |
| Job closeout readiness | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Petty cash IOUs` is clear only after settlement/rejection/cancellation |

Create the employee expense voucher from the job detail `Expenses` tab using `Employee Out-of-Pocket Claim`. To test company-funded cash spending separately, use `Petty Cash Expense`.

Create claim:

| Field | Input |
| --- | --- |
| Daily sheet | `JDS...` from section 10.1.3 |
| Funding Source | `Out of Pocket` |
| Merchant | `Test Vendor` |

Add line:

| Description | Qty | Unit Cost | Billable |
| --- | ---: | ---: | --- |
| `Parking fee` | `1` | `5` | `Yes` |

Submit, approve, and settle.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Claim status | Claim detail | `Settled` |
| Job costing | Job detail, `Costs` tab | Expense claim cost includes `5` |
| Convert to estimate | Claim detail | Billable line can be converted into draft estimate/change order |
| Daily sheet count | Job detail, `Daily Work` tab -> `Daily Field Sheets` | expense count increases |
| Job closeout readiness | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Expense claims` is clear after claim is settled or rejected |

### 11.4 Handover, Final Invoice, And Closeout

Go to `Service -> Handovers`.

Create handover for the job.

| Field | Input |
| --- | --- |
| Items returned | `Generator returned after repair` |
| Customer acknowledgement | `Customer accepted` |
| Notes | `Test handover` |

Complete handover and convert to invoice.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Handover status | Handover detail | `Completed` |
| Sales invoice | Handover detail / `Sales -> Invoices` | Invoice created and linked |
| Job costing | Job detail, `Costs` tab | Invoice value appears in costing summary |
| Invoice trail | Job detail, `Billing` tab -> `Quotations & Final Invoices` | linked invoice appears in the invoice table |
| Job status | Job detail | status becomes `Invoiced` after handover conversion |

Open the service job detail and review `Closeout Readiness` in either the `Overview` tab or the `Billing` tab.

Before final closeout, open the `Daily Work` tab and submit/approve the `JDS...` daily field sheet.

Expected before closing:

| Check | Where | Expected output |
| --- | --- | --- |
| Daily sheet | Job detail, `Daily Work` tab -> `Daily Field Sheets` | status reaches `Approved` |
| Material disposition | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Clear` |
| Expense claims | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Clear` |
| Petty cash IOUs | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Clear` |
| Work orders | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Clear` |
| Labor entries | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Clear` |
| Final invoice decision | Job detail, `Overview` or `Billing` tab -> `Closeout Readiness` | `Clear` because invoice was generated |

Click every closeout readiness tile once more after clearing the work:

| Tile | Expected output |
| --- | --- |
| Daily field sheets | shows approved daily sheet rows, no unexplained pending count |
| Expense claims | all job-linked claims are settled, rejected, or cancelled |
| Petty cash IOUs | all job-linked IOUs are settled, rejected, or cancelled |
| Direct purchase supplier bills | no pending supplier bill blocks the job |
| Draft material requisitions | no draft MRN blocks the job |
| Technician assignments | no unapproved assignment blocks the job |
| Labor entries | no submitted/pending labor entry blocks the job |
| Job detail work orders | related work orders are done or cancelled |
| Material disposition | every posted MRN line has full disposition |
| Final invoice decision | invoice exists, or job is explicitly marked not billable with a reason |

If this is a no-charge/warranty-only job and no invoice should be generated, open the `Billing` tab, click `Mark Not Billable`, and enter:

| Field | Input |
| --- | --- |
| Reason | `Warranty/no-charge job - final invoice not required` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Final invoice decision | Job detail, `Billing` tab -> `Closeout Readiness` | `Clear` |
| Job header | Job detail | `Invoice required: No` and reason is visible |

Complete the job and close it.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Job close | Job detail actions | close succeeds only when all closeout checks are clear |
| Locked materials | Job detail, `Materials` tab | adding MRN or material disposition is blocked with closed-job validation |
| Locked expenses | Job detail, `Expenses` tab | adding IOU or expense claim is blocked with closed-job validation |
| Locked labor | Job detail, `Daily Work` tab | adding assignment or progress is blocked with closed-job validation |
| Locked planning | Job detail, `Plan` tab | starting/completing operations is disabled or blocked after close |
| Reopen | Job detail actions | `Reopen` returns the job to an editable active state only for authorized users |
| Re-close | Job detail actions | closeout readiness is re-evaluated before the job can be closed again |

### 11.5 Files, Notes, And Job PDF

Open the job detail and use the `Files & Notes` tab.

Add comment:

| Field | Input |
| --- | --- |
| Comment | `Customer confirmed generator starts after repair.` |

Upload one small test attachment, such as a PDF or image:

| Field | Input |
| --- | --- |
| File | any small test file |
| Optional notes | `Before/after repair evidence` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Comment | Job detail, `Files & Notes` tab | new comment appears without leaving the job |
| Attachment | Job detail, `Files & Notes` tab | file row appears with type, notes, created time, and action link |
| Job PDF | Job detail -> `Download PDF` | PDF opens and uses the same job number shown in the header |

### 11.6 Final Service Job Audit Trail

After the job is closed, review the same job from each tab and confirm no data was lost during the workflow.

| Area | Where | Expected output |
| --- | --- | --- |
| Header | job detail header | status is `Closed`, equipment/customer/job type remain correct |
| Overview | `Overview` tab | intake, entitlement, and closeout readiness remain visible |
| Plan | `Plan` tab | operation is completed with actuals visible |
| Daily sheet | `Daily Work -> Daily Sheets` | `JDS...` is approved and shows staff/progress/MRN/return/expense/IOU counts |
| Staff/labor | `Daily Work -> Staff / Labor` | assignment and approved labor remain linked to the job or daily sheet |
| Progress | `Daily Work -> Progress` | technician/supervisor notes remain visible |
| Materials | `Materials` tab | posted MRN and material dispositions are traceable |
| Expenses | `Expenses` tab | settled IOU and expense claim are traceable |
| Billing | `Billing` tab | estimate and final invoice or not-billable reason are visible |
| Costs | `Costs` tab | material, labor, expense, invoice, and margin values match the source documents |
| Files & Notes | `Files & Notes` tab | comment and attachment remain visible |
| Reporting | `Reporting -> Service KPIs` | service job activity is included in KPI totals |

## 12. Finance Payment Checks

### Customer Payment

Go to `Finance -> Payments`.

Create incoming payment:

| Field | Input |
| --- | --- |
| Direction | `Incoming` |
| Counterparty Type | `Customer` |
| Counterparty | `CUS1` |
| Currency | Base currency |
| Amount | `42` |

Allocate to the sales invoice from section 6.2.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Payment detail | Payment detail | Allocated `42`, remaining `0` |
| AR | `Finance -> AR`, outstanding only | Invoice no longer outstanding |
| Invoice status | Invoice detail | `Paid` if fully allocated |

### Supplier Payment

Create outgoing payment:

| Field | Input |
| --- | --- |
| Direction | `Outgoing` |
| Counterparty Type | `Supplier` |
| Counterparty | `SUP1` |
| Currency | Base currency |
| Amount | `230` |

Allocate to GRN/AP entries.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Payment detail | Payment detail | Allocated up to `230`, remaining `0` if all AP entries selected |
| AP | `Finance -> AP`, outstanding only | GRN/AP entries no longer outstanding |

## 13. Reporting Checks

### Stock Ledger

Go to `Reporting -> Stock Ledger`.

Filter item `SKU-CORE`.

Expected movement trail:

| Movement | Expected Qty |
| --- | ---: |
| GRN 1 receipt | `+8` |
| GRN 2 receipt | `+12` |
| Transfer out MAIN | `-5` |
| Transfer in SEC | `+5` |
| Direct dispatch | `-6` |
| Customer return | `+1` |
| Supplier return | `-2` |
| Stock adjustment | `+1` |
| MRN consumption | `-2` |
| Job unused material return | `+1` |

Expected final balances:

| Warehouse | Expected `SKU-CORE` |
| --- | ---: |
| MAIN | `8` |
| SEC | `5` |
| Total | `13` |

### Costing

Go to `Reporting -> Costing`.

Filter item `SKU-CORE`.

Expected:

| Check | Expected |
| --- | ---: |
| Total on hand | `13` if the unused service material return was completed |
| Weighted avg cost | `5` |
| Inventory value | `65` if the unused service material return was completed |

### Service KPIs

Go to `Reporting -> Service KPIs`.

Expected:

| Check | Expected |
| --- | --- |
| Jobs | At least one service job appears in KPI totals |
| Material requisitions | Posted MRN count increases |
| Labor/expense/costing | Service job costing data is reflected if the report includes those values |

## 14. PDF And Document Checks

Open PDF/download links for at least:

| Area | Document |
| --- | --- |
| Procurement | PO |
| Procurement | GRN 1 |
| Procurement | GRN 2 |
| Sales | Invoice |
| Inventory | Stock adjustment |
| Service | Estimate |
| Service | Job Order PDF |
| Service | MRN |
| Service | Handover |

Expected:

| Check | Expected |
| --- | --- |
| PDF opens/downloads | Browser receives PDF |
| Document number | Matches screen |
| Totals | Match screen totals |
| Lines | Match entered items, quantities, batch/serial where applicable |

## 15. Final Reconciliation

After all sections above:

| Area | Where | Expected |
| --- | --- | --- |
| `SKU-CORE` stock | `Inventory -> Inventory Availability` | MAIN `8`, SEC `5`, total `13` after unused service material return |
| `SKU-BATCH` stock | Availability search `LOT-A` | MAIN `10`, batch `LOT-A` |
| `SKU-SERIAL` stock | Availability search `SER-001` | consumed/unavailable |
| `SKU-SERIAL` stock | Availability search `SER-002` | available `1` |
| Inventory value for `SKU-CORE` | `Reporting -> Costing` | `13 x 5 = 65` after unused service material return |
| Customer AR | `Finance -> AR` | sales invoice cleared if payment allocated |
| Supplier AP | `Finance -> AP` | GRN/AP entries cleared if payment allocated |
| Service job costing | Job detail, `Costs` tab | material issue `35`, labor cost `20`, expense `5`, material dispositions clear, plus invoice/estimate values from service flow |

## 16. Common Failures And Where To Look

| Problem | Check |
| --- | --- |
| GRN cannot post | Confirm PO is approved and receipt quantity does not exceed remaining quantity |
| Batch item cannot save | Confirm batch/lot is entered for `SKU-BATCH` |
| Serial item cannot save | Confirm serial count equals quantity and serials are unique |
| MRN says insufficient stock | Check `Inventory -> Inventory Availability` for the source warehouse |
| MRN serial not listed | Serial may already be consumed or not received into that warehouse |
| Stock value does not match | Check `Reporting -> Stock Ledger` movement quantities and unit costs |
| AP/AR does not clear | Confirm payment allocation is saved against the correct outstanding entry |
| Service estimate billing differs from raw total | Check warranty/contract entitlement on the job; covered labor/parts can bill at zero |
| Job cannot close | Open job detail `Overview` or `Billing` tab -> `Closeout Readiness`; clear the listed pending item such as material disposition, IOU, expense claim, work order, labor entry, or final invoice decision |
| Returned service material not back in stock | Confirm disposition type is `Unused returned` or `Incorrect returned`; `Used`, `Damaged`, and `Rejected / supplier return` do not add warehouse stock |
## Recent Workflow Updates

### Service Material Returns

- Job material issue/MRN continues to consume inventory when the MRN is posted.
- Material return/disposition is now for exceptions only: not needed returns, wrongly issued returns, damaged material, and rejected/supplier-return material.
- Creating a material return/disposition line saves a draft only. Drafts can be edited or voided without touching stock.
- Posting a material return/disposition updates inventory only for returnable outcomes:
  - Not needed returned: receipt back to the job warehouse.
  - Wrongly issued returned: receipt back to the job warehouse.
  - Rejected/supplier return: receipt back to the job warehouse so a supplier return can issue it out through procurement.
  - Damaged: no usable-stock receipt.
- Existing active material disposition records from the old immediate-post workflow are migrated as posted records.

### Equipment Warranty Entry

- Equipment creation and edit forms now allow selecting warranty coverage without the control appearing locked.
- Warranty coverage and warranty end date must be provided together.
- Entering a warranty end date auto-selects the standard labor-and-parts coverage when coverage is still set to no warranty.

### Finance Credit Notes

- Credit notes are split in the UI into A/R Credit Notes for customer-side credits and A/P Credit Notes for supplier-side credits.
- The backend still stores both in the shared credit note model, but user navigation follows standard ERP terminology.
- Customer returns create customer-side A/R credit notes. Supplier returns create supplier-side A/P credit notes.

### Sales Invoice Source Creation

- Sales invoice creation can now start from a posted AOD/dispatch or direct dispatch.
- Invoice lines are copied from the selected source document instead of being re-entered manually.

### Customer Returns

- Customer return invoice selection excludes draft invoices.
- When a customer return references an invoice, return line item selection is restricted to items on that invoice.
- Returns without an invoice can still select any item.
- The standalone stock visibility selector was removed from customer return detail to avoid a duplicate item-entry UI; stock context remains inside the add-line workflow.
