using System.Security.Claims;
using ISS.Api.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/assistant")]
[Authorize]
public sealed class AssistantController(
    AssistantCoordinator coordinator,
    AssistantSettingsService settingsService) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<AssistantChatResponse>> Chat(
        AssistantChatRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var actor = new AssistantActor(userId, roles);
        var access = await settingsService.GetAccessAsync(userId, roles, cancellationToken);
        if (!access.IsAllowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, access.DisabledReason ?? "AI mode is not available.");
        }

        var requestProvider = request.Provider;
        var provider = requestProvider is not null
            ? new AssistantResolvedProvider(
                ProfileId: null,
                Name: requestProvider.Kind ?? "assistant-provider",
                Kind: requestProvider.Kind ?? "openai-compatible",
                BaseUrl: requestProvider.BaseUrl ?? string.Empty,
                Model: requestProvider.Model ?? string.Empty,
                ApiKey: requestProvider.ApiKey)
            : await settingsService.ResolveProviderAsync(userId, request.ProviderProfileId, cancellationToken);
        var resolvedRequest = request with
        {
            Provider = provider is null
                ? null
                : new AssistantProviderConfigDto(provider.Kind, provider.BaseUrl, provider.Model, provider.ApiKey)
        };

        var response = await coordinator.HandleAsync(actor, resolvedRequest, cancellationToken);
        return Ok(response);
    }
}
