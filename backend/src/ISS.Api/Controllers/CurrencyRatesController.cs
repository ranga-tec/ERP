using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/currency-rates")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class CurrencyRatesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record CurrencyRateDto(
        Guid Id,
        Guid FromCurrencyId,
        string FromCurrencyCode,
        Guid ToCurrencyId,
        string ToCurrencyCode,
        decimal Rate,
        CurrencyRateType RateType,
        DateTimeOffset EffectiveFrom,
        string? Source,
        bool IsActive);

    public sealed record CreateCurrencyRateRequest(
        Guid FromCurrencyId,
        Guid ToCurrencyId,
        decimal Rate,
        CurrencyRateType RateType,
        DateTimeOffset EffectiveFrom,
        string? Source);

    public sealed record UpdateCurrencyRateRequest(
        Guid FromCurrencyId,
        Guid ToCurrencyId,
        decimal Rate,
        CurrencyRateType RateType,
        DateTimeOffset EffectiveFrom,
        string? Source,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CurrencyRateDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.CurrencyRates.AsNoTracking()
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.FromCurrency!.Code)
            .ThenBy(x => x.ToCurrency!.Code)
            .Select(x => new CurrencyRateDto(
                x.Id,
                x.FromCurrencyId,
                x.FromCurrency!.Code,
                x.ToCurrencyId,
                x.ToCurrency!.Code,
                x.Rate,
                x.RateType,
                x.EffectiveFrom,
                x.Source,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CurrencyRateDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.CurrencyRates.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CurrencyRateDto(
                x.Id,
                x.FromCurrencyId,
                x.FromCurrency!.Code,
                x.ToCurrencyId,
                x.ToCurrency!.Code,
                x.Rate,
                x.RateType,
                x.EffectiveFrom,
                x.Source,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyRateDto>> Create(CreateCurrencyRateRequest request, CancellationToken cancellationToken)
    {
        await ValidateCurrenciesAsync(request.FromCurrencyId, request.ToCurrencyId, cancellationToken);

        var item = new CurrencyRate(
            request.FromCurrencyId,
            request.ToCurrencyId,
            request.Rate,
            request.RateType,
            request.EffectiveFrom,
            request.Source);

        await dbContext.CurrencyRates.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(item.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CurrencyRateDto>> Update(Guid id, UpdateCurrencyRateRequest request, CancellationToken cancellationToken)
    {
        await ValidateCurrenciesAsync(request.FromCurrencyId, request.ToCurrencyId, cancellationToken);

        var item = await dbContext.CurrencyRates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(
            request.FromCurrencyId,
            request.ToCurrencyId,
            request.Rate,
            request.RateType,
            request.EffectiveFrom,
            request.Source,
            request.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.CurrencyRates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.CurrencyRates.Remove(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Currency rate is in use and cannot be deleted. Mark it inactive instead.");
        }

        return NoContent();
    }

    private async Task ValidateCurrenciesAsync(Guid fromCurrencyId, Guid toCurrencyId, CancellationToken cancellationToken)
    {
        var existingIds = await dbContext.Currencies.AsNoTracking()
            .Where(x => x.Id == fromCurrencyId || x.Id == toCurrencyId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!existingIds.Contains(fromCurrencyId) || !existingIds.Contains(toCurrencyId))
        {
            throw new DomainValidationException("Selected currency is invalid.");
        }
    }
}
