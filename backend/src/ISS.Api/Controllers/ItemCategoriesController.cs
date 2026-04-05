using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.Finance;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/item-categories")]
[Authorize(Roles = Roles.AllBusiness)]
public sealed class ItemCategoriesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ItemCategoryDto(
        Guid Id,
        string Code,
        string Name,
        Guid? RevenueAccountId,
        string? RevenueAccountCode,
        string? RevenueAccountName,
        Guid? ExpenseAccountId,
        string? ExpenseAccountCode,
        string? ExpenseAccountName,
        bool IsActive);

    public sealed record CreateItemCategoryRequest(
        string Code,
        string Name,
        Guid? RevenueAccountId,
        Guid? ExpenseAccountId);

    public sealed record UpdateItemCategoryRequest(
        string Code,
        string Name,
        Guid? RevenueAccountId,
        Guid? ExpenseAccountId,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemCategoryDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.ItemCategories.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new ItemCategoryDto(
                x.Id,
                x.Code,
                x.Name,
                x.RevenueAccountId,
                x.RevenueAccount != null ? x.RevenueAccount.Code : null,
                x.RevenueAccount != null ? x.RevenueAccount.Name : null,
                x.ExpenseAccountId,
                x.ExpenseAccount != null ? x.ExpenseAccount.Code : null,
                x.ExpenseAccount != null ? x.ExpenseAccount.Name : null,
                x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemCategoryDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.ItemCategories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemCategoryDto(
                x.Id,
                x.Code,
                x.Name,
                x.RevenueAccountId,
                x.RevenueAccount != null ? x.RevenueAccount.Code : null,
                x.RevenueAccount != null ? x.RevenueAccount.Name : null,
                x.ExpenseAccountId,
                x.ExpenseAccount != null ? x.ExpenseAccount.Code : null,
                x.ExpenseAccount != null ? x.ExpenseAccount.Name : null,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory},{Roles.Finance}")]
    public async Task<ActionResult<ItemCategoryDto>> Create(CreateItemCategoryRequest request, CancellationToken cancellationToken)
    {
        var accountAssignmentError = await ValidateAccountAssignmentsAsync(
            existingCategory: null,
            request.RevenueAccountId,
            request.ExpenseAccountId,
            cancellationToken);
        if (accountAssignmentError is not null)
        {
            return BadRequest(accountAssignmentError);
        }

        var item = new ItemCategory(request.Code, request.Name, request.RevenueAccountId, request.ExpenseAccountId);
        await dbContext.ItemCategories.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.ItemCategories.AsNoTracking()
            .Where(x => x.Id == item.Id)
            .Select(x => new ItemCategoryDto(
                x.Id,
                x.Code,
                x.Name,
                x.RevenueAccountId,
                x.RevenueAccount != null ? x.RevenueAccount.Code : null,
                x.RevenueAccount != null ? x.RevenueAccount.Name : null,
                x.ExpenseAccountId,
                x.ExpenseAccount != null ? x.ExpenseAccount.Code : null,
                x.ExpenseAccount != null ? x.ExpenseAccount.Name : null,
                x.IsActive))
            .FirstAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory},{Roles.Finance}")]
    public async Task<ActionResult<ItemCategoryDto>> Update(Guid id, UpdateItemCategoryRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.ItemCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var accountAssignmentError = await ValidateAccountAssignmentsAsync(
            item,
            request.RevenueAccountId,
            request.ExpenseAccountId,
            cancellationToken);
        if (accountAssignmentError is not null)
        {
            return BadRequest(accountAssignmentError);
        }

        item.Update(request.Code, request.Name, request.IsActive, request.RevenueAccountId, request.ExpenseAccountId);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.ItemCategories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemCategoryDto(
                x.Id,
                x.Code,
                x.Name,
                x.RevenueAccountId,
                x.RevenueAccount != null ? x.RevenueAccount.Code : null,
                x.RevenueAccount != null ? x.RevenueAccount.Name : null,
                x.ExpenseAccountId,
                x.ExpenseAccount != null ? x.ExpenseAccount.Code : null,
                x.ExpenseAccount != null ? x.ExpenseAccount.Name : null,
                x.IsActive))
            .FirstAsync(cancellationToken);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.ItemCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.ItemCategories.Remove(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Item category is in use and cannot be deleted. Remove dependent subcategories/items or mark inactive.");
        }

        return NoContent();
    }

    private async Task<string?> ValidateAccountAssignmentsAsync(
        ItemCategory? existingCategory,
        Guid? revenueAccountId,
        Guid? expenseAccountId,
        CancellationToken cancellationToken)
    {
        var revenueError = await ValidateLedgerAccountAssignmentAsync(
            selectedAccountId: revenueAccountId,
            existingAccountId: existingCategory?.RevenueAccountId,
            expectedType: LedgerAccountType.Revenue,
            label: "Revenue",
            cancellationToken);
        if (revenueError is not null)
        {
            return revenueError;
        }

        var expenseError = await ValidateLedgerAccountAssignmentAsync(
            selectedAccountId: expenseAccountId,
            existingAccountId: existingCategory?.ExpenseAccountId,
            expectedType: LedgerAccountType.Expense,
            label: "Expense",
            cancellationToken);
        if (expenseError is not null)
        {
            return expenseError;
        }

        return null;
    }

    private async Task<string?> ValidateLedgerAccountAssignmentAsync(
        Guid? selectedAccountId,
        Guid? existingAccountId,
        LedgerAccountType expectedType,
        string label,
        CancellationToken cancellationToken)
    {
        if (selectedAccountId is null)
        {
            return null;
        }

        var account = await dbContext.LedgerAccounts.AsNoTracking()
            .Where(x => x.Id == selectedAccountId.Value)
            .Select(x => new
            {
                x.Id,
                x.AccountType,
                x.AllowsPosting,
                x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            return $"{label} account does not exist.";
        }

        if (account.AccountType != expectedType)
        {
            return $"{label} account must be a {expectedType} account.";
        }

        var unchangedExistingAssignment = existingAccountId == selectedAccountId.Value;
        if (unchangedExistingAssignment)
        {
            return null;
        }

        if (!account.AllowsPosting)
        {
            return $"{label} account must allow posting.";
        }

        if (!account.IsActive)
        {
            return $"{label} account must be active.";
        }

        return null;
    }
}
