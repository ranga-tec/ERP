using Microsoft.AspNetCore.Identity;
using ISS.Domain.MasterData;

namespace ISS.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public Guid CompanyId { get; set; } = CompanyDefaults.DefaultCompanyId;
    public Company? Company { get; set; }
}
