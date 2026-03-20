using ISS.Domain.Common;

namespace ISS.Domain.Assistant;

public sealed class AssistantProviderProfile : AuditableEntity
{
    private AssistantProviderProfile() { }

    public AssistantProviderProfile(
        Guid userId,
        string name,
        AssistantProviderKind providerKind,
        string baseUrl,
        string model,
        string? apiKeyCiphertext)
    {
        UserId = userId;
        Update(name, providerKind, baseUrl, model, apiKeyCiphertext);
    }

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public AssistantProviderKind ProviderKind { get; private set; }
    public string BaseUrl { get; private set; } = null!;
    public string Model { get; private set; } = null!;

    [AuditSensitive]
    public string? ApiKeyCiphertext { get; private set; }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKeyCiphertext);

    public void Update(
        string name,
        AssistantProviderKind providerKind,
        string baseUrl,
        string model,
        string? apiKeyCiphertext)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        ProviderKind = providerKind;
        BaseUrl = Guard.NotNullOrWhiteSpace(baseUrl.TrimEnd('/'), nameof(baseUrl), maxLength: 512);
        Model = Guard.NotNullOrWhiteSpace(model, nameof(model), maxLength: 256);
        ApiKeyCiphertext = string.IsNullOrWhiteSpace(apiKeyCiphertext)
            ? null
            : Guard.NotNullOrWhiteSpace(apiKeyCiphertext, nameof(apiKeyCiphertext), maxLength: 4000);
    }

    public void ReplaceApiKey(string? apiKeyCiphertext)
    {
        ApiKeyCiphertext = string.IsNullOrWhiteSpace(apiKeyCiphertext)
            ? null
            : Guard.NotNullOrWhiteSpace(apiKeyCiphertext, nameof(apiKeyCiphertext), maxLength: 4000);
    }
}
