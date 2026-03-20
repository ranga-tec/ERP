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
    public sealed record GoodsReceiptLineDto(Guid Id, Guid? PurchaseOrderLineId, Guid ItemId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string> Serials);
    public sealed record GoodsReceiptReceiptPlanDto(IReadOnlyList<GoodsReceiptReceiptPlanLineDto> Lines);
    public sealed record GoodsReceiptReceiptPlanLineDto(
        Guid PurchaseOrderLineId,
        Guid ItemId,
        decimal OrderedQuantity,
        decimal PreviouslyReceivedQuantity,
        decimal ReservedInOtherDraftsQuantity,
        decimal AvailableQuantity,
        Guid? GoodsReceiptLineId,
        decimal CurrentQuantity,
        decimal UnitCost,
        string? BatchNumber,
        IReadOnlyList<string> Serials);

    public sealed record CreateGoodsReceiptRequest(Guid PurchaseOrderId, Guid WarehouseId);
    public sealed record AddGoodsReceiptLineRequest(Guid ItemId, Guid? PurchaseOrderLineId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateGoodsReceiptLineRequest(decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateGoodsReceiptReceiptPlanRequest(IReadOnlyList<UpdateGoodsReceiptReceiptPlanLineRequest> Lines);
    public sealed record UpdateGoodsReceiptReceiptPlanLineRequest(Guid PurchaseOrderLineId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);

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
                l.PurchaseOrderLineId,
                l.ItemId,
                l.Quantity,
                l.UnitCost,
                l.BatchNumber,
                l.Serials.Select(s => s.SerialNumber).ToList()))
                .ToList()));
    }

    [HttpGet("{id:guid}/receipt-plan")]
    public async Task<ActionResult<GoodsReceiptReceiptPlanDto>> ReceiptPlan(Guid id, CancellationToken cancellationToken)
    {
        var plan = await BuildReceiptPlanAsync(id, cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
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
        await procurementService.AddGoodsReceiptLineAsync(
            id,
            request.ItemId,
            request.PurchaseOrderLineId,
            request.Quantity,
            request.UnitCost,
            request.BatchNumber,
            request.Serials,
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateGoodsReceiptLineRequest request, CancellationToken cancellationToken)
    {
        await procurementService.UpdateGoodsReceiptLineAsync(id, lineId, request.Quantity, request.UnitCost, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> DeleteLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        await procurementService.RemoveGoodsReceiptLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/receipt-plan")]
    public async Task<ActionResult<GoodsReceiptReceiptPlanDto>> UpdateReceiptPlan(
        Guid id,
        UpdateGoodsReceiptReceiptPlanRequest request,
        CancellationToken cancellationToken)
    {
        await procurementService.ReplaceGoodsReceiptReceiptPlanAsync(
            id,
            request.Lines.Select(x => new ProcurementService.GoodsReceiptReceiptPlanLineInput(
                x.PurchaseOrderLineId,
                x.Quantity,
                x.UnitCost,
                x.BatchNumber,
                x.Serials)).ToList(),
            cancellationToken);

        var plan = await BuildReceiptPlanAsync(id, cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.PostGoodsReceiptAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task<GoodsReceiptReceiptPlanDto?> BuildReceiptPlanAsync(Guid goodsReceiptId, CancellationToken cancellationToken)
    {
        var grn = await dbContext.GoodsReceipts.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(x => x.Serials)
            .FirstOrDefaultAsync(x => x.Id == goodsReceiptId, cancellationToken);

        if (grn is null)
        {
            return null;
        }

        var po = await dbContext.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken);

        if (po is null)
        {
            return null;
        }

        var reservedInOtherDrafts = await dbContext.GoodsReceipts.AsNoTracking()
            .Where(x => x.PurchaseOrderId == po.Id && x.Status == GoodsReceiptStatus.Draft && x.Id != grn.Id)
            .SelectMany(x => x.Lines)
            .Where(x => x.PurchaseOrderLineId != null)
            .GroupBy(x => x.PurchaseOrderLineId!.Value)
            .Select(x => new { PurchaseOrderLineId = x.Key, Quantity = x.Sum(y => y.Quantity) })
            .ToDictionaryAsync(x => x.PurchaseOrderLineId, x => x.Quantity, cancellationToken);

        var explicitLines = grn.Lines
            .Where(x => x.PurchaseOrderLineId != null)
            .GroupBy(x => x.PurchaseOrderLineId!.Value)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).ToList());
        var fallbackLinesByItem = grn.Lines
            .Where(x => x.PurchaseOrderLineId == null)
            .GroupBy(x => x.ItemId)
            .ToDictionary(x => x.Key, x => new Queue<GoodsReceiptLine>(x.OrderBy(y => y.Id)));

        var lines = new List<GoodsReceiptReceiptPlanLineDto>(po.Lines.Count);
        foreach (var poLine in po.Lines)
        {
            GoodsReceiptLine? currentLine = null;
            if (explicitLines.TryGetValue(poLine.Id, out var mappedLines) && mappedLines.Count > 0)
            {
                currentLine = mappedLines[0];
            }
            else if (fallbackLinesByItem.TryGetValue(poLine.ItemId, out var fallbackLines) && fallbackLines.Count > 0)
            {
                currentLine = fallbackLines.Dequeue();
            }

            var reservedQuantity = reservedInOtherDrafts.GetValueOrDefault(poLine.Id);
            var availableQuantity = poLine.OrderedQuantity - poLine.ReceivedQuantity - reservedQuantity;

            lines.Add(new GoodsReceiptReceiptPlanLineDto(
                poLine.Id,
                poLine.ItemId,
                poLine.OrderedQuantity,
                poLine.ReceivedQuantity,
                reservedQuantity,
                Math.Max(0m, availableQuantity),
                currentLine?.Id,
                currentLine?.Quantity ?? 0m,
                currentLine?.UnitCost ?? poLine.UnitPrice,
                currentLine?.BatchNumber,
                currentLine?.Serials.Select(x => x.SerialNumber).ToList() ?? []));
        }

        return new GoodsReceiptReceiptPlanDto(lines);
    }
}
