using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class Supplier : AuditableEntity
{
    private Supplier() { }

    public Supplier(Guid companyId, string code, string name, string? phone, string? email, string? address)
    {
        CompanyId = companyId == Guid.Empty ? throw new DomainValidationException("Company is required.") : companyId;
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        IsActive = true;
        IsAuthorized = true;
    }

    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAuthorized { get; private set; }

    public void Update(Guid companyId, string code, string name, string? phone, string? email, string? address, bool isActive, bool isAuthorized = true)
    {
        CompanyId = companyId == Guid.Empty ? throw new DomainValidationException("Company is required.") : companyId;
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        IsActive = isActive;
        IsAuthorized = isAuthorized;
    }
}
