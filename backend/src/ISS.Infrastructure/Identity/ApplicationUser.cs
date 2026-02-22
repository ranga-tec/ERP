using Microsoft.AspNetCore.Identity;

namespace ISS.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
}

