using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/tax-conversions")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class TaxConversionsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record TaxConversionDto(
        Guid Id,
        Guid SourceTaxCodeId,
        string SourceTaxCode,
        string SourceTaxName,
        Guid TargetTaxCodeId,
        string TargetTaxCode,
        string TargetTaxName,
        decimal Multiplier,
        string? Notes,
        bool IsActive);

    public sealed record CreateTaxConversionRequest(Guid SourceTaxCodeId, Guid TargetTaxCodeId, decimal Multiplier, string? Notes);
    public sealed record UpdateTaxConversionRequest(Guid SourceTaxCodeId, Guid TargetTaxCodeId, decimal Multiplier, string? Notes, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaxConversionDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.TaxConversions.AsNoTracking()
            .OrderBy(x => x.SourceTaxCode!.Code)
            .ThenBy(x => x.TargetTaxCode!.Code)
            .Select(x => new TaxConversionDto(
                x.Id,
                x.SourceTaxCodeId,
                x.SourceTaxCode!.Code,
                x.SourceTaxCode.Name,
                x.TargetTaxCodeId,
                x.TargetTaxCode!.Code,
                x.TargetTaxCode.Name,
                x.Multiplier,
                x.Notes,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxConversionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.TaxConversions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TaxConversionDto(
                x.Id,
                x.SourceTaxCodeId,
                x.SourceTaxCode!.Code,
                x.SourceTaxCode.Name,
                x.TargetTaxCodeId,
                x.TargetTaxCode!.Code,
                x.TargetTaxCode.Name,
                x.Multiplier,
                x.Notes,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TaxConversionDto>> Create(CreateTaxConversionRequest request, CancellationToken cancellationToken)
    {
        await ValidateTaxCodesAsync(request.SourceTaxCodeId, request.TargetTaxCodeId, cancellationToken);

        var item = new TaxConversion(request.SourceTaxCodeId, request.TargetTaxCodeId, request.Multiplier, request.Notes);
        await dbContext.TaxConversions.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(item.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaxConversionDto>> Update(Guid id, UpdateTaxConversionRequest request, CancellationToken cancellationToken)
    {
        await ValidateTaxCodesAsync(request.SourceTaxCodeId, request.TargetTaxCodeId, cancellationToken);

        var item = await dbContext.TaxConversions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(request.SourceTaxCodeId, request.TargetTaxCodeId, request.Multiplier, request.Notes, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    private async Task ValidateTaxCodesAsync(Guid sourceTaxCodeId, Guid targetTaxCodeId, CancellationToken cancellationToken)
    {
        var existingIds = await dbContext.TaxCodes.AsNoTracking()
            .Where(x => x.Id == sourceTaxCodeId || x.Id == targetTaxCodeId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!existingIds.Contains(sourceTaxCodeId) || !existingIds.Contains(targetTaxCodeId))
        {
            throw new DomainValidationException("Selected tax code is invalid.");
        }
    }
}
