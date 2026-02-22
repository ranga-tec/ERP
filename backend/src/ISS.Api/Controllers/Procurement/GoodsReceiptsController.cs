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
[Route("api/procurement/goods-receipts")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement},{Roles.Inventory}")]
public sealed class GoodsReceiptsController(IIssDbContext dbContext, ProcurementService procurementService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record GoodsReceiptSummaryDto(Guid Id, string Number, Guid PurchaseOrderId, Guid WarehouseId, DateTimeOffset ReceivedAt, GoodsReceiptStatus Status);
    public sealed record GoodsReceiptDto(Guid Id, string Number, Guid PurchaseOrderId, Guid WarehouseId, DateTimeOffset ReceivedAt, GoodsReceiptStatus Status, IReadOnlyList<GoodsReceiptLineDto> Lines);
    public sealed record GoodsReceiptLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string> Serials);

    public sealed record CreateGoodsReceiptRequest(Guid PurchaseOrderId, Guid WarehouseId);
    public sealed record AddGoodsReceiptLineRequest(Guid ItemId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoodsReceiptSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var grns = await dbContext.GoodsReceipts.AsNoTracking()
            .OrderByDescending(x => x.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new GoodsReceiptSummaryDto(x.Id, x.Number, x.PurchaseOrderId, x.WarehouseId, x.ReceivedAt, x.Status))
            .ToListAsync(cancellationToken);

        return Ok(grns);
    }

    [HttpPost]
    public async Task<ActionResult<GoodsReceiptDto>> Create(CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        var id = await procurementService.CreateGoodsReceiptAsync(request.PurchaseOrderId, request.WarehouseId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoodsReceiptDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var grn = await dbContext.GoodsReceipts.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (grn is null)
        {
            return NotFound();
        }

        return Ok(new GoodsReceiptDto(
            grn.Id,
            grn.Number,
            grn.PurchaseOrderId,
            grn.WarehouseId,
            grn.ReceivedAt,
            grn.Status,
            grn.Lines.Select(l => new GoodsReceiptLineDto(
                l.Id,
                l.ItemId,
                l.Quantity,
                l.UnitCost,
                l.BatchNumber,
                l.Serials.Select(s => s.SerialNumber).ToList()))
                .ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.GoodsReceipt, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddGoodsReceiptLineRequest request, CancellationToken cancellationToken)
    {
        await procurementService.AddGoodsReceiptLineAsync(id, request.ItemId, request.Quantity, request.UnitCost, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.PostGoodsReceiptAsync(id, cancellationToken);
        return NoContent();
    }
}
