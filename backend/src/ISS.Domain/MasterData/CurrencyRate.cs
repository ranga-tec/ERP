using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public enum CurrencyRateType
{
    Spot = 1,
    Corporate = 2,
    Manual = 3
}

public sealed class CurrencyRate : AuditableEntity
{
    private CurrencyRate() { }

    public CurrencyRate(Guid fromCurrencyId, Guid toCurrencyId, decimal rate, CurrencyRateType rateType, DateTimeOffset effectiveFrom, string? source)
    {
        if (fromCurrencyId == toCurrencyId)
        {
            throw new DomainValidationException("From and to currencies cannot be the same.");
        }

        FromCurrencyId = fromCurrencyId;
        ToCurrencyId = toCurrencyId;
        Rate = Guard.Positive(rate, nameof(Rate));
        RateType = rateType;
        EffectiveFrom = effectiveFrom;
        Source = NormalizeSource(source);
        IsActive = true;
    }

    public Guid FromCurrencyId { get; private set; }
    public Currency? FromCurrency { get; private set; }

    public Guid ToCurrencyId { get; private set; }
    public Currency? ToCurrency { get; private set; }

    public decimal Rate { get; private set; }
    public CurrencyRateType RateType { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }
    public string? Source { get; private set; }
    public bool IsActive { get; private set; }

    public decimal Convert(decimal amount) => amount * Rate;

    public void Update(Guid fromCurrencyId, Guid toCurrencyId, decimal rate, CurrencyRateType rateType, DateTimeOffset effectiveFrom, string? source, bool isActive)
    {
        if (fromCurrencyId == toCurrencyId)
        {
            throw new DomainValidationException("From and to currencies cannot be the same.");
        }

        FromCurrencyId = fromCurrencyId;
        ToCurrencyId = toCurrencyId;
        Rate = Guard.Positive(rate, nameof(Rate));
        RateType = rateType;
        EffectiveFrom = effectiveFrom;
        Source = NormalizeSource(source);
        IsActive = isActive;
    }

    private static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        return Guard.NotNullOrWhiteSpace(source, nameof(Source), maxLength: 256);
    }
}
