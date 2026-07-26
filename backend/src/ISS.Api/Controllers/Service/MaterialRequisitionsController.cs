using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/material-requisitions")]
[Authorize]
public sealed class MaterialRequisitionsController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record MaterialRequisitionSummaryDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        Guid? ServiceJobDailySheetId,
        Guid WarehouseId,
        DateTimeOffset RequestedAt,
        string? Purpose,
        MaterialRequisitionStatus Status,
        int LineCount,
        decimal RequestedQuantity,
        decimal DispatchedQuantity,
        string Fulfilment);
    public sealed record MaterialRequisitionDto(Guid Id, string Number, Guid ServiceJobId, Guid? ServiceJobDailySheetId, Guid WarehouseId, DateTimeOffset RequestedAt, string? Purpose, MaterialRequisitionStatus Status, IReadOnlyList<MaterialRequisitionLineDto> Lines);
    public sealed record MaterialRequisitionLineDto(Guid Id, Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string> Serials);

    public sealed record CreateMaterialRequisitionRequest(Guid ServiceJobId, Guid WarehouseId, string? Purpose, Guid? ServiceJobDailySheetId);
    public sealed record UpdateMaterialRequisitionRequest(Guid ServiceJobId, Guid WarehouseId, string? Purpose, Guid? ServiceJobDailySheetId);
    public sealed record AddMaterialRequisitionLineRequest(Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateMaterialRequisitionLineRequest(decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialRequisitionSummaryDto>>> List([FromQuery] Guid? serviceJobId, [FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var query = dbContext.MaterialRequisitions.AsNoTracking();
        if (serviceJobId is not null)
        {
            query = query.Where(x => x.ServiceJobId == serviceJobId.Value);
        }

        var rows = await query
            .OrderByDescending(x => x.RequestedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.ServiceJobDailySheetId,
                x.WarehouseId,
                x.RequestedAt,
                x.Purpose,
                x.Status,
                LineCount = x.Lines.Count,
                RequestedQuantity = x.Lines.Sum(l => (decimal?)l.Quantity) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        // how much of each requisition has actually gone out on a dispatch
        var ids = rows.Select(x => x.Id).ToList();
        var dispatched = ids.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await dbContext.DirectDispatches.AsNoTracking()
                .Where(d => d.MaterialRequisitionId != null
                            && ids.Contains(d.MaterialRequisitionId!.Value)
                            && d.Status == ISS.Domain.Sales.DirectDispatchStatus.Posted)
                .SelectMany(d => d.Lines.Select(l => new { RequisitionId = d.MaterialRequisitionId!.Value, l.Quantity }))
                .GroupBy(x => x.RequisitionId)
                .Select(g => new { RequisitionId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.RequisitionId, x => x.Quantity, cancellationToken);

        var requisitions = rows.Select(x =>
        {
            var out_ = dispatched.GetValueOrDefault(x.Id);

            // Two routes take material out: posting the requisition consumes it straight from the
            // store, or an AOD dispatches against it. Report whichever applies rather than calling a
            // posted requisition "Requested" just because no dispatch was involved.
            var fulfilment =
                x.Status == MaterialRequisitionStatus.Voided ? "Voided"
                : x.Status == MaterialRequisitionStatus.Posted ? "Issued from store"
                : out_ <= 0 ? "Requested"
                : out_ >= x.RequestedQuantity ? "Issued" : "Partially issued";

            return new MaterialRequisitionSummaryDto(
                x.Id, x.Number, x.ServiceJobId, x.ServiceJobDailySheetId, x.WarehouseId,
                x.RequestedAt, x.Purpose, x.Status, x.LineCount,
                x.RequestedQuantity, out_, fulfilment);
        }).ToList();

        return Ok(requisitions);
    }

    [HttpPost]
    public async Task<ActionResult<MaterialRequisitionDto>> Create(CreateMaterialRequisitionRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await serviceManagementService.CreateMaterialRequisitionAsync(request.ServiceJobId, request.WarehouseId, request.Purpose, request.ServiceJobDailySheetId, cancellationToken);
        await NotifyMaterialRequisitionCreatedAsync(id, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MaterialRequisitionDto>> Update(Guid id, UpdateMaterialRequisitionRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionEdit, cancellationToken))
        {
            return Forbid();
        }

        await serviceManagementService.UpdateMaterialRequisitionAsync(
            id,
            request.ServiceJobId,
            request.WarehouseId,
            request.Purpose,
            request.ServiceJobDailySheetId,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaterialRequisitionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionView, cancellationToken))
        {
            return Forbid();
        }

        var mr = await dbContext.MaterialRequisitions.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (mr is null)
        {
            return NotFound();
        }

        return Ok(new MaterialRequisitionDto(
            mr.Id,
            mr.Number,
            mr.ServiceJobId,
            mr.ServiceJobDailySheetId,
            mr.WarehouseId,
            mr.RequestedAt,
            mr.Purpose,
            mr.Status,
            mr.Lines.Select(l => new MaterialRequisitionLineDto(l.Id, l.ItemId, l.Quantity, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.MaterialRequisition, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddMaterialRequisitionLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionEdit, cancellationToken))
        {
            return Forbid();
        }

        await serviceManagementService.AddMaterialRequisitionLineAsync(id, request.ItemId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateMaterialRequisitionLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionEdit, cancellationToken))
        {
            return Forbid();
        }

        await serviceManagementService.UpdateMaterialRequisitionLineAsync(
            id,
            lineId,
            request.Quantity,
            request.BatchNumber,
            request.Serials,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionEdit, cancellationToken))
        {
            return Forbid();
        }

        await serviceManagementService.RemoveMaterialRequisitionLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionPost, cancellationToken))
        {
            return Forbid();
        }

        await serviceManagementService.PostMaterialRequisitionAsync(id, cancellationToken);
        await NotifyMaterialRequisitionCreatorAsync(id, "Material requisition posted", "Your material requisition has been posted and stock was issued.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult> Void(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ServiceMaterialRequisitionVoid, cancellationToken))
        {
            return Forbid();
        }

        await serviceManagementService.VoidMaterialRequisitionAsync(id, cancellationToken);
        await NotifyMaterialRequisitionCreatorAsync(id, "Material requisition voided", "Your material requisition has been voided.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyMaterialRequisitionCreatedAsync(Guid id, CancellationToken cancellationToken)
    {
        var mr = await dbContext.MaterialRequisitions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy, x.Purpose })
            .FirstOrDefaultAsync(cancellationToken);

        if (mr is null)
        {
            return;
        }

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.ServiceMaterialRequisitionPost],
            excludeUserId: mr.CreatedBy,
            cancellationToken);

        notificationService.EnqueueInAppForUsers(
            recipients,
            "Material requisition waiting",
            $"{mr.Number} is waiting for stock issue/posting.",
            $"/service/material-requisitions/{mr.Id}",
            ReferenceTypes.MaterialRequisition,
            mr.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyMaterialRequisitionCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var mr = await dbContext.MaterialRequisitions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (mr is null || mr.CreatedBy is null || mr.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            mr.CreatedBy.Value,
            title,
            $"{mr.Number}: {message}",
            $"/service/material-requisitions/{mr.Id}",
            ReferenceTypes.MaterialRequisition,
            mr.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
