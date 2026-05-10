using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class Company : AuditableEntity
{
    private Company() { }

    public Company(string code, string name)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool EnforceAuthorizedSuppliersOnly { get; private set; }

    public void Update(string code, string name, bool isActive, bool enforceAuthorizedSuppliersOnly = false)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        IsActive = isActive;
        EnforceAuthorizedSuppliersOnly = enforceAuthorizedSuppliersOnly;
    }
}
