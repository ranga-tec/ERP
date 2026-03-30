using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Finance;
using ISS.Domain.MasterData;
using ISS.Domain.Procurement;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class ProcurementService(
    IIssDbContext dbContext,
    IDocumentNumberService documentNumberService,
    IClock clock,
    InventoryService inventoryService,
    NotificationService notificationService)
{
    public sealed record GoodsReceiptReceiptPlanLineInput(
        Guid PurchaseOrderLineId,
        decimal Quantity,
        decimal UnitCost,
        string? BatchNumber,
        IReadOnlyCollection<string>? SerialNumbers);

    public async Task<Guid> CreateRfqAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("RFQ", "RFQ", cancellationToken);
        var rfq = new RequestForQuote(number, supplierId, clock.UtcNow);
        await dbContext.RequestForQuotes.AddAsync(rfq, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return rfq.Id;
    }

    public async Task AddRfqLineAsync(Guid rfqId, Guid itemId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
    {
        var rfq = await dbContext.RequestForQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == rfqId, cancellationToken)
                  ?? throw new NotFoundException("RFQ not found.");

        var line = rfq.AddLine(itemId, quantity, notes);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRfqLineAsync(Guid rfqId, Guid lineId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
    {
        var rfq = await dbContext.RequestForQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == rfqId, cancellationToken)
                  ?? throw new NotFoundException("RFQ not found.");

        if (!rfq.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("RFQ line not found.");
        }

        rfq.UpdateLine(lineId, quantity, notes);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRfqLineAsync(Guid rfqId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var rfq = await dbContext.RequestForQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == rfqId, cancellationToken)
                  ?? throw new NotFoundException("RFQ not found.");

        var line = rfq.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("RFQ line not found.");

        rfq.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkRfqSentAsync(Guid rfqId, CancellationToken cancellationToken = default)
    {
        var rfq = await dbContext.RequestForQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == rfqId, cancellationToken)
                  ?? throw new NotFoundException("RFQ not found.");

        rfq.MarkSent();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreatePurchaseRequisitionAsync(string? notes, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("PR", "PR", cancellationToken);
        var pr = new PurchaseRequisition(number, clock.UtcNow, notes);
        await dbContext.PurchaseRequisitions.AddAsync(pr, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return pr.Id;
    }

    public async Task AddPurchaseRequisitionLineAsync(Guid purchaseRequisitionId, Guid itemId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Purchase requisition not found.");

        var line = pr.AddLine(itemId, quantity, notes);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePurchaseRequisitionLineAsync(
        Guid purchaseRequisitionId,
        Guid lineId,
        decimal quantity,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Purchase requisition not found.");

        if (!pr.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Purchase requisition line not found.");
        }

        pr.UpdateLine(lineId, quantity, notes);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePurchaseRequisitionLineAsync(
        Guid purchaseRequisitionId,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Purchase requisition not found.");

        var line = pr.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Purchase requisition line not found.");

        pr.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitPurchaseRequisitionAsync(Guid purchaseRequisitionId, CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Purchase requisition not found.");

        pr.Submit();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApprovePurchaseRequisitionAsync(Guid purchaseRequisitionId, CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseRequisitions.FirstOrDefaultAsync(x => x.Id == purchaseRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Purchase requisition not found.");

        pr.Approve();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectPurchaseRequisitionAsync(Guid purchaseRequisitionId, CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseRequisitions.FirstOrDefaultAsync(x => x.Id == purchaseRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Purchase requisition not found.");

        pr.Reject();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelPurchaseRequisitionAsync(Guid purchaseRequisitionId, CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseRequisitions.FirstOrDefaultAsync(x => x.Id == purchaseRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Purchase requisition not found.");

        pr.Cancel();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreatePurchaseOrderFromPurchaseRequisitionAsync(
        Guid purchaseRequisitionId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseRequisitions
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == purchaseRequisitionId, cancellationToken)
            ?? throw new NotFoundException("Purchase requisition not found.");

        if (pr.Status != PurchaseRequisitionStatus.Approved)
        {
            throw new DomainValidationException("Only approved purchase requisitions can be converted to PO.");
        }

        if (pr.Lines.Count == 0)
        {
            throw new DomainValidationException("Purchase requisition must have at least one line.");
        }

        var supplierExists = await dbContext.Suppliers.AsNoTracking().AnyAsync(x => x.Id == supplierId, cancellationToken);
        if (!supplierExists)
        {
            throw new NotFoundException("Supplier not found.");
        }

        var lineItemIds = pr.Lines.Select(x => x.ItemId).Distinct().ToList();
        var itemCosts = await dbContext.Items.AsNoTracking()
            .Where(x => lineItemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.DefaultUnitCost })
            .ToListAsync(cancellationToken);
        var itemCostById = itemCosts.ToDictionary(x => x.Id, x => x.DefaultUnitCost);

        var missingItemId = lineItemIds.FirstOrDefault(x => !itemCostById.ContainsKey(x));
        if (missingItemId != Guid.Empty)
        {
            throw new DomainValidationException($"Invalid item on purchase requisition: {missingItemId}");
        }

        var number = await documentNumberService.NextAsync("PO", "PO", cancellationToken);
        var po = new PurchaseOrder(number, supplierId, clock.UtcNow);
        await dbContext.PurchaseOrders.AddAsync(po, cancellationToken);

        foreach (var line in pr.Lines)
        {
            var poLine = po.AddLine(line.ItemId, line.Quantity, itemCostById[line.ItemId]);
            dbContext.DbContext.Add(poLine);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return po.Id;
    }

    public async Task<Guid> CreatePurchaseOrderAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("PO", "PO", cancellationToken);
        var po = new PurchaseOrder(number, supplierId, clock.UtcNow);
        await dbContext.PurchaseOrders.AddAsync(po, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return po.Id;
    }

    public async Task AddPurchaseOrderLineAsync(Guid purchaseOrderId, Guid itemId, decimal quantity, decimal unitPrice, CancellationToken cancellationToken = default)
    {
        var po = await dbContext.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseOrderId, cancellationToken)
                 ?? throw new NotFoundException("Purchase order not found.");

        var line = po.AddLine(itemId, quantity, unitPrice);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePurchaseOrderLineAsync(
        Guid purchaseOrderId,
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        var po = await dbContext.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseOrderId, cancellationToken)
                 ?? throw new NotFoundException("Purchase order not found.");

        if (!po.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Purchase order line not found.");
        }

        po.UpdateLine(lineId, quantity, unitPrice);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePurchaseOrderLineAsync(Guid purchaseOrderId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var po = await dbContext.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseOrderId, cancellationToken)
                 ?? throw new NotFoundException("Purchase order not found.");

        var line = po.Lines.FirstOrDefault(x => x.Id == lineId)
                   ?? throw new NotFoundException("Purchase order line not found.");

        po.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApprovePurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var po = await dbContext.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseOrderId, cancellationToken)
                 ?? throw new NotFoundException("Purchase order not found.");

        po.Approve();

        if (notificationService.Enabled)
        {
            var supplier = await dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == po.SupplierId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(supplier?.Email))
            {
                notificationService.EnqueueEmail(
                    supplier.Email!,
                    subject: $"Purchase order approved: {po.Number}",
                    body: $"Your purchase order {po.Number} has been approved.",
                    referenceType: "PO",
                    referenceId: po.Id);
            }

            if (!string.IsNullOrWhiteSpace(supplier?.Phone))
            {
                notificationService.EnqueueSms(
                    supplier.Phone!,
                    body: $"PO {po.Number} approved.",
                    referenceType: "PO",
                    referenceId: po.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateGoodsReceiptAsync(Guid purchaseOrderId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var po = await dbContext.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == purchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        EnsurePurchaseOrderReceivable(po);

        if (po.Lines.All(x => x.ReceivedQuantity >= x.OrderedQuantity))
        {
            throw new DomainValidationException("Purchase order has no remaining quantity to receive.");
        }

        var number = await documentNumberService.NextAsync("GRN", "GRN", cancellationToken);
        var grn = new GoodsReceipt(number, purchaseOrderId, warehouseId, clock.UtcNow);
        await dbContext.GoodsReceipts.AddAsync(grn, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return grn.Id;
    }

    public async Task<Guid> CreateDirectPurchaseAsync(
        Guid supplierId,
        Guid warehouseId,
        DateTimeOffset? purchasedAt,
        string? remarks,
        Guid? serviceJobId = null,
        CancellationToken cancellationToken = default)
    {
        if (serviceJobId is not null)
        {
            var jobExists = await dbContext.ServiceJobs.AsNoTracking()
                .AnyAsync(x => x.Id == serviceJobId.Value, cancellationToken);
            if (!jobExists)
            {
                throw new NotFoundException("Service job not found.");
            }
        }

        var number = await documentNumberService.NextAsync(ReferenceTypes.DirectPurchase, "DP", cancellationToken);
        var dp = new DirectPurchase(number, supplierId, warehouseId, purchasedAt ?? clock.UtcNow, remarks, serviceJobId);
        await dbContext.DirectPurchases.AddAsync(dp, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return dp.Id;
    }

    public async Task AddDirectPurchaseLineAsync(
        Guid directPurchaseId,
        Guid itemId,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var dp = await dbContext.DirectPurchases.Include(x => x.Lines).ThenInclude(x => x.Serials)
                     .FirstOrDefaultAsync(x => x.Id == directPurchaseId, cancellationToken)
                 ?? throw new NotFoundException("Direct purchase not found.");

        var line = dp.AddLine(itemId, quantity, unitPrice, taxPercent, batchNumber);
        if (serialNumbers is { Count: > 0 })
        {
            foreach (var serial in serialNumbers)
            {
                line.AddSerial(serial);
            }
        }

        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDirectPurchaseLineAsync(
        Guid directPurchaseId,
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var dp = await dbContext.DirectPurchases.Include(x => x.Lines).ThenInclude(x => x.Serials)
                     .FirstOrDefaultAsync(x => x.Id == directPurchaseId, cancellationToken)
                 ?? throw new NotFoundException("Direct purchase not found.");

        if (!dp.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Direct purchase line not found.");
        }

        dp.UpdateLine(lineId, quantity, unitPrice, taxPercent, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDirectPurchaseLineAsync(Guid directPurchaseId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var dp = await dbContext.DirectPurchases.Include(x => x.Lines).ThenInclude(x => x.Serials)
                     .FirstOrDefaultAsync(x => x.Id == directPurchaseId, cancellationToken)
                 ?? throw new NotFoundException("Direct purchase not found.");

        var line = dp.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Direct purchase line not found.");

        dp.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostDirectPurchaseAsync(Guid directPurchaseId, CancellationToken cancellationToken = default)
    {
        var dp = await dbContext.DirectPurchases.Include(x => x.Lines).ThenInclude(x => x.Serials)
                     .FirstOrDefaultAsync(x => x.Id == directPurchaseId, cancellationToken)
                 ?? throw new NotFoundException("Direct purchase not found.");

        var itemIds = dp.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        dp.Post();

        foreach (var line in dp.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on direct purchase.");
            }

            await inventoryService.RecordReceiptAsync(
                dp.PurchasedAt,
                dp.WarehouseId,
                item,
                line.Quantity,
                line.UnitPrice,
                ReferenceTypes.DirectPurchase,
                dp.Id,
                line.Id,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList(),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddGoodsReceiptLineAsync(
        Guid goodsReceiptId,
        Guid itemId,
        Guid? purchaseOrderLineId,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var grn = await dbContext.GoodsReceipts.Include(x => x.Lines).ThenInclude(l => l.Serials)
                      .FirstOrDefaultAsync(x => x.Id == goodsReceiptId, cancellationToken)
                  ?? throw new NotFoundException("Goods receipt not found.");

        var po = await dbContext.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        EnsurePurchaseOrderReceivable(po);

        var reservedInOtherDrafts = await GetGoodsReceiptDraftReservationsAsync(po.Id, grn.Id, cancellationToken);
        var resolvedPurchaseOrderLineId = ResolvePurchaseOrderLineId(
            po,
            grn,
            itemId,
            purchaseOrderLineId,
            quantity,
            reservedInOtherDrafts);
        var item = await dbContext.Items.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
            ?? throw new DomainValidationException("Invalid item on goods receipt.");

        ValidateGoodsReceiptTracking(item, quantity, batchNumber, serialNumbers);

        var line = grn.AddLine(itemId, quantity, unitCost, batchNumber, resolvedPurchaseOrderLineId);
        if (serialNumbers is { Count: > 0 })
        {
            foreach (var serial in serialNumbers)
            {
                line.AddSerial(serial);
            }
        }

        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateGoodsReceiptLineAsync(
        Guid goodsReceiptId,
        Guid lineId,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var grn = await dbContext.GoodsReceipts.Include(x => x.Lines).ThenInclude(x => x.Serials)
                      .FirstOrDefaultAsync(x => x.Id == goodsReceiptId, cancellationToken)
                  ?? throw new NotFoundException("Goods receipt not found.");

        var line = grn.Lines.FirstOrDefault(x => x.Id == lineId)
                   ?? throw new NotFoundException("Goods receipt line not found.");

        var po = await dbContext.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        EnsurePurchaseOrderReceivable(po);

        var reservedInOtherDrafts = await GetGoodsReceiptDraftReservationsAsync(po.Id, grn.Id, cancellationToken);
        if (line.PurchaseOrderLineId is { } purchaseOrderLineId)
        {
            ValidateGoodsReceiptLineQuantity(
                po,
                grn,
                purchaseOrderLineId,
                quantity,
                reservedInOtherDrafts,
                line.Id);
        }

        var item = await dbContext.Items.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == line.ItemId, cancellationToken)
            ?? throw new DomainValidationException("Invalid item on goods receipt.");

        ValidateGoodsReceiptTracking(item, quantity, batchNumber, serialNumbers);

        grn.UpdateLine(lineId, quantity, unitCost, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceGoodsReceiptReceiptPlanAsync(
        Guid goodsReceiptId,
        IReadOnlyCollection<GoodsReceiptReceiptPlanLineInput> lines,
        CancellationToken cancellationToken = default)
    {
        var grn = await dbContext.GoodsReceipts.Include(x => x.Lines).ThenInclude(x => x.Serials)
                      .FirstOrDefaultAsync(x => x.Id == goodsReceiptId, cancellationToken)
                  ?? throw new NotFoundException("Goods receipt not found.");

        var po = await dbContext.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        EnsurePurchaseOrderReceivable(po);

        var requestedLines = lines
            .Where(x => x.Quantity > 0m)
            .ToList();

        if (requestedLines.Count != requestedLines.Select(x => x.PurchaseOrderLineId).Distinct().Count())
        {
            throw new DomainValidationException("Each PO line can only appear once on a GRN receipt plan.");
        }

        var reservedInOtherDrafts = await GetGoodsReceiptDraftReservationsAsync(po.Id, grn.Id, cancellationToken);
        var requestedPurchaseOrderLineIds = requestedLines
            .Select(x => x.PurchaseOrderLineId)
            .ToHashSet();
        var poLineById = po.Lines.ToDictionary(x => x.Id);
        var itemIds = po.Lines
            .Where(x => requestedPurchaseOrderLineIds.Contains(x.Id))
            .Select(x => x.ItemId)
            .Distinct()
            .ToList();
        var itemById = await dbContext.Items.AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var requestedLine in requestedLines)
        {
            if (requestedLine.Quantity <= 0m)
            {
                throw new DomainValidationException("Receipt quantity must be positive.");
            }

            if (requestedLine.UnitCost < 0m)
            {
                throw new DomainValidationException("Unit cost must be 0 or greater.");
            }

            var poLine = poLineById.GetValueOrDefault(requestedLine.PurchaseOrderLineId)
                         ?? throw new DomainValidationException("PO line not found.");
            ValidateReceiptPlanLineQuantity(poLine, requestedLine.Quantity, reservedInOtherDrafts);

            var item = itemById.GetValueOrDefault(poLine.ItemId)
                       ?? throw new DomainValidationException("Invalid item on goods receipt.");
            ValidateGoodsReceiptTracking(item, requestedLine.Quantity, requestedLine.BatchNumber, requestedLine.SerialNumbers);
        }

        foreach (var existingLine in grn.Lines.ToList())
        {
            grn.RemoveLine(existingLine.Id);
            dbContext.DbContext.Remove(existingLine);
        }

        foreach (var requestedLine in requestedLines)
        {
            var poLine = po.Lines.First(x => x.Id == requestedLine.PurchaseOrderLineId);
            var line = grn.AddLine(
                poLine.ItemId,
                requestedLine.Quantity,
                requestedLine.UnitCost,
                requestedLine.BatchNumber,
                poLine.Id);

            if (requestedLine.SerialNumbers is { Count: > 0 })
            {
                foreach (var serialNumber in requestedLine.SerialNumbers)
                {
                    line.AddSerial(serialNumber);
                }
            }

            dbContext.DbContext.Add(line);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveGoodsReceiptLineAsync(Guid goodsReceiptId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var grn = await dbContext.GoodsReceipts.Include(x => x.Lines).ThenInclude(x => x.Serials)
                      .FirstOrDefaultAsync(x => x.Id == goodsReceiptId, cancellationToken)
                  ?? throw new NotFoundException("Goods receipt not found.");

        var line = grn.Lines.FirstOrDefault(x => x.Id == lineId)
                   ?? throw new NotFoundException("Goods receipt line not found.");

        grn.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostGoodsReceiptAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default)
    {
        var grn = await dbContext.GoodsReceipts.Include(x => x.Lines).ThenInclude(l => l.Serials)
                      .FirstOrDefaultAsync(x => x.Id == goodsReceiptId, cancellationToken)
                  ?? throw new NotFoundException("Goods receipt not found.");

        var po = await dbContext.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken)
                 ?? throw new NotFoundException("Purchase order not found.");

        if (po.Status != PurchaseOrderStatus.Approved && po.Status != PurchaseOrderStatus.PartiallyReceived)
        {
            throw new DomainValidationException("Purchase order must be approved before receiving.");
        }

        var itemIds = grn.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        grn.Post();

        foreach (var line in grn.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on goods receipt.");
            }

            await inventoryService.RecordReceiptAsync(
                grn.ReceivedAt,
                grn.WarehouseId,
                item,
                line.Quantity,
                line.UnitCost,
                ReferenceTypes.GoodsReceipt,
                grn.Id,
                line.Id,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList(),
                cancellationToken);

            if (line.PurchaseOrderLineId is { } purchaseOrderLineId)
            {
                po.ApplyReceiptToLine(purchaseOrderLineId, line.Quantity);
            }
            else
            {
                po.ApplyReceipt(line.ItemId, line.Quantity);
            }
        }

        var amount = grn.Lines.Sum(l => l.Quantity * l.UnitCost);
        await dbContext.AccountsPayableEntries.AddAsync(
            new ISS.Domain.Finance.AccountsPayableEntry(po.SupplierId, ReferenceTypes.GoodsReceipt, grn.Id, amount, clock.UtcNow),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void EnsurePurchaseOrderReceivable(PurchaseOrder po)
    {
        if (po.Status != PurchaseOrderStatus.Approved && po.Status != PurchaseOrderStatus.PartiallyReceived)
        {
            throw new DomainValidationException("Purchase order must be approved before receiving.");
        }
    }

    private async Task<Dictionary<Guid, decimal>> GetGoodsReceiptDraftReservationsAsync(
        Guid purchaseOrderId,
        Guid excludedGoodsReceiptId,
        CancellationToken cancellationToken)
    {
        return await dbContext.GoodsReceipts
            .AsNoTracking()
            .Where(x => x.PurchaseOrderId == purchaseOrderId && x.Status == GoodsReceiptStatus.Draft && x.Id != excludedGoodsReceiptId)
            .SelectMany(x => x.Lines)
            .Where(x => x.PurchaseOrderLineId != null)
            .GroupBy(x => x.PurchaseOrderLineId!.Value)
            .Select(x => new { PurchaseOrderLineId = x.Key, Quantity = x.Sum(y => y.Quantity) })
            .ToDictionaryAsync(x => x.PurchaseOrderLineId, x => x.Quantity, cancellationToken);
    }

    private static Guid ResolvePurchaseOrderLineId(
        PurchaseOrder po,
        GoodsReceipt grn,
        Guid itemId,
        Guid? requestedPurchaseOrderLineId,
        decimal quantity,
        IReadOnlyDictionary<Guid, decimal> reservedInOtherDrafts)
    {
        if (requestedPurchaseOrderLineId is { } explicitPurchaseOrderLineId)
        {
            ValidateGoodsReceiptLineQuantity(po, grn, explicitPurchaseOrderLineId, quantity, reservedInOtherDrafts, excludeGoodsReceiptLineId: null);

            var explicitLine = po.Lines.FirstOrDefault(x => x.Id == explicitPurchaseOrderLineId)
                               ?? throw new DomainValidationException("PO line not found.");

            if (explicitLine.ItemId != itemId)
            {
                throw new DomainValidationException("Selected PO line does not match the GRN item.");
            }

            return explicitPurchaseOrderLineId;
        }

        foreach (var poLine in po.Lines.Where(x => x.ItemId == itemId))
        {
            var currentDraftQuantity = grn.Lines
                .Where(x => x.PurchaseOrderLineId == poLine.Id)
                .Sum(x => x.Quantity);
            var availableQuantity = poLine.OrderedQuantity
                - poLine.ReceivedQuantity
                - reservedInOtherDrafts.GetValueOrDefault(poLine.Id)
                - currentDraftQuantity;

            if (availableQuantity >= quantity)
            {
                return poLine.Id;
            }
        }

        throw new DomainValidationException("GRN quantity exceeds the remaining quantity on the purchase order.");
    }

    private static void ValidateGoodsReceiptLineQuantity(
        PurchaseOrder po,
        GoodsReceipt grn,
        Guid purchaseOrderLineId,
        decimal quantity,
        IReadOnlyDictionary<Guid, decimal> reservedInOtherDrafts,
        Guid? excludeGoodsReceiptLineId)
    {
        var poLine = po.Lines.FirstOrDefault(x => x.Id == purchaseOrderLineId)
                     ?? throw new DomainValidationException("PO line not found.");

        var currentDraftQuantity = grn.Lines
            .Where(x => x.Id != excludeGoodsReceiptLineId && x.PurchaseOrderLineId == purchaseOrderLineId)
            .Sum(x => x.Quantity);
        var availableQuantity = poLine.OrderedQuantity
            - poLine.ReceivedQuantity
            - reservedInOtherDrafts.GetValueOrDefault(poLine.Id)
            - currentDraftQuantity;

        if (quantity > availableQuantity)
        {
            throw new DomainValidationException("GRN quantity exceeds the remaining quantity on the purchase order.");
        }
    }

    private static void ValidateReceiptPlanLineQuantity(
        PurchaseOrderLine poLine,
        decimal quantity,
        IReadOnlyDictionary<Guid, decimal> reservedInOtherDrafts)
    {
        var availableQuantity = poLine.OrderedQuantity
            - poLine.ReceivedQuantity
            - reservedInOtherDrafts.GetValueOrDefault(poLine.Id);

        if (quantity > availableQuantity)
        {
            throw new DomainValidationException("GRN quantity exceeds the remaining quantity on the purchase order.");
        }
    }

    private static void ValidateGoodsReceiptTracking(
        Item item,
        decimal quantity,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers)
    {
        if (item.TrackingType == TrackingType.Serial)
        {
            if (decimal.Truncate(quantity) != quantity)
            {
                throw new DomainValidationException("Quantity must be a whole number for serial-tracked items.");
            }

            var normalizedSerials = serialNumbers?
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (normalizedSerials is null || normalizedSerials.Count == 0)
            {
                throw new DomainValidationException("Serial numbers are required for serial-tracked items.");
            }

            if (normalizedSerials.Count != (int)quantity)
            {
                throw new DomainValidationException("Quantity must match serial count for serial-tracked items.");
            }

            var duplicates = normalizedSerials
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                throw new DomainValidationException($"Duplicate serial(s): {string.Join(", ", duplicates)}");
            }
        }

        if (item.TrackingType == TrackingType.Batch && string.IsNullOrWhiteSpace(batchNumber))
        {
            throw new DomainValidationException("Batch number is required for batch-tracked items.");
        }
    }

    public async Task<Guid> CreateSupplierInvoiceAsync(
        Guid supplierId,
        string invoiceNumber,
        DateTimeOffset invoiceDate,
        DateTimeOffset? dueDate,
        Guid? purchaseOrderId,
        Guid? goodsReceiptId,
        Guid? directPurchaseId,
        decimal subtotal,
        decimal discountAmount,
        decimal taxAmount,
        decimal freightAmount,
        decimal roundingAmount,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await ValidateSupplierInvoiceLinksAsync(
            supplierId,
            purchaseOrderId,
            goodsReceiptId,
            directPurchaseId,
            cancellationToken);

        var number = await documentNumberService.NextAsync(ReferenceTypes.SupplierInvoice, "SINV", cancellationToken);
        var invoice = new SupplierInvoice(
            number,
            supplierId,
            invoiceNumber,
            invoiceDate,
            dueDate,
            purchaseOrderId,
            goodsReceiptId,
            directPurchaseId,
            subtotal,
            discountAmount,
            taxAmount,
            freightAmount,
            roundingAmount,
            notes);

        await dbContext.SupplierInvoices.AddAsync(invoice, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    public async Task PostSupplierInvoiceAsync(Guid supplierInvoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SupplierInvoices.FirstOrDefaultAsync(x => x.Id == supplierInvoiceId, cancellationToken)
                      ?? throw new NotFoundException("Supplier invoice not found.");

        await ValidateSupplierInvoiceLinksAsync(
            invoice.SupplierId,
            invoice.PurchaseOrderId,
            invoice.GoodsReceiptId,
            invoice.DirectPurchaseId,
            cancellationToken,
            requirePostedLinkedDocuments: true);

        Guid? apEntryId = null;

        if (invoice.GoodsReceiptId is { } grnId)
        {
            var grnAp = await dbContext.AccountsPayableEntries
                .FirstOrDefaultAsync(x => x.ReferenceType == ReferenceTypes.GoodsReceipt && x.ReferenceId == grnId, cancellationToken);

            if (grnAp is not null)
            {
                if (Math.Abs(grnAp.Amount - invoice.GrandTotal) > 0.01m)
                {
                    throw new DomainValidationException(
                        $"Linked GRN already created AP amount {grnAp.Amount:0.00}, but invoice total is {invoice.GrandTotal:0.00}. " +
                        "Use debit/credit notes to record the variance in the current workflow.");
                }

                apEntryId = grnAp.Id;
            }
        }

        if (apEntryId is null)
        {
            var ap = new AccountsPayableEntry(invoice.SupplierId, ReferenceTypes.SupplierInvoice, invoice.Id, invoice.GrandTotal, clock.UtcNow);
            await dbContext.AccountsPayableEntries.AddAsync(ap, cancellationToken);
            apEntryId = ap.Id;
        }

        invoice.Post(apEntryId, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateSupplierReturnAsync(Guid supplierId, Guid warehouseId, string? reason, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("SR", "SR", cancellationToken);
        var sr = new SupplierReturn(number, supplierId, warehouseId, clock.UtcNow, reason);
        await dbContext.SupplierReturns.AddAsync(sr, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return sr.Id;
    }

    public async Task AddSupplierReturnLineAsync(
        Guid supplierReturnId,
        Guid itemId,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var sr = await dbContext.SupplierReturns.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == supplierReturnId, cancellationToken)
                 ?? throw new NotFoundException("Supplier return not found.");

        var line = sr.AddLine(itemId, quantity, unitCost, batchNumber);
        if (serialNumbers is { Count: > 0 })
        {
            foreach (var serial in serialNumbers)
            {
                line.AddSerial(serial);
            }
        }

        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSupplierReturnLineAsync(
        Guid supplierReturnId,
        Guid lineId,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var sr = await dbContext.SupplierReturns.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == supplierReturnId, cancellationToken)
                 ?? throw new NotFoundException("Supplier return not found.");

        if (!sr.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Supplier return line not found.");
        }

        sr.UpdateLine(lineId, quantity, unitCost, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSupplierReturnLineAsync(Guid supplierReturnId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var sr = await dbContext.SupplierReturns.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == supplierReturnId, cancellationToken)
                 ?? throw new NotFoundException("Supplier return not found.");

        var line = sr.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Supplier return line not found.");

        sr.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostSupplierReturnAsync(Guid supplierReturnId, CancellationToken cancellationToken = default)
    {
        var sr = await dbContext.SupplierReturns.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == supplierReturnId, cancellationToken)
                 ?? throw new NotFoundException("Supplier return not found.");

        var itemIds = sr.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        sr.Post();

        foreach (var line in sr.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on supplier return.");
            }

            await inventoryService.RecordIssueAsync(
                sr.ReturnDate,
                sr.WarehouseId,
                item,
                line.Quantity,
                line.UnitCost,
                ReferenceTypes.SupplierReturn,
                sr.Id,
                line.Id,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList(),
                cancellationToken);
        }

        var amount = sr.Lines.Sum(l => l.Quantity * l.UnitCost);

        var creditNumber = await documentNumberService.NextAsync(ReferenceTypes.CreditNote, ReferenceTypes.CreditNote, cancellationToken);
        var creditNote = new CreditNote(
            creditNumber,
            CounterpartyType.Supplier,
            sr.SupplierId,
            amount,
            clock.UtcNow,
            notes: $"Auto credit for supplier return {sr.Number}.",
            sourceReferenceType: ReferenceTypes.SupplierReturn,
            sourceReferenceId: sr.Id);

        await dbContext.CreditNotes.AddAsync(creditNote, cancellationToken);

        var apEntries = await dbContext.AccountsPayableEntries
            .Where(x => x.SupplierId == sr.SupplierId && x.Outstanding > 0)
            .OrderBy(x => x.PostedAt)
            .ToListAsync(cancellationToken);

        foreach (var ap in apEntries)
        {
            if (creditNote.RemainingAmount <= 0)
            {
                break;
            }

            var allocate = Math.Min(ap.Outstanding, creditNote.RemainingAmount);
            var allocation = creditNote.AllocateToAp(ap.Id, allocate);
            dbContext.DbContext.Add(allocation);
            ap.ApplyPayment(allocate);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateSupplierInvoiceLinksAsync(
        Guid supplierId,
        Guid? purchaseOrderId,
        Guid? goodsReceiptId,
        Guid? directPurchaseId,
        CancellationToken cancellationToken,
        bool requirePostedLinkedDocuments = false)
    {
        if (directPurchaseId is not null && (purchaseOrderId is not null || goodsReceiptId is not null))
        {
            throw new DomainValidationException("Supplier invoice cannot combine direct purchase and PO/GRN references.");
        }

        var supplierExists = await dbContext.Suppliers.AsNoTracking().AnyAsync(x => x.Id == supplierId, cancellationToken);
        if (!supplierExists)
        {
            throw new NotFoundException("Supplier not found.");
        }

        PurchaseOrder? po = null;

        if (purchaseOrderId is { } poId)
        {
            po = await dbContext.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == poId, cancellationToken)
                 ?? throw new NotFoundException("Purchase order not found.");

            if (po.SupplierId != supplierId)
            {
                throw new DomainValidationException("Purchase order supplier does not match supplier invoice.");
            }

            if (requirePostedLinkedDocuments && po.Status == PurchaseOrderStatus.Draft)
            {
                throw new DomainValidationException("Linked purchase order must be approved before posting supplier invoice.");
            }
        }

        if (goodsReceiptId is { } grnId)
        {
            var grn = await dbContext.GoodsReceipts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == grnId, cancellationToken)
                      ?? throw new NotFoundException("Goods receipt not found.");

            if (requirePostedLinkedDocuments && grn.Status != GoodsReceiptStatus.Posted)
            {
                throw new DomainValidationException("Linked goods receipt must be posted before posting supplier invoice.");
            }

            po ??= await dbContext.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken)
                   ?? throw new NotFoundException("Purchase order linked to goods receipt not found.");

            if (purchaseOrderId is not null && grn.PurchaseOrderId != purchaseOrderId.Value)
            {
                throw new DomainValidationException("Linked goods receipt does not belong to the selected purchase order.");
            }

            if (po.SupplierId != supplierId)
            {
                throw new DomainValidationException("Linked goods receipt supplier does not match supplier invoice.");
            }
        }

        if (directPurchaseId is { } dpId)
        {
            var dp = await dbContext.DirectPurchases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dpId, cancellationToken)
                     ?? throw new NotFoundException("Direct purchase not found.");

            if (dp.SupplierId != supplierId)
            {
                throw new DomainValidationException("Linked direct purchase supplier does not match supplier invoice.");
            }

            if (requirePostedLinkedDocuments && dp.Status != DirectPurchaseStatus.Posted)
            {
                throw new DomainValidationException("Linked direct purchase must be posted before posting supplier invoice.");
            }
        }
    }
}
