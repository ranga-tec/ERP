using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ISS.Application.Common;
using ISS.Domain.Assistant;
using ISS.Domain.Common;

namespace ISS.Api.Assistant;

public sealed class AssistantProviderGateway(HttpClient httpClient, ILogger<AssistantProviderGateway> logger)
{
    private const string AnthropicVersion = "2023-06-01";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    internal async Task<AssistantInterpretation?> TryInterpretAsync(
        AssistantResolvedProvider? provider,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (provider is null || string.IsNullOrWhiteSpace(provider.Model))
        {
            return null;
        }

        if (!AssistantProviderKindHelper.TryParse(provider.Kind, out var kind))
        {
            return null;
        }

        try
        {
            var content = await CompleteTextAsync(
                kind,
                provider.BaseUrl,
                provider.Model,
                provider.ApiKey,
                systemPrompt,
                userPrompt,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var cleaned = StripCodeFence(content);
            return JsonSerializer.Deserialize<AssistantInterpretation>(cleaned, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Assistant LLM interpretation failed for provider {ProviderName}", provider.Name);
            return null;
        }
    }

    internal async Task<AssistantConnectionTestDto> TestConnectionAsync(
        AssistantProviderConfigDto provider,
        CancellationToken cancellationToken)
    {
        var validated = ValidateConfig(provider, requireModel: false);
        if (!string.IsNullOrWhiteSpace(validated.Message))
        {
            return new AssistantConnectionTestDto(false, validated.Message!);
        }

        try
        {
            if (string.IsNullOrWhiteSpace(validated.Config!.Model))
            {
                var models = await ListModelsAsync(validated.Config, cancellationToken);
                return models.Count > 0
                    ? new AssistantConnectionTestDto(true, $"Connection succeeded. Found {models.Count} model(s).")
                    : new AssistantConnectionTestDto(true, "Connection succeeded, but no models were returned.");
            }

            var content = await CompleteTextAsync(
                validated.Kind,
                validated.Config.BaseUrl!,
                validated.Config.Model!,
                validated.Config.ApiKey,
                "Reply with the exact text OK.",
                "Reply with the exact text OK.",
                cancellationToken);

            return string.IsNullOrWhiteSpace(content)
                ? new AssistantConnectionTestDto(false, "The provider responded without any content.")
                : new AssistantConnectionTestDto(true, $"Connection succeeded. Response: {content.Trim()}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Assistant provider connection test failed.");
            return new AssistantConnectionTestDto(false, ex.Message);
        }
    }

    internal async Task<IReadOnlyList<AssistantModelOptionDto>> ListModelsAsync(
        AssistantProviderConfigDto provider,
        CancellationToken cancellationToken)
    {
        var validated = ValidateConfig(provider, requireModel: false);
        if (!string.IsNullOrWhiteSpace(validated.Message))
        {
            throw new DomainValidationException(validated.Message!);
        }

        try
        {
            return validated.Kind switch
            {
                AssistantProviderKind.OpenAi or AssistantProviderKind.OpenAiCompatible => await ListOpenAiModelsAsync(validated.Config!, cancellationToken),
                AssistantProviderKind.Anthropic => await ListAnthropicModelsAsync(validated.Config!, cancellationToken),
                AssistantProviderKind.Ollama => await ListOllamaModelsAsync(validated.Config!, cancellationToken),
                _ => []
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Assistant provider model discovery failed.");
            throw new DomainValidationException($"Could not discover models: {ex.Message}");
        }
    }

    private (AssistantProviderKind Kind, AssistantProviderConfigDto? Config, string? Message) ValidateConfig(
        AssistantProviderConfigDto provider,
        bool requireModel)
    {
        if (!AssistantProviderKindHelper.TryParse(provider.Kind, out var kind))
        {
            return (default, null, "Select a valid AI provider.");
        }

        var baseUrl = AssistantProviderKindHelper.NormalizeBaseUrl(kind, provider.BaseUrl);
        var model = provider.Model?.Trim();
        var apiKey = provider.ApiKey?.Trim();

        if (requireModel && string.IsNullOrWhiteSpace(model))
        {
            return (kind, null, "Model is required.");
        }

        if (AssistantProviderKindHelper.RequiresApiKey(kind) && string.IsNullOrWhiteSpace(apiKey))
        {
            return (kind, null, "API key is required for this provider.");
        }

        return (kind, new AssistantProviderConfigDto(
            AssistantProviderKindHelper.ToApiValue(kind),
            baseUrl,
            string.IsNullOrWhiteSpace(model) ? null : model,
            string.IsNullOrWhiteSpace(apiKey) ? null : apiKey), null);
    }

    private async Task<string?> CompleteTextAsync(
        AssistantProviderKind kind,
        string baseUrl,
        string model,
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        return kind switch
        {
            AssistantProviderKind.OpenAi or AssistantProviderKind.OpenAiCompatible => await CompleteOpenAiAsync(baseUrl, model, apiKey, systemPrompt, userPrompt, cancellationToken),
            AssistantProviderKind.Anthropic => await CompleteAnthropicAsync(baseUrl, model, apiKey, systemPrompt, userPrompt, cancellationToken),
            AssistantProviderKind.Ollama => await CompleteOllamaAsync(baseUrl, model, systemPrompt, userPrompt, cancellationToken),
            _ => null
        };
    }

    private async Task<string?> CompleteOpenAiAsync(
        string baseUrl,
        string model,
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.1m,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            return null;
        }

        var message = choices[0].GetProperty("message");
        return message.TryGetProperty("content", out var contentElement)
            ? ReadContentText(contentElement)
            : null;
    }

    private async Task<string?> CompleteAnthropicAsync(
        string baseUrl,
        string model,
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Content = JsonContent.Create(new
        {
            model,
            max_tokens = 400,
            temperature = 0m,
            system = systemPrompt,
            messages = new object[]
            {
                new { role = "user", content = userPrompt },
            },
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) &&
                type.GetString() == "text" &&
                block.TryGetProperty("text", out var text))
            {
                parts.Add(text.GetString() ?? string.Empty);
            }
        }

        return string.Join(Environment.NewLine, parts).Trim();
    }

    private async Task<string?> CompleteOllamaAsync(
        string baseUrl,
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/chat");
        request.Content = JsonContent.Create(new
        {
            model,
            stream = false,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.GetString();
    }

    private async Task<IReadOnlyList<AssistantModelOptionDto>> ListOpenAiModelsAsync(
        AssistantProviderConfigDto provider,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{provider.BaseUrl!.TrimEnd('/')}/models");
        if (!string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return data.EnumerateArray()
            .Select(item =>
            {
                var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                return string.IsNullOrWhiteSpace(id) ? null : new AssistantModelOptionDto(id!, id!);
            })
            .OfType<AssistantModelOptionDto>()
            .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<AssistantModelOptionDto>> ListAnthropicModelsAsync(
        AssistantProviderConfigDto provider,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{provider.BaseUrl!.TrimEnd('/')}/v1/models");
        request.Headers.Add("x-api-key", provider.ApiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return data.EnumerateArray()
            .Select(item =>
            {
                var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var displayName = item.TryGetProperty("display_name", out var displayElement) ? displayElement.GetString() : id;
                return string.IsNullOrWhiteSpace(id) ? null : new AssistantModelOptionDto(id!, displayName ?? id!);
            })
            .OfType<AssistantModelOptionDto>()
            .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<AssistantModelOptionDto>> ListOllamaModelsAsync(
        AssistantProviderConfigDto provider,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{provider.BaseUrl!.TrimEnd('/')}/api/tags");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return models.EnumerateArray()
            .Select(item =>
            {
                var name = item.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                var model = item.TryGetProperty("model", out var modelElement) ? modelElement.GetString() : name;
                return string.IsNullOrWhiteSpace(name) ? null : new AssistantModelOptionDto(name!, model ?? name!);
            })
            .OfType<AssistantModelOptionDto>()
            .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ReadContentText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join(
                Environment.NewLine,
                element.EnumerateArray()
                    .SelectMany(ReadContentParts)
                    .Where(static x => !string.IsNullOrWhiteSpace(x))),
            _ => element.ToString()
        };
    }

    private static IEnumerable<string> ReadContentParts(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            yield return element.GetString() ?? string.Empty;
            yield break;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("text", out var text))
        {
            yield return text.GetString() ?? string.Empty;
        }
    }

    private static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lines = trimmed.Split('\n');
        if (lines.Length <= 2)
        {
            return trimmed.Trim('`');
        }

        return string.Join('\n', lines.Skip(1).Take(lines.Length - 2)).Trim();
    }
}
