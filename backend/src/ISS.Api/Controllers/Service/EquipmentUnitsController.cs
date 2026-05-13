using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Common;
using ISS.Domain.MasterData;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/equipment-units")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
public sealed class EquipmentUnitsController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    ICurrentUser currentUser) : ControllerBase
{
    public sealed record EquipmentUnitDto(
        Guid Id,
        Guid ItemId,
        string SerialNumber,
        Guid CustomerId,
        DateTimeOffset? PurchasedAt,
        DateTimeOffset? WarrantyUntil,
        ServiceCoverageScope WarrantyCoverage,
        int? ServiceIntervalDays,
        DateTimeOffset? NextServiceDueAt,
        DateTimeOffset? NextRepairDueAt,
        bool HasActiveWarranty);

    public sealed record CreateEquipmentUnitRequest(
        Guid ItemId,
        string SerialNumber,
        Guid CustomerId,
        DateTimeOffset? PurchasedAt,
        DateTimeOffset? WarrantyUntil,
        ServiceCoverageScope? WarrantyCoverage,
        int? ServiceIntervalDays,
        DateTimeOffset? NextServiceDueAt,
        DateTimeOffset? NextRepairDueAt);

    public sealed record CreateExternalEquipmentUnitRequest(
        string ItemSku,
        string ItemName,
        string? UnitOfMeasure,
        string SerialNumber,
        Guid CustomerId,
        DateTimeOffset? PurchasedAt,
        DateTimeOffset? WarrantyUntil,
        ServiceCoverageScope? WarrantyCoverage,
        int? ServiceIntervalDays,
        DateTimeOffset? NextServiceDueAt,
        DateTimeOffset? NextRepairDueAt);

    public sealed record UpdateEquipmentUnitRequest(
        Guid CustomerId,
        DateTimeOffset? PurchasedAt,
        DateTimeOffset? WarrantyUntil,
        ServiceCoverageScope? WarrantyCoverage,
        int? ServiceIntervalDays,
        DateTimeOffset? NextServiceDueAt,
        DateTimeOffset? NextRepairDueAt);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EquipmentUnitDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 5000);

        var units = await dbContext.EquipmentUnits.AsNoTracking()
            .OrderBy(x => x.SerialNumber)
            .Skip(skip)
            .Take(take)
            .Select(x => new EquipmentUnitDto(
                x.Id,
                x.ItemId,
                x.SerialNumber,
                x.CustomerId,
                x.PurchasedAt,
                x.WarrantyUntil,
                x.WarrantyCoverage,
                x.ServiceIntervalDays,
                x.NextServiceDueAt,
                x.NextRepairDueAt,
                x.WarrantyUntil != null && x.WarrantyCoverage != ServiceCoverageScope.None && x.WarrantyUntil >= DateTimeOffset.UtcNow))
            .ToListAsync(cancellationToken);

        return Ok(units);
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentUnitDto>> Create(CreateEquipmentUnitRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateEquipmentUnitAsync(
            request.ItemId,
            request.SerialNumber,
            request.CustomerId,
            request.PurchasedAt,
            request.WarrantyUntil,
            request.WarrantyUntil is null ? ServiceCoverageScope.None : request.WarrantyCoverage ?? ServiceCoverageScope.LaborAndParts,
            request.ServiceIntervalDays,
            request.NextServiceDueAt,
            request.NextRepairDueAt,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("external")]
    public async Task<ActionResult<EquipmentUnitDto>> CreateExternal(CreateExternalEquipmentUnitRequest request, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId ?? CompanyDefaults.DefaultCompanyId;
        var sku = request.ItemSku?.Trim() ?? "";
        var name = request.ItemName?.Trim() ?? "";
        var serialNumber = request.SerialNumber?.Trim() ?? "";
        var unitOfMeasure = string.IsNullOrWhiteSpace(request.UnitOfMeasure) ? "PCS" : request.UnitOfMeasure.Trim();

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainValidationException("Equipment item SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Equipment item name is required.");
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new DomainValidationException("Serial number is required.");
        }

        var companyExists = await dbContext.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, cancellationToken);
        if (!companyExists)
        {
            return BadRequest("Current company does not exist.");
        }

        var customerExists = await dbContext.Customers.AsNoTracking().AnyAsync(x => x.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            return BadRequest("Selected customer does not exist.");
        }

        var skuExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Sku == sku, cancellationToken);
        if (skuExists)
        {
            return Conflict("An item with this SKU already exists. Select the existing equipment item instead.");
        }

        var serialExists = await dbContext.EquipmentUnits.AsNoTracking().AnyAsync(x => x.SerialNumber == serialNumber, cancellationToken);
        if (serialExists)
        {
            return Conflict("An equipment unit with this serial number already exists.");
        }

        await using var transaction = await dbContext.DbContext.Database.BeginTransactionAsync(cancellationToken);

        var item = new Item(
            companyId,
            sku,
            name,
            ItemType.Equipment,
            TrackingType.Serial,
            unitOfMeasure,
            brandId: null,
            barcode: null,
            defaultUnitCost: 0);

        await dbContext.Items.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var id = await serviceManagementService.CreateEquipmentUnitAsync(
            item.Id,
            serialNumber,
            request.CustomerId,
            request.PurchasedAt,
            request.WarrantyUntil,
            request.WarrantyUntil is null ? ServiceCoverageScope.None : request.WarrantyCoverage ?? ServiceCoverageScope.LaborAndParts,
            request.ServiceIntervalDays,
            request.NextServiceDueAt,
            request.NextRepairDueAt,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EquipmentUnitDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var unit = await dbContext.EquipmentUnits.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EquipmentUnitDto(
                x.Id,
                x.ItemId,
                x.SerialNumber,
                x.CustomerId,
                x.PurchasedAt,
                x.WarrantyUntil,
                x.WarrantyCoverage,
                x.ServiceIntervalDays,
                x.NextServiceDueAt,
                x.NextRepairDueAt,
                x.WarrantyUntil != null && x.WarrantyCoverage != ServiceCoverageScope.None && x.WarrantyUntil >= DateTimeOffset.UtcNow))
            .FirstOrDefaultAsync(cancellationToken);

        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EquipmentUnitDto>> Update(Guid id, UpdateEquipmentUnitRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.UpdateEquipmentUnitAsync(
            id,
            request.CustomerId,
            request.PurchasedAt,
            request.WarrantyUntil,
            request.WarrantyUntil is null ? ServiceCoverageScope.None : request.WarrantyCoverage ?? ServiceCoverageScope.LaborAndParts,
            request.ServiceIntervalDays,
            request.NextServiceDueAt,
            request.NextRepairDueAt,
            cancellationToken);
        return await Get(id, cancellationToken);
    }
}
