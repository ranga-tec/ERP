using ISS.Domain.Common;

namespace ISS.Domain.Security;

public sealed class UserPermissionOverride : AuditableEntity
{
    private UserPermissionOverride() { }

    public UserPermissionOverride(Guid userId, string permissionKey, bool isGranted)
    {
        UserId = userId;
        PermissionKey = Guard.NotNullOrWhiteSpace(permissionKey, nameof(permissionKey), 128);
        IsGranted = isGranted;
    }

    public Guid UserId { get; private set; }
    public string PermissionKey { get; private set; } = null!;
    public bool IsGranted { get; private set; }

    public void Update(bool isGranted) => IsGranted = isGranted;
}
