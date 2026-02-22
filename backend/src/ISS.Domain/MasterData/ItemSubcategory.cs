using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class ItemSubcategory : AuditableEntity
{
    private ItemSubcategory() { }

    public ItemSubcategory(Guid categoryId, string code, string name)
    {
        CategoryId = categoryId;
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        IsActive = true;
    }

    public Guid CategoryId { get; private set; }
    public ItemCategory? Category { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public void Update(Guid categoryId, string code, string name, bool isActive)
    {
        CategoryId = categoryId;
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        IsActive = isActive;
    }
}
