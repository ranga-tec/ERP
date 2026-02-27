using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/taxes")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance},{Roles.Sales},{Roles.Procurement},{Roles.Service}")]
public sealed class TaxesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record TaxCodeDto(
        Guid Id,
        string Code,
        string Name,
        decimal RatePercent,
        bool IsInclusive,
        TaxScope Scope,
        string? Description,
        bool IsActive);

    public sealed record CreateTaxCodeRequest(string Code, string Name, decimal RatePercent, bool IsInclusive, TaxScope Scope, string? Description);
    public sealed record UpdateTaxCodeRequest(string Code, string Name, decimal RatePercent, bool IsInclusive, TaxScope Scope, string? Description, bool IsActive);

    public sealed record ConvertTaxRequest(Guid SourceTaxCodeId, Guid TargetTaxCodeId, decimal TaxableAmount, bool PreferConversionMatrix = true);
    public sealed record ConvertTaxResponse(
        Guid SourceTaxCodeId,
        Guid TargetTaxCodeId,
        decimal TaxableAmount,
        decimal SourceRatePercent,
        decimal TargetRatePercent,
        decimal SourceTaxAmount,
        decimal TargetTaxAmount,
        decimal EffectiveMultiplier,
        bool UsedConversionMatrix);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaxCodeDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.TaxCodes.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new TaxCodeDto(x.Id, x.Code, x.Name, x.RatePercent, x.IsInclusive, x.Scope, x.Description, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxCodeDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.TaxCodes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TaxCodeDto(x.Id, x.Code, x.Name, x.RatePercent, x.IsInclusive, x.Scope, x.Description, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TaxCodeDto>> Create(CreateTaxCodeRequest request, CancellationToken cancellationToken)
    {
        var item = new TaxCode(request.Code, request.Name, request.RatePercent, request.IsInclusive, request.Scope, request.Description);
        await dbContext.TaxCodes.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(item.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaxCodeDto>> Update(Guid id, UpdateTaxCodeRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.TaxCodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(
            request.Code,
            request.Name,
            request.RatePercent,
            request.IsInclusive,
            request.Scope,
            request.Description,
            request.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("convert")]
    public async Task<ActionResult<ConvertTaxResponse>> Convert(ConvertTaxRequest request, CancellationToken cancellationToken)
    {
        if (request.TaxableAmount < 0m)
        {
            return BadRequest("taxableAmount cannot be negative.");
        }

        var source = await dbContext.TaxCodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.SourceTaxCodeId && x.IsActive, cancellationToken);
        if (source is null)
        {
            return NotFound("Source tax code not found.");
        }

        var target = await dbContext.TaxCodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TargetTaxCodeId && x.IsActive, cancellationToken);
        if (target is null)
        {
            return NotFound("Target tax code not found.");
        }

        var sourceTaxAmount = source.CalculateTaxAmount(request.TaxableAmount);
        var computedTargetTaxAmount = target.CalculateTaxAmount(request.TaxableAmount);

        var effectiveMultiplier = sourceTaxAmount == 0m ? 0m : (computedTargetTaxAmount / sourceTaxAmount);
        var usedConversionMatrix = false;

        if (request.PreferConversionMatrix)
        {
            var conversion = await dbContext.TaxConversions.AsNoTracking()
                .Where(x => x.IsActive)
                .FirstOrDefaultAsync(
                    x => x.SourceTaxCodeId == request.SourceTaxCodeId
                         && x.TargetTaxCodeId == request.TargetTaxCodeId,
                    cancellationToken);

            if (conversion is not null)
            {
                computedTargetTaxAmount = conversion.Convert(sourceTaxAmount);
                effectiveMultiplier = conversion.Multiplier;
                usedConversionMatrix = true;
            }
        }

        return Ok(new ConvertTaxResponse(
            request.SourceTaxCodeId,
            request.TargetTaxCodeId,
            request.TaxableAmount,
            source.RatePercent,
            target.RatePercent,
            sourceTaxAmount,
            computedTargetTaxAmount,
            effectiveMultiplier,
            usedConversionMatrix));
    }
}
