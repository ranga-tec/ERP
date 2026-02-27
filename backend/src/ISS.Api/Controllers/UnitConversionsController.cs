using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/uom-conversions")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class UnitConversionsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record UnitConversionDto(
        Guid Id,
        Guid FromUnitOfMeasureId,
        string FromUnitOfMeasureCode,
        string FromUnitOfMeasureName,
        Guid ToUnitOfMeasureId,
        string ToUnitOfMeasureCode,
        string ToUnitOfMeasureName,
        decimal Factor,
        string? Notes,
        bool IsActive);

    public sealed record CreateUnitConversionRequest(Guid FromUnitOfMeasureId, Guid ToUnitOfMeasureId, decimal Factor, string? Notes);
    public sealed record UpdateUnitConversionRequest(Guid FromUnitOfMeasureId, Guid ToUnitOfMeasureId, decimal Factor, string? Notes, bool IsActive);

    public sealed record ConversionResultDto(
        Guid FromUnitOfMeasureId,
        Guid ToUnitOfMeasureId,
        decimal InputQuantity,
        decimal OutputQuantity,
        decimal Factor,
        bool UsedInverseConversion);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UnitConversionDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.UnitConversions.AsNoTracking()
            .OrderBy(x => x.FromUnitOfMeasure!.Code)
            .ThenBy(x => x.ToUnitOfMeasure!.Code)
            .Select(x => new UnitConversionDto(
                x.Id,
                x.FromUnitOfMeasureId,
                x.FromUnitOfMeasure!.Code,
                x.FromUnitOfMeasure.Name,
                x.ToUnitOfMeasureId,
                x.ToUnitOfMeasure!.Code,
                x.ToUnitOfMeasure.Name,
                x.Factor,
                x.Notes,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitConversionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.UnitConversions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UnitConversionDto(
                x.Id,
                x.FromUnitOfMeasureId,
                x.FromUnitOfMeasure!.Code,
                x.FromUnitOfMeasure.Name,
                x.ToUnitOfMeasureId,
                x.ToUnitOfMeasure!.Code,
                x.ToUnitOfMeasure.Name,
                x.Factor,
                x.Notes,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<UnitConversionDto>> Create(CreateUnitConversionRequest request, CancellationToken cancellationToken)
    {
        await ValidateUomsAsync(request.FromUnitOfMeasureId, request.ToUnitOfMeasureId, cancellationToken);

        var item = new UnitConversion(
            request.FromUnitOfMeasureId,
            request.ToUnitOfMeasureId,
            request.Factor,
            request.Notes);

        await dbContext.UnitConversions.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await Get(item.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UnitConversionDto>> Update(Guid id, UpdateUnitConversionRequest request, CancellationToken cancellationToken)
    {
        await ValidateUomsAsync(request.FromUnitOfMeasureId, request.ToUnitOfMeasureId, cancellationToken);

        var item = await dbContext.UnitConversions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(
            request.FromUnitOfMeasureId,
            request.ToUnitOfMeasureId,
            request.Factor,
            request.Notes,
            request.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("convert")]
    public async Task<ActionResult<ConversionResultDto>> Convert(
        [FromQuery] Guid fromUnitOfMeasureId,
        [FromQuery] Guid toUnitOfMeasureId,
        [FromQuery] decimal quantity,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0m)
        {
            return BadRequest("quantity must be positive.");
        }

        if (fromUnitOfMeasureId == toUnitOfMeasureId)
        {
            return Ok(new ConversionResultDto(
                fromUnitOfMeasureId,
                toUnitOfMeasureId,
                quantity,
                quantity,
                1m,
                UsedInverseConversion: false));
        }

        var direct = await dbContext.UnitConversions.AsNoTracking()
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(
                x => x.FromUnitOfMeasureId == fromUnitOfMeasureId
                     && x.ToUnitOfMeasureId == toUnitOfMeasureId,
                cancellationToken);

        if (direct is not null)
        {
            return Ok(new ConversionResultDto(
                fromUnitOfMeasureId,
                toUnitOfMeasureId,
                quantity,
                direct.Convert(quantity),
                direct.Factor,
                UsedInverseConversion: false));
        }

        var inverse = await dbContext.UnitConversions.AsNoTracking()
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(
                x => x.FromUnitOfMeasureId == toUnitOfMeasureId
                     && x.ToUnitOfMeasureId == fromUnitOfMeasureId,
                cancellationToken);

        if (inverse is null)
        {
            return NotFound("No active conversion found for this UoM pair.");
        }

        var factor = 1m / inverse.Factor;
        return Ok(new ConversionResultDto(
            fromUnitOfMeasureId,
            toUnitOfMeasureId,
            quantity,
            quantity * factor,
            factor,
            UsedInverseConversion: true));
    }

    private async Task ValidateUomsAsync(Guid fromUnitOfMeasureId, Guid toUnitOfMeasureId, CancellationToken cancellationToken)
    {
        var existingIds = await dbContext.UnitOfMeasures.AsNoTracking()
            .Where(x => x.Id == fromUnitOfMeasureId || x.Id == toUnitOfMeasureId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!existingIds.Contains(fromUnitOfMeasureId) || !existingIds.Contains(toUnitOfMeasureId))
        {
            throw new DomainValidationException("Selected UoM is invalid.");
        }
    }
}
