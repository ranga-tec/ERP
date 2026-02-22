using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Service;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class ServiceManagementService(
    IIssDbContext dbContext,
    IDocumentNumberService documentNumberService,
    IClock clock,
    InventoryService inventoryService)
{
    public async Task<Guid> CreateEquipmentUnitAsync(
        Guid itemId,
        string serialNumber,
        Guid customerId,
        DateTimeOffset? purchasedAt,
        DateTimeOffset? warrantyUntil,
        CancellationToken cancellationToken = default)
    {
        var unit = new EquipmentUnit(itemId, serialNumber, customerId, purchasedAt, warrantyUntil);
        await dbContext.EquipmentUnits.AddAsync(unit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return unit.Id;
    }

    public async Task<Guid> CreateServiceJobAsync(Guid equipmentUnitId, Guid customerId, string problemDescription, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("SJ", "SJ", cancellationToken);
        var job = new ServiceJob(number, equipmentUnitId, customerId, clock.UtcNow, problemDescription);
        await dbContext.ServiceJobs.AddAsync(job, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job.Id;
    }

    public async Task StartServiceJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs.FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");

        job.Start();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteServiceJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs.FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");

        job.Complete(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseServiceJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs.FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");

        job.Close();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateWorkOrderAsync(Guid serviceJobId, string description, Guid? assignedToUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = new WorkOrder(serviceJobId, description, assignedToUserId);
        await dbContext.WorkOrders.AddAsync(workOrder, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return workOrder.Id;
    }

    public async Task<Guid> CreateMaterialRequisitionAsync(Guid serviceJobId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var number = await documentNumberService.NextAsync("MR", "MR", cancellationToken);
        var mr = new MaterialRequisition(number, serviceJobId, warehouseId, clock.UtcNow);
        await dbContext.MaterialRequisitions.AddAsync(mr, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return mr.Id;
    }

    public async Task AddMaterialRequisitionLineAsync(
        Guid materialRequisitionId,
        Guid itemId,
        decimal quantity,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var mr = await dbContext.MaterialRequisitions.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == materialRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Material requisition not found.");

        var line = mr.AddLine(itemId, quantity, batchNumber);
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

    public async Task PostMaterialRequisitionAsync(Guid materialRequisitionId, CancellationToken cancellationToken = default)
    {
        var mr = await dbContext.MaterialRequisitions.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == materialRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Material requisition not found.");

        var itemIds = mr.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var itemById = items.ToDictionary(i => i.Id, i => i);

        mr.Post();

        foreach (var line in mr.Lines)
        {
            if (!itemById.TryGetValue(line.ItemId, out var item))
            {
                throw new DomainValidationException("Invalid item on material requisition.");
            }

            await inventoryService.RecordConsumptionAsync(
                mr.RequestedAt,
                mr.WarehouseId,
                item,
                line.Quantity,
                unitCost: item.DefaultUnitCost,
                ReferenceTypes.MaterialRequisition,
                mr.Id,
                line.Id,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList(),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddQualityCheckAsync(Guid serviceJobId, bool passed, string? notes, CancellationToken cancellationToken = default)
    {
        var qc = new QualityCheck(serviceJobId, clock.UtcNow, passed, notes);
        await dbContext.QualityChecks.AddAsync(qc, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return qc.Id;
    }
}
