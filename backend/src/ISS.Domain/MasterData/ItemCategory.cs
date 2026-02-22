using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class ItemCategory : AuditableEntity
{
    private ItemCategory() { }

    public ItemCategory(string code, string name)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public void Update(string code, string name, bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        IsActive = isActive;
    }
}
