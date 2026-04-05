using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/items/account-options")]
[Authorize(Roles = Roles.AllBusiness)]
public sealed class ItemAccountOptionsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ItemAccountOptionDto(
        Guid Id,
        string Code,
        string Name,
        int AccountType,
        bool AllowsPosting,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemAccountOptionDto>>> List(CancellationToken cancellationToken)
    {
        var options = await dbContext.LedgerAccounts.AsNoTracking()
            .Where(x => x.AccountType == LedgerAccountType.Revenue || x.AccountType == LedgerAccountType.Expense)
            .OrderBy(x => x.AccountType)
            .ThenByDescending(x => x.IsActive)
            .ThenByDescending(x => x.AllowsPosting)
            .ThenBy(x => x.Code)
            .Select(x => new ItemAccountOptionDto(
                x.Id,
                x.Code,
                x.Name,
                (int)x.AccountType,
                x.AllowsPosting,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(options);
    }
}
