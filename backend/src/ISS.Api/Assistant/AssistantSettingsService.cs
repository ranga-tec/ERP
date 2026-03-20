using ISS.Api.Security;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Assistant;
using ISS.Domain.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Assistant;

public sealed class AssistantSettingsService(
    IIssDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    AssistantProviderGateway providerGateway)
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("ISS.Api.Assistant.ProviderSecrets.v1");

    internal async Task<AssistantSettingsDto> GetSettingsAsync(
        Guid userId,
        HashSet<string> userRoles,
        CancellationToken cancellationToken)
    {
        var policy = await LoadPolicyAsync(cancellationToken);
        var preference = await LoadPreferenceAsync(userId, cancellationToken);
        var providers = await LoadProviderDtosAsync(userId, preference?.ActiveProviderProfileId, cancellationToken);
        var access = EvaluateAccess(policy, preference, userRoles);

        return new AssistantSettingsDto(
            CanManagePolicy: userRoles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase),
            CanManageProviders: (policy?.AllowUserManagedProviders ?? true) || userRoles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase),
            IsAllowed: access.IsAllowed,
            DisabledReason: access.DisabledReason,
            Policy: ToPolicyDto(policy),
            Preference: ToPreferenceDto(preference),
            Providers: providers,
            AvailableRoles: Roles.All,
            UserRoles: userRoles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    internal async Task<AssistantSettingsAccess> GetAccessAsync(
        Guid userId,
        HashSet<string> userRoles,
        CancellationToken cancellationToken)
    {
        var policy = await LoadPolicyAsync(cancellationToken);
        var preference = await LoadPreferenceAsync(userId, cancellationToken);
        var evaluated = EvaluateAccess(policy, preference, userRoles);

        return new AssistantSettingsAccess(
            userRoles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase),
            (policy?.AllowUserManagedProviders ?? true) || userRoles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase),
            evaluated.IsAllowed,
            evaluated.DisabledReason,
            policy,
            preference);
    }

    internal async Task<AssistantPolicyDto> UpdatePolicyAsync(
        HashSet<string> userRoles,
        AssistantPolicyDto request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin(userRoles);

        var policy = await EnsurePolicyAsync(cancellationToken);
        var allowedRoles = request.AllowedRoles
            .Where(role => Roles.All.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        policy.Update(request.IsEnabled, request.AllowUserManagedProviders, allowedRoles);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPolicyDto(policy);
    }

    internal async Task<AssistantUserPreferenceDto> UpdatePreferenceAsync(
        Guid userId,
        AssistantUserPreferenceDto request,
        CancellationToken cancellationToken)
    {
        if (request.ActiveProviderProfileId is Guid providerId)
        {
            var providerExists = await dbContext.AssistantProviderProfiles.AsNoTracking()
                .AnyAsync(x => x.Id == providerId && x.UserId == userId, cancellationToken);

            if (!providerExists)
            {
                throw new NotFoundException("AI provider profile not found.");
            }
        }

        var preference = await EnsurePreferenceAsync(userId, cancellationToken);
        preference.Update(request.AssistantEnabled, request.ActiveProviderProfileId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPreferenceDto(preference);
    }

    internal async Task<AssistantProviderProfileDto> SaveProviderAsync(
        Guid userId,
        AssistantProviderProfileUpsertRequest request,
        Guid? providerProfileId,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeProviderRequest(request, requireModel: true);

        AssistantProviderProfile profile;
        if (providerProfileId is Guid existingId)
        {
            profile = await dbContext.AssistantProviderProfiles
                .FirstOrDefaultAsync(x => x.Id == existingId && x.UserId == userId, cancellationToken)
                ?? throw new NotFoundException("AI provider profile not found.");

            var ciphertext = ResolveStoredApiKeyCiphertext(profile, normalized.Kind, normalized.ApiKey);
            profile.Update(normalized.Name, normalized.Kind, normalized.BaseUrl, normalized.Model, ciphertext);
        }
        else
        {
            if (AssistantProviderKindHelper.RequiresApiKey(normalized.Kind) && string.IsNullOrWhiteSpace(normalized.ApiKey))
            {
                throw new DomainValidationException("API key is required for this provider.");
            }

            profile = new AssistantProviderProfile(
                userId,
                normalized.Name,
                normalized.Kind,
                normalized.BaseUrl,
                normalized.Model,
                ProtectApiKey(normalized.ApiKey));
            await dbContext.AssistantProviderProfiles.AddAsync(profile, cancellationToken);
        }

        if (request.ActivateAfterSave)
        {
            var preference = await EnsurePreferenceAsync(userId, cancellationToken);
            preference.Update(preference.AssistantEnabled, profile.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToProviderDtoAsync(profile.Id, userId, cancellationToken);
    }

    internal async Task DeleteProviderAsync(
        Guid userId,
        Guid providerProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.AssistantProviderProfiles
            .FirstOrDefaultAsync(x => x.Id == providerProfileId && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("AI provider profile not found.");

        var preference = await dbContext.AssistantUserPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (preference?.ActiveProviderProfileId == providerProfileId)
        {
            preference.Update(preference.AssistantEnabled, null);
        }

        dbContext.DbContext.Remove(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal async Task<AssistantConnectionTestDto> TestProviderAsync(
        AssistantProviderTestRequest request,
        CancellationToken cancellationToken)
    {
        var config = NormalizeProviderConfig(request.Kind, request.BaseUrl, request.Model, request.ApiKey, requireModel: false);
        return await providerGateway.TestConnectionAsync(config, cancellationToken);
    }

    internal async Task<IReadOnlyList<AssistantModelOptionDto>> DiscoverModelsAsync(
        AssistantProviderTestRequest request,
        CancellationToken cancellationToken)
    {
        var config = NormalizeProviderConfig(request.Kind, request.BaseUrl, request.Model, request.ApiKey, requireModel: false);
        return await providerGateway.ListModelsAsync(config, cancellationToken);
    }

    internal async Task<AssistantResolvedProvider?> ResolveProviderAsync(
        Guid userId,
        Guid? requestedProviderProfileId,
        CancellationToken cancellationToken)
    {
        var targetProfileId = requestedProviderProfileId;
        if (targetProfileId is null)
        {
            targetProfileId = await dbContext.AssistantUserPreferences.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.ActiveProviderProfileId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (targetProfileId is null)
        {
            return null;
        }

        var profile = await dbContext.AssistantProviderProfiles.AsNoTracking()
            .Where(x => x.Id == targetProfileId.Value && x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ProviderKind,
                x.BaseUrl,
                x.Model,
                x.ApiKeyCiphertext
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        return new AssistantResolvedProvider(
            profile.Id,
            profile.Name,
            AssistantProviderKindHelper.ToApiValue(profile.ProviderKind),
            profile.BaseUrl,
            profile.Model,
            UnprotectApiKey(profile.ApiKeyCiphertext));
    }

    private static void EnsureAdmin(HashSet<string> userRoles)
    {
        if (!userRoles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Only administrators can update the AI access policy.");
        }
    }

    private async Task<AssistantAccessPolicy?> LoadPolicyAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AssistantAccessPolicies.AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AssistantUserPreference?> LoadPreferenceAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.AssistantUserPreferences.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    private async Task<AssistantAccessPolicy> EnsurePolicyAsync(CancellationToken cancellationToken)
    {
        var policy = await dbContext.AssistantAccessPolicies
            .FirstOrDefaultAsync(x => x.ScopeKey == AssistantAccessPolicy.DefaultScopeKey, cancellationToken);

        if (policy is not null)
        {
            return policy;
        }

        policy = new AssistantAccessPolicy(
            isEnabled: true,
            allowUserManagedProviders: true,
            allowedRoles: Roles.All);

        await dbContext.AssistantAccessPolicies.AddAsync(policy, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private async Task<AssistantUserPreference> EnsurePreferenceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await dbContext.AssistantUserPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (preference is not null)
        {
            return preference;
        }

        preference = new AssistantUserPreference(userId, assistantEnabled: true, activeProviderProfileId: null);
        await dbContext.AssistantUserPreferences.AddAsync(preference, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return preference;
    }

    private async Task<IReadOnlyList<AssistantProviderProfileDto>> LoadProviderDtosAsync(
        Guid userId,
        Guid? activeProviderProfileId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AssistantProviderProfiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => new AssistantProviderProfileDto(
                x.Id,
                x.Name,
                AssistantProviderKindHelper.ToApiValue(x.ProviderKind),
                x.BaseUrl,
                x.Model,
                x.ApiKeyCiphertext != null && x.ApiKeyCiphertext != string.Empty,
                activeProviderProfileId == x.Id,
                x.LastModifiedAt ?? x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<AssistantProviderProfileDto> ToProviderDtoAsync(
        Guid providerProfileId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var activeProviderProfileId = await dbContext.AssistantUserPreferences.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ActiveProviderProfileId)
            .FirstOrDefaultAsync(cancellationToken);

        return await dbContext.AssistantProviderProfiles.AsNoTracking()
            .Where(x => x.Id == providerProfileId && x.UserId == userId)
            .Select(x => new AssistantProviderProfileDto(
                x.Id,
                x.Name,
                AssistantProviderKindHelper.ToApiValue(x.ProviderKind),
                x.BaseUrl,
                x.Model,
                x.ApiKeyCiphertext != null && x.ApiKeyCiphertext != string.Empty,
                activeProviderProfileId == x.Id,
                x.LastModifiedAt ?? x.CreatedAt))
            .FirstAsync(cancellationToken);
    }

    private static AssistantPolicyDto ToPolicyDto(AssistantAccessPolicy? policy)
    {
        if (policy is null)
        {
            return new AssistantPolicyDto(true, true, Roles.All);
        }

        return new AssistantPolicyDto(policy.IsEnabled, policy.AllowUserManagedProviders, policy.AllowedRoles.ToArray());
    }

    private static AssistantUserPreferenceDto ToPreferenceDto(AssistantUserPreference? preference)
        => new(preference?.AssistantEnabled ?? true, preference?.ActiveProviderProfileId);

    private static EvaluatedAccess EvaluateAccess(
        AssistantAccessPolicy? policy,
        AssistantUserPreference? preference,
        HashSet<string> userRoles)
    {
        var resolvedPolicy = ToPolicyDto(policy);
        var assistantEnabled = preference?.AssistantEnabled ?? true;
        var roleAllowed = resolvedPolicy.AllowedRoles.Count > 0 &&
            userRoles.Any(role => resolvedPolicy.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));

        if (!resolvedPolicy.IsEnabled)
        {
            return new EvaluatedAccess(false, "AI mode is disabled by an administrator.");
        }

        if (!roleAllowed)
        {
            return new EvaluatedAccess(false, "Your assigned role does not currently have AI access.");
        }

        if (!assistantEnabled)
        {
            return new EvaluatedAccess(false, "AI mode is turned off in your personal settings.");
        }

        return new EvaluatedAccess(true, null);
    }

    private NormalizedProviderRequest NormalizeProviderRequest(AssistantProviderProfileUpsertRequest request, bool requireModel)
    {
        if (!AssistantProviderKindHelper.TryParse(request.Kind, out var kind))
        {
            throw new DomainValidationException("Select a valid AI provider.");
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Profile name is required.");
        }

        var baseUrl = AssistantProviderKindHelper.NormalizeBaseUrl(kind, request.BaseUrl);
        var model = request.Model?.Trim();
        if (requireModel && string.IsNullOrWhiteSpace(model))
        {
            throw new DomainValidationException("Model is required.");
        }

        var apiKey = NormalizeApiKey(kind, request.ApiKey);
        return new NormalizedProviderRequest(name, kind, baseUrl, model ?? string.Empty, apiKey);
    }

    private AssistantProviderConfigDto NormalizeProviderConfig(
        string kindValue,
        string? baseUrl,
        string? model,
        string? apiKey,
        bool requireModel)
    {
        if (!AssistantProviderKindHelper.TryParse(kindValue, out var kind))
        {
            throw new DomainValidationException("Select a valid AI provider.");
        }

        var normalizedBaseUrl = AssistantProviderKindHelper.NormalizeBaseUrl(kind, baseUrl);
        var normalizedModel = model?.Trim();
        if (requireModel && string.IsNullOrWhiteSpace(normalizedModel))
        {
            throw new DomainValidationException("Model is required.");
        }

        var normalizedApiKey = NormalizeApiKey(kind, apiKey);
        return new AssistantProviderConfigDto(
            AssistantProviderKindHelper.ToApiValue(kind),
            normalizedBaseUrl,
            string.IsNullOrWhiteSpace(normalizedModel) ? null : normalizedModel,
            normalizedApiKey);
    }

    private string? ResolveStoredApiKeyCiphertext(
        AssistantProviderProfile profile,
        AssistantProviderKind nextKind,
        string? apiKey)
    {
        var incoming = NormalizeApiKey(nextKind, apiKey);
        if (!string.IsNullOrWhiteSpace(incoming))
        {
            return ProtectApiKey(incoming);
        }

        if (nextKind != profile.ProviderKind)
        {
            return AssistantProviderKindHelper.RequiresApiKey(nextKind)
                ? throw new DomainValidationException("API key is required when switching to this provider type.")
                : null;
        }

        if (AssistantProviderKindHelper.RequiresApiKey(nextKind) && !profile.HasApiKey)
        {
            throw new DomainValidationException("API key is required for this provider.");
        }

        return profile.ApiKeyCiphertext;
    }

    private static string? NormalizeApiKey(AssistantProviderKind kind, string? apiKey)
    {
        var trimmed = apiKey?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            if (AssistantProviderKindHelper.RequiresApiKey(kind))
            {
                return null;
            }

            return null;
        }

        return trimmed;
    }

    private string? ProtectApiKey(string? apiKey)
        => string.IsNullOrWhiteSpace(apiKey) ? null : protector.Protect(apiKey);

    private string? UnprotectApiKey(string? ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
        {
            return null;
        }

        try
        {
            return protector.Unprotect(ciphertext);
        }
        catch
        {
            return null;
        }
    }

    internal sealed record AssistantSettingsAccess(
        bool CanManagePolicy,
        bool CanManageProviders,
        bool IsAllowed,
        string? DisabledReason,
        AssistantAccessPolicy? PolicyEntity,
        AssistantUserPreference? PreferenceEntity);

    private sealed record EvaluatedAccess(bool IsAllowed, string? DisabledReason);

    private sealed record NormalizedProviderRequest(
        string Name,
        AssistantProviderKind Kind,
        string BaseUrl,
        string Model,
        string? ApiKey);
}
