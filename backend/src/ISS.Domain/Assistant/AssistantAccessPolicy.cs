using ISS.Domain.Common;

namespace ISS.Domain.Assistant;

public sealed class AssistantAccessPolicy : AuditableEntity
{
    public const string DefaultScopeKey = "default";

    private AssistantAccessPolicy() { }

    public AssistantAccessPolicy(bool isEnabled, bool allowUserManagedProviders, IEnumerable<string> allowedRoles)
    {
        ScopeKey = DefaultScopeKey;
        Update(isEnabled, allowUserManagedProviders, allowedRoles);
    }

    public string ScopeKey { get; private set; } = DefaultScopeKey;
    public bool IsEnabled { get; private set; }
    public bool AllowUserManagedProviders { get; private set; }
    public string AllowedRolesCsv { get; private set; } = string.Empty;

    public IReadOnlyList<string> AllowedRoles =>
        AllowedRolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public void Update(bool isEnabled, bool allowUserManagedProviders, IEnumerable<string> allowedRoles)
    {
        IsEnabled = isEnabled;
        AllowUserManagedProviders = allowUserManagedProviders;

        var normalizedRoles = (allowedRoles ?? [])
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        AllowedRolesCsv = string.Join(",", normalizedRoles);
    }
}
