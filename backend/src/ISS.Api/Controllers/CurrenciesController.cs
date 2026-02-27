using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/currencies")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class CurrenciesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record CurrencyDto(Guid Id, string Code, string Name, string Symbol, int MinorUnits, bool IsBase, bool IsActive);

    public sealed record CreateCurrencyRequest(string Code, string Name, string Symbol, int MinorUnits, bool IsBase);
    public sealed record UpdateCurrencyRequest(string Code, string Name, string Symbol, int MinorUnits, bool IsBase, bool IsActive);

    public sealed record ConvertCurrencyRequest(
        string FromCurrencyCode,
        string ToCurrencyCode,
        decimal Amount,
        DateTimeOffset? OnDate = null,
        CurrencyRateType? RateType = null);

    public sealed record ConvertCurrencyResponse(
        string FromCurrencyCode,
        string ToCurrencyCode,
        decimal Amount,
        decimal ConvertedAmount,
        decimal AppliedRate,
        CurrencyRateType AppliedRateType,
        DateTimeOffset AppliedRateEffectiveFrom,
        bool UsedInverseRate);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CurrencyDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.Currencies.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new CurrencyDto(x.Id, x.Code, x.Name, x.Symbol, x.MinorUnits, x.IsBase, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CurrencyDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Currencies.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CurrencyDto(x.Id, x.Code, x.Name, x.Symbol, x.MinorUnits, x.IsBase, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyDto>> Create(CreateCurrencyRequest request, CancellationToken cancellationToken)
    {
        await EnsureBaseCurrencyConstraintAsync(request.IsBase, null, cancellationToken);

        var item = new Currency(request.Code, request.Name, request.Symbol, request.MinorUnits, request.IsBase);
        await dbContext.Currencies.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(item.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CurrencyDto>> Update(Guid id, UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Currencies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        await EnsureBaseCurrencyConstraintAsync(request.IsBase, item.Id, cancellationToken);

        item.Update(request.Code, request.Name, request.Symbol, request.MinorUnits, request.IsBase, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("convert")]
    public async Task<ActionResult<ConvertCurrencyResponse>> Convert(ConvertCurrencyRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount < 0m)
        {
            return BadRequest("amount cannot be negative.");
        }

        var fromCode = request.FromCurrencyCode.Trim().ToUpperInvariant();
        var toCode = request.ToCurrencyCode.Trim().ToUpperInvariant();
        if (fromCode == toCode)
        {
            return Ok(new ConvertCurrencyResponse(
                fromCode,
                toCode,
                request.Amount,
                request.Amount,
                1m,
                request.RateType ?? CurrencyRateType.Spot,
                request.OnDate ?? DateTimeOffset.UtcNow,
                UsedInverseRate: false));
        }

        var asOf = request.OnDate ?? DateTimeOffset.UtcNow;

        var direct = await FindRateAsync(fromCode, toCode, request.RateType, asOf, cancellationToken);
        if (direct is not null)
        {
            return Ok(new ConvertCurrencyResponse(
                fromCode,
                toCode,
                request.Amount,
                request.Amount * direct.Value.Rate,
                direct.Value.Rate,
                direct.Value.RateType,
                direct.Value.EffectiveFrom,
                UsedInverseRate: false));
        }

        var inverse = await FindRateAsync(toCode, fromCode, request.RateType, asOf, cancellationToken);
        if (inverse is null)
        {
            return NotFound($"No active FX rate found for {fromCode}/{toCode} as of {asOf:u}.");
        }

        var applied = 1m / inverse.Value.Rate;
        return Ok(new ConvertCurrencyResponse(
            fromCode,
            toCode,
            request.Amount,
            request.Amount * applied,
            applied,
            inverse.Value.RateType,
            inverse.Value.EffectiveFrom,
            UsedInverseRate: true));
    }

    private async Task EnsureBaseCurrencyConstraintAsync(bool isBase, Guid? updatingId, CancellationToken cancellationToken)
    {
        if (!isBase)
        {
            return;
        }

        var anotherBaseExists = await dbContext.Currencies.AsNoTracking()
            .AnyAsync(x => x.IsBase && (updatingId == null || x.Id != updatingId.Value), cancellationToken);

        if (anotherBaseExists)
        {
            throw new InvalidOperationException("Another base currency already exists. Disable it first.");
        }
    }

    private async Task<(decimal Rate, CurrencyRateType RateType, DateTimeOffset EffectiveFrom)?> FindRateAsync(
        string fromCode,
        string toCode,
        CurrencyRateType? rateType,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var rates =
            from rate in dbContext.CurrencyRates.AsNoTracking()
            join fromCurrency in dbContext.Currencies.AsNoTracking() on rate.FromCurrencyId equals fromCurrency.Id
            join toCurrency in dbContext.Currencies.AsNoTracking() on rate.ToCurrencyId equals toCurrency.Id
            where rate.IsActive
                  && fromCurrency.Code == fromCode
                  && toCurrency.Code == toCode
                  && rate.EffectiveFrom <= asOf
                  && (rateType == null || rate.RateType == rateType.Value)
            orderby rate.EffectiveFrom descending
            select new { rate.Rate, rate.RateType, rate.EffectiveFrom };

        var item = await rates.FirstOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        return (item.Rate, item.RateType, item.EffectiveFrom);
    }
}
