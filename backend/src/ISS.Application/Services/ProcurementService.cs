using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Finance;
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

    public async Task MarkRfqSentAsync(Guid rfqId, CancellationToken cancellationToken = default)
    {
        var rfq = await dbContext.RequestForQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == rfqId, cancellationToken)
                  ?? throw new NotFoundException("RFQ not found.");

        rfq.MarkSent();
        await dbContext.SaveChangesAsync(cancellationToken);
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
        var number = await documentNumberService.NextAsync("GRN", "GRN", cancellationToken);
        var grn = new GoodsReceipt(number, purchaseOrderId, warehouseId, clock.UtcNow);
        await dbContext.GoodsReceipts.AddAsync(grn, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return grn.Id;
    }

    public async Task AddGoodsReceiptLineAsync(
        Guid goodsReceiptId,
        Guid itemId,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var grn = await dbContext.GoodsReceipts.Include(x => x.Lines).ThenInclude(l => l.Serials)
                      .FirstOrDefaultAsync(x => x.Id == goodsReceiptId, cancellationToken)
                  ?? throw new NotFoundException("Goods receipt not found.");

        var line = grn.AddLine(itemId, quantity, unitCost, batchNumber);
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

            po.ApplyReceipt(line.ItemId, line.Quantity);
        }

        var amount = grn.Lines.Sum(l => l.Quantity * l.UnitCost);
        await dbContext.AccountsPayableEntries.AddAsync(
            new ISS.Domain.Finance.AccountsPayableEntry(po.SupplierId, ReferenceTypes.GoodsReceipt, grn.Id, amount, clock.UtcNow),
            cancellationToken);

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
}
