using ISS.Api.Security;
using ISS.Application.Abstractions;
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
public sealed class ItemCategoriesController(IIssDbContext dbContext, ICurrentUser currentUser) : ControllerBase
{
    public sealed record ItemCategoryDto(
        Guid Id,
        Guid CompanyId,
        string? CompanyCode,
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
        Guid? CompanyId,
        string Code,
        string Name,
        Guid? RevenueAccountId,
        Guid? ExpenseAccountId);

    public sealed record UpdateItemCategoryRequest(
        Guid? CompanyId,
        string Code,
        string Name,
        Guid? RevenueAccountId,
        Guid? ExpenseAccountId,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemCategoryDto>>> List([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId);
        var query = dbContext.ItemCategories.AsNoTracking().Where(x => x.CompanyId == resolvedCompanyId);

        var items = await query
            .OrderBy(x => x.Code)
            .Select(x => new ItemCategoryDto(
                x.Id,
                x.CompanyId,
                x.Company != null ? x.Company.Code : null,
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
            .Where(x => x.Id == id && x.CompanyId == ResolveCompanyId(null))
            .Select(x => new ItemCategoryDto(
                x.Id,
                x.CompanyId,
                x.Company != null ? x.Company.Code : null,
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
        var companyId = ResolveCompanyId(request.CompanyId);
        var companyExists = await dbContext.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, cancellationToken);
        if (!companyExists)
        {
            return BadRequest("Selected company does not exist.");
        }

        var accountAssignmentError = await ValidateAccountAssignmentsAsync(
            existingCategory: null,
            request.RevenueAccountId,
            request.ExpenseAccountId,
            cancellationToken);
        if (accountAssignmentError is not null)
        {
            return BadRequest(accountAssignmentError);
        }

        var item = new ItemCategory(companyId, request.Code, request.Name, request.RevenueAccountId, request.ExpenseAccountId);
        await dbContext.ItemCategories.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.ItemCategories.AsNoTracking()
            .Where(x => x.Id == item.Id)
            .Select(x => new ItemCategoryDto(
                x.Id,
                x.CompanyId,
                x.Company != null ? x.Company.Code : null,
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
        var item = await dbContext.ItemCategories.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == ResolveCompanyId(null), cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var companyId = ResolveCompanyId(request.CompanyId ?? item.CompanyId);
        var companyExists = await dbContext.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, cancellationToken);
        if (!companyExists)
        {
            return BadRequest("Selected company does not exist.");
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

        item.Update(companyId, request.Code, request.Name, request.IsActive, request.RevenueAccountId, request.ExpenseAccountId);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.ItemCategories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemCategoryDto(
                x.Id,
                x.CompanyId,
                x.Company != null ? x.Company.Code : null,
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
        var item = await dbContext.ItemCategories.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == ResolveCompanyId(null), cancellationToken);
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

    private Guid ResolveCompanyId(Guid? requestedCompanyId)
    {
        if (User.IsInRole(Roles.Admin) && requestedCompanyId is not null)
        {
            return requestedCompanyId.Value;
        }

        return currentUser.CompanyId ?? CompanyDefaults.DefaultCompanyId;
    }
}
