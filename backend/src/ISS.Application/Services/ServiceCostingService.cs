using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Inventory;
using ISS.Domain.Procurement;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class ServiceCostingService(IIssDbContext dbContext)
{
    public sealed record EstimateSnapshot(
        Guid Id,
        string Number,
        int RevisionNumber,
        ServiceEstimateStatus Status,
        DateTimeOffset IssuedAt,
        decimal Total);

    public sealed record InvoiceSnapshot(
        Guid Id,
        string Number,
        SalesInvoiceStatus Status,
        DateTimeOffset InvoiceDate,
        decimal Total);

    public sealed record MaterialCostLine(
        DateTimeOffset OccurredAt,
        Guid MaterialRequisitionId,
        string MaterialRequisitionNumber,
        Guid WarehouseId,
        string WarehouseCode,
        Guid ItemId,
        string ItemSku,
        string ItemName,
        decimal Quantity,
        decimal UnitCost,
        decimal LineTotal);

    public sealed record DirectPurchaseCostLine(
        DateTimeOffset PurchasedAt,
        Guid DirectPurchaseId,
        string DirectPurchaseNumber,
        Guid SupplierId,
        string SupplierCode,
        Guid ItemId,
        string ItemSku,
        string ItemName,
        decimal Quantity,
        decimal UnitPrice,
        decimal TaxPercent,
        decimal LineTotal);

    public sealed record ExpenseClaimCostLine(
        DateTimeOffset ExpenseDate,
        Guid ExpenseClaimId,
        string ExpenseClaimNumber,
        ServiceExpenseFundingSource FundingSource,
        ServiceExpenseClaimStatus Status,
        Guid? ItemId,
        string? ItemSku,
        string? ItemName,
        string Description,
        decimal Quantity,
        decimal UnitCost,
        bool BillableToCustomer,
        Guid? ConvertedToServiceEstimateId,
        Guid? ConvertedToServiceEstimateLineId,
        decimal LineTotal);

    public sealed record ServiceJobCostingDto(
        Guid ServiceJobId,
        string JobNumber,
        decimal? LatestApprovedEstimateTotal,
        decimal? LatestDraftEstimateTotal,
        decimal DraftInvoiceTotal,
        decimal PostedInvoiceTotal,
        decimal MaterialConsumedCost,
        decimal DirectPurchaseCost,
        decimal ApprovedExpenseClaimCost,
        decimal PendingExpenseClaimCost,
        decimal BillableExpenseClaimCost,
        decimal UnconvertedBillableExpenseClaimCost,
        decimal TotalActualCost,
        decimal? QuotedGrossMargin,
        decimal PostedGrossMargin,
        IReadOnlyList<EstimateSnapshot> Estimates,
        IReadOnlyList<InvoiceSnapshot> Invoices,
        IReadOnlyList<MaterialCostLine> MaterialLines,
        IReadOnlyList<DirectPurchaseCostLine> DirectPurchaseLines,
        IReadOnlyList<ExpenseClaimCostLine> ExpenseClaimLines);

    public async Task<ServiceJobCostingDto> GetServiceJobCostingAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        var estimates = await dbContext.ServiceEstimates.AsNoTracking()
            .Where(x => x.ServiceJobId == serviceJobId)
            .OrderByDescending(x => x.RevisionNumber)
            .ThenByDescending(x => x.IssuedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new EstimateSnapshot(
                x.Id,
                x.Number,
                x.RevisionNumber,
                x.Status,
                x.IssuedAt,
                x.Lines.Sum(l => (l.Quantity * l.UnitPrice) + ((l.Quantity * l.UnitPrice) * (l.TaxPercent / 100m)))))
            .ToListAsync(cancellationToken);

        var invoiceIds = await dbContext.ServiceHandovers.AsNoTracking()
            .Where(x => x.ServiceJobId == serviceJobId && x.SalesInvoiceId != null)
            .Select(x => x.SalesInvoiceId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var invoices = invoiceIds.Count == 0
            ? new List<InvoiceSnapshot>()
            : await dbContext.SalesInvoices.AsNoTracking()
                .Where(x => invoiceIds.Contains(x.Id) && x.Status != SalesInvoiceStatus.Voided)
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new InvoiceSnapshot(x.Id, x.Number, x.Status, x.InvoiceDate, x.Total))
                .ToListAsync(cancellationToken);

        var materialLines = await (
            from movement in dbContext.InventoryMovements.AsNoTracking()
            join materialRequisition in dbContext.MaterialRequisitions.AsNoTracking() on movement.ReferenceId equals materialRequisition.Id
            join warehouse in dbContext.Warehouses.AsNoTracking() on movement.WarehouseId equals warehouse.Id
            join item in dbContext.Items.AsNoTracking() on movement.ItemId equals item.Id
            where movement.ReferenceType == ReferenceTypes.MaterialRequisition
                  && movement.Type == InventoryMovementType.Consumption
                  && materialRequisition.ServiceJobId == serviceJobId
                  && materialRequisition.Status == MaterialRequisitionStatus.Posted
            orderby movement.OccurredAt descending, materialRequisition.Number descending
            select new MaterialCostLine(
                movement.OccurredAt,
                materialRequisition.Id,
                materialRequisition.Number,
                warehouse.Id,
                warehouse.Code,
                item.Id,
                item.Sku,
                item.Name,
                movement.Quantity >= 0m ? movement.Quantity : -movement.Quantity,
                movement.UnitCost,
                (movement.Quantity >= 0m ? movement.Quantity : -movement.Quantity) * movement.UnitCost))
            .ToListAsync(cancellationToken);

        var directPurchaseLines = await (
            from directPurchase in dbContext.DirectPurchases.AsNoTracking()
            from line in directPurchase.Lines
            join supplier in dbContext.Suppliers.AsNoTracking() on directPurchase.SupplierId equals supplier.Id
            join item in dbContext.Items.AsNoTracking() on line.ItemId equals item.Id
            where directPurchase.ServiceJobId == serviceJobId
                  && directPurchase.Status == DirectPurchaseStatus.Posted
            orderby directPurchase.PurchasedAt descending, directPurchase.Number descending
            select new DirectPurchaseCostLine(
                directPurchase.PurchasedAt,
                directPurchase.Id,
                directPurchase.Number,
                supplier.Id,
                supplier.Code,
                item.Id,
                item.Sku,
                item.Name,
                line.Quantity,
                line.UnitPrice,
                line.TaxPercent,
                (line.Quantity * line.UnitPrice) + ((line.Quantity * line.UnitPrice) * (line.TaxPercent / 100m))))
            .ToListAsync(cancellationToken);

        var expenseClaimLines = await (
            from claim in dbContext.ServiceExpenseClaims.AsNoTracking()
            from line in claim.Lines
            join item in dbContext.Items.AsNoTracking() on line.ItemId equals item.Id into itemJoin
            from item in itemJoin.DefaultIfEmpty()
            where claim.ServiceJobId == serviceJobId
            orderby claim.ExpenseDate descending, claim.Number descending
            select new ExpenseClaimCostLine(
                claim.ExpenseDate,
                claim.Id,
                claim.Number,
                claim.FundingSource,
                claim.Status,
                line.ItemId,
                item != null ? item.Sku : null,
                item != null ? item.Name : null,
                line.Description,
                line.Quantity,
                line.UnitCost,
                line.BillableToCustomer,
                line.ConvertedToServiceEstimateId,
                line.ConvertedToServiceEstimateLineId,
                line.Quantity * line.UnitCost))
            .ToListAsync(cancellationToken);

        var latestApprovedEstimateTotal = estimates
            .Where(x => x.Status == ServiceEstimateStatus.Approved)
            .OrderByDescending(x => x.RevisionNumber)
            .ThenByDescending(x => x.IssuedAt)
            .Select(x => (decimal?)x.Total)
            .FirstOrDefault();

        var latestDraftEstimateTotal = estimates
            .Where(x => x.Status == ServiceEstimateStatus.Draft)
            .OrderByDescending(x => x.RevisionNumber)
            .ThenByDescending(x => x.IssuedAt)
            .Select(x => (decimal?)x.Total)
            .FirstOrDefault();

        var draftInvoiceTotal = invoices
            .Where(x => x.Status == SalesInvoiceStatus.Draft)
            .Sum(x => x.Total);

        var postedInvoiceTotal = invoices
            .Where(x => x.Status is SalesInvoiceStatus.Posted or SalesInvoiceStatus.Paid)
            .Sum(x => x.Total);

        var materialConsumedCost = materialLines.Sum(x => x.LineTotal);
        var directPurchaseCost = directPurchaseLines.Sum(x => x.LineTotal);
        var approvedExpenseClaimCost = expenseClaimLines
            .Where(x => x.Status is ServiceExpenseClaimStatus.Approved or ServiceExpenseClaimStatus.Settled)
            .Sum(x => x.LineTotal);
        var pendingExpenseClaimCost = expenseClaimLines
            .Where(x => x.Status == ServiceExpenseClaimStatus.Submitted)
            .Sum(x => x.LineTotal);
        var billableExpenseClaimCost = expenseClaimLines
            .Where(x => x.BillableToCustomer
                        && (x.Status is ServiceExpenseClaimStatus.Approved or ServiceExpenseClaimStatus.Settled))
            .Sum(x => x.LineTotal);
        var unconvertedBillableExpenseClaimCost = expenseClaimLines
            .Where(x => x.BillableToCustomer
                        && x.ConvertedToServiceEstimateLineId is null
                        && (x.Status is ServiceExpenseClaimStatus.Approved or ServiceExpenseClaimStatus.Settled))
            .Sum(x => x.LineTotal);

        var totalActualCost = materialConsumedCost + directPurchaseCost + approvedExpenseClaimCost;
        var quotedRevenue = latestApprovedEstimateTotal ?? latestDraftEstimateTotal;
        decimal? quotedGrossMargin = quotedRevenue is null ? null : quotedRevenue.Value - totalActualCost;
        var postedGrossMargin = postedInvoiceTotal - totalActualCost;

        return new ServiceJobCostingDto(
            serviceJobId,
            job.Number,
            latestApprovedEstimateTotal,
            latestDraftEstimateTotal,
            draftInvoiceTotal,
            postedInvoiceTotal,
            materialConsumedCost,
            directPurchaseCost,
            approvedExpenseClaimCost,
            pendingExpenseClaimCost,
            billableExpenseClaimCost,
            unconvertedBillableExpenseClaimCost,
            totalActualCost,
            quotedGrossMargin,
            postedGrossMargin,
            estimates,
            invoices,
            materialLines,
            directPurchaseLines,
            expenseClaimLines);
    }
}
