using ISS.Domain.Common;

namespace ISS.Domain.Service;

public sealed class ServiceTechnician : AuditableEntity
{
    private ServiceTechnician() { }

    public ServiceTechnician(
        string code,
        string name,
        decimal defaultCostRate,
        decimal defaultBillingRate,
        string? phone,
        string? notes)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        Update(defaultCostRate, defaultBillingRate, phone, notes, true);
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal DefaultCostRate { get; private set; }
    public decimal DefaultBillingRate { get; private set; }
    public string? Phone { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public void Rename(string code, string name)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
    }

    public void Update(decimal defaultCostRate, decimal defaultBillingRate, string? phone, string? notes, bool isActive)
    {
        DefaultCostRate = Guard.NotNegative(defaultCostRate, nameof(DefaultCostRate));
        DefaultBillingRate = Guard.NotNegative(defaultBillingRate, nameof(DefaultBillingRate));
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        IsActive = isActive;
    }
}
