using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class Customer : AuditableEntity
{
    private Customer() { }

    public Customer(string code, string name, string? phone, string? email, string? address)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, string? phone, string? email, string? address, bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        IsActive = isActive;
    }
}

