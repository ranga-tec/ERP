using ISS.Api.Security;
using ISS.Application.Abstractions;
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
public sealed class PurchaseOrdersController(IIssDbContext dbContext, ProcurementService procurementService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record PurchaseOrderSummaryDto(Guid Id, string Number, Guid SupplierId, DateTimeOffset OrderDate, PurchaseOrderStatus Status, decimal Total);
    public sealed record PurchaseOrderDto(Guid Id, string Number, Guid SupplierId, DateTimeOffset OrderDate, PurchaseOrderStatus Status, decimal Total, IReadOnlyList<PurchaseOrderLineDto> Lines);
    public sealed record PurchaseOrderLineDto(Guid Id, Guid ItemId, decimal OrderedQuantity, decimal ReceivedQuantity, decimal UnitPrice, decimal LineTotal);

    public sealed record CreatePurchaseOrderRequest(Guid SupplierId);
    public sealed record AddPurchaseOrderLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice);
    public sealed record UpdatePurchaseOrderLineRequest(decimal Quantity, decimal UnitPrice);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
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
                x.OrderDate,
                x.Status,
                x.Lines.Sum(l => l.OrderedQuantity * l.UnitPrice)))
            .ToListAsync(cancellationToken);

        return Ok(pos);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var id = await procurementService.CreatePurchaseOrderAsync(request.SupplierId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var po = await dbContext.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (po is null)
        {
            return NotFound();
        }

        return Ok(new PurchaseOrderDto(
            po.Id,
            po.Number,
            po.SupplierId,
            po.OrderDate,
            po.Status,
            po.Total,
            po.Lines.Select(l => new PurchaseOrderLineDto(l.Id, l.ItemId, l.OrderedQuantity, l.ReceivedQuantity, l.UnitPrice, l.LineTotal)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.PurchaseOrder, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddPurchaseOrderLineRequest request, CancellationToken cancellationToken)
    {
        await procurementService.AddPurchaseOrderLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdatePurchaseOrderLineRequest request, CancellationToken cancellationToken)
    {
        await procurementService.UpdatePurchaseOrderLineAsync(id, lineId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> DeleteLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        await procurementService.RemovePurchaseOrderLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.ApprovePurchaseOrderAsync(id, cancellationToken);
        return NoContent();
    }
}
