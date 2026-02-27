using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class PaymentType : AuditableEntity
{
    private PaymentType() { }

    public PaymentType(string code, string name, string? description)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        Description = NormalizeDescription(description);
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, string? description, bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
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
