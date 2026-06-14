using ISS.Application.Persistence;
using ISS.Domain.Security;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Security;

public sealed class AccessControlService(
    UserManager<ApplicationUser> userManager,
    IIssDbContext dbContext)
{
    public IReadOnlyList<AppPermissionDefinition> Definitions => AppPermissions.All;

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var roles = await userManager.GetRolesAsync(user);
        return await GetEffectivePermissionsAsync(userId, roles, cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken cancellationToken = default)
    {
        var permissions = await GetEffectivePermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permissionKey);
    }

    public async Task<IReadOnlyList<Guid>> GetActiveUserIdsWithAnyPermissionAsync(
        IEnumerable<string> permissionKeys,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var requested = permissionKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requested.Length == 0)
        {
            return Array.Empty<Guid>();
        }

        var now = DateTimeOffset.UtcNow;
        var users = await userManager.Users.AsNoTracking()
            .Where(x => x.LockoutEnd == null || x.LockoutEnd <= now)
            .OrderBy(x => x.Email)
            .ToListAsync(cancellationToken);

        var result = new List<Guid>();
        foreach (var user in users)
        {
            if (excludeUserId.HasValue && user.Id == excludeUserId.Value)
            {
                continue;
            }

            var roles = await userManager.GetRolesAsync(user);
            var permissions = await GetEffectivePermissionsAsync(user.Id, roles, cancellationToken);
            if (requested.Any(permissions.Contains))
            {
                result.Add(user.Id);
            }
        }

        return result;
    }

    public async Task SetExplicitPermissionsAsync(Guid userId, IEnumerable<string> grantedPermissionKeys, CancellationToken cancellationToken = default)
    {
        var userExists = await userManager.Users.AsNoTracking().AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException("User not found.");
        }

        var known = AppPermissions.AllKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var granted = grantedPermissionKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invalid = granted.Where(x => !known.Contains(x)).ToArray();
        if (invalid.Length > 0)
        {
            throw new InvalidOperationException($"Invalid permission(s): {string.Join(", ", invalid)}");
        }

        var existing = await dbContext.UserPermissionOverrides
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var permissionKey in known)
        {
            var isGranted = granted.Contains(permissionKey);
            var row = existing.FirstOrDefault(x => string.Equals(x.PermissionKey, permissionKey, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                dbContext.UserPermissionOverrides.Add(new UserPermissionOverride(userId, permissionKey, isGranted));
            }
            else
            {
                row.Update(isGranted);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetToRoleDefaultsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.UserPermissionOverrides
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        dbContext.UserPermissionOverrides.RemoveRange(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasExplicitOverridesAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.UserPermissionOverrides.AnyAsync(x => x.UserId == userId, cancellationToken);

    private async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var roleList = roles.ToArray();
        if (roleList.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            return AppPermissions.AllKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var permissions = RoleDefaultPermissions(roleList).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var overrides = await dbContext.UserPermissionOverrides.AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var permissionOverride in overrides)
        {
            if (permissionOverride.IsGranted)
            {
                permissions.Add(permissionOverride.PermissionKey);
            }
            else
            {
                permissions.Remove(permissionOverride.PermissionKey);
            }
        }

        return permissions;
    }

    private static IEnumerable<string> RoleDefaultPermissions(IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleSet.Contains(Roles.Finance))
        {
            yield return AppPermissions.PettyCashIouView;
            yield return AppPermissions.PettyCashIouCreate;
            yield return AppPermissions.PettyCashIouSubmit;
            yield return AppPermissions.PettyCashIouApprove;
            yield return AppPermissions.PettyCashIouReject;
            yield return AppPermissions.PettyCashIouRelease;
            yield return AppPermissions.PettyCashIouSettle;
            yield return AppPermissions.ServiceExpenseClaimView;
            yield return AppPermissions.ServiceExpenseClaimCreate;
            yield return AppPermissions.ServiceExpenseClaimEdit;
            yield return AppPermissions.ServiceExpenseClaimSubmit;
            yield return AppPermissions.ServiceExpenseClaimApprove;
            yield return AppPermissions.ServiceExpenseClaimReject;
            yield return AppPermissions.ServiceExpenseClaimSettle;
            yield return AppPermissions.ServiceExpenseClaimConvert;
            yield return AppPermissions.ServiceMaterialRequisitionView;
        }

        if (roleSet.Contains(Roles.Inventory))
        {
            yield return AppPermissions.ServiceMaterialRequisitionView;
            yield return AppPermissions.ServiceMaterialRequisitionEdit;
            yield return AppPermissions.ServiceMaterialRequisitionPost;
            yield return AppPermissions.ServiceMaterialRequisitionVoid;
            yield return AppPermissions.ProcurementGoodsReceiptView;
            yield return AppPermissions.ProcurementGoodsReceiptCreate;
            yield return AppPermissions.ProcurementGoodsReceiptEdit;
            yield return AppPermissions.ProcurementGoodsReceiptPost;
            yield return AppPermissions.ProcurementDirectPurchaseView;
            yield return AppPermissions.ProcurementDirectPurchaseCreate;
            yield return AppPermissions.ProcurementDirectPurchaseEdit;
            yield return AppPermissions.ProcurementDirectPurchasePost;
            yield return AppPermissions.ProcurementSupplierReturnView;
            yield return AppPermissions.ProcurementSupplierReturnCreate;
            yield return AppPermissions.ProcurementSupplierReturnEdit;
            yield return AppPermissions.ProcurementSupplierReturnPost;
            yield return AppPermissions.InventoryStockAdjustmentView;
            yield return AppPermissions.InventoryStockAdjustmentCreate;
            yield return AppPermissions.InventoryStockAdjustmentEdit;
            yield return AppPermissions.InventoryStockAdjustmentPost;
            yield return AppPermissions.InventoryStockAdjustmentVoid;
            yield return AppPermissions.InventoryStockTransferView;
            yield return AppPermissions.InventoryStockTransferCreate;
            yield return AppPermissions.InventoryStockTransferEdit;
            yield return AppPermissions.InventoryStockTransferPost;
            yield return AppPermissions.InventoryStockTransferVoid;
        }

        if (roleSet.Contains(Roles.Procurement))
        {
            yield return AppPermissions.ProcurementPurchaseRequisitionView;
            yield return AppPermissions.ProcurementPurchaseRequisitionCreate;
            yield return AppPermissions.ProcurementPurchaseRequisitionEdit;
            yield return AppPermissions.ProcurementPurchaseRequisitionSubmit;
            yield return AppPermissions.ProcurementPurchaseRequisitionApprove;
            yield return AppPermissions.ProcurementPurchaseRequisitionReject;
            yield return AppPermissions.ProcurementPurchaseRequisitionCancel;
            yield return AppPermissions.ProcurementPurchaseRequisitionConvert;
            yield return AppPermissions.ProcurementRfqView;
            yield return AppPermissions.ProcurementRfqCreate;
            yield return AppPermissions.ProcurementRfqEdit;
            yield return AppPermissions.ProcurementRfqSend;
            yield return AppPermissions.ProcurementPurchaseOrderView;
            yield return AppPermissions.ProcurementPurchaseOrderCreate;
            yield return AppPermissions.ProcurementPurchaseOrderEdit;
            yield return AppPermissions.ProcurementPurchaseOrderApprove;
            yield return AppPermissions.ProcurementGoodsReceiptView;
            yield return AppPermissions.ProcurementGoodsReceiptCreate;
            yield return AppPermissions.ProcurementGoodsReceiptEdit;
            yield return AppPermissions.ProcurementGoodsReceiptPost;
            yield return AppPermissions.ProcurementDirectPurchaseView;
            yield return AppPermissions.ProcurementDirectPurchaseCreate;
            yield return AppPermissions.ProcurementDirectPurchaseEdit;
            yield return AppPermissions.ProcurementDirectPurchasePost;
            yield return AppPermissions.ProcurementSupplierInvoiceView;
            yield return AppPermissions.ProcurementSupplierInvoiceCreate;
            yield return AppPermissions.ProcurementSupplierInvoiceEdit;
            yield return AppPermissions.ProcurementSupplierInvoicePost;
            yield return AppPermissions.ProcurementSupplierReturnView;
            yield return AppPermissions.ProcurementSupplierReturnCreate;
            yield return AppPermissions.ProcurementSupplierReturnEdit;
            yield return AppPermissions.ProcurementSupplierReturnPost;
        }

        if (roleSet.Contains(Roles.Finance))
        {
            yield return AppPermissions.ProcurementDirectPurchaseView;
            yield return AppPermissions.ProcurementDirectPurchaseCreate;
            yield return AppPermissions.ProcurementDirectPurchaseEdit;
            yield return AppPermissions.ProcurementDirectPurchasePost;
            yield return AppPermissions.ProcurementSupplierInvoiceView;
            yield return AppPermissions.ProcurementSupplierInvoiceCreate;
            yield return AppPermissions.ProcurementSupplierInvoiceEdit;
            yield return AppPermissions.ProcurementSupplierInvoicePost;
        }

        if (roleSet.Contains(Roles.Service))
        {
            yield return AppPermissions.PettyCashIouView;
            yield return AppPermissions.PettyCashIouCreate;
            yield return AppPermissions.PettyCashIouSubmit;
            yield return AppPermissions.ServiceExpenseClaimView;
            yield return AppPermissions.ServiceExpenseClaimCreate;
            yield return AppPermissions.ServiceExpenseClaimEdit;
            yield return AppPermissions.ServiceExpenseClaimSubmit;
            yield return AppPermissions.ServiceMaterialRequisitionView;
            yield return AppPermissions.ServiceMaterialRequisitionCreate;
            yield return AppPermissions.ServiceMaterialRequisitionEdit;
            yield return AppPermissions.ServiceMaterialRequisitionVoid;
            yield return AppPermissions.ServiceDailySheetSubmit;
            yield return AppPermissions.ServiceDailySheetApprove;
            yield return AppPermissions.ServiceDailySheetReject;
            yield return AppPermissions.ServiceJobAssignmentCreate;
            yield return AppPermissions.ServiceJobAssignmentApprove;
            yield return AppPermissions.ServiceJobAssignmentReject;
            yield return AppPermissions.ServiceWorkOrderTimeEntrySubmit;
            yield return AppPermissions.ServiceWorkOrderTimeEntryApprove;
            yield return AppPermissions.ServiceWorkOrderTimeEntryReject;
            yield return AppPermissions.ServiceEstimateView;
            yield return AppPermissions.ServiceEstimateCreate;
            yield return AppPermissions.ServiceEstimateEdit;
            yield return AppPermissions.ServiceEstimateApprove;
            yield return AppPermissions.ServiceEstimateReject;
            yield return AppPermissions.ServiceEstimateSend;
            yield return AppPermissions.ServiceEstimateRevise;
        }

        if (roleSet.Contains(Roles.Sales))
        {
            yield return AppPermissions.ServiceEstimateView;
            yield return AppPermissions.ServiceEstimateCreate;
            yield return AppPermissions.ServiceEstimateEdit;
            yield return AppPermissions.ServiceEstimateApprove;
            yield return AppPermissions.ServiceEstimateReject;
            yield return AppPermissions.ServiceEstimateSend;
            yield return AppPermissions.ServiceEstimateRevise;
        }
    }
}
