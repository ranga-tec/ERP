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
[Route("api/procurement/supplier-returns")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement},{Roles.Inventory}")]
public sealed class SupplierReturnsController(IIssDbContext dbContext, ProcurementService procurementService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record SupplierReturnSummaryDto(Guid Id, string Number, Guid SupplierId, Guid WarehouseId, DateTimeOffset ReturnDate, SupplierReturnStatus Status, string? Reason);
    public sealed record SupplierReturnDto(Guid Id, string Number, Guid SupplierId, Guid WarehouseId, DateTimeOffset ReturnDate, SupplierReturnStatus Status, string? Reason, IReadOnlyList<SupplierReturnLineDto> Lines);
    public sealed record SupplierReturnLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string> Serials);

    public sealed record CreateSupplierReturnRequest(Guid SupplierId, Guid WarehouseId, string? Reason);
    public sealed record AddSupplierReturnLineRequest(Guid ItemId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateSupplierReturnLineRequest(decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierReturnSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var returns = await dbContext.SupplierReturns.AsNoTracking()
            .OrderByDescending(x => x.ReturnDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new SupplierReturnSummaryDto(x.Id, x.Number, x.SupplierId, x.WarehouseId, x.ReturnDate, x.Status, x.Reason))
            .ToListAsync(cancellationToken);

        return Ok(returns);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierReturnDto>> Create(CreateSupplierReturnRequest request, CancellationToken cancellationToken)
    {
        var id = await procurementService.CreateSupplierReturnAsync(request.SupplierId, request.WarehouseId, request.Reason, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierReturnDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var sr = await dbContext.SupplierReturns.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sr is null)
        {
            return NotFound();
        }

        return Ok(new SupplierReturnDto(
            sr.Id,
            sr.Number,
            sr.SupplierId,
            sr.WarehouseId,
            sr.ReturnDate,
            sr.Status,
            sr.Reason,
            sr.Lines.Select(l => new SupplierReturnLineDto(
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
        var doc = await pdfService.RenderAsync(PdfDocumentType.SupplierReturn, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddSupplierReturnLineRequest request, CancellationToken cancellationToken)
    {
        await procurementService.AddSupplierReturnLineAsync(id, request.ItemId, request.Quantity, request.UnitCost, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateSupplierReturnLineRequest request, CancellationToken cancellationToken)
    {
        await procurementService.UpdateSupplierReturnLineAsync(
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
        await procurementService.RemoveSupplierReturnLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.PostSupplierReturnAsync(id, cancellationToken);
        return NoContent();
    }
}
