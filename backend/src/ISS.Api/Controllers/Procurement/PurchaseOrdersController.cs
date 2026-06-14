using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Procurement;

[ApiController]
[Route("api/procurement/purchase-orders")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement}")]
public sealed class PurchaseOrdersController(
    IIssDbContext dbContext,
    ProcurementService procurementService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record PurchaseOrderSummaryDto(
        Guid Id,
        string Number,
        Guid SupplierId,
        string? SupplierCode,
        string? SupplierName,
        DateTimeOffset OrderDate,
        PurchaseOrderStatus Status,
        decimal Total);

    public sealed record PurchaseOrderDto(
        Guid Id,
        string Number,
        Guid SupplierId,
        string? SupplierCode,
        string? SupplierName,
        DateTimeOffset OrderDate,
        PurchaseOrderStatus Status,
        decimal Total,
        IReadOnlyList<PurchaseOrderLineDto> Lines);

    public sealed record PurchaseOrderLineDto(
        Guid Id,
        Guid ItemId,
        string? ItemSku,
        string? ItemName,
        decimal OrderedQuantity,
        decimal ReceivedQuantity,
        decimal UnitPrice,
        decimal LineTotal);

    public sealed record CreatePurchaseOrderRequest(Guid SupplierId);
    public sealed record AddPurchaseOrderLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice);
    public sealed record UpdatePurchaseOrderLineRequest(decimal Quantity, decimal UnitPrice);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseOrderView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var pos = await dbContext.PurchaseOrders.AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new PurchaseOrderSummaryDto(
                x.Id,
                x.Number,
                x.SupplierId,
                dbContext.Suppliers
                    .Where(s => s.Id == x.SupplierId)
                    .Select(s => s.Code)
                    .FirstOrDefault(),
                dbContext.Suppliers
                    .Where(s => s.Id == x.SupplierId)
                    .Select(s => s.Name)
                    .FirstOrDefault(),
                x.OrderDate,
                x.Status,
                x.Lines.Sum(l => l.OrderedQuantity * l.UnitPrice)))
            .ToListAsync(cancellationToken);

        return Ok(pos);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseOrderCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await procurementService.CreatePurchaseOrderAsync(request.SupplierId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseOrderView, cancellationToken))
        {
            return Forbid();
        }

        var po = await dbContext.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (po is null)
        {
            return NotFound();
        }

        var itemIds = po.Lines.Select(x => x.ItemId).Distinct().ToArray();
        var itemById = await dbContext.Items.AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Sku, x.Name })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var supplier = await dbContext.Suppliers.AsNoTracking()
            .Where(x => x.Id == po.SupplierId)
            .Select(x => new { x.Code, x.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new PurchaseOrderDto(
            po.Id,
            po.Number,
            po.SupplierId,
            supplier?.Code,
            supplier?.Name,
            po.OrderDate,
            po.Status,
            po.Total,
            po.Lines.Select(l => new PurchaseOrderLineDto(
                l.Id,
                l.ItemId,
                itemById.GetValueOrDefault(l.ItemId)?.Sku,
                itemById.GetValueOrDefault(l.ItemId)?.Name,
                l.OrderedQuantity,
                l.ReceivedQuantity,
                l.UnitPrice,
                l.LineTotal)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseOrderView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.PurchaseOrder, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddPurchaseOrderLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseOrderEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.AddPurchaseOrderLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdatePurchaseOrderLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseOrderEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.UpdatePurchaseOrderLineAsync(id, lineId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> DeleteLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseOrderEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.RemovePurchaseOrderLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementPurchaseOrderApprove, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.ApprovePurchaseOrderAsync(id, cancellationToken);
        await NotifyPurchaseOrderCreatorAsync(id, "Purchase order approved", "Your purchase order has been approved.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyPurchaseOrderCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var po = await dbContext.PurchaseOrders.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (po is null || po.CreatedBy is null || po.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            po.CreatedBy.Value,
            title,
            $"{po.Number}: {message}",
            $"/procurement/purchase-orders/{po.Id}",
            ReferenceTypes.PurchaseOrder,
            po.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
