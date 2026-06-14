using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Sales;

[ApiController]
[Route("api/sales/dispatches")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Inventory},{Roles.Finance}")]
public sealed class DispatchesController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record DispatchSummaryDto(Guid Id, string Number, Guid SalesOrderId, Guid WarehouseId, DateTimeOffset DispatchedAt, DispatchStatus Status, DateTimeOffset? WarrantyUntil, ServiceCoverageScope WarrantyCoverage, int? ServiceIntervalDays, DateTimeOffset? NextServiceDueAt, int LineCount);
    public sealed record DispatchDto(Guid Id, string Number, Guid SalesOrderId, Guid WarehouseId, DateTimeOffset DispatchedAt, DispatchStatus Status, DateTimeOffset? WarrantyUntil, ServiceCoverageScope WarrantyCoverage, int? ServiceIntervalDays, DateTimeOffset? NextServiceDueAt, IReadOnlyList<DispatchLineDto> Lines);
    public sealed record DispatchLineDto(Guid Id, Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string> Serials);

    public sealed record CreateDispatchRequest(Guid SalesOrderId, Guid WarehouseId, DateTimeOffset? WarrantyUntil, ServiceCoverageScope? WarrantyCoverage, int? ServiceIntervalDays, DateTimeOffset? NextServiceDueAt);
    public sealed record AddDispatchLineRequest(Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateDispatchLineRequest(decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DispatchSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDispatchView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var dispatches = await dbContext.DispatchNotes.AsNoTracking()
            .OrderByDescending(x => x.DispatchedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new DispatchSummaryDto(x.Id, x.Number, x.SalesOrderId, x.WarehouseId, x.DispatchedAt, x.Status, x.WarrantyUntil, x.WarrantyCoverage, x.ServiceIntervalDays, x.NextServiceDueAt, x.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(dispatches);
    }

    [HttpPost]
    public async Task<ActionResult<DispatchDto>> Create(CreateDispatchRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDispatchCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateDispatchAsync(
            request.SalesOrderId,
            request.WarehouseId,
            request.WarrantyUntil,
            request.WarrantyUntil is null ? ServiceCoverageScope.None : request.WarrantyCoverage ?? ServiceCoverageScope.LaborAndParts,
            request.ServiceIntervalDays,
            request.NextServiceDueAt,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DispatchDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDispatchView, cancellationToken))
        {
            return Forbid();
        }

        var dispatch = await dbContext.DispatchNotes.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (dispatch is null)
        {
            return NotFound();
        }

        return Ok(new DispatchDto(
            dispatch.Id,
            dispatch.Number,
            dispatch.SalesOrderId,
            dispatch.WarehouseId,
            dispatch.DispatchedAt,
            dispatch.Status,
            dispatch.WarrantyUntil,
            dispatch.WarrantyCoverage,
            dispatch.ServiceIntervalDays,
            dispatch.NextServiceDueAt,
            dispatch.Lines.Select(l => new DispatchLineDto(l.Id, l.ItemId, l.Quantity, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDispatchView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.DispatchNote, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddDispatchLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.AddDispatchLineAsync(id, request.ItemId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateDispatchLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.UpdateDispatchLineAsync(id, lineId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.RemoveDispatchLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDispatchPost, cancellationToken))
        {
            return Forbid();
        }

        await salesService.PostDispatchAsync(id, cancellationToken);
        await NotifyDispatchCreatorAsync(id, "Dispatch posted", "Your dispatch note has been posted.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyDispatchCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var dispatch = await dbContext.DispatchNotes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (dispatch is null || dispatch.CreatedBy is null || dispatch.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            dispatch.CreatedBy.Value,
            title,
            $"{dispatch.Number}: {message}",
            $"/sales/dispatches/{dispatch.Id}",
            ReferenceTypes.DispatchNote,
            dispatch.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
