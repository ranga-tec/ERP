using ISS.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed record ResolvedItemAccountMapping(Guid? RevenueAccountId, Guid? ExpenseAccountId);

public sealed class DocumentAccountMappingService(IIssDbContext dbContext)
{
    public async Task<IReadOnlyDictionary<Guid, ResolvedItemAccountMapping>> ResolveForItemsAsync(
        IEnumerable<Guid> itemIds,
        CancellationToken cancellationToken = default)
    {
        var ids = itemIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, ResolvedItemAccountMapping>();
        }

        var items = await dbContext.Items.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.RevenueAccountId,
                x.ExpenseAccountId,
                CategoryRevenueAccountId = x.Category != null ? x.Category.RevenueAccountId : null,
                CategoryExpenseAccountId = x.Category != null ? x.Category.ExpenseAccountId : null
            })
            .ToListAsync(cancellationToken);

        return items.ToDictionary(
            item => item.Id,
            item => new ResolvedItemAccountMapping(
                item.RevenueAccountId ?? item.CategoryRevenueAccountId,
                item.ExpenseAccountId ?? item.CategoryExpenseAccountId));
    }

    public async Task<Guid?> ResolveRevenueAccountIdAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var accountsByItemId = await ResolveForItemsAsync([itemId], cancellationToken);
        return accountsByItemId.TryGetValue(itemId, out var mapping) ? mapping.RevenueAccountId : null;
    }

    public async Task<Guid?> ResolveExpenseAccountIdAsync(Guid? itemId, CancellationToken cancellationToken = default)
    {
        if (itemId is not { } resolvedItemId)
        {
            return null;
        }

        var accountsByItemId = await ResolveForItemsAsync([resolvedItemId], cancellationToken);
        return accountsByItemId.TryGetValue(resolvedItemId, out var mapping) ? mapping.ExpenseAccountId : null;
    }
}
