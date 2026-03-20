using ISS.Domain.Common;

namespace ISS.Domain.Assistant;

public sealed class AssistantUserPreference : AuditableEntity
{
    private AssistantUserPreference() { }

    public AssistantUserPreference(Guid userId, bool assistantEnabled, Guid? activeProviderProfileId)
    {
        UserId = userId;
        Update(assistantEnabled, activeProviderProfileId);
    }

    public Guid UserId { get; private set; }
    public bool AssistantEnabled { get; private set; }
    public Guid? ActiveProviderProfileId { get; private set; }

    public void Update(bool assistantEnabled, Guid? activeProviderProfileId)
    {
        AssistantEnabled = assistantEnabled;
        ActiveProviderProfileId = activeProviderProfileId;
    }
}
