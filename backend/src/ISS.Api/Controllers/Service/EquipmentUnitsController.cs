using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Common;
using ISS.Domain.MasterData;
using ISS.Domain.Service;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/equipment-units")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
public sealed class EquipmentUnitsController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    ICurrentUser currentUser,
    UserManager<ApplicationUser> userManager) : ControllerBase
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
        bool HasActiveWarranty,
        bool IsActive,
        DateTimeOffset CreatedAt,
        string? CreatedByName,
        DateTimeOffset? LastModifiedAt,
        string? LastModifiedByName);

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
        DateTimeOffset? NextRepairDueAt,
        bool? IsActive);

    /// <param name="includeInactive">
    /// Defaults to false so equipment pickers never offer a retired unit. The maintenance
    /// list passes true and filters in the UI.
    /// </param>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EquipmentUnitDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 5000);

        var rows = await dbContext.EquipmentUnits.AsNoTracking()
            .Where(x => includeInactive || x.IsActive)
            .OrderBy(x => x.SerialNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var userLabels = await ResolveUserLabelsAsync(
            rows.SelectMany(x => new[] { x.CreatedBy, x.LastModifiedBy }),
            cancellationToken);

        var units = rows.Select(x => ToDto(x, userLabels)).ToList();

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
        var row = await dbContext.EquipmentUnits.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (row is null)
        {
            return NotFound();
        }

        var userLabels = await ResolveUserLabelsAsync([row.CreatedBy, row.LastModifiedBy], cancellationToken);

        return Ok(ToDto(row, userLabels));
    }

    private static EquipmentUnitDto ToDto(EquipmentUnit unit, IReadOnlyDictionary<Guid, string> userLabels)
        => new(
            unit.Id,
            unit.ItemId,
            unit.SerialNumber,
            unit.CustomerId,
            unit.PurchasedAt,
            unit.WarrantyUntil,
            unit.WarrantyCoverage,
            unit.ServiceIntervalDays,
            unit.NextServiceDueAt,
            unit.NextRepairDueAt,
            unit.HasActiveWarranty(DateTimeOffset.UtcNow),
            unit.IsActive,
            unit.CreatedAt,
            LookupUser(unit.CreatedBy, userLabels),
            unit.LastModifiedAt,
            LookupUser(unit.LastModifiedBy, userLabels));

    private static string? LookupUser(Guid? userId, IReadOnlyDictionary<Guid, string> userLabels)
        => userId is { } id && userLabels.TryGetValue(id, out var label) ? label : null;

    /// <summary>Resolves audit user ids to a display name, falling back to email then user name.</summary>
    private async Task<IReadOnlyDictionary<Guid, string>> ResolveUserLabelsAsync(
        IEnumerable<Guid?> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await userManager.Users.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                Label = !string.IsNullOrWhiteSpace(x.DisplayName)
                    ? x.DisplayName
                    : x.Email ?? x.UserName ?? x.Id.ToString()
            })
            .ToDictionaryAsync(x => x.Id, x => x.Label, cancellationToken);
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
            request.IsActive,
            cancellationToken);
        return await Get(id, cancellationToken);
    }
}
