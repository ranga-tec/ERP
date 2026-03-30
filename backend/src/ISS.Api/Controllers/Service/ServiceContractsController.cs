using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/contracts")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
public sealed class ServiceContractsController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService) : ControllerBase
{
    public sealed record ServiceContractSummaryDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        Guid EquipmentUnitId,
        ServiceContractType ContractType,
        ServiceCoverageScope Coverage,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        bool IsActive,
        string CurrentState);

    public sealed record ServiceContractDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        Guid EquipmentUnitId,
        ServiceContractType ContractType,
        ServiceCoverageScope Coverage,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        string? Notes,
        bool IsActive,
        string CurrentState);

    public sealed record CreateServiceContractRequest(
        Guid CustomerId,
        Guid EquipmentUnitId,
        ServiceContractType ContractType,
        ServiceCoverageScope Coverage,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        string? Notes);

    public sealed record UpdateServiceContractRequest(
        Guid CustomerId,
        Guid EquipmentUnitId,
        ServiceContractType ContractType,
        ServiceCoverageScope Coverage,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        string? Notes,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceContractSummaryDto>>> List(
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? equipmentUnitId = null,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = dbContext.ServiceContracts.AsNoTracking();

        if (customerId is { } customer)
        {
            query = query.Where(x => x.CustomerId == customer);
        }

        if (equipmentUnitId is { } equipment)
        {
            query = query.Where(x => x.EquipmentUnitId == equipment);
        }

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive && x.StartDate <= now && x.EndDate >= now);
        }

        var contracts = await query
            .OrderByDescending(x => x.EndDate)
            .ThenBy(x => x.Number)
            .ToListAsync(cancellationToken);

        return Ok(contracts.Select(x => new ServiceContractSummaryDto(
            x.Id,
            x.Number,
            x.CustomerId,
            x.EquipmentUnitId,
            x.ContractType,
            x.Coverage,
            x.StartDate,
            x.EndDate,
            x.IsActive,
            DeriveState(x, now))).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ServiceContractDto>> Create(CreateServiceContractRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateServiceContractAsync(
            request.CustomerId,
            request.EquipmentUnitId,
            request.ContractType,
            request.Coverage,
            request.StartDate,
            request.EndDate,
            request.Notes,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceContractDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var contract = await dbContext.ServiceContracts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ServiceContractDto(
                x.Id,
                x.Number,
                x.CustomerId,
                x.EquipmentUnitId,
                x.ContractType,
                x.Coverage,
                x.StartDate,
                x.EndDate,
                x.Notes,
                x.IsActive,
                x.IsActive
                    ? (x.EndDate < now ? "Expired" : (x.StartDate > now ? "Scheduled" : "Active"))
                    : "Inactive"))
            .FirstOrDefaultAsync(cancellationToken);

        return contract is null ? NotFound() : Ok(contract);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceContractDto>> Update(Guid id, UpdateServiceContractRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.UpdateServiceContractAsync(
            id,
            request.CustomerId,
            request.EquipmentUnitId,
            request.ContractType,
            request.Coverage,
            request.StartDate,
            request.EndDate,
            request.Notes,
            request.IsActive,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    private static string DeriveState(ServiceContract contract, DateTimeOffset now)
    {
        if (!contract.IsActive)
        {
            return "Inactive";
        }

        if (contract.EndDate < now)
        {
            return "Expired";
        }

        if (contract.StartDate > now)
        {
            return "Scheduled";
        }

        return "Active";
    }
}
