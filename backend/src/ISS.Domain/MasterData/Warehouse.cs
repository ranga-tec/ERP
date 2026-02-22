using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class Warehouse : AuditableEntity
{
    private Warehouse() { }

    public Warehouse(string code, string name, string? address)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        Address = address?.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, string? address, bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        Address = address?.Trim();
        IsActive = isActive;
    }
}

