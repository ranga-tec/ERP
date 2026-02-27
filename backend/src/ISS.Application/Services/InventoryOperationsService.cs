using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class InventoryOperationsService(
    IIssDbContext dbContext,
    IDocumentNumberService documentNumberService,
    IClock clock,
    InventoryService inventoryService)
{
    public async Task<Guid> CreateStockAdjustmentAsync(Guid warehouseId, string? reason, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("ADJ", "ADJ", cancellationToken);
        var adjustment = new StockAdjustment(number, warehouseId, clock.UtcNow, reason);
        await dbContext.StockAdjustments.AddAsync(adjustment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return adjustment.Id;
    }

    public async Task AddStockAdjustmentLineAsync(
        Guid stockAdjustmentId,
        Guid itemId,
        decimal quantityDelta,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments.Include(x => x.Lines).ThenInclude(l => l.Serials)
                             .FirstOrDefaultAsync(x => x.Id == stockAdjustmentId, cancellationToken)
                         ?? throw new NotFoundException("Stock adjustment not found.");

        var line = adjustment.AddLine(itemId, quantityDelta, unitCost, batchNumber);

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

    public async Task UpdateStockAdjustmentLineAsync(
        Guid stockAdjustmentId,
        Guid lineId,
        decimal quantityDelta,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments.Include(x => x.Lines).ThenInclude(l => l.Serials)
                             .FirstOrDefaultAsync(x => x.Id == stockAdjustmentId, cancellationToken)
                         ?? throw new NotFoundException("Stock adjustment not found.");

        if (!adjustment.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Stock adjustment line not found.");
        }

        adjustment.UpdateLine(lineId, quantityDelta, unitCost, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveStockAdjustmentLineAsync(Guid stockAdjustmentId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments.Include(x => x.Lines).ThenInclude(l => l.Serials)
                             .FirstOrDefaultAsync(x => x.Id == stockAdjustmentId, cancellationToken)
                         ?? throw new NotFoundException("Stock adjustment not found.");

        var line = adjustment.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Stock adjustment line not found.");

        adjustment.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostStockAdjustmentAsync(Guid stockAdjustmentId, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments.Include(x => x.Lines).ThenInclude(l => l.Serials)
                             .FirstOrDefaultAsync(x => x.Id == stockAdjustmentId, cancellationToken)
                         ?? throw new NotFoundException("Stock adjustment not found.");

        var itemIds = adjustment.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        adjustment.Post();

        foreach (var line in adjustment.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on stock adjustment.");
            }

            await inventoryService.RecordAdjustmentAsync(
                adjustment.AdjustedAt,
                adjustment.WarehouseId,
                item,
                line.QuantityDelta,
                line.UnitCost,
                ReferenceTypes.StockAdjustment,
                adjustment.Id,
                line.Id,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList(),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task VoidStockAdjustmentAsync(Guid stockAdjustmentId, CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments.FirstOrDefaultAsync(x => x.Id == stockAdjustmentId, cancellationToken)
                         ?? throw new NotFoundException("Stock adjustment not found.");

        adjustment.Void();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateStockTransferAsync(Guid fromWarehouseId, Guid toWarehouseId, string? notes, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("TRF", "TRF", cancellationToken);
        var transfer = new StockTransfer(number, fromWarehouseId, toWarehouseId, clock.UtcNow, notes);
        await dbContext.StockTransfers.AddAsync(transfer, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return transfer.Id;
    }

    public async Task AddStockTransferLineAsync(
        Guid stockTransferId,
        Guid itemId,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockTransfers.Include(x => x.Lines).ThenInclude(l => l.Serials)
                           .FirstOrDefaultAsync(x => x.Id == stockTransferId, cancellationToken)
                       ?? throw new NotFoundException("Stock transfer not found.");

        var line = transfer.AddLine(itemId, quantity, unitCost, batchNumber);

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

    public async Task UpdateStockTransferLineAsync(
        Guid stockTransferId,
        Guid lineId,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockTransfers.Include(x => x.Lines).ThenInclude(l => l.Serials)
                           .FirstOrDefaultAsync(x => x.Id == stockTransferId, cancellationToken)
                       ?? throw new NotFoundException("Stock transfer not found.");

        if (!transfer.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Stock transfer line not found.");
        }

        transfer.UpdateLine(lineId, quantity, unitCost, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveStockTransferLineAsync(Guid stockTransferId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockTransfers.Include(x => x.Lines).ThenInclude(l => l.Serials)
                           .FirstOrDefaultAsync(x => x.Id == stockTransferId, cancellationToken)
                       ?? throw new NotFoundException("Stock transfer not found.");

        var line = transfer.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Stock transfer line not found.");

        transfer.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostStockTransferAsync(Guid stockTransferId, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockTransfers.Include(x => x.Lines).ThenInclude(l => l.Serials)
                           .FirstOrDefaultAsync(x => x.Id == stockTransferId, cancellationToken)
                       ?? throw new NotFoundException("Stock transfer not found.");

        var itemIds = transfer.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        transfer.Post();

        foreach (var line in transfer.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on stock transfer.");
            }

            var serials = line.Serials.Select(s => s.SerialNumber).ToList();

            await inventoryService.RecordTransferOutAsync(
                transfer.TransferDate,
                transfer.FromWarehouseId,
                item,
                line.Quantity,
                line.UnitCost,
                ReferenceTypes.StockTransfer,
                transfer.Id,
                line.Id,
                line.BatchNumber,
                serials,
                cancellationToken);

            await inventoryService.RecordTransferInAsync(
                transfer.TransferDate,
                transfer.ToWarehouseId,
                item,
                line.Quantity,
                line.UnitCost,
                ReferenceTypes.StockTransfer,
                transfer.Id,
                line.Id,
                line.BatchNumber,
                serials,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task VoidStockTransferAsync(Guid stockTransferId, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.StockTransfers.FirstOrDefaultAsync(x => x.Id == stockTransferId, cancellationToken)
                       ?? throw new NotFoundException("Stock transfer not found.");

        transfer.Void();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
