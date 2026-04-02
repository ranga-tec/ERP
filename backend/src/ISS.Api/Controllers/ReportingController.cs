using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using ISS.Domain.Inventory;
using ISS.Domain.Procurement;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/reporting")]
[Authorize(Roles = Roles.AllBusiness)]
public sealed class ReportingController(IIssDbContext dbContext, InventoryService inventoryService) : ControllerBase
{
    public sealed record DashboardDto(
        int OpenServiceJobs,
        decimal ArOutstanding,
        decimal ApOutstanding,
        int ReorderAlerts,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<DashboardMetricDto> HeroMetrics,
        IReadOnlyList<DashboardAlertDto> Alerts,
        IReadOnlyList<DashboardSectionDto> Sections,
        IReadOnlyList<DashboardQuickActionDto> QuickActions);
    public sealed record DashboardMetricDto(
        string Label,
        string ValueType,
        int? Count,
        decimal? Amount,
        string Description,
        string? Href);
    public sealed record DashboardAlertDto(
        string Title,
        string Description,
        string Severity,
        int Count,
        string? Href);
    public sealed record DashboardSectionDto(
        string Key,
        string Title,
        string Description,
        IReadOnlyList<DashboardMetricDto> Metrics);
    public sealed record DashboardQuickActionDto(
        string Label,
        string Description,
        string Href);
    public sealed record StockLedgerRowDto(
        DateTimeOffset OccurredAt,
        InventoryMovementType MovementType,
        Guid WarehouseId,
        string WarehouseCode,
        string WarehouseName,
        Guid ItemId,
        string ItemSku,
        string ItemName,
        decimal Quantity,
        decimal UnitCost,
        decimal LineValue,
        decimal RunningQuantity,
        string ReferenceType,
        Guid ReferenceId,
        string? BatchNumber,
        string? SerialNumber);
    public sealed record StockLedgerReportDto(
        DateTimeOffset? From,
        DateTimeOffset? To,
        Guid? WarehouseId,
        Guid? ItemId,
        int Count,
        decimal NetQuantity,
        IReadOnlyList<StockLedgerRowDto> Rows);

    public sealed record AgingBucketsDto(decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal DaysOver90, decimal Total);
    public sealed record ArAgingRowDto(Guid CustomerId, string CustomerCode, string CustomerName, AgingBucketsDto Buckets);
    public sealed record ApAgingRowDto(Guid SupplierId, string SupplierCode, string SupplierName, AgingBucketsDto Buckets);
    public sealed record AgingReportDto(DateTimeOffset AsOf, IReadOnlyList<ArAgingRowDto> AccountsReceivable, AgingBucketsDto ArTotals, IReadOnlyList<ApAgingRowDto> AccountsPayable, AgingBucketsDto ApTotals);

    public sealed record TaxSummaryReportDto(
        DateTimeOffset From,
        DateTimeOffset To,
        decimal SalesTaxableSubtotal,
        decimal SalesTaxTotal,
        decimal PurchaseTaxableSubtotal,
        decimal PurchaseTaxTotal,
        decimal NetTaxPayable,
        int SalesInvoiceCount,
        int SupplierInvoiceCount);

    public sealed record ServiceKpiReportDto(
        DateTimeOffset From,
        DateTimeOffset To,
        int OpenedJobs,
        int InProgressJobs,
        int CompletedJobs,
        int ClosedJobs,
        int CancelledJobs,
        decimal? AverageCompletionHours,
        int OpenJobsOlderThan7Days,
        int OpenJobsOlderThan30Days,
        int EstimatesIssued,
        int EstimatesApproved,
        int HandoversCompleted,
        int MaterialRequisitionsPosted,
        decimal PartsConsumedQuantity);

    public sealed record CostingRowDto(
        Guid ItemId,
        string ItemSku,
        string ItemName,
        string UnitOfMeasure,
        decimal DefaultUnitCost,
        decimal? WeightedAverageCost,
        decimal? LastReceiptCost,
        DateTimeOffset? LastReceiptAt,
        decimal OnHandQuantity,
        decimal InventoryValue,
        decimal? CostVariancePercent);

    public sealed record CostingReportDto(
        Guid? WarehouseId,
        Guid? ItemId,
        string BaseCurrencyCode,
        int Count,
        decimal TotalOnHandQuantity,
        decimal TotalInventoryValue,
        IReadOnlyList<CostingRowDto> Rows);

    private sealed record StockLedgerRawRow(
        DateTimeOffset OccurredAt,
        DateTimeOffset CreatedAt,
        InventoryMovementType MovementType,
        Guid WarehouseId,
        string WarehouseCode,
        string WarehouseName,
        Guid ItemId,
        string ItemSku,
        string ItemName,
        decimal Quantity,
        decimal UnitCost,
        string ReferenceType,
        Guid ReferenceId,
        string? BatchNumber,
        string? SerialNumber);

    private sealed record ArAgingRawRow(Guid CustomerId, string CustomerCode, string CustomerName, decimal Outstanding, DateTimeOffset PostedAt);
    private sealed record ApAgingRawRow(Guid SupplierId, string SupplierCode, string SupplierName, decimal Outstanding, DateTimeOffset PostedAt);

    private readonly record struct AgingBuckets(decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal DaysOver90)
    {
        public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + DaysOver90;

        public AgingBucketsDto ToDto() => new(Current, Days1To30, Days31To60, Days61To90, DaysOver90, Total);

        public static AgingBuckets operator +(AgingBuckets left, AgingBuckets right)
            => new(
                left.Current + right.Current,
                left.Days1To30 + right.Days1To30,
                left.Days31To60 + right.Days31To60,
                left.Days61To90 + right.Days61To90,
                left.DaysOver90 + right.DaysOver90);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var canAnalytics = HasAnyRole(Roles.Admin, Roles.Reporting);
        var canProcurementArea = HasAnyRole(Roles.Admin, Roles.Procurement, Roles.Finance);
        var canInventoryArea = HasAnyRole(Roles.Admin, Roles.Inventory, Roles.Reporting);
        var canSalesQuotesOrdersArea = HasAnyRole(Roles.Admin, Roles.Sales, Roles.Finance);
        var canSalesDispatchArea = HasAnyRole(Roles.Admin, Roles.Sales, Roles.Inventory, Roles.Finance);
        var canDirectDispatchArea = HasAnyRole(Roles.Admin, Roles.Sales, Roles.Inventory, Roles.Service);
        var canSalesInvoicesArea = HasAnyRole(Roles.Admin, Roles.Sales, Roles.Inventory, Roles.Finance);
        var canServiceCoreArea = HasAnyRole(Roles.Admin, Roles.Service, Roles.Sales);
        var canServiceEstimatesArea = HasAnyRole(Roles.Admin, Roles.Service, Roles.Sales, Roles.Finance);
        var canExpenseClaimsArea = HasAnyRole(Roles.Admin, Roles.Service, Roles.Finance);
        var canMaterialRequisitionsArea = HasAnyRole(Roles.Admin, Roles.Service, Roles.Inventory);
        var canFinanceArea = HasAnyRole(Roles.Admin, Roles.Finance);

        var canViewServiceSummary = canServiceCoreArea || canServiceEstimatesArea || canExpenseClaimsArea || canMaterialRequisitionsArea || canAnalytics;
        var canViewFinanceSummary = canFinanceArea || canAnalytics;
        var canViewInventorySummary = canInventoryArea || canAnalytics;
        var canViewProcurementSummary = canProcurementArea;
        var canViewSalesSummary = canSalesQuotesOrdersArea || canSalesDispatchArea || canDirectDispatchArea || canSalesInvoicesArea;

        var serviceSummaryHref = canServiceCoreArea ? "/service/jobs" : canAnalytics ? "/reporting/service-kpis" : null;
        var serviceEstimateHref = canServiceEstimatesArea ? "/service/estimates" : canAnalytics ? "/reporting/service-kpis" : null;
        var expenseClaimHref = canExpenseClaimsArea ? "/service/expense-claims" : canAnalytics ? "/reporting/service-kpis" : null;
        var materialRequisitionHref = canMaterialRequisitionsArea ? "/service/material-requisitions" : canAnalytics ? "/reporting/service-kpis" : null;
        var financeArHref = canFinanceArea ? "/finance/ar" : canAnalytics ? "/reporting/aging" : null;
        var financeApHref = canFinanceArea ? "/finance/ap" : canAnalytics ? "/reporting/aging" : null;
        var reorderHref = canInventoryArea ? "/inventory/reorder-alerts" : canAnalytics ? "/reporting/stock-ledger" : null;
        var inventoryAdjustmentHref = canInventoryArea ? "/inventory/stock-adjustments" : canAnalytics ? "/reporting/stock-ledger" : null;
        var inventoryTransferHref = canInventoryArea ? "/inventory/stock-transfers" : canAnalytics ? "/reporting/stock-ledger" : null;

        var openServiceJobs = 0;
        var inProgressServiceJobs = 0;
        var completedServiceJobs = 0;
        var openJobsOlderThan7Days = 0;
        var draftServiceEstimates = 0;
        var pendingEstimateCustomerApprovals = 0;
        var workOrdersInProgress = 0;
        var draftMaterialRequisitions = 0;
        var submittedExpenseClaims = 0;
        var approvedExpenseClaims = 0;

        if (canViewServiceSummary)
        {
            var serviceJobs = await dbContext.ServiceJobs.AsNoTracking()
                .Select(x => new { x.Status, x.OpenedAt })
                .ToListAsync(cancellationToken);

            openServiceJobs = serviceJobs.Count(x => x.Status is ServiceJobStatus.Open or ServiceJobStatus.InProgress);
            inProgressServiceJobs = serviceJobs.Count(x => x.Status == ServiceJobStatus.InProgress);
            completedServiceJobs = serviceJobs.Count(x => x.Status == ServiceJobStatus.Completed);
            openJobsOlderThan7Days = serviceJobs.Count(x =>
                x.Status is ServiceJobStatus.Open or ServiceJobStatus.InProgress &&
                (generatedAt - x.OpenedAt).TotalDays > 7d);

            var workOrderStatusCounts = await dbContext.WorkOrders.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            workOrdersInProgress = GetCount(workOrderStatusCounts, WorkOrderStatus.InProgress);

            var estimateSnapshots = await dbContext.ServiceEstimates.AsNoTracking()
                .Select(x => new { x.Status, x.CustomerApprovalStatus })
                .ToListAsync(cancellationToken);
            draftServiceEstimates = estimateSnapshots.Count(x => x.Status == ServiceEstimateStatus.Draft);
            pendingEstimateCustomerApprovals = estimateSnapshots.Count(x =>
                x.Status == ServiceEstimateStatus.Draft &&
                x.CustomerApprovalStatus == ServiceEstimateCustomerApprovalStatus.Pending);

            var expenseClaimStatusCounts = await dbContext.ServiceExpenseClaims.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            submittedExpenseClaims = GetCount(expenseClaimStatusCounts, ServiceExpenseClaimStatus.Submitted);
            approvedExpenseClaims = GetCount(expenseClaimStatusCounts, ServiceExpenseClaimStatus.Approved);

            var materialReqStatusCounts = await dbContext.MaterialRequisitions.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftMaterialRequisitions = GetCount(materialReqStatusCounts, MaterialRequisitionStatus.Draft);
        }

        var reorderAlerts = 0;
        var draftStockAdjustments = 0;
        var draftStockTransfers = 0;
        if (canViewInventorySummary)
        {
            reorderAlerts = await CountReorderAlertsAsync(cancellationToken);

            var stockAdjustmentStatusCounts = await dbContext.StockAdjustments.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftStockAdjustments = GetCount(stockAdjustmentStatusCounts, StockAdjustmentStatus.Draft);

            var stockTransferStatusCounts = await dbContext.StockTransfers.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftStockTransfers = GetCount(stockTransferStatusCounts, StockTransferStatus.Draft);
        }

        var draftPurchaseRequisitions = 0;
        var submittedPurchaseRequisitions = 0;
        var sentRfqs = 0;
        var activePurchaseOrders = 0;
        var draftDirectPurchases = 0;
        var draftSupplierInvoices = 0;
        if (canViewProcurementSummary)
        {
            var purchaseRequisitionStatusCounts = await dbContext.PurchaseRequisitions.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftPurchaseRequisitions = GetCount(purchaseRequisitionStatusCounts, PurchaseRequisitionStatus.Draft);
            submittedPurchaseRequisitions = GetCount(purchaseRequisitionStatusCounts, PurchaseRequisitionStatus.Submitted);

            var requestForQuoteStatusCounts = await dbContext.RequestForQuotes.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            sentRfqs = GetCount(requestForQuoteStatusCounts, RequestForQuoteStatus.Sent);

            var purchaseOrderStatusCounts = await dbContext.PurchaseOrders.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            activePurchaseOrders =
                GetCount(purchaseOrderStatusCounts, PurchaseOrderStatus.Approved) +
                GetCount(purchaseOrderStatusCounts, PurchaseOrderStatus.PartiallyReceived);

            var directPurchaseStatusCounts = await dbContext.DirectPurchases.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftDirectPurchases = GetCount(directPurchaseStatusCounts, DirectPurchaseStatus.Draft);

            var supplierInvoiceStatusCounts = await dbContext.SupplierInvoices.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftSupplierInvoices = GetCount(supplierInvoiceStatusCounts, SupplierInvoiceStatus.Draft);
        }

        var draftSalesQuotes = 0;
        var sentSalesQuotes = 0;
        var acceptedSalesQuotes = 0;
        var confirmedSalesOrders = 0;
        var draftDispatches = 0;
        var draftDirectDispatches = 0;
        var draftSalesInvoices = 0;
        if (canViewSalesSummary)
        {
            var salesQuoteStatusCounts = await dbContext.SalesQuotes.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftSalesQuotes = GetCount(salesQuoteStatusCounts, SalesQuoteStatus.Draft);
            sentSalesQuotes = GetCount(salesQuoteStatusCounts, SalesQuoteStatus.Sent);
            acceptedSalesQuotes = GetCount(salesQuoteStatusCounts, SalesQuoteStatus.Accepted);

            var salesOrderStatusCounts = await dbContext.SalesOrders.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            confirmedSalesOrders = GetCount(salesOrderStatusCounts, SalesOrderStatus.Confirmed);

            var dispatchStatusCounts = await dbContext.DispatchNotes.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftDispatches = GetCount(dispatchStatusCounts, DispatchStatus.Draft);

            var directDispatchStatusCounts = await dbContext.DirectDispatches.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftDirectDispatches = GetCount(directDispatchStatusCounts, DirectDispatchStatus.Draft);

            var salesInvoiceStatusCounts = await dbContext.SalesInvoices.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            draftSalesInvoices = GetCount(salesInvoiceStatusCounts, SalesInvoiceStatus.Draft);
        }

        var arOutstanding = 0m;
        var arCustomersWithBalances = 0;
        var arOverdueEntries = 0;
        var apOutstanding = 0m;
        var apSuppliersAwaitingPayment = 0;
        var apOverdueEntries = 0;
        if (canViewFinanceSummary)
        {
            var arEntries = await dbContext.AccountsReceivableEntries.AsNoTracking()
                .Where(x => x.Outstanding > 0m)
                .Select(x => new { x.CustomerId, x.Outstanding, x.PostedAt })
                .ToListAsync(cancellationToken);
            arOutstanding = arEntries.Sum(x => x.Outstanding);
            arCustomersWithBalances = arEntries.Select(x => x.CustomerId).Distinct().Count();
            arOverdueEntries = arEntries.Count(x => (generatedAt.Date - x.PostedAt.Date).TotalDays > 30d);

            var apEntries = await dbContext.AccountsPayableEntries.AsNoTracking()
                .Where(x => x.Outstanding > 0m)
                .Select(x => new { x.SupplierId, x.Outstanding, x.PostedAt })
                .ToListAsync(cancellationToken);
            apOutstanding = apEntries.Sum(x => x.Outstanding);
            apSuppliersAwaitingPayment = apEntries.Select(x => x.SupplierId).Distinct().Count();
            apOverdueEntries = apEntries.Count(x => (generatedAt.Date - x.PostedAt.Date).TotalDays > 30d);
        }

        var heroMetrics = new List<DashboardMetricDto>();
        if (canViewServiceSummary)
        {
            heroMetrics.Add(CreateCountMetric(
                "Open service jobs",
                openServiceJobs,
                "Jobs that still need operational follow-up.",
                serviceSummaryHref));
        }

        if (canViewInventorySummary)
        {
            heroMetrics.Add(CreateCountMetric(
                "Reorder alerts",
                reorderAlerts,
                "Items already at or below their reorder point.",
                reorderHref));
        }

        if (canViewFinanceSummary)
        {
            heroMetrics.Add(CreateCurrencyMetric(
                "AR outstanding",
                arOutstanding,
                "Customer balances still open for collection.",
                financeArHref));
            heroMetrics.Add(CreateCurrencyMetric(
                "AP outstanding",
                apOutstanding,
                "Supplier balances still waiting for settlement.",
                financeApHref));
        }

        if (canViewProcurementSummary)
        {
            heroMetrics.Add(CreateCountMetric(
                "POs awaiting receipt",
                activePurchaseOrders,
                "Approved or partially received purchase orders in flight.",
                "/procurement/purchase-orders"));
        }

        if (canSalesQuotesOrdersArea)
        {
            heroMetrics.Add(CreateCountMetric(
                "Confirmed sales orders",
                confirmedSalesOrders,
                "Customer orders ready for dispatch or completion.",
                "/sales/orders"));
        }

        if (canServiceEstimatesArea || canAnalytics)
        {
            heroMetrics.Add(CreateCountMetric(
                "Estimates pending customer",
                pendingEstimateCustomerApprovals,
                "Issued estimates still waiting on customer response.",
                serviceEstimateHref));
        }

        var alerts = new List<DashboardAlertDto>();
        if (canViewInventorySummary && reorderAlerts > 0)
        {
            alerts.Add(CreateAlert(
                "Inventory replenishment required",
                $"{reorderAlerts} items are at or below reorder point and should be reviewed.",
                reorderAlerts >= 10 ? "high" : "medium",
                reorderAlerts,
                reorderHref));
        }

        if (canViewServiceSummary && openJobsOlderThan7Days > 0)
        {
            alerts.Add(CreateAlert(
                "Aged service workload",
                $"{openJobsOlderThan7Days} open or in-progress jobs are older than seven days.",
                openJobsOlderThan7Days >= 10 ? "high" : "medium",
                openJobsOlderThan7Days,
                serviceSummaryHref));
        }

        if (canViewFinanceSummary && arOverdueEntries > 0)
        {
            alerts.Add(CreateAlert(
                "Overdue receivables",
                $"{arOverdueEntries} receivable entries are more than 30 days old.",
                arOverdueEntries >= 10 ? "high" : "medium",
                arOverdueEntries,
                financeArHref));
        }

        if (canViewFinanceSummary && apOverdueEntries > 0)
        {
            alerts.Add(CreateAlert(
                "Overdue payables",
                $"{apOverdueEntries} payable entries are more than 30 days old.",
                apOverdueEntries >= 10 ? "high" : "medium",
                apOverdueEntries,
                financeApHref));
        }

        var sections = new List<DashboardSectionDto>();
        if (canViewProcurementSummary)
        {
            sections.Add(new DashboardSectionDto(
                "procurement",
                "Procurement Pipeline",
                "Monitor sourcing queues from internal demand through invoice posting.",
                [
                    CreateCountMetric("Draft requisitions", draftPurchaseRequisitions, "Requests still being prepared for approval.", "/procurement/purchase-requisitions"),
                    CreateCountMetric("Submitted requisitions", submittedPurchaseRequisitions, "Requests waiting for approval or sourcing action.", "/procurement/purchase-requisitions"),
                    CreateCountMetric("Sent RFQs", sentRfqs, "Supplier quote requests still awaiting closure.", "/procurement/rfqs"),
                    CreateCountMetric("POs awaiting receipt", activePurchaseOrders, "Approved or partially received orders that still need goods receipt.", "/procurement/purchase-orders"),
                    CreateCountMetric("Draft direct purchases", draftDirectPurchases, "Spot-buy documents not yet posted into stock.", "/procurement/direct-purchases"),
                    CreateCountMetric("Draft supplier invoices", draftSupplierInvoices, "Supplier bills not yet posted to accounts payable.", "/procurement/supplier-invoices")
                ]));
        }

        var salesMetrics = new List<DashboardMetricDto>();
        if (canSalesQuotesOrdersArea)
        {
            salesMetrics.Add(CreateCountMetric("Draft quotes", draftSalesQuotes, "Commercial proposals still being prepared.", "/sales/quotes"));
            salesMetrics.Add(CreateCountMetric("Sent quotes", sentSalesQuotes, "Customer quotes awaiting decision.", "/sales/quotes"));
            salesMetrics.Add(CreateCountMetric("Accepted quotes", acceptedSalesQuotes, "Won quotes that should convert into orders.", "/sales/quotes"));
            salesMetrics.Add(CreateCountMetric("Confirmed sales orders", confirmedSalesOrders, "Orders ready for dispatch or completion.", "/sales/orders"));
        }

        if (canSalesDispatchArea)
        {
            salesMetrics.Add(CreateCountMetric("Draft dispatches", draftDispatches, "Warehouse dispatch notes not yet posted.", "/sales/dispatches"));
        }

        if (canDirectDispatchArea)
        {
            salesMetrics.Add(CreateCountMetric("Draft direct dispatches", draftDirectDispatches, "Immediate dispatch documents still open.", "/sales/direct-dispatches"));
        }

        if (canSalesInvoicesArea)
        {
            salesMetrics.Add(CreateCountMetric("Draft sales invoices", draftSalesInvoices, "Invoices prepared but not yet posted to receivables.", "/sales/invoices"));
        }

        if (salesMetrics.Count > 0)
        {
            sections.Add(new DashboardSectionDto(
                "sales",
                "Sales Pipeline",
                "Keep commercial, fulfilment, and billing queues moving without stale drafts.",
                salesMetrics));
        }

        var serviceMetrics = new List<DashboardMetricDto>();
        if (canViewServiceSummary)
        {
            serviceMetrics.Add(CreateCountMetric("Open jobs", openServiceJobs, "Active service jobs still in the workshop queue.", serviceSummaryHref));
            serviceMetrics.Add(CreateCountMetric("Jobs in progress", inProgressServiceJobs, "Jobs currently being worked on.", serviceSummaryHref));
            serviceMetrics.Add(CreateCountMetric("Completed awaiting closure", completedServiceJobs, "Jobs finished operationally but not yet administratively closed.", serviceSummaryHref));
            serviceMetrics.Add(CreateCountMetric("Work orders in progress", workOrdersInProgress, "Technician work orders actively underway.", canServiceCoreArea ? "/service/work-orders" : serviceSummaryHref));
        }

        if (canServiceEstimatesArea || canAnalytics)
        {
            serviceMetrics.Add(CreateCountMetric("Draft estimates", draftServiceEstimates, "Estimates still being prepared before customer release.", serviceEstimateHref));
            serviceMetrics.Add(CreateCountMetric("Pending customer approvals", pendingEstimateCustomerApprovals, "Estimates sent to customers and awaiting decision.", serviceEstimateHref));
        }

        if (canMaterialRequisitionsArea || canAnalytics)
        {
            serviceMetrics.Add(CreateCountMetric("Draft material requisitions", draftMaterialRequisitions, "Parts requests not yet posted for issue.", materialRequisitionHref));
        }

        if (canExpenseClaimsArea || canAnalytics)
        {
            serviceMetrics.Add(CreateCountMetric("Submitted expense claims", submittedExpenseClaims, "Claims awaiting review or approval.", expenseClaimHref));
            serviceMetrics.Add(CreateCountMetric("Approved unsettled claims", approvedExpenseClaims, "Approved claims still waiting for settlement.", expenseClaimHref));
        }

        if (serviceMetrics.Count > 0)
        {
            sections.Add(new DashboardSectionDto(
                "service",
                "Service Operations",
                "Surface workshop bottlenecks, customer approvals, and service-side finance queues.",
                serviceMetrics));
        }

        if (canViewInventorySummary)
        {
            sections.Add(new DashboardSectionDto(
                "inventory",
                "Inventory Control",
                "Watch replenishment pressure and draft warehouse transactions before they stall operations.",
                [
                    CreateCountMetric("Reorder alerts", reorderAlerts, "Items already below their reorder point.", reorderHref),
                    CreateCountMetric("Draft stock adjustments", draftStockAdjustments, "Adjustment documents waiting to be posted.", inventoryAdjustmentHref),
                    CreateCountMetric("Draft stock transfers", draftStockTransfers, "Transfer documents waiting for warehouse movement.", inventoryTransferHref)
                ]));
        }

        if (canViewFinanceSummary)
        {
            sections.Add(new DashboardSectionDto(
                "finance",
                "Finance Control",
                "Track open balances, overdue collections, and supplier obligations from one view.",
                [
                    CreateCurrencyMetric("AR outstanding", arOutstanding, "Total customer balance still open.", financeArHref),
                    CreateCountMetric("Customers with balances", arCustomersWithBalances, "Distinct customers currently owing money.", financeArHref),
                    CreateCountMetric("Overdue AR > 30 days", arOverdueEntries, "Receivable entries older than 30 days.", financeArHref),
                    CreateCurrencyMetric("AP outstanding", apOutstanding, "Total supplier balance still unpaid.", financeApHref),
                    CreateCountMetric("Suppliers awaiting payment", apSuppliersAwaitingPayment, "Distinct suppliers with open payables.", financeApHref),
                    CreateCountMetric("Overdue AP > 30 days", apOverdueEntries, "Payable entries older than 30 days.", financeApHref)
                ]));
        }

        var quickActions = new List<DashboardQuickActionDto>();
        AddQuickAction(quickActions, canProcurementArea, "Purchase requisitions", "Open internal demand and approval queues.", "/procurement/purchase-requisitions");
        AddQuickAction(quickActions, canProcurementArea, "Purchase orders", "Move approved buying documents through receiving.", "/procurement/purchase-orders");
        AddQuickAction(quickActions, canProcurementArea, "Supplier invoices", "Post supplier bills into accounts payable.", "/procurement/supplier-invoices");
        AddQuickAction(quickActions, canSalesQuotesOrdersArea, "Sales quotes", "Manage live quotations and accepted opportunities.", "/sales/quotes");
        AddQuickAction(quickActions, canSalesQuotesOrdersArea, "Sales orders", "Follow confirmed customer orders into fulfilment.", "/sales/orders");
        AddQuickAction(quickActions, canSalesDispatchArea, "Dispatches", "Review warehouse dispatch documents before posting.", "/sales/dispatches");
        AddQuickAction(quickActions, canDirectDispatchArea, "Direct dispatches", "Handle immediate stock issues tied to sales or service.", "/sales/direct-dispatches");
        AddQuickAction(quickActions, canSalesInvoicesArea, "Sales invoices", "Post customer invoices and hand off to finance.", "/sales/invoices");
        AddQuickAction(quickActions, canInventoryArea, "Reorder alerts", "Review stock that needs replenishment.", "/inventory/reorder-alerts");
        AddQuickAction(quickActions, canInventoryArea, "Stock transfers", "Balance stock between warehouses.", "/inventory/stock-transfers");
        AddQuickAction(quickActions, canInventoryArea, "On hand", "Inspect item balances by warehouse.", "/inventory/onhand");
        AddQuickAction(quickActions, canServiceCoreArea, "Service jobs", "Run the workshop queue and customer jobs.", "/service/jobs");
        AddQuickAction(quickActions, canServiceCoreArea, "Work orders", "Manage technician execution and progress.", "/service/work-orders");
        AddQuickAction(quickActions, canServiceEstimatesArea, "Service estimates", "Keep customer approvals moving.", "/service/estimates");
        AddQuickAction(quickActions, canMaterialRequisitionsArea, "Material requisitions", "Issue parts from stock to service work.", "/service/material-requisitions");
        AddQuickAction(quickActions, canExpenseClaimsArea, "Expense claims", "Approve and settle field or workshop expenses.", "/service/expense-claims");
        AddQuickAction(quickActions, canFinanceArea, "Accounts receivable", "Chase collections and review customer balances.", "/finance/ar");
        AddQuickAction(quickActions, canFinanceArea, "Accounts payable", "Manage supplier liabilities and payment timing.", "/finance/ap");
        AddQuickAction(quickActions, canFinanceArea, "Payments", "Post settlements against open balances.", "/finance/payments");
        AddQuickAction(quickActions, canAnalytics, "Stock ledger", "Review movement history and running balances.", "/reporting/stock-ledger");
        AddQuickAction(quickActions, canAnalytics, "AR/AP aging", "Analyze overdue balances by aging bucket.", "/reporting/aging");
        AddQuickAction(quickActions, canAnalytics, "Service KPIs", "Track throughput and completion performance.", "/reporting/service-kpis");
        AddQuickAction(quickActions, canAnalytics, "Costing", "Audit weighted average cost and inventory value.", "/reporting/costing");

        return Ok(new DashboardDto(
            canViewServiceSummary ? openServiceJobs : 0,
            canViewFinanceSummary ? arOutstanding : 0m,
            canViewFinanceSummary ? apOutstanding : 0m,
            canViewInventorySummary ? reorderAlerts : 0,
            generatedAt,
            heroMetrics,
            alerts,
            sections,
            quickActions));
    }

    [HttpGet("stock-ledger")]
    [Authorize(Roles = Roles.AdminOrReporting)]
    public async Task<ActionResult<StockLedgerReportDto>> StockLedger(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? itemId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 1000);

        var movements = dbContext.InventoryMovements.AsNoTracking();
        if (warehouseId is not null)
        {
            movements = movements.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (itemId is not null)
        {
            movements = movements.Where(x => x.ItemId == itemId.Value);
        }

        if (from is not null)
        {
            movements = movements.Where(x => x.OccurredAt >= from.Value);
        }

        if (to is not null)
        {
            movements = movements.Where(x => x.OccurredAt <= to.Value);
        }

        var rawRows = await (
            from m in movements
            join w in dbContext.Warehouses.AsNoTracking() on m.WarehouseId equals w.Id
            join i in dbContext.Items.AsNoTracking() on m.ItemId equals i.Id
            orderby m.OccurredAt, m.CreatedAt
            select new StockLedgerRawRow(
                m.OccurredAt,
                m.CreatedAt,
                m.Type,
                m.WarehouseId,
                w.Code,
                w.Name,
                m.ItemId,
                i.Sku,
                i.Name,
                m.Quantity,
                m.UnitCost,
                m.ReferenceType,
                m.ReferenceId,
                m.BatchNumber,
                m.SerialNumber))
            .Take(take)
            .ToListAsync(cancellationToken);

        var runningByPair = new Dictionary<(Guid WarehouseId, Guid ItemId), decimal>();
        var rows = new List<StockLedgerRowDto>(rawRows.Count);
        foreach (var row in rawRows)
        {
            var key = (row.WarehouseId, row.ItemId);
            runningByPair.TryGetValue(key, out var runningQty);
            runningQty += row.Quantity;
            runningByPair[key] = runningQty;

            rows.Add(new StockLedgerRowDto(
                row.OccurredAt,
                row.MovementType,
                row.WarehouseId,
                row.WarehouseCode,
                row.WarehouseName,
                row.ItemId,
                row.ItemSku,
                row.ItemName,
                row.Quantity,
                row.UnitCost,
                row.Quantity * row.UnitCost,
                runningQty,
                row.ReferenceType,
                row.ReferenceId,
                row.BatchNumber,
                row.SerialNumber));
        }

        return Ok(new StockLedgerReportDto(
            from,
            to,
            warehouseId,
            itemId,
            rows.Count,
            rows.Sum(x => x.Quantity),
            rows));
    }

    [HttpGet("aging")]
    [Authorize(Roles = Roles.AdminOrReporting)]
    public async Task<ActionResult<AgingReportDto>> Aging(
        [FromQuery] DateTimeOffset? asOf = null,
        CancellationToken cancellationToken = default)
    {
        var reportAsOf = asOf ?? DateTimeOffset.UtcNow;

        var arRows = await (
            from ar in dbContext.AccountsReceivableEntries.AsNoTracking()
            join c in dbContext.Customers.AsNoTracking() on ar.CustomerId equals c.Id
            where ar.Outstanding > 0m
            select new ArAgingRawRow(c.Id, c.Code, c.Name, ar.Outstanding, ar.PostedAt))
            .ToListAsync(cancellationToken);

        var apRows = await (
            from ap in dbContext.AccountsPayableEntries.AsNoTracking()
            join s in dbContext.Suppliers.AsNoTracking() on ap.SupplierId equals s.Id
            where ap.Outstanding > 0m
            select new ApAgingRawRow(s.Id, s.Code, s.Name, ap.Outstanding, ap.PostedAt))
            .ToListAsync(cancellationToken);

        var arGrouped = arRows
            .GroupBy(x => new { x.CustomerId, x.CustomerCode, x.CustomerName })
            .Select(g =>
            {
                var buckets = g.Aggregate(
                    new AgingBuckets(),
                    (acc, x) => acc + BucketizeAge(x.Outstanding, x.PostedAt, reportAsOf));
                return new ArAgingRowDto(g.Key.CustomerId, g.Key.CustomerCode, g.Key.CustomerName, buckets.ToDto());
            })
            .OrderBy(x => x.CustomerCode)
            .ToList();

        var apGrouped = apRows
            .GroupBy(x => new { x.SupplierId, x.SupplierCode, x.SupplierName })
            .Select(g =>
            {
                var buckets = g.Aggregate(
                    new AgingBuckets(),
                    (acc, x) => acc + BucketizeAge(x.Outstanding, x.PostedAt, reportAsOf));
                return new ApAgingRowDto(g.Key.SupplierId, g.Key.SupplierCode, g.Key.SupplierName, buckets.ToDto());
            })
            .OrderBy(x => x.SupplierCode)
            .ToList();

        var arTotals = arGrouped.Aggregate(new AgingBuckets(), (acc, x) => acc + FromDto(x.Buckets)).ToDto();
        var apTotals = apGrouped.Aggregate(new AgingBuckets(), (acc, x) => acc + FromDto(x.Buckets)).ToDto();

        return Ok(new AgingReportDto(reportAsOf, arGrouped, arTotals, apGrouped, apTotals));
    }

    [HttpGet("tax-summary")]
    [Authorize(Roles = Roles.AdminOrReporting)]
    public async Task<ActionResult<TaxSummaryReportDto>> TaxSummary(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var reportTo = to ?? DateTimeOffset.UtcNow;
        var reportFrom = from ?? reportTo.AddDays(-30);
        if (reportFrom > reportTo)
        {
            return BadRequest("'from' must be earlier than or equal to 'to'.");
        }

        var salesInvoiceQuery = dbContext.SalesInvoices.AsNoTracking()
            .Where(x => x.InvoiceDate >= reportFrom && x.InvoiceDate <= reportTo)
            .Where(x => x.Status == SalesInvoiceStatus.Posted || x.Status == SalesInvoiceStatus.Paid);

        var salesInvoiceCount = await salesInvoiceQuery.CountAsync(cancellationToken);

        var salesLines = await salesInvoiceQuery
            .SelectMany(x => x.Lines.Select(l => new
            {
                l.Quantity,
                l.UnitPrice,
                l.DiscountPercent,
                l.TaxPercent
            }))
            .ToListAsync(cancellationToken);

        var purchases = await dbContext.SupplierInvoices.AsNoTracking()
            .Where(x => x.InvoiceDate >= reportFrom && x.InvoiceDate <= reportTo)
            .Where(x => x.Status == SupplierInvoiceStatus.Posted)
            .Select(x => new { x.Subtotal, x.TaxAmount })
            .ToListAsync(cancellationToken);

        var salesTaxable = salesLines.Sum(l =>
        {
            var lineBase = l.Quantity * l.UnitPrice;
            var discounted = lineBase * (1m - (l.DiscountPercent / 100m));
            return discounted;
        });
        var salesTax = salesLines.Sum(l =>
        {
            var lineBase = l.Quantity * l.UnitPrice;
            var discounted = lineBase * (1m - (l.DiscountPercent / 100m));
            return discounted * (l.TaxPercent / 100m);
        });
        var purchaseTaxable = purchases.Sum(x => x.Subtotal);
        var purchaseTax = purchases.Sum(x => x.TaxAmount);

        return Ok(new TaxSummaryReportDto(
            reportFrom,
            reportTo,
            salesTaxable,
            salesTax,
            purchaseTaxable,
            purchaseTax,
            salesTax - purchaseTax,
            salesInvoiceCount,
            purchases.Count));
    }

    [HttpGet("service-kpis")]
    [Authorize(Roles = Roles.AdminOrReporting)]
    public async Task<ActionResult<ServiceKpiReportDto>> ServiceKpis(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var reportTo = to ?? DateTimeOffset.UtcNow;
        var reportFrom = from ?? reportTo.AddDays(-30);
        if (reportFrom > reportTo)
        {
            return BadRequest("'from' must be earlier than or equal to 'to'.");
        }

        var jobs = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => x.OpenedAt >= reportFrom && x.OpenedAt <= reportTo)
            .Select(x => new { x.Status, x.OpenedAt, x.CompletedAt })
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var completionHours = jobs
            .Where(x => x.CompletedAt is not null)
            .Select(x => (decimal)(x.CompletedAt!.Value - x.OpenedAt).TotalHours)
            .Where(x => x >= 0m)
            .ToList();

        var estimates = await dbContext.ServiceEstimates.AsNoTracking()
            .Where(x => x.IssuedAt >= reportFrom && x.IssuedAt <= reportTo)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);

        var handoversCompleted = await dbContext.ServiceHandovers.AsNoTracking()
            .CountAsync(x => x.HandoverDate >= reportFrom && x.HandoverDate <= reportTo && x.Status == ServiceHandoverStatus.Completed, cancellationToken);

        var materialReqsPosted = await dbContext.MaterialRequisitions.AsNoTracking()
            .CountAsync(x => x.RequestedAt >= reportFrom && x.RequestedAt <= reportTo && x.Status == MaterialRequisitionStatus.Posted, cancellationToken);

        var partsConsumedQty = (await dbContext.InventoryMovements.AsNoTracking()
            .Where(x => x.OccurredAt >= reportFrom && x.OccurredAt <= reportTo && x.Type == InventoryMovementType.Consumption)
            .Select(x => (decimal?)(x.Quantity >= 0m ? x.Quantity : -x.Quantity))
            .SumAsync(cancellationToken)) ?? 0m;

        return Ok(new ServiceKpiReportDto(
            reportFrom,
            reportTo,
            jobs.Count(x => x.Status == ServiceJobStatus.Open),
            jobs.Count(x => x.Status == ServiceJobStatus.InProgress),
            jobs.Count(x => x.Status == ServiceJobStatus.Completed),
            jobs.Count(x => x.Status == ServiceJobStatus.Closed),
            jobs.Count(x => x.Status == ServiceJobStatus.Cancelled),
            completionHours.Count == 0 ? null : Math.Round(completionHours.Average(), 2),
            jobs.Count(x => (x.Status is ServiceJobStatus.Open or ServiceJobStatus.InProgress) && (now - x.OpenedAt).TotalDays > 7),
            jobs.Count(x => (x.Status is ServiceJobStatus.Open or ServiceJobStatus.InProgress) && (now - x.OpenedAt).TotalDays > 30),
            estimates.Count,
            estimates.Count(x => x == ServiceEstimateStatus.Approved),
            handoversCompleted,
            materialReqsPosted,
            partsConsumedQty));
    }

    [HttpGet("costing")]
    [Authorize(Roles = Roles.AdminOrReporting)]
    public async Task<ActionResult<CostingReportDto>> Costing(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? itemId = null,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 2000);

        var baseCurrencyCode = await dbContext.Currencies.AsNoTracking()
            .Where(x => x.IsActive && x.IsBase)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken) ?? "USD";

        var itemsQuery = dbContext.Items.AsNoTracking();
        if (itemId is not null)
        {
            itemsQuery = itemsQuery.Where(x => x.Id == itemId.Value);
        }

        var items = await itemsQuery
            .OrderBy(x => x.Sku)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.Sku,
                x.Name,
                x.UnitOfMeasure,
                x.DefaultUnitCost
            })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return Ok(new CostingReportDto(
                warehouseId,
                itemId,
                baseCurrencyCode,
                0,
                0m,
                0m,
                []));
        }

        var itemIds = items.Select(x => x.Id).ToList();
        var movementsQuery = dbContext.InventoryMovements.AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId));

        if (warehouseId is not null)
        {
            movementsQuery = movementsQuery.Where(x => x.WarehouseId == warehouseId.Value);
        }

        var movements = await movementsQuery
            .Select(x => new
            {
                x.ItemId,
                x.Quantity,
                x.UnitCost,
                x.OccurredAt,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var movementsByItem = movements
            .GroupBy(x => x.ItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<CostingRowDto>(items.Count);
        foreach (var item in items)
        {
            if (!movementsByItem.TryGetValue(item.Id, out var itemMovements))
            {
                rows.Add(new CostingRowDto(
                    item.Id,
                    item.Sku,
                    item.Name,
                    item.UnitOfMeasure,
                    item.DefaultUnitCost,
                    WeightedAverageCost: null,
                    LastReceiptCost: null,
                    LastReceiptAt: null,
                    OnHandQuantity: 0m,
                    InventoryValue: 0m,
                    CostVariancePercent: null));
                continue;
            }

            var onHand = itemMovements.Sum(x => x.Quantity);
            var inbound = itemMovements.Where(x => x.Quantity > 0m).ToList();
            var inboundQty = inbound.Sum(x => x.Quantity);
            var inboundValue = inbound.Sum(x => x.Quantity * x.UnitCost);
            var weightedAverage = inboundQty > 0m ? inboundValue / inboundQty : (decimal?)null;

            var lastReceipt = inbound
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            var costingUnit = weightedAverage ?? item.DefaultUnitCost;
            var value = onHand * costingUnit;

            decimal? variancePercent = null;
            if (weightedAverage is not null && item.DefaultUnitCost > 0m)
            {
                variancePercent = ((weightedAverage.Value - item.DefaultUnitCost) / item.DefaultUnitCost) * 100m;
            }

            rows.Add(new CostingRowDto(
                item.Id,
                item.Sku,
                item.Name,
                item.UnitOfMeasure,
                item.DefaultUnitCost,
                weightedAverage,
                lastReceipt?.UnitCost,
                lastReceipt?.OccurredAt,
                onHand,
                value,
                variancePercent));
        }

        return Ok(new CostingReportDto(
            warehouseId,
            itemId,
            baseCurrencyCode,
            rows.Count,
            rows.Sum(x => x.OnHandQuantity),
            rows.Sum(x => x.InventoryValue),
            rows));
    }

    private bool HasAnyRole(params string[] roles)
        => roles.Any(role => User.IsInRole(role));

    private async Task<int> CountReorderAlertsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ReorderSettings.AsNoTracking().ToListAsync(cancellationToken);
        var alerts = 0;
        foreach (var setting in settings)
        {
            var onHand = await inventoryService.GetOnHandAsync(setting.WarehouseId, setting.ItemId, null, cancellationToken);
            if (onHand <= setting.ReorderPoint)
            {
                alerts++;
            }
        }

        return alerts;
    }

    private static int GetCount<TStatus>(IReadOnlyDictionary<TStatus, int> counts, TStatus status)
        where TStatus : notnull
        => counts.TryGetValue(status, out var count) ? count : 0;

    private static DashboardMetricDto CreateCountMetric(string label, int count, string description, string? href)
        => new(label, "count", count, null, description, href);

    private static DashboardMetricDto CreateCurrencyMetric(string label, decimal amount, string description, string? href)
        => new(label, "currency", null, amount, description, href);

    private static DashboardAlertDto CreateAlert(string title, string description, string severity, int count, string? href)
        => new(title, description, severity, count, href);

    private static void AddQuickAction(
        ICollection<DashboardQuickActionDto> quickActions,
        bool include,
        string label,
        string description,
        string href)
    {
        if (!include || quickActions.Any(x => x.Href == href))
        {
            return;
        }

        quickActions.Add(new DashboardQuickActionDto(label, description, href));
    }

    private static AgingBuckets BucketizeAge(decimal amount, DateTimeOffset postedAt, DateTimeOffset asOf)
    {
        var ageDays = (int)Math.Floor((asOf.Date - postedAt.Date).TotalDays);
        if (ageDays <= 0)
        {
            return new AgingBuckets(Current: amount, 0m, 0m, 0m, 0m);
        }

        if (ageDays <= 30)
        {
            return new AgingBuckets(0m, amount, 0m, 0m, 0m);
        }

        if (ageDays <= 60)
        {
            return new AgingBuckets(0m, 0m, amount, 0m, 0m);
        }

        if (ageDays <= 90)
        {
            return new AgingBuckets(0m, 0m, 0m, amount, 0m);
        }

        return new AgingBuckets(0m, 0m, 0m, 0m, amount);
    }

    private static AgingBuckets FromDto(AgingBucketsDto dto)
        => new(dto.Current, dto.Days1To30, dto.Days31To60, dto.Days61To90, dto.DaysOver90);
}
