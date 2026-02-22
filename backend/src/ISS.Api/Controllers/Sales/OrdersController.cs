using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Sales;

[ApiController]
[Route("api/sales/orders")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales}")]
public sealed class OrdersController(IIssDbContext dbContext, SalesService salesService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record SalesOrderSummaryDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset OrderDate, SalesOrderStatus Status, decimal Total);
    public sealed record SalesOrderDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset OrderDate, SalesOrderStatus Status, decimal Total, IReadOnlyList<SalesOrderLineDto> Lines);
    public sealed record SalesOrderLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal LineTotal);

    public sealed record CreateOrderRequest(Guid CustomerId);
    public sealed record AddOrderLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesOrderSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
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
        var id = await salesService.CreateOrderAsync(request.CustomerId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
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
        var doc = await pdfService.RenderAsync(PdfDocumentType.SalesOrder, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddOrderLineRequest request, CancellationToken cancellationToken)
    {
        await salesService.AddOrderLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        await salesService.ConfirmOrderAsync(id, cancellationToken);
        return NoContent();
    }
}
