using ISS.Api.Security;
using ISS.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController(
    ICurrentUser currentUser,
    AccessControlService accessControl) : ControllerBase
{
    public sealed record CurrentPermissionsDto(IReadOnlyList<string> Permissions);

    [HttpGet("permissions")]
    public async Task<ActionResult<CurrentPermissionsDto>> Permissions(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var permissions = await accessControl.GetEffectivePermissionsAsync(userId, cancellationToken);
        return Ok(new CurrentPermissionsDto(permissions.OrderBy(x => x).ToArray()));
    }
}
