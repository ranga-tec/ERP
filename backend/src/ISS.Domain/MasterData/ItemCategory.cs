using ISS.Domain.Common;
using ISS.Domain.Finance;

namespace ISS.Domain.MasterData;

public sealed class ItemCategory : AuditableEntity
{
    private ItemCategory() { }

    public ItemCategory(string code, string name, Guid? revenueAccountId = null, Guid? expenseAccountId = null)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        RevenueAccountId = revenueAccountId;
        ExpenseAccountId = expenseAccountId;
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid? RevenueAccountId { get; private set; }
    public LedgerAccount? RevenueAccount { get; private set; }
    public Guid? ExpenseAccountId { get; private set; }
    public LedgerAccount? ExpenseAccount { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, bool isActive, Guid? revenueAccountId = null, Guid? expenseAccountId = null)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        RevenueAccountId = revenueAccountId;
        ExpenseAccountId = expenseAccountId;
        IsActive = isActive;
    }
}
