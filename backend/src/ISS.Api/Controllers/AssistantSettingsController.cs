using System.Security.Claims;
using ISS.Api.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/assistant/settings")]
[Authorize]
public sealed class AssistantSettingsController(AssistantSettingsService settingsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AssistantSettingsDto>> Get(CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var userId, out var roles))
        {
            return Unauthorized();
        }

        return Ok(await settingsService.GetSettingsAsync(userId, roles, cancellationToken));
    }

    [HttpPut("policy")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssistantPolicyDto>> UpdatePolicy(
        AssistantPolicyDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out _, out var roles))
        {
            return Unauthorized();
        }

        return Ok(await settingsService.UpdatePolicyAsync(roles, request, cancellationToken));
    }

    [HttpPut("preference")]
    public async Task<ActionResult<AssistantUserPreferenceDto>> UpdatePreference(
        AssistantUserPreferenceDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var userId, out _))
        {
            return Unauthorized();
        }

        return Ok(await settingsService.UpdatePreferenceAsync(userId, request, cancellationToken));
    }

    [HttpPost("providers")]
    public async Task<ActionResult<AssistantProviderProfileDto>> CreateProvider(
        AssistantProviderProfileUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var userId, out var roles))
        {
            return Unauthorized();
        }

        var access = await settingsService.GetAccessAsync(userId, roles, cancellationToken);
        if (!access.CanManageProviders)
        {
            return Forbid();
        }

        return Ok(await settingsService.SaveProviderAsync(userId, request, providerProfileId: null, cancellationToken));
    }

    [HttpPut("providers/{id:guid}")]
    public async Task<ActionResult<AssistantProviderProfileDto>> UpdateProvider(
        Guid id,
        AssistantProviderProfileUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var userId, out var roles))
        {
            return Unauthorized();
        }

        var access = await settingsService.GetAccessAsync(userId, roles, cancellationToken);
        if (!access.CanManageProviders)
        {
            return Forbid();
        }

        return Ok(await settingsService.SaveProviderAsync(userId, request, id, cancellationToken));
    }

    [HttpDelete("providers/{id:guid}")]
    public async Task<ActionResult> DeleteProvider(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var userId, out var roles))
        {
            return Unauthorized();
        }

        var access = await settingsService.GetAccessAsync(userId, roles, cancellationToken);
        if (!access.CanManageProviders)
        {
            return Forbid();
        }

        await settingsService.DeleteProviderAsync(userId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("providers/test")]
    public async Task<ActionResult<AssistantConnectionTestDto>> TestProvider(
        AssistantProviderTestRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var userId, out var roles))
        {
            return Unauthorized();
        }

        var access = await settingsService.GetAccessAsync(userId, roles, cancellationToken);
        if (!access.CanManageProviders)
        {
            return Forbid();
        }

        return Ok(await settingsService.TestProviderAsync(request, cancellationToken));
    }

    [HttpPost("providers/models")]
    public async Task<ActionResult<IReadOnlyList<AssistantModelOptionDto>>> DiscoverModels(
        AssistantProviderTestRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var userId, out var roles))
        {
            return Unauthorized();
        }

        var access = await settingsService.GetAccessAsync(userId, roles, cancellationToken);
        if (!access.CanManageProviders)
        {
            return Forbid();
        }

        return Ok(await settingsService.DiscoverModelsAsync(request, cancellationToken));
    }

    private bool TryGetActor(out Guid userId, out HashSet<string> roles)
    {
        roles = User.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }
}
