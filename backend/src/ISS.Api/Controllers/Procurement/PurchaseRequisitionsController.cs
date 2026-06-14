using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Procurement;

[ApiController]
[Route("api/procurement/purchase-requisitions")]
[Authorize]
public sealed class PurchaseRequisitionsController(
    IIssDbContext dbContext,
    ProcurementService procurementService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record PurchaseRequisitionSummaryDto(
        Guid Id,
        string Number,
        DateTimeOffset RequestDate,
        PurchaseRequisitionStatus Status,
        int LineCount,
        string? Notes);

    public sealed record PurchaseRequisitionDto(
        Guid Id,
        string Number,
        DateTimeOffset RequestDate,
        PurchaseRequisitionStatus Status,
        string? Notes,
        IReadOnlyList<PurchaseRequisitionLineDto> Lines);

    public sealed record PurchaseRequisitionLineDto(Guid Id, Guid ItemId, decimal Quantity, string? Notes);

    public sealed record CreatePurchaseRequisitionRequest(string? Notes);
    public sealed record AddPurchaseRequisitionLineRequest(Guid ItemId, decimal Quantity, string? Notes);
    public sealed record UpdatePurchaseRequisitionLineRequest(decimal Quantity, string? Notes);
    public sealed record ConvertToPurchaseOrderRequest(Guid SupplierId);
    public sealed record PurchaseOrderRefDto(Guid Id, string Number);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseRequisitionSummaryDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var items = await dbContext.PurchaseRequisitions.AsNoTracking()
            .OrderByDescending(x => x.RequestDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new PurchaseRequisitionSummaryDto(
                x.Id,
                x.Number,
                x.RequestDate,
                x.Status,
                x.Lines.Count,
                x.Notes))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseRequisitionDto>> Create(CreatePurchaseRequisitionRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await procurementService.CreatePurchaseRequisitionAsync(request.Notes, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseRequisitionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionView, cancellationToken))
        {
            return Forbid();
        }

        var pr = await dbContext.PurchaseRequisitions.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (pr is null)
        {
            return NotFound();
        }

        return Ok(new PurchaseRequisitionDto(
            pr.Id,
            pr.Number,
            pr.RequestDate,
            pr.Status,
            pr.Notes,
            pr.Lines.Select(x => new PurchaseRequisitionLineDto(x.Id, x.ItemId, x.Quantity, x.Notes)).ToList()));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddPurchaseRequisitionLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.AddPurchaseRequisitionLineAsync(id, request.ItemId, request.Quantity, request.Notes, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdatePurchaseRequisitionLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.UpdatePurchaseRequisitionLineAsync(id, lineId, request.Quantity, request.Notes, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.RemovePurchaseRequisitionLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionSubmit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.SubmitPurchaseRequisitionAsync(id, cancellationToken);
        await NotifyPurchaseRequisitionSubmittedAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionApprove, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.ApprovePurchaseRequisitionAsync(id, cancellationToken);
        await NotifyPurchaseRequisitionCreatorAsync(id, "Purchase requisition approved", "Your purchase requisition has been approved.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionReject, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.RejectPurchaseRequisitionAsync(id, cancellationToken);
        await NotifyPurchaseRequisitionCreatorAsync(id, "Purchase requisition rejected", "Your purchase requisition has been rejected.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionCancel, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.CancelPurchaseRequisitionAsync(id, cancellationToken);
        await NotifyPurchaseRequisitionCreatorAsync(id, "Purchase requisition cancelled", "Your purchase requisition has been cancelled.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/convert-to-po")]
    public async Task<ActionResult<PurchaseOrderRefDto>> ConvertToPo(Guid id, ConvertToPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseRequisitionConvert, cancellationToken))
        {
            return Forbid();
        }

        var purchaseOrderId = await procurementService.CreatePurchaseOrderFromPurchaseRequisitionAsync(id, request.SupplierId, cancellationToken);

        var po = await dbContext.PurchaseOrders.AsNoTracking()
            .Where(x => x.Id == purchaseOrderId)
            .Select(x => new PurchaseOrderRefDto(x.Id, x.Number))
            .FirstAsync(cancellationToken);

        return Ok(po);
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyPurchaseRequisitionSubmittedAsync(Guid id, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequisitions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (pr is null)
        {
            return;
        }

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.ProcurementPurchaseRequisitionApprove, AppPermissions.ProcurementPurchaseRequisitionReject],
            excludeUserId: pr.CreatedBy,
            cancellationToken);

        notificationService.EnqueueInAppForUsers(
            recipients,
            "Purchase requisition waiting",
            $"{pr.Number} is waiting for approval.",
            $"/procurement/purchase-requisitions/{pr.Id}",
            ReferenceTypes.PurchaseRequisition,
            pr.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyPurchaseRequisitionCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequisitions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (pr is null || pr.CreatedBy is null || pr.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            pr.CreatedBy.Value,
            title,
            $"{pr.Number}: {message}",
            $"/procurement/purchase-requisitions/{pr.Id}",
            ReferenceTypes.PurchaseRequisition,
            pr.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
