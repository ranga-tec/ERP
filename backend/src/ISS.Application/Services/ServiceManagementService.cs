using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Finance;
using ISS.Domain.MasterData;
using ISS.Domain.Procurement;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class ServiceManagementService(
    IIssDbContext dbContext,
    IDocumentNumberService documentNumberService,
    IClock clock,
    InventoryService inventoryService,
    NotificationService notificationService,
    DocumentAccountMappingService documentAccountMappingService)
{
    public sealed record ServiceJobCloseoutCheck(string Key, string Label, bool IsClear, int PendingCount, string Detail);
    public sealed record ServiceInvoiceManualLineInput(
        Guid ItemId,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountPercent,
        decimal TaxPercent);

    private sealed record ServiceEntitlementSnapshot(
        Guid? ServiceContractId,
        ServiceEntitlementSource EntitlementSource,
        ServiceCoverageScope EntitlementCoverage,
        CustomerBillingTreatment CustomerBillingTreatment,
        string? EntitlementSummary);

    public async Task<Guid> CreateEquipmentUnitAsync(
        Guid itemId,
        string serialNumber,
        Guid customerId,
        DateTimeOffset? purchasedAt,
        DateTimeOffset? warrantyUntil,
        ServiceCoverageScope warrantyCoverage,
        int? serviceIntervalDays = null,
        DateTimeOffset? nextServiceDueAt = null,
        DateTimeOffset? nextRepairDueAt = null,
        CancellationToken cancellationToken = default)
    {
        var unit = new EquipmentUnit(itemId, serialNumber, customerId, purchasedAt, warrantyUntil, warrantyCoverage, serviceIntervalDays, nextServiceDueAt, nextRepairDueAt);
        await dbContext.EquipmentUnits.AddAsync(unit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return unit.Id;
    }

    public async Task UpdateEquipmentUnitAsync(
        Guid equipmentUnitId,
        Guid customerId,
        DateTimeOffset? purchasedAt,
        DateTimeOffset? warrantyUntil,
        ServiceCoverageScope warrantyCoverage,
        int? serviceIntervalDays = null,
        DateTimeOffset? nextServiceDueAt = null,
        DateTimeOffset? nextRepairDueAt = null,
        CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.EquipmentUnits
            .FirstOrDefaultAsync(x => x.Id == equipmentUnitId, cancellationToken)
            ?? throw new NotFoundException("Equipment unit not found.");

        unit.Update(customerId, purchasedAt, warrantyUntil, warrantyCoverage, serviceIntervalDays, nextServiceDueAt, nextRepairDueAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateServiceContractAsync(
        Guid customerId,
        Guid equipmentUnitId,
        ServiceContractType contractType,
        ServiceCoverageScope coverage,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var equipmentUnit = await dbContext.EquipmentUnits.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == equipmentUnitId, cancellationToken)
            ?? throw new NotFoundException("Equipment unit not found.");

        if (equipmentUnit.CustomerId != customerId)
        {
            throw new DomainValidationException("Service contracts must use the same customer that owns the equipment unit.");
        }

        var number = await documentNumberService.NextAsync(ReferenceTypes.ServiceContract, "SC", cancellationToken);
        var contract = new ServiceContract(number, customerId, equipmentUnitId, contractType, coverage, startDate, endDate, notes);
        await dbContext.ServiceContracts.AddAsync(contract, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return contract.Id;
    }

    public async Task UpdateServiceContractAsync(
        Guid serviceContractId,
        Guid customerId,
        Guid equipmentUnitId,
        ServiceContractType contractType,
        ServiceCoverageScope coverage,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? notes,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.ServiceContracts
            .FirstOrDefaultAsync(x => x.Id == serviceContractId, cancellationToken)
            ?? throw new NotFoundException("Service contract not found.");

        var equipmentUnit = await dbContext.EquipmentUnits.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == equipmentUnitId, cancellationToken)
            ?? throw new NotFoundException("Equipment unit not found.");

        if (equipmentUnit.CustomerId != customerId)
        {
            throw new DomainValidationException("Service contracts must use the same customer that owns the equipment unit.");
        }

        contract.Update(customerId, equipmentUnitId, contractType, coverage, startDate, endDate, notes, isActive);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateServiceJobAsync(
        Guid equipmentUnitId,
        Guid customerId,
        string problemDescription,
        ServiceJobKind kind = ServiceJobKind.Service,
        DateTimeOffset? expectedCompletionAt = null,
        string? siteLocation = null,
        DateTimeOffset? estimatedStartAt = null,
        string? jobDescription = null,
        string? customerComplaint = null,
        string? internalRemarks = null,
        string? responsibleOfficerName = null,
        CancellationToken cancellationToken = default)
    {
        var entitlement = await EvaluateServiceEntitlementAsync(equipmentUnitId, customerId, clock.UtcNow, cancellationToken);
        var number = await documentNumberService.NextAsync("SJ", "SJ", cancellationToken);
        var job = new ServiceJob(
            number,
            equipmentUnitId,
            customerId,
            clock.UtcNow,
            problemDescription,
            kind,
            entitlement.ServiceContractId,
            entitlement.EntitlementSource,
            entitlement.EntitlementCoverage,
            entitlement.CustomerBillingTreatment,
            clock.UtcNow,
            entitlement.EntitlementSummary,
            expectedCompletionAt,
            siteLocation,
            estimatedStartAt,
            jobDescription,
            customerComplaint,
            internalRemarks,
            responsibleOfficerName);
        await dbContext.ServiceJobs.AddAsync(job, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job.Id;
    }

    public async Task RefreshServiceJobEntitlementAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs
            .FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        var entitlement = await EvaluateServiceEntitlementAsync(job.EquipmentUnitId, job.CustomerId, clock.UtcNow, cancellationToken);
        job.ApplyEntitlement(
            entitlement.ServiceContractId,
            entitlement.EntitlementSource,
            entitlement.EntitlementCoverage,
            entitlement.CustomerBillingTreatment,
            clock.UtcNow,
            entitlement.EntitlementSummary);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateServiceJobAsync(
        Guid serviceJobId,
        Guid equipmentUnitId,
        Guid customerId,
        string problemDescription,
        ServiceJobKind kind,
        DateTimeOffset? expectedCompletionAt = null,
        string? siteLocation = null,
        DateTimeOffset? estimatedStartAt = null,
        string? jobDescription = null,
        string? customerComplaint = null,
        string? internalRemarks = null,
        string? responsibleOfficerName = null,
        CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs
            .FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        job.Update(
            equipmentUnitId,
            customerId,
            problemDescription,
            kind,
            expectedCompletionAt,
            siteLocation,
            estimatedStartAt,
            jobDescription,
            customerComplaint,
            internalRemarks,
            responsibleOfficerName);

        var entitlement = await EvaluateServiceEntitlementAsync(equipmentUnitId, customerId, clock.UtcNow, cancellationToken);
        job.ApplyEntitlement(
            entitlement.ServiceContractId,
            entitlement.EntitlementSource,
            entitlement.EntitlementCoverage,
            entitlement.CustomerBillingTreatment,
            clock.UtcNow,
            entitlement.EntitlementSummary);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task StartServiceJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs.FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");

        job.Start(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddServiceJobOperationAsync(
        Guid serviceJobId,
        int sequence,
        string name,
        string? description,
        Guid? plannedItemId,
        decimal plannedQuantity,
        decimal estimatedLaborHours,
        DateTimeOffset? requiredAt,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);
        await EnsurePlannedOperationItemExistsAsync(plannedItemId, cancellationToken);

        var operation = new ServiceJobOperation(
            serviceJobId,
            sequence,
            name,
            description,
            plannedItemId,
            plannedQuantity,
            estimatedLaborHours,
            requiredAt,
            notes);

        await dbContext.ServiceJobOperations.AddAsync(operation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return operation.Id;
    }

    public async Task UpdateServiceJobOperationAsync(
        Guid serviceJobId,
        Guid operationId,
        int sequence,
        string name,
        string? description,
        Guid? plannedItemId,
        decimal plannedQuantity,
        decimal estimatedLaborHours,
        DateTimeOffset? requiredAt,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);
        await EnsurePlannedOperationItemExistsAsync(plannedItemId, cancellationToken);

        var operation = await ResolveServiceJobOperationAsync(serviceJobId, operationId, cancellationToken);
        operation.Update(sequence, name, description, plannedItemId, plannedQuantity, estimatedLaborHours, requiredAt, notes);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task StartServiceJobOperationAsync(Guid serviceJobId, Guid operationId, CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var operation = await ResolveServiceJobOperationAsync(serviceJobId, operationId, cancellationToken);
        operation.Start(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteServiceJobOperationAsync(Guid serviceJobId, Guid operationId, CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var operation = await ResolveServiceJobOperationAsync(serviceJobId, operationId, cancellationToken);
        operation.Complete(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SkipServiceJobOperationAsync(Guid serviceJobId, Guid operationId, CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var operation = await ResolveServiceJobOperationAsync(serviceJobId, operationId, cancellationToken);
        operation.Skip();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveServiceJobOperationAsync(Guid serviceJobId, Guid operationId, CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var operation = await ResolveServiceJobOperationAsync(serviceJobId, operationId, cancellationToken);
        if (operation.Status == ServiceJobOperationStatus.Completed)
        {
            throw new DomainValidationException("Completed job operations cannot be deleted.");
        }

        dbContext.ServiceJobOperations.Remove(operation);
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

        await EnsureServiceJobReadyToCloseAsync(serviceJobId, cancellationToken);
        job.Close();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReopenServiceJobAsync(Guid serviceJobId, string? reason, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs.FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");

        job.Reopen(reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateServiceJobDailySheetAsync(
        Guid serviceJobId,
        DateTimeOffset? sheetDate,
        string preparedByName,
        string? siteLocation,
        string? shiftName,
        string? weatherOrSiteCondition,
        string workPlanned,
        string? workCompleted,
        string? workPending,
        string? problemsFound,
        string? customerInstructions,
        string? technicianNotes,
        string? supervisorNotes,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var number = await documentNumberService.NextAsync(ReferenceTypes.ServiceJobDailySheet, "JDS", cancellationToken);
        var dailySheet = new ServiceJobDailySheet(
            number,
            serviceJobId,
            sheetDate ?? clock.UtcNow,
            preparedByName,
            siteLocation,
            shiftName,
            weatherOrSiteCondition,
            workPlanned,
            workCompleted,
            workPending,
            problemsFound,
            customerInstructions,
            technicianNotes,
            supervisorNotes);

        await dbContext.ServiceJobDailySheets.AddAsync(dailySheet, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return dailySheet.Id;
    }

    public async Task SubmitServiceJobDailySheetAsync(Guid serviceJobId, Guid dailySheetId, CancellationToken cancellationToken = default)
    {
        var dailySheet = await ResolveDailySheetAsync(serviceJobId, dailySheetId, cancellationToken);
        dailySheet.Submit(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveServiceJobDailySheetAsync(Guid serviceJobId, Guid dailySheetId, CancellationToken cancellationToken = default)
    {
        var dailySheet = await ResolveDailySheetAsync(serviceJobId, dailySheetId, cancellationToken);
        dailySheet.Approve(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectServiceJobDailySheetAsync(Guid serviceJobId, Guid dailySheetId, string? reason, CancellationToken cancellationToken = default)
    {
        var dailySheet = await ResolveDailySheetAsync(serviceJobId, dailySheetId, cancellationToken);
        dailySheet.Reject(clock.UtcNow, reason);
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

        var estimate = await CreateDraftServiceEstimateInternalAsync(serviceJobId, validUntil, terms, cancellationToken);
        await dbContext.ServiceEstimates.AddAsync(estimate, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return estimate.Id;
    }

    public async Task UpdateServiceEstimateAsync(
        Guid serviceEstimateId,
        DateTimeOffset? validUntil,
        string? terms,
        CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates
            .FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        estimate.UpdateHeader(validUntil, terms);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateServiceExpenseClaimAsync(
        Guid serviceJobId,
        Guid? claimedByUserId,
        string claimedByName,
        ServiceExpenseFundingSource fundingSource,
        DateTimeOffset? expenseDate,
        string? merchantName,
        string? receiptReference,
        string? notes,
        Guid? serviceJobDailySheetId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);
        await EnsureDailySheetBelongsToJobAsync(serviceJobId, serviceJobDailySheetId, cancellationToken);

        var number = await documentNumberService.NextAsync(ReferenceTypes.ServiceExpenseClaim, "SEC", cancellationToken);
        var claim = new ServiceExpenseClaim(
            number,
            serviceJobId,
            claimedByUserId,
            claimedByName,
            fundingSource,
            expenseDate ?? clock.UtcNow,
            merchantName,
            receiptReference,
            notes,
            serviceJobDailySheetId);

        await dbContext.ServiceExpenseClaims.AddAsync(claim, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return claim.Id;
    }

    public async Task<Guid> ReviseServiceEstimateAsync(
        Guid sourceServiceEstimateId,
        DateTimeOffset? validUntil,
        string? terms,
        CancellationToken cancellationToken = default)
    {
        var sourceEstimate = await dbContext.ServiceEstimates
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == sourceServiceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        if (sourceEstimate.Status == ServiceEstimateStatus.Draft)
        {
            throw new DomainValidationException("Draft estimates can already be edited directly.");
        }

        var revisedEstimate = await CreateRevisedEstimateInternalAsync(sourceEstimate, validUntil, terms, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return revisedEstimate.Id;
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

        if (itemId is not null)
        {
            var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == itemId.Value, cancellationToken);
            if (!itemExists)
            {
                throw new NotFoundException("Item not found.");
            }
        }

        var pricedUnitPrice = await ApplyEstimateUnitPriceAsync(estimate.ServiceJobId, kind, unitPrice, cancellationToken);
        var line = estimate.AddLine(kind, itemId, description, quantity, pricedUnitPrice, taxPercent);
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

        if (itemId is not null)
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

        var pricedUnitPrice = await ApplyEstimateUnitPriceAsync(estimate.ServiceJobId, kind, unitPrice, cancellationToken);
        estimate.UpdateLine(lineId, kind, itemId, description, quantity, pricedUnitPrice, taxPercent);
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

        estimate.Approve(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SendServiceEstimateToCustomerAsync(Guid serviceEstimateId, string? appBaseUrl = null, CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        if (estimate.Status != ServiceEstimateStatus.Draft)
        {
            throw new DomainValidationException("Only draft estimates can be sent to customer.");
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

        estimate.MarkSentToCustomer(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectServiceEstimateAsync(Guid serviceEstimateId, CancellationToken cancellationToken = default)
    {
        var estimate = await dbContext.ServiceEstimates.FirstOrDefaultAsync(x => x.Id == serviceEstimateId, cancellationToken)
            ?? throw new NotFoundException("Service estimate not found.");

        estimate.Reject(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddServiceExpenseClaimLineAsync(
        Guid serviceExpenseClaimId,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitCost,
        bool billableToCustomer,
        CancellationToken cancellationToken = default)
    {
        var claim = await dbContext.ServiceExpenseClaims.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceExpenseClaimId, cancellationToken)
            ?? throw new NotFoundException("Service expense claim not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(claim.ServiceJobId, cancellationToken);

        if (itemId is { } itemRef)
        {
            var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == itemRef, cancellationToken);
            if (!itemExists)
            {
                throw new NotFoundException("Item not found.");
            }
        }

        var expenseAccountId = await documentAccountMappingService.ResolveExpenseAccountIdAsync(itemId, cancellationToken);
        var line = claim.AddLine(itemId, description, quantity, unitCost, billableToCustomer, expenseAccountId);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateServiceExpenseClaimLineAsync(
        Guid serviceExpenseClaimId,
        Guid lineId,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitCost,
        bool billableToCustomer,
        CancellationToken cancellationToken = default)
    {
        var claim = await dbContext.ServiceExpenseClaims.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceExpenseClaimId, cancellationToken)
            ?? throw new NotFoundException("Service expense claim not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(claim.ServiceJobId, cancellationToken);

        if (itemId is { } itemRef)
        {
            var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == itemRef, cancellationToken);
            if (!itemExists)
            {
                throw new NotFoundException("Item not found.");
            }
        }

        if (!claim.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Service expense claim line not found.");
        }

        var expenseAccountId = await documentAccountMappingService.ResolveExpenseAccountIdAsync(itemId, cancellationToken);
        claim.UpdateLine(lineId, itemId, description, quantity, unitCost, billableToCustomer, expenseAccountId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveServiceExpenseClaimLineAsync(Guid serviceExpenseClaimId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var claim = await dbContext.ServiceExpenseClaims.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceExpenseClaimId, cancellationToken)
            ?? throw new NotFoundException("Service expense claim not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(claim.ServiceJobId, cancellationToken);

        var line = claim.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Service expense claim line not found.");

        claim.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitServiceExpenseClaimAsync(Guid serviceExpenseClaimId, CancellationToken cancellationToken = default)
    {
        var claim = await dbContext.ServiceExpenseClaims.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceExpenseClaimId, cancellationToken)
            ?? throw new NotFoundException("Service expense claim not found.");

        await RefreshServiceExpenseClaimAccountsAsync(claim, cancellationToken);
        claim.Submit(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveServiceExpenseClaimAsync(Guid serviceExpenseClaimId, CancellationToken cancellationToken = default)
    {
        var claim = await dbContext.ServiceExpenseClaims
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceExpenseClaimId, cancellationToken)
            ?? throw new NotFoundException("Service expense claim not found.");

        await RefreshServiceExpenseClaimAccountsAsync(claim, cancellationToken);
        claim.Approve(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectServiceExpenseClaimAsync(
        Guid serviceExpenseClaimId,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var claim = await dbContext.ServiceExpenseClaims
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceExpenseClaimId, cancellationToken)
            ?? throw new NotFoundException("Service expense claim not found.");

        claim.Reject(clock.UtcNow, rejectionReason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SettleServiceExpenseClaimAsync(
        Guid serviceExpenseClaimId,
        Guid? settlementPaymentTypeId,
        Guid? settlementPettyCashFundId,
        string? settlementReference,
        CancellationToken cancellationToken = default)
    {
        var claim = await dbContext.ServiceExpenseClaims
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceExpenseClaimId, cancellationToken)
            ?? throw new NotFoundException("Service expense claim not found.");

        if (settlementPaymentTypeId is { } paymentTypeId)
        {
            var paymentTypeExists = await dbContext.PaymentTypes.AsNoTracking().AnyAsync(x => x.Id == paymentTypeId, cancellationToken);
            if (!paymentTypeExists)
            {
                throw new NotFoundException("Payment type not found.");
            }
        }

        PettyCashFund? pettyCashFund = null;
        if (settlementPettyCashFundId is { } pettyCashFundId)
        {
            pettyCashFund = await dbContext.PettyCashFunds
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == pettyCashFundId, cancellationToken)
                ?? throw new NotFoundException("Petty cash fund not found.");
        }

        claim.Settle(clock.UtcNow, settlementPaymentTypeId, settlementPettyCashFundId, settlementReference);

        if (pettyCashFund is not null)
        {
            var transaction = pettyCashFund.RecordExpenseSettlement(
                claim.Total,
                clock.UtcNow,
                claim.Id,
                settlementReference ?? claim.Number,
                notes: $"Expense claim {claim.Number} settled.");
            dbContext.DbContext.Add(transaction);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(Guid ServiceEstimateId, int AddedLineCount)> ConvertBillableExpenseClaimToEstimateAsync(
        Guid serviceExpenseClaimId,
        Guid? serviceEstimateId,
        decimal taxPercent,
        DateTimeOffset? validUntil,
        string? terms,
        CancellationToken cancellationToken = default)
    {
        if (taxPercent < 0m)
        {
            throw new DomainValidationException("Tax percent cannot be negative.");
        }

        var claim = await dbContext.ServiceExpenseClaims
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == serviceExpenseClaimId, cancellationToken)
            ?? throw new NotFoundException("Service expense claim not found.");

        if (claim.Status is not (ServiceExpenseClaimStatus.Approved or ServiceExpenseClaimStatus.Settled))
        {
            throw new DomainValidationException("Only approved or settled expense claims can be converted to a service estimate.");
        }

        var linesToConvert = claim.Lines
            .Where(x => x.BillableToCustomer && x.ConvertedToServiceEstimateLineId is null)
            .ToList();

        if (linesToConvert.Count == 0)
        {
            throw new DomainValidationException("No unconverted billable lines are available on this expense claim.");
        }

        var estimate = await ResolveDraftEstimateForExpenseConversionAsync(
            claim.ServiceJobId,
            serviceEstimateId,
            validUntil,
            terms,
            cancellationToken);

        var claimItemIds = linesToConvert
            .Where(x => x.ItemId.HasValue)
            .Select(x => x.ItemId!.Value)
            .Distinct()
            .ToList();

        var itemTypeById = claimItemIds.Count > 0
            ? await dbContext.Items.AsNoTracking()
                .Where(x => claimItemIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Type, cancellationToken)
            : new Dictionary<Guid, ItemType>();

        foreach (var line in linesToConvert)
        {
            var lineKind = line.ItemId is { } itemId && itemTypeById.TryGetValue(itemId, out var itemType) && itemType == ItemType.SparePart
                ? ServiceEstimateLineKind.Part
                : ServiceEstimateLineKind.Expense;
            var pricedUnitPrice = await ApplyEstimateUnitPriceAsync(claim.ServiceJobId, lineKind, line.UnitCost, cancellationToken);
            var estimateLine = estimate.AddLine(
                lineKind,
                line.ItemId,
                line.Description,
                line.Quantity,
                pricedUnitPrice,
                taxPercent);
            dbContext.DbContext.Add(estimateLine);
            line.MarkConvertedToEstimate(estimate.Id, estimateLine.Id, clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (estimate.Id, linesToConvert.Count);
    }

    internal async Task RefreshServiceExpenseClaimAccountsAsync(ServiceExpenseClaim claim, CancellationToken cancellationToken)
    {
        var expenseAccountsByItemId = await documentAccountMappingService.ResolveForItemsAsync(
            claim.Lines
                .Where(line => line.ItemId is not null)
                .Select(line => line.ItemId!.Value),
            cancellationToken);

        foreach (var line in claim.Lines)
        {
            line.AssignExpenseAccount(
                line.ItemId is { } itemId && expenseAccountsByItemId.TryGetValue(itemId, out var mapping)
                    ? mapping.ExpenseAccountId
                    : null);
        }
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

    public async Task UpdateServiceHandoverAsync(
        Guid serviceHandoverId,
        string itemsReturned,
        int? postServiceWarrantyMonths,
        string? customerAcknowledgement,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var handover = await dbContext.ServiceHandovers.FirstOrDefaultAsync(x => x.Id == serviceHandoverId, cancellationToken)
            ?? throw new NotFoundException("Service handover not found.");

        handover.UpdateDraft(itemsReturned, postServiceWarrantyMonths, customerAcknowledgement, notes);
        await dbContext.SaveChangesAsync(cancellationToken);
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
        Guid? expenseItemId,
        ServiceLaborBillingSource laborBillingSource,
        DateTimeOffset? dueDate,
        IReadOnlyCollection<ServiceInvoiceManualLineInput>? manualLines = null,
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

        var directLines = (manualLines ?? Array.Empty<ServiceInvoiceManualLineInput>())
            .Where(line => line.ItemId != Guid.Empty)
            .ToList();

        var approvedBillableTimeEntries = await dbContext.WorkOrderTimeEntries
            .Where(x => x.ServiceJobId == job.Id
                        && x.Status == WorkOrderTimeEntryStatus.Approved
                        && x.BillableToCustomer
                        && x.SalesInvoiceLineId == null)
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var useApprovedTimeEntriesForLabor = laborBillingSource switch
        {
            ServiceLaborBillingSource.ApprovedTimeEntries => true,
            ServiceLaborBillingSource.Estimate => false,
            _ => approvedBillableTimeEntries.Count > 0
        };

        if (laborBillingSource == ServiceLaborBillingSource.ApprovedTimeEntries
            && approvedBillableTimeEntries.Count == 0)
        {
            throw new DomainValidationException("No approved uninvoiced labor entries are available for billing.");
        }

        ServiceEstimate? estimate = null;
        if (directLines.Count == 0 || serviceEstimateId is not null)
        {
            IQueryable<ServiceEstimate> estimateQuery = dbContext.ServiceEstimates.AsNoTracking()
                .Include(x => x.Lines)
                .Where(x => x.ServiceJobId == job.Id && x.Status == ServiceEstimateStatus.Approved);

            if (serviceEstimateId is { } estimateId)
            {
                estimateQuery = estimateQuery.Where(x => x.Id == estimateId);
            }

            estimate = await estimateQuery
                .OrderByDescending(x => x.IssuedAt)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (estimate is null && directLines.Count == 0 && !useApprovedTimeEntriesForLabor)
            {
                throw new DomainValidationException("No approved service estimate or manual invoice lines found for handover conversion.");
            }

            if (estimate is not null && estimate.Lines.Count == 0)
            {
                throw new DomainValidationException("Approved estimate has no lines.");
            }
        }

        var hasEstimateLabor = estimate?.Lines.Any(x => x.Kind == ServiceEstimateLineKind.Labor) == true;
        if ((hasEstimateLabor || useApprovedTimeEntriesForLabor) && laborItemId is null)
        {
            throw new DomainValidationException(
                useApprovedTimeEntriesForLabor
                    ? "Labor item is required to convert approved labor timesheets into sales invoice lines."
                    : "Labor item is required to convert labor estimate lines into sales invoice lines.");
        }

        var hasExpenseWithoutItem = estimate?.Lines.Any(x => x.Kind == ServiceEstimateLineKind.Expense && x.ItemId is null) == true;
        if (hasExpenseWithoutItem && expenseItemId is null)
        {
            throw new DomainValidationException("Expense item is required to convert expense estimate lines without an item into sales invoice lines.");
        }

        if (laborItemId is not null)
        {
            var laborItemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == laborItemId.Value, cancellationToken);
            if (!laborItemExists)
            {
                throw new NotFoundException("Labor item not found.");
            }
        }

        if (expenseItemId is not null)
        {
            var expenseItemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == expenseItemId.Value, cancellationToken);
            if (!expenseItemExists)
            {
                throw new NotFoundException("Expense item not found.");
            }
        }

        var number = await documentNumberService.NextAsync(ReferenceTypes.SalesInvoice, "INV", cancellationToken);
        var invoice = new SalesInvoice(number, job.CustomerId, clock.UtcNow, dueDate);
        await dbContext.SalesInvoices.AddAsync(invoice, cancellationToken);

        foreach (var line in directLines)
        {
            var revenueAccountId = await documentAccountMappingService.ResolveRevenueAccountIdAsync(line.ItemId, cancellationToken);
            var invoiceLine = invoice.AddLine(
                line.ItemId,
                line.Quantity,
                line.UnitPrice,
                line.DiscountPercent,
                line.TaxPercent,
                revenueAccountId);
            dbContext.DbContext.Add(invoiceLine);
        }

        foreach (var line in estimate?.Lines ?? [])
        {
            if (line.Kind == ServiceEstimateLineKind.Labor && useApprovedTimeEntriesForLabor)
            {
                continue;
            }

            var invoiceItemId = line.Kind switch
            {
                ServiceEstimateLineKind.Part => line.ItemId ?? throw new DomainValidationException("Part estimate line is missing item."),
                ServiceEstimateLineKind.Labor => laborItemId!.Value,
                ServiceEstimateLineKind.Expense => line.ItemId ?? expenseItemId!.Value,
                _ => throw new DomainValidationException("Unsupported service estimate line kind.")
            };
            var revenueAccountId = await documentAccountMappingService.ResolveRevenueAccountIdAsync(invoiceItemId, cancellationToken);

            var invoiceLine = invoice.AddLine(
                invoiceItemId,
                line.Quantity,
                ServiceEntitlementRules.ApplyEstimateUnitPrice(job.EntitlementCoverage, line.Kind, line.UnitPrice),
                discountPercent: 0m,
                taxPercent: line.TaxPercent,
                revenueAccountId: revenueAccountId);
            dbContext.DbContext.Add(invoiceLine);
        }

        if (useApprovedTimeEntriesForLabor)
        {
            foreach (var timeEntry in approvedBillableTimeEntries)
            {
                var revenueAccountId = await documentAccountMappingService.ResolveRevenueAccountIdAsync(laborItemId!.Value, cancellationToken);
                var invoiceLine = invoice.AddLine(
                    laborItemId!.Value,
                    timeEntry.BillableHours,
                    ServiceEntitlementRules.ApplyEstimateUnitPrice(job.EntitlementCoverage, ServiceEstimateLineKind.Labor, timeEntry.BillingRate),
                    discountPercent: 0m,
                    taxPercent: timeEntry.TaxPercent,
                    revenueAccountId: revenueAccountId);
                dbContext.DbContext.Add(invoiceLine);
                timeEntry.MarkInvoiced(invoice.Id, invoiceLine.Id, clock.UtcNow);
            }
        }

        handover.LinkSalesInvoice(invoice.Id, clock.UtcNow);
        var invoiceJob = await dbContext.ServiceJobs.FirstOrDefaultAsync(x => x.Id == job.Id, cancellationToken)
            ?? throw new NotFoundException("Service job not found.");
        invoiceJob.MarkInvoiced();
        await dbContext.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    public async Task MarkServiceJobFinalInvoiceNotRequiredAsync(Guid serviceJobId, string reason, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ServiceJobs.FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        job.MarkFinalInvoiceNotRequired(reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ServiceEstimate> ResolveDraftEstimateForExpenseConversionAsync(
        Guid serviceJobId,
        Guid? preferredEstimateId,
        DateTimeOffset? validUntil,
        string? terms,
        CancellationToken cancellationToken)
    {
        if (preferredEstimateId is { } estimateId)
        {
            var selectedEstimate = await dbContext.ServiceEstimates
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == estimateId, cancellationToken)
                ?? throw new NotFoundException("Service estimate not found.");

            if (selectedEstimate.ServiceJobId != serviceJobId)
            {
                throw new DomainValidationException("Selected service estimate does not belong to this service job.");
            }

            return selectedEstimate.Status == ServiceEstimateStatus.Draft
                ? selectedEstimate
                : await CreateRevisedEstimateInternalAsync(selectedEstimate, validUntil, terms, cancellationToken);
        }

        var latestDraft = await dbContext.ServiceEstimates
            .Include(x => x.Lines)
            .Where(x => x.ServiceJobId == serviceJobId && x.Status == ServiceEstimateStatus.Draft)
            .OrderByDescending(x => x.RevisionNumber)
            .ThenByDescending(x => x.IssuedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestDraft is not null)
        {
            return latestDraft;
        }

        var latestExisting = await dbContext.ServiceEstimates
            .Include(x => x.Lines)
            .Where(x => x.ServiceJobId == serviceJobId)
            .OrderByDescending(x => x.RevisionNumber)
            .ThenByDescending(x => x.IssuedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestExisting is not null)
        {
            return await CreateRevisedEstimateInternalAsync(latestExisting, validUntil, terms, cancellationToken);
        }

        var estimate = await CreateDraftServiceEstimateInternalAsync(serviceJobId, validUntil, terms, cancellationToken);
        await dbContext.ServiceEstimates.AddAsync(estimate, cancellationToken);
        return estimate;
    }

    private async Task<ServiceEstimate> CreateDraftServiceEstimateInternalAsync(
        Guid serviceJobId,
        DateTimeOffset? validUntil,
        string? terms,
        CancellationToken cancellationToken)
    {
        var number = await documentNumberService.NextAsync("SE", "SE", cancellationToken);
        return new ServiceEstimate(number, serviceJobId, clock.UtcNow, validUntil, terms);
    }

    private async Task<ServiceEstimate> CreateRevisedEstimateInternalAsync(
        ServiceEstimate sourceEstimate,
        DateTimeOffset? validUntil,
        string? terms,
        CancellationToken cancellationToken)
    {
        var number = await documentNumberService.NextAsync("SE", "SE", cancellationToken);
        var revisedEstimate = new ServiceEstimate(
            number,
            sourceEstimate.ServiceJobId,
            clock.UtcNow,
            validUntil ?? sourceEstimate.ValidUntil,
            string.IsNullOrWhiteSpace(terms) ? sourceEstimate.Terms : terms,
            revisedFromEstimateId: sourceEstimate.Id,
            revisionNumber: sourceEstimate.RevisionNumber + 1);

        await dbContext.ServiceEstimates.AddAsync(revisedEstimate, cancellationToken);

        foreach (var line in sourceEstimate.Lines)
        {
            var pricedUnitPrice = await ApplyEstimateUnitPriceAsync(
                sourceEstimate.ServiceJobId,
                line.Kind,
                line.UnitPrice,
                cancellationToken);
            var copiedLine = revisedEstimate.AddLine(
                line.Kind,
                line.ItemId,
                line.Description,
                line.Quantity,
                pricedUnitPrice,
                line.TaxPercent);
            dbContext.DbContext.Add(copiedLine);
        }

        return revisedEstimate;
    }

    public async Task<Guid> CreateWorkOrderAsync(Guid serviceJobId, string description, Guid? assignedToUserId, CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var workOrder = new WorkOrder(serviceJobId, description, assignedToUserId);
        await dbContext.WorkOrders.AddAsync(workOrder, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return workOrder.Id;
    }

    public async Task AddWorkOrderTimeEntryAsync(
        Guid workOrderId,
        Guid? technicianUserId,
        string technicianName,
        DateTimeOffset? workDate,
        string workDescription,
        decimal hoursWorked,
        decimal costRate,
        bool billableToCustomer,
        decimal? billableHours,
        decimal? billingRate,
        decimal? taxPercent,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await dbContext.WorkOrders
            .Include(x => x.TimeEntries)
            .FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(workOrder.ServiceJobId, cancellationToken);

        var entry = workOrder.AddTimeEntry(
            technicianUserId,
            technicianName,
            workDate ?? clock.UtcNow,
            workDescription,
            hoursWorked,
            costRate,
            billableToCustomer,
            billableToCustomer ? billableHours ?? hoursWorked : 0m,
            billableToCustomer ? billingRate ?? 0m : 0m,
            billableToCustomer ? taxPercent ?? 0m : 0m,
            notes);

        dbContext.DbContext.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateWorkOrderTimeEntryAsync(
        Guid workOrderId,
        Guid timeEntryId,
        Guid? technicianUserId,
        string technicianName,
        DateTimeOffset? workDate,
        string workDescription,
        decimal hoursWorked,
        decimal costRate,
        bool billableToCustomer,
        decimal? billableHours,
        decimal? billingRate,
        decimal? taxPercent,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await dbContext.WorkOrders
            .Include(x => x.TimeEntries)
            .FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(workOrder.ServiceJobId, cancellationToken);

        if (!workOrder.TimeEntries.Any(x => x.Id == timeEntryId))
        {
            throw new NotFoundException("Work-order labor entry not found.");
        }

        workOrder.UpdateTimeEntry(
            timeEntryId,
            technicianUserId,
            technicianName,
            workDate ?? clock.UtcNow,
            workDescription,
            hoursWorked,
            costRate,
            billableToCustomer,
            billableToCustomer ? billableHours ?? hoursWorked : 0m,
            billableToCustomer ? billingRate ?? 0m : 0m,
            billableToCustomer ? taxPercent ?? 0m : 0m,
            notes);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveWorkOrderTimeEntryAsync(Guid workOrderId, Guid timeEntryId, CancellationToken cancellationToken = default)
    {
        var workOrder = await dbContext.WorkOrders
            .Include(x => x.TimeEntries)
            .FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        var timeEntry = workOrder.TimeEntries.FirstOrDefault(x => x.Id == timeEntryId)
            ?? throw new NotFoundException("Work-order labor entry not found.");

        workOrder.RemoveTimeEntry(timeEntryId);
        dbContext.DbContext.Remove(timeEntry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitWorkOrderTimeEntryAsync(Guid workOrderId, Guid timeEntryId, CancellationToken cancellationToken = default)
    {
        var timeEntry = await dbContext.WorkOrderTimeEntries
            .FirstOrDefaultAsync(x => x.WorkOrderId == workOrderId && x.Id == timeEntryId, cancellationToken)
            ?? throw new NotFoundException("Work-order labor entry not found.");

        timeEntry.Submit(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveWorkOrderTimeEntryAsync(Guid workOrderId, Guid timeEntryId, CancellationToken cancellationToken = default)
    {
        var timeEntry = await dbContext.WorkOrderTimeEntries
            .FirstOrDefaultAsync(x => x.WorkOrderId == workOrderId && x.Id == timeEntryId, cancellationToken)
            ?? throw new NotFoundException("Work-order labor entry not found.");

        timeEntry.Approve(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectWorkOrderTimeEntryAsync(
        Guid workOrderId,
        Guid timeEntryId,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var timeEntry = await dbContext.WorkOrderTimeEntries
            .FirstOrDefaultAsync(x => x.WorkOrderId == workOrderId && x.Id == timeEntryId, cancellationToken)
            ?? throw new NotFoundException("Work-order labor entry not found.");

        timeEntry.Reject(clock.UtcNow, rejectionReason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task StartWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await dbContext.WorkOrders.FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(workOrder.ServiceJobId, cancellationToken);
        workOrder.Start();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkWorkOrderDoneAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await dbContext.WorkOrders.FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(workOrder.ServiceJobId, cancellationToken);
        workOrder.MarkDone();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await dbContext.WorkOrders.FirstOrDefaultAsync(x => x.Id == workOrderId, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(workOrder.ServiceJobId, cancellationToken);
        workOrder.Cancel();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddServiceJobAssignmentAsync(
        Guid serviceJobId,
        Guid? technicianId,
        string? employeeName,
        string role,
        string assignedTask,
        DateTimeOffset? assignedDate,
        DateTimeOffset? workStartAt,
        DateTimeOffset? workEndAt,
        decimal normalHours,
        decimal overtimeHours,
        string? dailyWorkDescription,
        Guid? serviceJobDailySheetId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);
        await EnsureDailySheetBelongsToJobAsync(serviceJobId, serviceJobDailySheetId, cancellationToken);

        string resolvedEmployeeName;
        if (technicianId is not null)
        {
            var technician = await dbContext.ServiceTechnicians.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == technicianId.Value, cancellationToken)
                ?? throw new NotFoundException("Technician not found.");
            if (!technician.IsActive)
            {
                throw new DomainValidationException("Inactive technicians cannot be assigned to service jobs.");
            }

            resolvedEmployeeName = technician.Name;
        }
        else
        {
            resolvedEmployeeName = Guard.NotNullOrWhiteSpace(employeeName, nameof(employeeName), 256);
        }

        var assignment = new ServiceJobAssignment(
            serviceJobId,
            technicianId,
            resolvedEmployeeName,
            role,
            assignedTask,
            assignedDate ?? clock.UtcNow,
            workStartAt,
            workEndAt,
            normalHours,
            overtimeHours,
            dailyWorkDescription,
            serviceJobDailySheetId);

        await dbContext.ServiceJobAssignments.AddAsync(assignment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return assignment.Id;
    }

    public async Task ApproveServiceJobAssignmentAsync(Guid serviceJobId, Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.ServiceJobAssignments
            .FirstOrDefaultAsync(x => x.ServiceJobId == serviceJobId && x.Id == assignmentId, cancellationToken)
            ?? throw new NotFoundException("Service job assignment not found.");

        assignment.Approve(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectServiceJobAssignmentAsync(Guid serviceJobId, Guid assignmentId, string? reason, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.ServiceJobAssignments
            .FirstOrDefaultAsync(x => x.ServiceJobId == serviceJobId && x.Id == assignmentId, cancellationToken)
            ?? throw new NotFoundException("Service job assignment not found.");

        assignment.Reject(clock.UtcNow, reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveServiceJobAssignmentAsync(Guid serviceJobId, Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.ServiceJobAssignments
            .FirstOrDefaultAsync(x => x.ServiceJobId == serviceJobId && x.Id == assignmentId, cancellationToken)
            ?? throw new NotFoundException("Service job assignment not found.");

        if (assignment.ApprovalStatus == ServiceJobAssignmentApprovalStatus.Approved)
        {
            throw new DomainValidationException("Approved service job assignments cannot be deleted.");
        }

        dbContext.ServiceJobAssignments.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddServiceJobProgressUpdateAsync(
        Guid serviceJobId,
        DateTimeOffset? progressDate,
        string workCompleted,
        string? workPending,
        string? problemsFound,
        string? additionalPartsRequired,
        string? additionalLaborRequired,
        string? customerInstructions,
        string? siteIssues,
        string? technicianNotes,
        string? supervisorNotes,
        Guid? serviceJobDailySheetId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);
        await EnsureDailySheetBelongsToJobAsync(serviceJobId, serviceJobDailySheetId, cancellationToken);

        var update = new ServiceJobProgressUpdate(
            serviceJobId,
            progressDate ?? clock.UtcNow,
            workCompleted,
            workPending,
            problemsFound,
            additionalPartsRequired,
            additionalLaborRequired,
            customerInstructions,
            siteIssues,
            technicianNotes,
            supervisorNotes,
            serviceJobDailySheetId);

        await dbContext.ServiceJobProgressUpdates.AddAsync(update, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return update.Id;
    }

    public async Task<Guid> CreateMaterialRequisitionAsync(Guid serviceJobId, Guid warehouseId, string? purpose = null, Guid? serviceJobDailySheetId = null, CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);
        await EnsureDailySheetBelongsToJobAsync(serviceJobId, serviceJobDailySheetId, cancellationToken);
        var number = await documentNumberService.NextAsync("MR", "MR", cancellationToken);
        var mr = new MaterialRequisition(number, serviceJobId, warehouseId, clock.UtcNow, purpose, serviceJobDailySheetId);
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

        await EnsureServiceJobAcceptsNewCostsAsync(mr.ServiceJobId, cancellationToken);

        await EnsureMaterialRequisitionLineAvailabilityAsync(mr, itemId, lineId: null, quantity, batchNumber, serialNumbers, cancellationToken);

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

        await EnsureServiceJobAcceptsNewCostsAsync(mr.ServiceJobId, cancellationToken);

        if (!mr.Lines.Any(x => x.Id == lineId))
        {
            throw new NotFoundException("Material requisition line not found.");
        }

        var line = mr.Lines.First(x => x.Id == lineId);
        await EnsureMaterialRequisitionLineAvailabilityAsync(mr, line.ItemId, lineId, quantity, batchNumber, serialNumbers, cancellationToken);

        mr.UpdateLine(lineId, quantity, batchNumber, serialNumbers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMaterialRequisitionLineAsync(Guid materialRequisitionId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var mr = await dbContext.MaterialRequisitions.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == materialRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Material requisition not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(mr.ServiceJobId, cancellationToken);

        var line = mr.Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("Material requisition line not found.");

        mr.RemoveLine(lineId);
        dbContext.DbContext.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureMaterialRequisitionLineAvailabilityAsync(
        MaterialRequisition mr,
        Guid itemId,
        Guid? lineId,
        decimal quantity,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
            ?? throw new DomainValidationException("Invalid item on material requisition.");

        if (item.TrackingType == TrackingType.Serial)
        {
            await inventoryService.EnsureAvailableForIssueAsync(mr.WarehouseId, item, quantity, batchNumber, serialNumbers, cancellationToken);

            var requestedSerials = serialNumbers?
                .Select(serial => serial.Trim())
                .Where(serial => serial.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

            var duplicateSerial = mr.Lines
                .Where(line => line.Id != lineId)
                .SelectMany(line => line.Serials)
                .Select(serial => serial.SerialNumber)
                .FirstOrDefault(requestedSerials.Contains);

            if (duplicateSerial is not null)
            {
                throw new DomainValidationException($"Serial '{duplicateSerial}' is already selected on this material requisition.");
            }

            return;
        }

        var normalizedBatch = string.IsNullOrWhiteSpace(batchNumber) ? null : batchNumber.Trim();
        var existingQuantity = mr.Lines
            .Where(line => line.Id != lineId && line.ItemId == itemId && MaterialRequisitionBatchMatches(line.BatchNumber, normalizedBatch))
            .Sum(line => line.Quantity);

        await inventoryService.EnsureAvailableForIssueAsync(
            mr.WarehouseId,
            item,
            existingQuantity + quantity,
            normalizedBatch,
            serialNumbers,
            cancellationToken);
    }

    private static bool MaterialRequisitionBatchMatches(string? existingBatch, string? requestedBatch)
        => string.IsNullOrWhiteSpace(requestedBatch)
            || string.Equals(existingBatch?.Trim(), requestedBatch, StringComparison.OrdinalIgnoreCase);

    public async Task PostMaterialRequisitionAsync(Guid materialRequisitionId, CancellationToken cancellationToken = default)
    {
        var mr = await dbContext.MaterialRequisitions.Include(x => x.Lines).ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == materialRequisitionId, cancellationToken)
                 ?? throw new NotFoundException("Material requisition not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(mr.ServiceJobId, cancellationToken);

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

    public async Task VoidMaterialRequisitionAsync(Guid materialRequisitionId, CancellationToken cancellationToken = default)
    {
        var mr = await dbContext.MaterialRequisitions.FirstOrDefaultAsync(x => x.Id == materialRequisitionId, cancellationToken)
            ?? throw new NotFoundException("Material requisition not found.");

        await EnsureServiceJobAcceptsNewCostsAsync(mr.ServiceJobId, cancellationToken);
        mr.Void();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddServiceJobMaterialDispositionAsync(
        Guid serviceJobId,
        Guid materialRequisitionLineId,
        ServiceJobMaterialDispositionKind kind,
        decimal quantity,
        string? condition,
        string reason,
        ServiceJobMaterialChargeTo chargeTo,
        Guid? supplierReturnId,
        string? responsiblePerson,
        IReadOnlyCollection<string>? serialNumbers,
        Guid? serviceJobDailySheetId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);
        await EnsureDailySheetBelongsToJobAsync(serviceJobId, serviceJobDailySheetId, cancellationToken);

        var line = await dbContext.Set<MaterialRequisitionLine>()
            .Include(x => x.Serials)
            .FirstOrDefaultAsync(x => x.Id == materialRequisitionLineId, cancellationToken)
            ?? throw new NotFoundException("Material requisition line not found.");

        var mr = await dbContext.MaterialRequisitions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == line.MaterialRequisitionId, cancellationToken)
            ?? throw new NotFoundException("Material requisition not found.");

        if (mr.ServiceJobId != serviceJobId)
        {
            throw new DomainValidationException("Material requisition line does not belong to this service job.");
        }

        if (mr.Status != MaterialRequisitionStatus.Posted)
        {
            throw new DomainValidationException("Only posted material requisition lines can receive material disposition.");
        }

        var item = await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == line.ItemId, cancellationToken)
            ?? throw new DomainValidationException("Invalid item on material disposition.");

        var alreadyDisposed = await dbContext.ServiceJobMaterialDispositions.AsNoTracking()
            .Where(x => x.MaterialRequisitionLineId == materialRequisitionLineId)
            .Where(x => !x.IsVoided)
            .SumAsync(x => (decimal?)x.Quantity, cancellationToken) ?? 0m;
        if (alreadyDisposed + quantity > line.Quantity)
        {
            throw new DomainValidationException($"Material disposition exceeds issued quantity. Remaining quantity is {line.Quantity - alreadyDisposed}.");
        }

        if (kind == ServiceJobMaterialDispositionKind.RejectedSupplierReturn && supplierReturnId is not null)
        {
            var supplierReturnExists = await dbContext.SupplierReturns.AsNoTracking().AnyAsync(x => x.Id == supplierReturnId.Value, cancellationToken);
            if (!supplierReturnExists)
            {
                throw new NotFoundException("Supplier return not found.");
            }
        }

        var disposition = new ServiceJobMaterialDisposition(
            serviceJobId,
            mr.Id,
            line.Id,
            line.ItemId,
            mr.WarehouseId,
            kind,
            quantity,
            item.DefaultUnitCost,
            line.BatchNumber,
            condition ?? kind.ToString(),
            reason,
            chargeTo,
            supplierReturnId,
            responsiblePerson,
            serviceJobDailySheetId);
        disposition.ReplaceSerials(serialNumbers);

        if (item.TrackingType == TrackingType.Serial)
        {
            var requestedSerials = serialNumbers?
                .Select(serial => serial.Trim())
                .Where(serial => serial.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            if (requestedSerials.Count != quantity)
            {
                throw new DomainValidationException("Serial count must match disposition quantity.");
            }

            var issuedSerials = line.Serials.Select(x => x.SerialNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var invalidSerial = requestedSerials.FirstOrDefault(serial => !issuedSerials.Contains(serial));
            if (invalidSerial is not null)
            {
                throw new DomainValidationException($"Serial '{invalidSerial}' was not issued on this material requisition line.");
            }
        }

        await dbContext.ServiceJobMaterialDispositions.AddAsync(disposition, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return disposition.Id;
    }

    public async Task PostServiceJobMaterialDispositionAsync(
        Guid serviceJobId,
        Guid dispositionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var disposition = await dbContext.ServiceJobMaterialDispositions
            .Include(x => x.Serials)
            .FirstOrDefaultAsync(x => x.ServiceJobId == serviceJobId && x.Id == dispositionId, cancellationToken)
            ?? throw new NotFoundException("Material disposition not found.");

        if (disposition.IsPosted)
        {
            throw new DomainValidationException("Material disposition is already posted.");
        }

        if (disposition.Kind is ServiceJobMaterialDispositionKind.UnusedReturned
            or ServiceJobMaterialDispositionKind.IncorrectReturned
            or ServiceJobMaterialDispositionKind.RejectedSupplierReturn)
        {
            var item = await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == disposition.ItemId, cancellationToken)
                ?? throw new DomainValidationException("Invalid item on material disposition.");

            await inventoryService.RecordReceiptAsync(
                clock.UtcNow,
                disposition.WarehouseId,
                item,
                disposition.Quantity,
                disposition.UnitCost,
                ReferenceTypes.ServiceJobMaterialDisposition,
                disposition.Id,
                disposition.Id,
                disposition.BatchNumber,
                disposition.Serials.Select(x => x.SerialNumber).ToList(),
                cancellationToken);
        }

        disposition.Post(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateServiceJobMaterialDispositionAsync(
        Guid serviceJobId,
        Guid dispositionId,
        string? condition,
        string reason,
        ServiceJobMaterialChargeTo chargeTo,
        Guid? supplierReturnId,
        string? responsiblePerson,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var disposition = await dbContext.ServiceJobMaterialDispositions
            .FirstOrDefaultAsync(x => x.ServiceJobId == serviceJobId && x.Id == dispositionId, cancellationToken)
            ?? throw new NotFoundException("Material disposition not found.");

        if (disposition.Kind == ServiceJobMaterialDispositionKind.RejectedSupplierReturn && supplierReturnId is not null)
        {
            var supplierReturnExists = await dbContext.SupplierReturns.AsNoTracking().AnyAsync(x => x.Id == supplierReturnId.Value, cancellationToken);
            if (!supplierReturnExists)
            {
                throw new NotFoundException("Supplier return not found.");
            }
        }

        disposition.UpdateDetails(
            condition ?? disposition.Kind.ToString(),
            reason,
            chargeTo,
            supplierReturnId,
            responsiblePerson);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task VoidServiceJobMaterialDispositionAsync(
        Guid serviceJobId,
        Guid dispositionId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        var disposition = await dbContext.ServiceJobMaterialDispositions
            .Include(x => x.Serials)
            .FirstOrDefaultAsync(x => x.ServiceJobId == serviceJobId && x.Id == dispositionId, cancellationToken)
            ?? throw new NotFoundException("Material disposition not found.");

        if (disposition.IsVoided)
        {
            throw new DomainValidationException("Material disposition is already voided.");
        }

        if (disposition.IsPosted && disposition.Kind is ServiceJobMaterialDispositionKind.UnusedReturned
            or ServiceJobMaterialDispositionKind.IncorrectReturned
            or ServiceJobMaterialDispositionKind.RejectedSupplierReturn)
        {
            var item = await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == disposition.ItemId, cancellationToken)
                ?? throw new DomainValidationException("Invalid item on material disposition.");

            await inventoryService.RecordIssueAsync(
                clock.UtcNow,
                disposition.WarehouseId,
                item,
                disposition.Quantity,
                disposition.UnitCost,
                ReferenceTypes.ServiceJobMaterialDisposition,
                disposition.Id,
                disposition.Id,
                disposition.BatchNumber,
                disposition.Serials.Select(x => x.SerialNumber).ToList(),
                cancellationToken);
        }

        disposition.Void(clock.UtcNow, reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ServiceEntitlementSnapshot> EvaluateServiceEntitlementAsync(
        Guid equipmentUnitId,
        Guid customerId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var equipmentUnit = await dbContext.EquipmentUnits.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == equipmentUnitId, cancellationToken)
            ?? throw new NotFoundException("Equipment unit not found.");

        var activeContract = await dbContext.ServiceContracts.AsNoTracking()
            .Where(x => x.EquipmentUnitId == equipmentUnitId
                        && x.CustomerId == customerId
                        && x.IsActive
                        && x.StartDate <= asOf
                        && x.EndDate >= asOf)
            .OrderByDescending(x => x.EndDate)
            .ThenByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeContract is not null)
        {
            return new ServiceEntitlementSnapshot(
                activeContract.Id,
                ServiceEntitlementSource.ServiceContract,
                activeContract.Coverage,
                ServiceEntitlementRules.ToBillingTreatment(activeContract.Coverage),
                $"Contract {activeContract.Number} ({activeContract.ContractType}) covers {activeContract.Coverage} until {activeContract.EndDate:yyyy-MM-dd}.");
        }

        if (equipmentUnit.HasActiveWarranty(asOf))
        {
            return new ServiceEntitlementSnapshot(
                null,
                ServiceEntitlementSource.ManufacturerWarranty,
                equipmentUnit.WarrantyCoverage,
                ServiceEntitlementRules.ToBillingTreatment(equipmentUnit.WarrantyCoverage),
                $"Manufacturer warranty covers {equipmentUnit.WarrantyCoverage} until {equipmentUnit.WarrantyUntil:yyyy-MM-dd}.");
        }

        return new ServiceEntitlementSnapshot(
            null,
            ServiceEntitlementSource.None,
            ServiceCoverageScope.None,
            CustomerBillingTreatment.Billable,
            "No active warranty or service contract entitlement was found.");
    }

    private async Task<decimal> ApplyEstimateUnitPriceAsync(
        Guid serviceJobId,
        ServiceEstimateLineKind kind,
        decimal unitPrice,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.ServiceJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceJobId, cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        return ServiceEntitlementRules.ApplyEstimateUnitPrice(job.EntitlementCoverage, kind, unitPrice);
    }

    private async Task EnsureServiceJobAcceptsNewCostsAsync(Guid serviceJobId, CancellationToken cancellationToken)
    {
        var status = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => x.Id == serviceJobId)
            .Select(x => (ServiceJobStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        if (status == ServiceJobStatus.Closed)
        {
            throw new DomainValidationException("Closed service jobs cannot receive new expenses, bills, material issues, or labor entries.");
        }
    }

    private async Task EnsurePlannedOperationItemExistsAsync(Guid? plannedItemId, CancellationToken cancellationToken)
    {
        if (plannedItemId is null)
        {
            return;
        }

        var exists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == plannedItemId.Value, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Planned item not found.");
        }
    }

    private async Task<ServiceJobOperation> ResolveServiceJobOperationAsync(Guid serviceJobId, Guid operationId, CancellationToken cancellationToken)
    {
        return await dbContext.ServiceJobOperations
            .FirstOrDefaultAsync(x => x.ServiceJobId == serviceJobId && x.Id == operationId, cancellationToken)
            ?? throw new NotFoundException("Service job operation not found.");
    }

    private async Task<ServiceJobDailySheet> ResolveDailySheetAsync(Guid serviceJobId, Guid dailySheetId, CancellationToken cancellationToken)
    {
        await EnsureServiceJobAcceptsNewCostsAsync(serviceJobId, cancellationToken);

        return await dbContext.ServiceJobDailySheets
            .FirstOrDefaultAsync(x => x.ServiceJobId == serviceJobId && x.Id == dailySheetId, cancellationToken)
            ?? throw new NotFoundException("Service job daily sheet not found.");
    }

    private async Task EnsureDailySheetBelongsToJobAsync(Guid serviceJobId, Guid? dailySheetId, CancellationToken cancellationToken)
    {
        if (dailySheetId is null)
        {
            return;
        }

        var dailySheet = await dbContext.ServiceJobDailySheets.AsNoTracking()
            .Where(x => x.Id == dailySheetId.Value)
            .Select(x => new { x.ServiceJobId, x.Status })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Service job daily sheet not found.");

        if (dailySheet.ServiceJobId != serviceJobId)
        {
            throw new DomainValidationException("Daily sheet does not belong to this service job.");
        }

        if (dailySheet.Status is ServiceJobDailySheetStatus.Approved)
        {
            throw new DomainValidationException("Approved daily sheets cannot receive new operational entries.");
        }
    }

    public async Task<IReadOnlyList<ServiceJobCloseoutCheck>> GetServiceJobCloseoutChecksAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var jobExists = await dbContext.ServiceJobs.AsNoTracking().AnyAsync(x => x.Id == serviceJobId, cancellationToken);
        if (!jobExists)
        {
            throw new NotFoundException("Service job not found.");
        }

        var openExpenseClaims = await dbContext.ServiceExpenseClaims.AsNoTracking()
            .CountAsync(x => x.ServiceJobId == serviceJobId
                             && x.Status != ServiceExpenseClaimStatus.Settled
                             && x.Status != ServiceExpenseClaimStatus.Rejected,
                cancellationToken);

        var pendingDailySheets = await dbContext.ServiceJobDailySheets.AsNoTracking()
            .CountAsync(x => x.ServiceJobId == serviceJobId
                             && (x.Status == ServiceJobDailySheetStatus.Draft || x.Status == ServiceJobDailySheetStatus.Submitted),
                cancellationToken);

        var openIous = await dbContext.PettyCashIous.AsNoTracking()
            .CountAsync(x => x.ServiceJobId == serviceJobId
                             && x.Status != PettyCashIouStatus.Settled
                             && x.Status != PettyCashIouStatus.Rejected
                             && x.Status != PettyCashIouStatus.Cancelled,
                cancellationToken);

        var openDirectPurchaseBills = await dbContext.DirectPurchases.AsNoTracking()
            .Where(x => x.ServiceJobId == serviceJobId && x.Status == DirectPurchaseStatus.Posted)
            .CountAsync(x => !dbContext.SupplierInvoices.Any(invoice =>
                    invoice.DirectPurchaseId == x.Id && invoice.Status == SupplierInvoiceStatus.Posted),
                cancellationToken);

        var draftMaterialRequisitions = await dbContext.MaterialRequisitions.AsNoTracking()
            .CountAsync(x => x.ServiceJobId == serviceJobId && x.Status == MaterialRequisitionStatus.Draft, cancellationToken);

        var pendingAssignments = await dbContext.ServiceJobAssignments.AsNoTracking()
            .CountAsync(x => x.ServiceJobId == serviceJobId && x.ApprovalStatus == ServiceJobAssignmentApprovalStatus.Pending, cancellationToken);

        var pendingLaborEntries = await dbContext.WorkOrderTimeEntries.AsNoTracking()
            .CountAsync(x => x.ServiceJobId == serviceJobId
                             && (x.Status == WorkOrderTimeEntryStatus.Draft || x.Status == WorkOrderTimeEntryStatus.Submitted),
                cancellationToken);

        var unfinishedWorkOrders = await dbContext.WorkOrders.AsNoTracking()
            .CountAsync(x => x.ServiceJobId == serviceJobId
                             && (x.Status == WorkOrderStatus.Open || x.Status == WorkOrderStatus.InProgress),
                cancellationToken);

        var uninvoicedBillableLabor = await dbContext.WorkOrderTimeEntries.AsNoTracking()
            .CountAsync(x => x.ServiceJobId == serviceJobId
                             && x.BillableToCustomer
                             && x.Status == WorkOrderTimeEntryStatus.Approved
                             && x.SalesInvoiceLineId == null,
                cancellationToken);

        var materialLineRows = await (
            from mr in dbContext.MaterialRequisitions.AsNoTracking()
            from line in mr.Lines
            where mr.ServiceJobId == serviceJobId && mr.Status == MaterialRequisitionStatus.Posted
            select new { LineId = line.Id, line.Quantity })
            .ToListAsync(cancellationToken);
        var lineIds = materialLineRows.Select(x => x.LineId).ToList();
        var dispositionRows = lineIds.Count == 0
            ? []
            : await dbContext.ServiceJobMaterialDispositions.AsNoTracking()
                .Where(x => lineIds.Contains(x.MaterialRequisitionLineId))
                .Where(x => !x.IsVoided)
                .GroupBy(x => x.MaterialRequisitionLineId)
                .Select(x => new { LineId = x.Key, Quantity = x.Sum(d => d.Quantity) })
                .ToListAsync(cancellationToken);
        var disposedByLine = dispositionRows.ToDictionary(x => x.LineId, x => x.Quantity);
        var undisposedMaterialLines = materialLineRows.Count(line => !disposedByLine.TryGetValue(line.LineId, out var disposedQty) || disposedQty < line.Quantity);

        var finalInvoiceResolved = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => x.Id == serviceJobId)
            .Select(x => x.FinalInvoiceNotRequired)
            .FirstAsync(cancellationToken);
        if (!finalInvoiceResolved)
        {
            finalInvoiceResolved = await dbContext.ServiceHandovers.AsNoTracking()
                .AnyAsync(x => x.ServiceJobId == serviceJobId && x.SalesInvoiceId != null, cancellationToken);
        }

        return
        [
            CreateCloseoutCheck(
                "daily-field-sheets",
                "Daily field sheets",
                pendingDailySheets,
                "Submit and approve or reject every daily field sheet before closing."),
            CreateCloseoutCheck(
                "expense-claims",
                "Expense claims",
                openExpenseClaims,
                "All job expense claims must be settled or rejected."),
            CreateCloseoutCheck(
                "petty-cash-ious",
                "Petty cash IOUs",
                openIous,
                "All job IOUs must be settled, rejected, or cancelled."),
            CreateCloseoutCheck(
                "direct-purchase-bills",
                "Direct purchase supplier bills",
                openDirectPurchaseBills,
                "All posted job-linked direct purchases must have posted supplier invoices."),
            CreateCloseoutCheck(
                "material-requisitions",
                "Draft material requisitions",
                draftMaterialRequisitions,
                "Post or void draft material requisitions before closing the job."),
            CreateCloseoutCheck(
                "job-assignments",
                "Technician assignments",
                pendingAssignments,
                "Approve or reject all job assignments before closing."),
            CreateCloseoutCheck(
                "labor-entries",
                "Labor entries",
                pendingLaborEntries,
                "Submit and approve or reject all draft/submitted labor entries before closing."),
            CreateCloseoutCheck(
                "work-orders",
                "Job detail work orders",
                unfinishedWorkOrders,
                "Mark work orders done or cancel them before closing."),
            CreateCloseoutCheck(
                "billable-labor",
                "Uninvoiced billable labor",
                uninvoicedBillableLabor,
                "Convert approved billable labor through handover invoicing or mark it non-billable before closing."),
            CreateCloseoutCheck(
                "material-disposition",
                "Material disposition",
                undisposedMaterialLines,
                "Mark every posted material issue line as used, returned, damaged, or supplier-returned before closing."),
            CreateCloseoutCheck(
                "final-invoice",
                "Final invoice decision",
                finalInvoiceResolved ? 0 : 1,
                "Generate the final invoice from a completed handover, or mark the job as not billable with a reason.")
        ];
    }

    private async Task EnsureServiceJobReadyToCloseAsync(Guid serviceJobId, CancellationToken cancellationToken)
    {
        var openChecks = await GetServiceJobCloseoutChecksAsync(serviceJobId, cancellationToken);
        var firstOpenCheck = openChecks.FirstOrDefault(x => !x.IsClear);
        if (firstOpenCheck is not null)
        {
            throw new DomainValidationException($"Service job cannot close: {firstOpenCheck.Detail}");
        }
    }

    private static ServiceJobCloseoutCheck CreateCloseoutCheck(string key, string label, int pendingCount, string detail)
        => new(key, label, pendingCount == 0, pendingCount, pendingCount == 0 ? "Clear." : detail);

    public async Task<Guid> AddQualityCheckAsync(Guid serviceJobId, bool passed, string? notes, CancellationToken cancellationToken = default)
    {
        var qc = new QualityCheck(serviceJobId, clock.UtcNow, passed, notes);
        await dbContext.QualityChecks.AddAsync(qc, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return qc.Id;
    }
}
