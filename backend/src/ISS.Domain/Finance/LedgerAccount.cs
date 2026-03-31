using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public enum LedgerAccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public sealed class LedgerAccount : AuditableEntity
{
    private LedgerAccount() { }

    public LedgerAccount(
        string code,
        string name,
        LedgerAccountType accountType,
        Guid? parentAccountId,
        bool allowsPosting,
        string? description)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        AccountType = accountType;
        ParentAccountId = parentAccountId;
        AllowsPosting = allowsPosting;
        Description = NormalizeDescription(description);
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public LedgerAccountType AccountType { get; private set; }
    public Guid? ParentAccountId { get; private set; }
    public LedgerAccount? ParentAccount { get; private set; }
    public bool AllowsPosting { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        LedgerAccountType accountType,
        Guid? parentAccountId,
        bool allowsPosting,
        string? description,
        bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        AccountType = accountType;
        ParentAccountId = parentAccountId;
        AllowsPosting = allowsPosting;
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
