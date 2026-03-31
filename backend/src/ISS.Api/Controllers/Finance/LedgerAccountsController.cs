using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/accounts")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class LedgerAccountsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record LedgerAccountDto(
        Guid Id,
        string Code,
        string Name,
        int AccountType,
        Guid? ParentAccountId,
        string? ParentAccountCode,
        string? ParentAccountName,
        bool AllowsPosting,
        string? Description,
        bool IsActive);

    public sealed record CreateLedgerAccountRequest(
        string Code,
        string Name,
        int AccountType,
        Guid? ParentAccountId,
        bool AllowsPosting,
        string? Description);

    public sealed record UpdateLedgerAccountRequest(
        string Code,
        string Name,
        int AccountType,
        Guid? ParentAccountId,
        bool AllowsPosting,
        string? Description,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LedgerAccountDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.LedgerAccounts.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new LedgerAccountDto(
                x.Id,
                x.Code,
                x.Name,
                (int)x.AccountType,
                x.ParentAccountId,
                x.ParentAccount != null ? x.ParentAccount.Code : null,
                x.ParentAccount != null ? x.ParentAccount.Name : null,
                x.AllowsPosting,
                x.Description,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LedgerAccountDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.LedgerAccounts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new LedgerAccountDto(
                x.Id,
                x.Code,
                x.Name,
                (int)x.AccountType,
                x.ParentAccountId,
                x.ParentAccount != null ? x.ParentAccount.Code : null,
                x.ParentAccount != null ? x.ParentAccount.Name : null,
                x.AllowsPosting,
                x.Description,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<LedgerAccountDto>> Create(CreateLedgerAccountRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(LedgerAccountType), request.AccountType))
        {
            return BadRequest(new { error = "Invalid account type." });
        }

        if (request.ParentAccountId is not null)
        {
            var parentExists = await dbContext.LedgerAccounts.AsNoTracking()
                .AnyAsync(x => x.Id == request.ParentAccountId.Value, cancellationToken);
            if (!parentExists)
            {
                return BadRequest(new { error = "Selected parent account does not exist." });
            }
        }

        var item = new LedgerAccount(
            request.Code,
            request.Name,
            (LedgerAccountType)request.AccountType,
            request.ParentAccountId,
            request.AllowsPosting,
            request.Description);

        await dbContext.LedgerAccounts.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(item.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LedgerAccountDto>> Update(Guid id, UpdateLedgerAccountRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(LedgerAccountType), request.AccountType))
        {
            return BadRequest(new { error = "Invalid account type." });
        }

        if (request.ParentAccountId == id)
        {
            return BadRequest(new { error = "An account cannot be its own parent." });
        }

        var item = await dbContext.LedgerAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (request.ParentAccountId is not null)
        {
            var parentExists = await dbContext.LedgerAccounts.AsNoTracking()
                .AnyAsync(x => x.Id == request.ParentAccountId.Value, cancellationToken);
            if (!parentExists)
            {
                return BadRequest(new { error = "Selected parent account does not exist." });
            }
        }

        item.Update(
            request.Code,
            request.Name,
            (LedgerAccountType)request.AccountType,
            request.ParentAccountId,
            request.AllowsPosting,
            request.Description,
            request.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.LedgerAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.LedgerAccounts.Remove(item);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Account is in use or has child accounts and cannot be deleted. Mark it inactive or reassign children first.");
        }

        return NoContent();
    }
}
