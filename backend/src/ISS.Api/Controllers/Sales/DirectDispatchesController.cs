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
[Route("api/sales/direct-dispatches")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Inventory},{Roles.Service}")]
public sealed class DirectDispatchesController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record DirectDispatchSummaryDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        Guid? CustomerId,
        Guid? ServiceJobId,
        DateTimeOffset DispatchedAt,
        DirectDispatchStatus Status,
        DateTimeOffset? WarrantyUntil,
        ServiceCoverageScope WarrantyCoverage,
        int? ServiceIntervalDays,
        DateTimeOffset? NextServiceDueAt,
        string? Reason,
        int LineCount);

    public sealed record DirectDispatchLineDto(Guid Id, Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string> Serials);
    public sealed record DirectDispatchDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        Guid? CustomerId,
        Guid? ServiceJobId,
        DateTimeOffset DispatchedAt,
        DirectDispatchStatus Status,
        DateTimeOffset? WarrantyUntil,
        ServiceCoverageScope WarrantyCoverage,
        int? ServiceIntervalDays,
        DateTimeOffset? NextServiceDueAt,
        string? Reason,
        IReadOnlyList<DirectDispatchLineDto> Lines);

    public sealed record CreateDirectDispatchRequest(Guid WarehouseId, Guid? CustomerId, Guid? ServiceJobId, string? Reason, DateTimeOffset? WarrantyUntil, ServiceCoverageScope? WarrantyCoverage, int? ServiceIntervalDays, DateTimeOffset? NextServiceDueAt);
    public sealed record AddDirectDispatchLineRequest(Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateDirectDispatchLineRequest(decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DirectDispatchSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.DirectDispatches.AsNoTracking()
            .OrderByDescending(x => x.DispatchedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new DirectDispatchSummaryDto(
                x.Id,
                x.Number,
                x.WarehouseId,
                x.CustomerId,
                x.ServiceJobId,
                x.DispatchedAt,
                x.Status,
                x.WarrantyUntil,
                x.WarrantyCoverage,
                x.ServiceIntervalDays,
                x.NextServiceDueAt,
                x.Reason,
                x.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<DirectDispatchDto>> Create(CreateDirectDispatchRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateDirectDispatchAsync(
            request.WarehouseId,
            request.CustomerId,
            request.ServiceJobId,
            request.Reason,
            request.WarrantyUntil,
            request.WarrantyUntil is null ? ServiceCoverageScope.None : request.WarrantyCoverage ?? ServiceCoverageScope.LaborAndParts,
            request.ServiceIntervalDays,
            request.NextServiceDueAt,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DirectDispatchDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchView, cancellationToken))
        {
            return Forbid();
        }

        var dispatch = await dbContext.DirectDispatches.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (dispatch is null)
        {
            return NotFound();
        }

        return Ok(new DirectDispatchDto(
            dispatch.Id,
            dispatch.Number,
            dispatch.WarehouseId,
            dispatch.CustomerId,
            dispatch.ServiceJobId,
            dispatch.DispatchedAt,
            dispatch.Status,
            dispatch.WarrantyUntil,
            dispatch.WarrantyCoverage,
            dispatch.ServiceIntervalDays,
            dispatch.NextServiceDueAt,
            dispatch.Reason,
            dispatch.Lines.Select(l => new DirectDispatchLineDto(l.Id, l.ItemId, l.Quantity, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.DirectDispatch, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddDirectDispatchLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.AddDirectDispatchLineAsync(id, request.ItemId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateDirectDispatchLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.UpdateDirectDispatchLineAsync(id, lineId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.RemoveDirectDispatchLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchPost, cancellationToken))
        {
            return Forbid();
        }

        await salesService.PostDirectDispatchAsync(id, cancellationToken);
        await NotifyDirectDispatchCreatorAsync(id, "Direct dispatch posted", "Your direct dispatch has been posted.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyDirectDispatchCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var dispatch = await dbContext.DirectDispatches.AsNoTracking()
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
            $"/sales/direct-dispatches/{dispatch.Id}",
            ReferenceTypes.DirectDispatch,
            dispatch.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
