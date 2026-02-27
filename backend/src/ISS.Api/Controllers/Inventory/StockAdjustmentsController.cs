using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Inventory;

[ApiController]
[Route("api/inventory/stock-adjustments")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class StockAdjustmentsController(IIssDbContext dbContext, InventoryOperationsService inventoryOperationsService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record StockAdjustmentSummaryDto(Guid Id, string Number, Guid WarehouseId, DateTimeOffset AdjustedAt, StockAdjustmentStatus Status);

    public sealed record StockAdjustmentDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        DateTimeOffset AdjustedAt,
        StockAdjustmentStatus Status,
        string? Reason,
        IReadOnlyList<StockAdjustmentLineDto> Lines);

    public sealed record StockAdjustmentLineDto(
        Guid Id,
        Guid ItemId,
        decimal QuantityDelta,
        decimal UnitCost,
        string? BatchNumber,
        IReadOnlyList<string> Serials);

    public sealed record CreateStockAdjustmentRequest(Guid WarehouseId, string? Reason);
    public sealed record AddStockAdjustmentLineRequest(Guid ItemId, decimal QuantityDelta, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateStockAdjustmentLineRequest(decimal QuantityDelta, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockAdjustmentSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var adjustments = await dbContext.StockAdjustments.AsNoTracking()
            .OrderByDescending(x => x.AdjustedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new StockAdjustmentSummaryDto(x.Id, x.Number, x.WarehouseId, x.AdjustedAt, x.Status))
            .ToListAsync(cancellationToken);

        return Ok(adjustments);
    }

    [HttpPost]
    public async Task<ActionResult<StockAdjustmentDto>> Create(CreateStockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var id = await inventoryOperationsService.CreateStockAdjustmentAsync(request.WarehouseId, request.Reason, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockAdjustmentDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var adj = await dbContext.StockAdjustments.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (adj is null)
        {
            return NotFound();
        }

        return Ok(new StockAdjustmentDto(
            adj.Id,
            adj.Number,
            adj.WarehouseId,
            adj.AdjustedAt,
            adj.Status,
            adj.Reason,
            adj.Lines.Select(l => new StockAdjustmentLineDto(
                l.Id,
                l.ItemId,
                l.QuantityDelta,
                l.UnitCost,
                l.BatchNumber,
                l.Serials.Select(s => s.SerialNumber).ToList()))
                .ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.StockAdjustment, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddStockAdjustmentLineRequest request, CancellationToken cancellationToken)
    {
        await inventoryOperationsService.AddStockAdjustmentLineAsync(
            id,
            request.ItemId,
            request.QuantityDelta,
            request.UnitCost,
            request.BatchNumber,
            request.Serials,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateStockAdjustmentLineRequest request, CancellationToken cancellationToken)
    {
        await inventoryOperationsService.UpdateStockAdjustmentLineAsync(
            id,
            lineId,
            request.QuantityDelta,
            request.UnitCost,
            request.BatchNumber,
            request.Serials,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        await inventoryOperationsService.RemoveStockAdjustmentLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await inventoryOperationsService.PostStockAdjustmentAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult> Void(Guid id, CancellationToken cancellationToken)
    {
        await inventoryOperationsService.VoidStockAdjustmentAsync(id, cancellationToken);
        return NoContent();
    }
}
