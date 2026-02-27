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
[Authorize(Roles = $"{Roles.Admin},{Roles.Reporting}")]
public sealed class ReportingController(IIssDbContext dbContext, InventoryService inventoryService) : ControllerBase
{
    public sealed record DashboardDto(int OpenServiceJobs, decimal ArOutstanding, decimal ApOutstanding, int ReorderAlerts);
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
        var openServiceJobs = await dbContext.ServiceJobs.AsNoTracking()
            .CountAsync(x => x.Status == ServiceJobStatus.Open || x.Status == ServiceJobStatus.InProgress, cancellationToken);

        var arOutstanding = await dbContext.AccountsReceivableEntries.AsNoTracking()
            .SumAsync(x => x.Outstanding, cancellationToken);

        var apOutstanding = await dbContext.AccountsPayableEntries.AsNoTracking()
            .SumAsync(x => x.Outstanding, cancellationToken);

        var settings = await dbContext.ReorderSettings.AsNoTracking().ToListAsync(cancellationToken);
        var alerts = 0;
        foreach (var s in settings)
        {
            var onHand = await inventoryService.GetOnHandAsync(s.WarehouseId, s.ItemId, null, cancellationToken);
            if (onHand <= s.ReorderPoint)
            {
                alerts++;
            }
        }

        return Ok(new DashboardDto(openServiceJobs, arOutstanding, apOutstanding, alerts));
    }

    [HttpGet("stock-ledger")]
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
