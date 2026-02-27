using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class Currency : AuditableEntity
{
    private Currency() { }

    public Currency(string code, string name, string symbol, int minorUnits, bool isBase)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 3).ToUpperInvariant();
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 64);
        Symbol = Guard.NotNullOrWhiteSpace(symbol, nameof(Symbol), maxLength: 8);
        if (minorUnits is < 0 or > 6)
        {
            throw new DomainValidationException("Minor units must be between 0 and 6.");
        }

        MinorUnits = minorUnits;
        IsBase = isBase;
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Symbol { get; private set; } = null!;
    public int MinorUnits { get; private set; }
    public bool IsBase { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, string symbol, int minorUnits, bool isBase, bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 3).ToUpperInvariant();
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 64);
        Symbol = Guard.NotNullOrWhiteSpace(symbol, nameof(Symbol), maxLength: 8);
        if (minorUnits is < 0 or > 6)
        {
            throw new DomainValidationException("Minor units must be between 0 and 6.");
        }

        MinorUnits = minorUnits;
        IsBase = isBase;
        IsActive = isActive;
    }
}
