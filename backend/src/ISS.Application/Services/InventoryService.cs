using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Inventory;
using ISS.Domain.MasterData;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class InventoryService(IIssDbContext dbContext)
{
    public async Task<decimal> GetOnHandAsync(Guid warehouseId, Guid itemId, string? batchNumber = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryMovements.AsNoTracking()
            .Where(m => m.WarehouseId == warehouseId && m.ItemId == itemId);

        if (!string.IsNullOrWhiteSpace(batchNumber))
        {
            var trimmed = batchNumber.Trim();
            query = query.Where(m => m.BatchNumber == trimmed);
        }

        return await query.SumAsync(m => m.Quantity, cancellationToken);
    }

    public async Task<bool> IsSerialInStockAsync(Guid warehouseId, Guid itemId, string serialNumber, CancellationToken cancellationToken = default)
    {
        serialNumber = serialNumber.Trim();
        var qty = await dbContext.InventoryMovements.AsNoTracking()
            .Where(m => m.WarehouseId == warehouseId && m.ItemId == itemId && m.SerialNumber == serialNumber)
            .SumAsync(m => m.Quantity, cancellationToken);
        return qty > 0;
    }

    public async Task RecordReceiptAsync(
        DateTimeOffset occurredAt,
        Guid warehouseId,
        Item item,
        decimal quantity,
        decimal unitCost,
        string referenceType,
        Guid referenceId,
        Guid? referenceLineId,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        ValidateTracking(item, quantity, batchNumber, serialNumbers);

        if (item.TrackingType == TrackingType.Serial)
        {
            foreach (var serial in serialNumbers!)
            {
                await dbContext.InventoryMovements.AddAsync(
                    new InventoryMovement(
                        occurredAt,
                        InventoryMovementType.Receipt,
                        warehouseId,
                        item.Id,
                        quantity: 1m,
                        unitCost,
                        referenceType,
                        referenceId,
                        referenceLineId,
                        serialNumber: serial.Trim(),
                        batchNumber: null),
                    cancellationToken);
            }

            return;
        }

        await dbContext.InventoryMovements.AddAsync(
            new InventoryMovement(
                occurredAt,
                InventoryMovementType.Receipt,
                warehouseId,
                item.Id,
                quantity,
                unitCost,
                referenceType,
                referenceId,
                referenceLineId,
                serialNumber: null,
                batchNumber: batchNumber?.Trim()),
            cancellationToken);
    }

    public async Task RecordAdjustmentAsync(
        DateTimeOffset occurredAt,
        Guid warehouseId,
        Item item,
        decimal quantityDelta,
        decimal unitCost,
        string referenceType,
        Guid referenceId,
        Guid? referenceLineId,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        if (quantityDelta == 0m)
        {
            throw new DomainValidationException("Quantity delta cannot be zero.");
        }

        var quantity = Math.Abs(quantityDelta);
        ValidateTracking(item, quantity, batchNumber, serialNumbers);

        if (item.TrackingType == TrackingType.Serial)
        {
            foreach (var serial in serialNumbers!)
            {
                if (quantityDelta < 0m && !await IsSerialInStockAsync(warehouseId, item.Id, serial, cancellationToken))
                {
                    throw new DomainValidationException($"Serial '{serial}' is not in stock.");
                }

                await dbContext.InventoryMovements.AddAsync(
                    new InventoryMovement(
                        occurredAt,
                        InventoryMovementType.Adjustment,
                        warehouseId,
                        item.Id,
                        quantity: quantityDelta > 0m ? 1m : -1m,
                        unitCost,
                        referenceType,
                        referenceId,
                        referenceLineId,
                        serialNumber: serial.Trim(),
                        batchNumber: null),
                    cancellationToken);
            }

            return;
        }

        if (quantityDelta < 0m)
        {
            var onHand = await GetOnHandAsync(warehouseId, item.Id, batchNumber, cancellationToken);
            if (onHand - quantity < 0)
            {
                throw new DomainValidationException($"Insufficient stock for item '{item.Sku}' in warehouse '{warehouseId}'.");
            }
        }

        await dbContext.InventoryMovements.AddAsync(
            new InventoryMovement(
                occurredAt,
                InventoryMovementType.Adjustment,
                warehouseId,
                item.Id,
                quantity: quantityDelta > 0m ? quantity : -quantity,
                unitCost,
                referenceType,
                referenceId,
                referenceLineId,
                serialNumber: null,
                batchNumber: batchNumber?.Trim()),
            cancellationToken);
    }

    public async Task RecordTransferOutAsync(
        DateTimeOffset occurredAt,
        Guid warehouseId,
        Item item,
        decimal quantity,
        decimal unitCost,
        string referenceType,
        Guid referenceId,
        Guid? referenceLineId,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        ValidateTracking(item, quantity, batchNumber, serialNumbers);

        if (item.TrackingType == TrackingType.Serial)
        {
            foreach (var serial in serialNumbers!)
            {
                if (!await IsSerialInStockAsync(warehouseId, item.Id, serial, cancellationToken))
                {
                    throw new DomainValidationException($"Serial '{serial}' is not in stock.");
                }

                await dbContext.InventoryMovements.AddAsync(
                    new InventoryMovement(
                        occurredAt,
                        InventoryMovementType.TransferOut,
                        warehouseId,
                        item.Id,
                        quantity: -1m,
                        unitCost,
                        referenceType,
                        referenceId,
                        referenceLineId,
                        serialNumber: serial.Trim(),
                        batchNumber: null),
                    cancellationToken);
            }

            return;
        }

        var onHand = await GetOnHandAsync(warehouseId, item.Id, batchNumber, cancellationToken);
        if (onHand - quantity < 0)
        {
            throw new DomainValidationException($"Insufficient stock for item '{item.Sku}' in warehouse '{warehouseId}'.");
        }

        await dbContext.InventoryMovements.AddAsync(
            new InventoryMovement(
                occurredAt,
                InventoryMovementType.TransferOut,
                warehouseId,
                item.Id,
                quantity: -quantity,
                unitCost,
                referenceType,
                referenceId,
                referenceLineId,
                serialNumber: null,
                batchNumber: batchNumber?.Trim()),
            cancellationToken);
    }

    public async Task RecordTransferInAsync(
        DateTimeOffset occurredAt,
        Guid warehouseId,
        Item item,
        decimal quantity,
        decimal unitCost,
        string referenceType,
        Guid referenceId,
        Guid? referenceLineId,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        ValidateTracking(item, quantity, batchNumber, serialNumbers);

        if (item.TrackingType == TrackingType.Serial)
        {
            foreach (var serial in serialNumbers!)
            {
                await dbContext.InventoryMovements.AddAsync(
                    new InventoryMovement(
                        occurredAt,
                        InventoryMovementType.TransferIn,
                        warehouseId,
                        item.Id,
                        quantity: 1m,
                        unitCost,
                        referenceType,
                        referenceId,
                        referenceLineId,
                        serialNumber: serial.Trim(),
                        batchNumber: null),
                    cancellationToken);
            }

            return;
        }

        await dbContext.InventoryMovements.AddAsync(
            new InventoryMovement(
                occurredAt,
                InventoryMovementType.TransferIn,
                warehouseId,
                item.Id,
                quantity,
                unitCost,
                referenceType,
                referenceId,
                referenceLineId,
                serialNumber: null,
                batchNumber: batchNumber?.Trim()),
            cancellationToken);
    }

    public async Task RecordIssueAsync(
        DateTimeOffset occurredAt,
        Guid warehouseId,
        Item item,
        decimal quantity,
        decimal unitCost,
        string referenceType,
        Guid referenceId,
        Guid? referenceLineId,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        ValidateTracking(item, quantity, batchNumber, serialNumbers);

        if (item.TrackingType == TrackingType.Serial)
        {
            foreach (var serial in serialNumbers!)
            {
                if (!await IsSerialInStockAsync(warehouseId, item.Id, serial, cancellationToken))
                {
                    throw new DomainValidationException($"Serial '{serial}' is not in stock.");
                }

                await dbContext.InventoryMovements.AddAsync(
                    new InventoryMovement(
                        occurredAt,
                        InventoryMovementType.Issue,
                        warehouseId,
                        item.Id,
                        quantity: -1m,
                        unitCost,
                        referenceType,
                        referenceId,
                        referenceLineId,
                        serialNumber: serial.Trim(),
                        batchNumber: null),
                    cancellationToken);
            }

            return;
        }

        var onHand = await GetOnHandAsync(warehouseId, item.Id, batchNumber, cancellationToken);
        if (onHand - quantity < 0)
        {
            throw new DomainValidationException($"Insufficient stock for item '{item.Sku}' in warehouse '{warehouseId}'.");
        }

        await dbContext.InventoryMovements.AddAsync(
            new InventoryMovement(
                occurredAt,
                InventoryMovementType.Issue,
                warehouseId,
                item.Id,
                quantity: -quantity,
                unitCost,
                referenceType,
                referenceId,
                referenceLineId,
                serialNumber: null,
                batchNumber: batchNumber?.Trim()),
            cancellationToken);
    }

    public async Task RecordConsumptionAsync(
        DateTimeOffset occurredAt,
        Guid warehouseId,
        Item item,
        decimal quantity,
        decimal unitCost,
        string referenceType,
        Guid referenceId,
        Guid? referenceLineId,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        ValidateTracking(item, quantity, batchNumber, serialNumbers);

        if (item.TrackingType == TrackingType.Serial)
        {
            foreach (var serial in serialNumbers!)
            {
                if (!await IsSerialInStockAsync(warehouseId, item.Id, serial, cancellationToken))
                {
                    throw new DomainValidationException($"Serial '{serial}' is not in stock.");
                }

                await dbContext.InventoryMovements.AddAsync(
                    new InventoryMovement(
                        occurredAt,
                        InventoryMovementType.Consumption,
                        warehouseId,
                        item.Id,
                        quantity: -1m,
                        unitCost,
                        referenceType,
                        referenceId,
                        referenceLineId,
                        serialNumber: serial.Trim(),
                        batchNumber: null),
                    cancellationToken);
            }

            return;
        }

        var onHand = await GetOnHandAsync(warehouseId, item.Id, batchNumber, cancellationToken);
        if (onHand - quantity < 0)
        {
            throw new DomainValidationException($"Insufficient stock for item '{item.Sku}' in warehouse '{warehouseId}'.");
        }

        await dbContext.InventoryMovements.AddAsync(
            new InventoryMovement(
                occurredAt,
                InventoryMovementType.Consumption,
                warehouseId,
                item.Id,
                quantity: -quantity,
                unitCost,
                referenceType,
                referenceId,
                referenceLineId,
                serialNumber: null,
                batchNumber: batchNumber?.Trim()),
            cancellationToken);
    }

    private static void ValidateTracking(Item item, decimal quantity, string? batchNumber, IReadOnlyCollection<string>? serialNumbers)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Quantity must be positive.");
        }

        if (item.TrackingType == TrackingType.Serial)
        {
            if (decimal.Truncate(quantity) != quantity)
            {
                throw new DomainValidationException("Quantity must be a whole number for serial-tracked items.");
            }

            if (serialNumbers is null || serialNumbers.Count == 0)
            {
                throw new DomainValidationException("Serial numbers are required for serial-tracked items.");
            }

            if (serialNumbers.Count != (int)quantity)
            {
                throw new DomainValidationException("Quantity must match serial count for serial-tracked items.");
            }

            var duplicates = serialNumbers
                .Select(s => s.Trim())
                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
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
}
