using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class ServiceManagementService(
    IIssDbContext dbContext,
    IDocumentNumberService documentNumberService,
    IClock clock,
    InventoryService inventoryService,
    NotificationService notificationService)
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

    public async Task<Guid> CreateServiceEstimateAsync(
        Guid serviceJobId,
        DateTimeOffset? validUntil,
        string? terms,
        CancellationToken cancellationToken = default)
    {
        var jobExists = await dbContext.ServiceJobs.AsNoTracking().AnyAsync(x => x.Id == serviceJobId, cancellationToken);
        if (!jobExists)
        {
            throw new NotFoundException("Service job not found.");
        }

        var number = await documentNumberService.NextAsync("SE", "SE", cancellationToken);
        var estimate = new ServiceEstimate(number, serviceJobId, clock.UtcNow, validUntil, terms);
        await dbContext.ServiceEstimates.AddAsync(estimate, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return estimate.Id;
    }

    public async Task AddServiceEstimateLineAsync(
        Guid serviceEstimateId,
        ServiceEstimateLineKind kind,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent,
        CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        if (kind == ServiceEstimateLineKind.Part && itemId is not null)
        {
            var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == itemId.Value, cancellationToken);
            if (!itemExists)
            {
                throw new NotFoundException("Item not found.");
            }
        }

        var line = estimate.AddLine(kind, itemId, description, quantity, unitPrice, taxPercent);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateServiceEstimateLineAsync(
        Guid serviceEstimateId,
        Guid lineId,
        ServiceEstimateLineKind kind,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent,
        CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        if (kind == ServiceEstimateLineKind.Part && itemId is not null)
        {
            var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == itemId.Value, cancellationToken);
            if (!itemExists)
            {
                throw new NotFoundException("Item not found.");
            }
        }

        if (!estimate.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Service estimate line not found.");
        }

        estimate.UpdateLine(lineId, kind, itemId, description, quantity, unitPrice, taxPercent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveServiceEstimateLineAsync(Guid serviceEstimateId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        var line = estimate.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Service estimate line not found.");

        estimate.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveServiceEstimateAsync(Guid serviceEstimateId, CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        estimate.Approve();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SendServiceEstimateToCustomerAsync(Guid serviceEstimateId, string? appBaseUrl = null, CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        if (estimate.Status == ServiceEstimateStatus.Rejected)
        {
            throw new DomainValidationException("Rejected estimates cannot be sent to customer.");
        }

        if (estimate.Lines.Count == 0)
        {
            throw new DomainValidationException("Estimate must have at least one line before sending.");
        }

        var job = await dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == estimate.ServiceJobId, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");
        var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.CustomerId, cancellationToken)
                       ?? throw new NotFoundException("Customer not found.");

        if (string.IsNullOrWhiteSpace(customer.Email) && string.IsNullOrWhiteSpace(customer.Phone))
        {
            throw new DomainValidationException("Customer does not have email or phone for sending estimate.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(appBaseUrl) ? null : appBaseUrl.TrimEnd('/');
        var estimateLink = baseUrl is null ? $"/service/estimates/{estimate.Id}" : $"{baseUrl}/service/estimates/{estimate.Id}";
        var pdfLink = baseUrl is null
            ? $"/api/backend/service/estimates/{estimate.Id}/pdf"
            : $"{baseUrl}/api/backend/service/estimates/{estimate.Id}/pdf";

        var statusText = estimate.Status == ServiceEstimateStatus.Approved ? "Approved" : "Draft";
        var body = $"Service estimate {estimate.Number} for job {job.Number}. Total {estimate.Total:0.00}. Status: {statusText}. " +
                   $"View: {estimateLink} PDF: {pdfLink}";

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            notificationService.EnqueueEmail(
                customer.Email!,
                subject: $"Service estimate {estimate.Number}",
                body: body,
                referenceType: ReferenceTypes.ServiceEstimate,
                referenceId: estimate.Id);
        }

        if (!string.IsNullOrWhiteSpace(customer.Phone))
        {
            notificationService.EnqueueSms(
                customer.Phone!,
                body: $"Estimate {estimate.Number} for job {job.Number}: {estimate.Total:0.00}. {estimateLink}",
                referenceType: ReferenceTypes.ServiceEstimate,
                referenceId: estimate.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectServiceEstimateAsync(Guid serviceEstimateId, CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates.FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        estimate.Reject();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateServiceHandoverAsync(
        Guid serviceJobId,
        string itemsReturned,
        int? postServiceWarrantyMonths,
        string? customerAcknowledgement,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var jobExists = await dbContext.ServiceJobs.AsNoTracking().AnyAsync(x => x.Id == serviceJobId, cancellationToken);
        if (!jobExists)
        {
            throw new NotFoundException("Service job not found.");
        }

        var number = await documentNumberService.NextAsync("SH", "SH", cancellationToken);
        var handover = new ServiceHandover(
            number,
            serviceJobId,
            clock.UtcNow,
            itemsReturned,
            postServiceWarrantyMonths,
            customerAcknowledgement,
            notes);

        await dbContext.ServiceHandovers.AddAsync(handover, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return handover.Id;
    }

    public async Task CompleteServiceHandoverAsync(Guid serviceHandoverId, CancellationToken cancellationToken = default)
    {
        var handover = await dbContext.ServiceHandovers.FirstOrDefaultAsync(x => x.Id == serviceHandoverId, cancellationToken)
            ?? throw new NotFoundException("Service handover not found.");

        handover.Complete();

        var job = await dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == handover.ServiceJobId, cancellationToken);
        if (job is not null)
        {
            var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.CustomerId, cancellationToken);
            var pickupMessage = $"Service job {job.Number} is ready for pickup. Handover {handover.Number}.";

            if (!string.IsNullOrWhiteSpace(customer?.Email))
            {
                notificationService.EnqueueEmail(
                    customer.Email!,
                    subject: $"Ready for pickup: {job.Number}",
                    body: $"{pickupMessage} Please contact support/service desk for delivery confirmation.",
                    referenceType: ReferenceTypes.ServiceHandover,
                    referenceId: handover.Id);
            }

            if (!string.IsNullOrWhiteSpace(customer?.Phone))
            {
                notificationService.EnqueueSms(
                    customer.Phone!,
                    body: pickupMessage,
                    referenceType: ReferenceTypes.ServiceHandover,
                    referenceId: handover.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelServiceHandoverAsync(Guid serviceHandoverId, CancellationToken cancellationToken = default)
    {
        var handover = await dbContext.ServiceHandovers.FirstOrDefaultAsync(x => x.Id == serviceHandoverId, cancellationToken)
            ?? throw new NotFoundException("Service handover not found.");

        handover.Cancel();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> ConvertServiceHandoverToSalesInvoiceAsync(
        Guid serviceHandoverId,
        Guid? serviceEstimateId,
        Guid? laborItemId,
        DateTimeOffset? dueDate,
        CancellationToken cancellationToken = default)
    {
        var handover = await dbContext.ServiceHandovers.FirstOrDefaultAsync(x => x.Id == serviceHandoverId, cancellationToken)
            ?? throw new NotFoundException("Service handover not found.");

        if (handover.SalesInvoiceId is { } existingInvoiceId)
        {
            return existingInvoiceId;
        }

        if (handover.Status != ServiceHandoverStatus.Completed)
        {
            throw new DomainValidationException("Only completed service handovers can be converted to sales invoice.");
        }

        var job = await dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == handover.ServiceJobId, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");

        IQueryable<ServiceEstimate> estimateQuery = dbContext.ServiceEstimates.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.ServiceJobId == job.Id && x.Status == ServiceEstimateStatus.Approved);

        if (serviceEstimateId is { } estimateId)
        {
            estimateQuery = estimateQuery.Where(x => x.Id == estimateId);
        }

        var estimate = await estimateQuery
            .OrderByDescending(x => x.IssuedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainValidationException("No approved service estimate found for handover conversion.");

        if (estimate.Lines.Count == 0)
        {
            throw new DomainValidationException("Approved estimate has no lines.");
        }

        var hasLabor = estimate.Lines.Any(x => x.Kind == ServiceEstimateLineKind.Labor);
        if (hasLabor && laborItemId is null)
        {
            throw new DomainValidationException("Labor item is required to convert labor estimate lines into sales invoice lines.");
        }

        if (laborItemId is not null)
        {
            var laborItemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == laborItemId.Value, cancellationToken);
            if (!laborItemExists)
            {
                throw new NotFoundException("Labor item not found.");
            }
        }

        var number = await documentNumberService.NextAsync(ReferenceTypes.SalesInvoice, "INV", cancellationToken);
        var invoice = new SalesInvoice(number, job.CustomerId, clock.UtcNow, dueDate);
        await dbContext.SalesInvoices.AddAsync(invoice, cancellationToken);

        foreach (var line in estimate.Lines)
        {
            var invoiceItemId = line.Kind switch
            {
                ServiceEstimateLineKind.Part => line.ItemId ?? throw new DomainValidationException("Part estimate line is missing item."),
                ServiceEstimateLineKind.Labor => laborItemId!.Value,
                _ => throw new DomainValidationException("Unsupported service estimate line kind.")
            };

            var invoiceLine = invoice.AddLine(
                invoiceItemId,
                line.Quantity,
                line.UnitPrice,
                discountPercent: 0m,
                taxPercent: line.TaxPercent);
            dbContext.DbContext.Add(invoiceLine);
        }

        handover.LinkSalesInvoice(invoice.Id, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return invoice.Id;
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

    public async Task UpdateMaterialRequisitionLineAsync(
        Guid materialRequisitionId,
        Guid lineId,
        decimal quantity,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken = default)
    {
        var mr = await dbContext.MaterialRequisitions.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == materialRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Material requisition not found.");

        if (!mr.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Material requisition line not found.");
        }

        mr.UpdateLine(lineId, quantity, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMaterialRequisitionLineAsync(Guid materialRequisitionId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var mr = await dbContext.MaterialRequisitions.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == materialRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Material requisition not found.");

        var line = mr.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Material requisition line not found.");

        mr.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
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
