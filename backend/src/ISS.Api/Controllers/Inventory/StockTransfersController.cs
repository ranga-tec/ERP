using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
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
public sealed class StockTransfersController(
    IIssDbContext dbContext,
    InventoryOperationsService inventoryOperationsService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
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
    public sealed record UpdateStockTransferLineRequest(decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockTransferSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferView, cancellationToken))
        {
            return Forbid();
        }

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
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await inventoryOperationsService.CreateStockTransferAsync(request.FromWarehouseId, request.ToWarehouseId, request.Notes, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockTransferDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferView, cancellationToken))
        {
            return Forbid();
        }

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
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.StockTransfer, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddStockTransferLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferEdit, cancellationToken))
        {
            return Forbid();
        }

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

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateStockTransferLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferEdit, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.UpdateStockTransferLineAsync(
            id,
            lineId,
            request.Quantity,
            request.UnitCost,
            request.BatchNumber,
            request.Serials,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferEdit, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.RemoveStockTransferLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferPost, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.PostStockTransferAsync(id, cancellationToken);
        await NotifyStockTransferCreatorAsync(id, "Stock transfer posted", "Your stock transfer has been posted.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult> Void(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockTransferVoid, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.VoidStockTransferAsync(id, cancellationToken);
        await NotifyStockTransferCreatorAsync(id, "Stock transfer voided", "Your stock transfer has been voided.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyStockTransferCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var transfer = await dbContext.StockTransfers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (transfer is null || transfer.CreatedBy is null || transfer.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            transfer.CreatedBy.Value,
            title,
            $"{transfer.Number}: {message}",
            $"/inventory/stock-transfers/{transfer.Id}",
            ReferenceTypes.StockTransfer,
            transfer.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
