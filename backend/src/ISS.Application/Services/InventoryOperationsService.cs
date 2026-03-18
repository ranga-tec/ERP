using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Inventory;
using ISS.Domain.MasterData;
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

    public Task<decimal> GetStockAdjustmentPreviewQuantityAsync(
        Guid warehouseId,
        Guid itemId,
        string? batchNumber,
        CancellationToken cancellationToken = default)
        => inventoryService.GetOnHandAsync(warehouseId, itemId, batchNumber, cancellationToken);

    public async Task AddStockAdjustmentLineAsync(
        Guid stockAdjustmentId,
        Guid itemId,
        decimal? countedQuantity,
        decimal? quantityDelta,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var adjustment = await dbContext.StockAdjustments.Include(x => x.Lines).ThenInclude(l => l.Serials)
                             .FirstOrDefaultAsync(x => x.Id == stockAdjustmentId, cancellationToken)
                         ?? throw new NotFoundException("Stock adjustment not found.");

        var line = await AddOrUpdateStockAdjustmentLineAsync(
            adjustment,
            existingLineId: null,
            itemId,
            countedQuantity,
            quantityDelta,
            unitCost,
            batchNumber,
            serialNumbers,
            cancellationToken);

        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStockAdjustmentLineAsync(
        Guid stockAdjustmentId,
        Guid lineId,
        decimal? countedQuantity,
        decimal? quantityDelta,
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

        await AddOrUpdateStockAdjustmentLineAsync(
            adjustment,
            lineId,
            itemId: null,
            countedQuantity,
            quantityDelta,
            unitCost,
            batchNumber,
            serialNumbers,
            cancellationToken);
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

            if (line.CountedQuantity is null)
            {
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
                continue;
            }

            if (item.TrackingType == TrackingType.Serial)
            {
                var countedSerials = line.Serials
                    .Select(s => s.SerialNumber.Trim())
                    .Where(s => s.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (line.CountedQuantity.Value != countedSerials.Count)
                {
                    throw new DomainValidationException("Counted quantity must match serial count for serial-tracked stock adjustments.");
                }

                var currentSerials = await inventoryService.GetSerialsOnHandAsync(adjustment.WarehouseId, item.Id, cancellationToken);
                line.RefreshVariance(currentSerials.Count);

                var serialsToRemove = currentSerials
                    .Except(countedSerials, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (serialsToRemove.Count > 0)
                {
                    await inventoryService.RecordAdjustmentAsync(
                        adjustment.AdjustedAt,
                        adjustment.WarehouseId,
                        item,
                        -serialsToRemove.Count,
                        line.UnitCost,
                        ReferenceTypes.StockAdjustment,
                        adjustment.Id,
                        line.Id,
                        line.BatchNumber,
                        serialsToRemove,
                        cancellationToken);
                }

                var serialsToAdd = countedSerials
                    .Except(currentSerials, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (serialsToAdd.Count > 0)
                {
                    await inventoryService.RecordAdjustmentAsync(
                        adjustment.AdjustedAt,
                        adjustment.WarehouseId,
                        item,
                        serialsToAdd.Count,
                        line.UnitCost,
                        ReferenceTypes.StockAdjustment,
                        adjustment.Id,
                        line.Id,
                        line.BatchNumber,
                        serialsToAdd,
                        cancellationToken);
                }

                continue;
            }

            var currentOnHand = await inventoryService.GetOnHandAsync(adjustment.WarehouseId, item.Id, line.BatchNumber, cancellationToken);
            line.RefreshVariance(currentOnHand);
            if (line.QuantityDelta == 0m)
            {
                continue;
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

    private async Task<StockAdjustmentLine> AddOrUpdateStockAdjustmentLineAsync(
        StockAdjustment adjustment,
        Guid? existingLineId,
        Guid? itemId,
        decimal? countedQuantity,
        decimal? quantityDelta,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken)
    {
        if (countedQuantity is not null)
        {
            var effectiveItemId = itemId
                ?? adjustment.Lines.FirstOrDefault(x => x.Id == existingLineId)?.ItemId
                ?? throw new DomainValidationException("Stock adjustment line not found.");

            EnsureCountedLineKeyIsUnique(adjustment, effectiveItemId, batchNumber, existingLineId);

            StockAdjustmentLine line;
            if (existingLineId is null)
            {
                line = adjustment.AddCountedLine(effectiveItemId, countedQuantity.Value, unitCost, batchNumber);
            }
            else
            {
                adjustment.UpdateLineCounted(existingLineId.Value, countedQuantity.Value, unitCost, batchNumber, serialNumbers);
                line = adjustment.Lines.First(x => x.Id == existingLineId.Value);
            }

            var currentOnHand = await inventoryService.GetOnHandAsync(adjustment.WarehouseId, effectiveItemId, batchNumber, cancellationToken);
            line.RefreshVariance(currentOnHand);
            if (existingLineId is null && serialNumbers is { Count: > 0 })
            {
                foreach (var serial in serialNumbers)
                {
                    line.AddSerial(serial);
                }
            }

            return line;
        }

        if (quantityDelta is null)
        {
            throw new DomainValidationException("Either counted quantity or quantity delta is required.");
        }

        if (existingLineId is null)
        {
            var line = adjustment.AddLine(itemId ?? throw new DomainValidationException("Item is required."), quantityDelta.Value, unitCost, batchNumber);
            if (serialNumbers is { Count: > 0 })
            {
                foreach (var serial in serialNumbers)
                {
                    line.AddSerial(serial);
                }
            }

            return line;
        }

        adjustment.UpdateLine(existingLineId.Value, quantityDelta.Value, unitCost, batchNumber, serialNumbers);
        return adjustment.Lines.First(x => x.Id == existingLineId.Value);
    }

    private static void EnsureCountedLineKeyIsUnique(StockAdjustment adjustment, Guid itemId, string? batchNumber, Guid? existingLineId)
    {
        var normalizedBatch = batchNumber?.Trim() ?? string.Empty;
        var duplicateExists = adjustment.Lines.Any(line =>
            line.Id != existingLineId
            && line.ItemId == itemId
            && string.Equals(line.BatchNumber ?? string.Empty, normalizedBatch, StringComparison.OrdinalIgnoreCase));

        if (duplicateExists)
        {
            throw new DomainValidationException("Only one counted line is allowed per item and batch on a stock adjustment.");
        }
    }
}
