using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Sales;

[ApiController]
[Route("api/sales/orders")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Finance},{Roles.Inventory}")]
public sealed class OrdersController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record SalesOrderSummaryDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset OrderDate, SalesOrderStatus Status, decimal Total);
    public sealed record SalesOrderDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset OrderDate, SalesOrderStatus Status, decimal Total, IReadOnlyList<SalesOrderLineDto> Lines);
    public sealed record SalesOrderLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal LineTotal);

    public sealed record CreateOrderRequest(Guid CustomerId);
    public sealed record AddOrderLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice);
    public sealed record UpdateOrderLineRequest(decimal Quantity, decimal UnitPrice);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesOrderSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesOrderView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var orders = await dbContext.SalesOrders.AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new SalesOrderSummaryDto(
                x.Id,
                x.Number,
                x.CustomerId,
                x.OrderDate,
                x.Status,
                x.Lines.Sum(l => l.Quantity * l.UnitPrice)))
            .ToListAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<SalesOrderDto>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesOrderCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateOrderAsync(request.CustomerId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesOrderView, cancellationToken))
        {
            return Forbid();
        }

        var order = await dbContext.SalesOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(new SalesOrderDto(
            order.Id,
            order.Number,
            order.CustomerId,
            order.OrderDate,
            order.Status,
            order.Total,
            order.Lines.Select(l => new SalesOrderLineDto(l.Id, l.ItemId, l.Quantity, l.UnitPrice, l.LineTotal)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesOrderView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.SalesOrder, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddOrderLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesOrderEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.AddOrderLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateOrderLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesOrderEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.UpdateOrderLineAsync(id, lineId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesOrderEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.RemoveOrderLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesOrderConfirm, cancellationToken))
        {
            return Forbid();
        }

        await salesService.ConfirmOrderAsync(id, cancellationToken);
        await NotifyOrderCreatorAsync(id, "Sales order confirmed", "Your sales order has been confirmed.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyOrderCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var order = await dbContext.SalesOrders.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null || order.CreatedBy is null || order.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            order.CreatedBy.Value,
            title,
            $"{order.Number}: {message}",
            $"/sales/orders/{order.Id}",
            ReferenceTypes.SalesOrder,
            order.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
