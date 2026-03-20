using ISS.Domain.Assistant;

namespace ISS.Api.Assistant;

internal static class AssistantProviderKindHelper
{
    internal static bool TryParse(string? value, out AssistantProviderKind kind)
    {
        switch ((value ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "openai":
                kind = AssistantProviderKind.OpenAi;
                return true;
            case "openai-compatible":
            case "openai_compatible":
            case "openaicompatible":
            case "compatible":
                kind = AssistantProviderKind.OpenAiCompatible;
                return true;
            case "anthropic":
            case "claude":
                kind = AssistantProviderKind.Anthropic;
                return true;
            case "ollama":
                kind = AssistantProviderKind.Ollama;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    internal static string ToApiValue(AssistantProviderKind kind) => kind switch
    {
        AssistantProviderKind.OpenAi => "openai",
        AssistantProviderKind.OpenAiCompatible => "openai-compatible",
        AssistantProviderKind.Anthropic => "anthropic",
        AssistantProviderKind.Ollama => "ollama",
        _ => "openai-compatible"
    };

    internal static string DefaultBaseUrl(AssistantProviderKind kind) => kind switch
    {
        AssistantProviderKind.OpenAi => "https://api.openai.com/v1",
        AssistantProviderKind.OpenAiCompatible => "https://api.openai.com/v1",
        AssistantProviderKind.Anthropic => "https://api.anthropic.com",
        AssistantProviderKind.Ollama => "http://localhost:11434",
        _ => "https://api.openai.com/v1"
    };

    internal static bool RequiresApiKey(AssistantProviderKind kind)
        => kind is AssistantProviderKind.OpenAi or AssistantProviderKind.Anthropic;

    internal static string NormalizeBaseUrl(AssistantProviderKind kind, string? baseUrl)
    {
        var trimmed = baseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return DefaultBaseUrl(kind);
        }

        return trimmed.TrimEnd('/');
    }
}
