using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public enum TaxScope
{
    Sales = 1,
    Purchase = 2,
    Both = 3
}

public sealed class TaxCode : AuditableEntity
{
    private TaxCode() { }

    public TaxCode(string code, string name, decimal ratePercent, bool isInclusive, TaxScope scope, string? description)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        RatePercent = Guard.NotNegative(ratePercent, nameof(RatePercent));
        IsInclusive = isInclusive;
        Scope = scope;
        Description = NormalizeDescription(description);
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal RatePercent { get; private set; }
    public bool IsInclusive { get; private set; }
    public TaxScope Scope { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public decimal CalculateTaxAmount(decimal taxableAmount) => taxableAmount * (RatePercent / 100m);

    public void Update(string code, string name, decimal ratePercent, bool isInclusive, TaxScope scope, string? description, bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        RatePercent = Guard.NotNegative(ratePercent, nameof(RatePercent));
        IsInclusive = isInclusive;
        Scope = scope;
        Description = NormalizeDescription(description);
        IsActive = isActive;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return Guard.NotNullOrWhiteSpace(description, nameof(Description), maxLength: 512);
    }
}
