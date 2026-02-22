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
[Route("api/inventory/stock-transfers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class StockTransfersController(IIssDbContext dbContext, InventoryOperationsService inventoryOperationsService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record StockTransferSummaryDto(Guid Id, string Number, Guid FromWarehouseId, Guid ToWarehouseId, DateTimeOffset TransferDate, StockTransferStatus Status);

    public sealed record StockTransferDto(
        Guid Id,
        string Number,
        Guid FromWarehouseId,
        Guid ToWarehouseId,
        DateTimeOffset TransferDate,
        StockTransferStatus Status,
        string? Notes,
        IReadOnlyList<StockTransferLineDto> Lines);

    public sealed record StockTransferLineDto(
        Guid Id,
        Guid ItemId,
        decimal Quantity,
        decimal UnitCost,
        string? BatchNumber,
        IReadOnlyList<string> Serials);

    public sealed record CreateStockTransferRequest(Guid FromWarehouseId, Guid ToWarehouseId, string? Notes);
    public sealed record AddStockTransferLineRequest(Guid ItemId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockTransferSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var transfers = await dbContext.StockTransfers.AsNoTracking()
            .OrderByDescending(x => x.TransferDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new StockTransferSummaryDto(x.Id, x.Number, x.FromWarehouseId, x.ToWarehouseId, x.TransferDate, x.Status))
            .ToListAsync(cancellationToken);

        return Ok(transfers);
    }

    [HttpPost]
    public async Task<ActionResult<StockTransferDto>> Create(CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        var id = await inventoryOperationsService.CreateStockTransferAsync(request.FromWarehouseId, request.ToWarehouseId, request.Notes, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockTransferDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await dbContext.StockTransfers.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (transfer is null)
        {
            return NotFound();
        }

        return Ok(new StockTransferDto(
            transfer.Id,
            transfer.Number,
            transfer.FromWarehouseId,
            transfer.ToWarehouseId,
            transfer.TransferDate,
            transfer.Status,
            transfer.Notes,
            transfer.Lines.Select(l => new StockTransferLineDto(
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
        var doc = await pdfService.RenderAsync(PdfDocumentType.StockTransfer, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddStockTransferLineRequest request, CancellationToken cancellationToken)
    {
        await inventoryOperationsService.AddStockTransferLineAsync(
            id,
            request.ItemId,
            request.Quantity,
            request.UnitCost,
            request.BatchNumber,
            request.Serials,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await inventoryOperationsService.PostStockTransferAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult> Void(Guid id, CancellationToken cancellationToken)
    {
        await inventoryOperationsService.VoidStockTransferAsync(id, cancellationToken);
        return NoContent();
    }
}
