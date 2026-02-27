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
[Route("api/sales/dispatches")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Inventory}")]
public sealed class DispatchesController(IIssDbContext dbContext, SalesService salesService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record DispatchSummaryDto(Guid Id, string Number, Guid SalesOrderId, Guid WarehouseId, DateTimeOffset DispatchedAt, DispatchStatus Status, int LineCount);
    public sealed record DispatchDto(Guid Id, string Number, Guid SalesOrderId, Guid WarehouseId, DateTimeOffset DispatchedAt, DispatchStatus Status, IReadOnlyList<DispatchLineDto> Lines);
    public sealed record DispatchLineDto(Guid Id, Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string> Serials);

    public sealed record CreateDispatchRequest(Guid SalesOrderId, Guid WarehouseId);
    public sealed record AddDispatchLineRequest(Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateDispatchLineRequest(decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DispatchSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var dispatches = await dbContext.DispatchNotes.AsNoTracking()
            .OrderByDescending(x => x.DispatchedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new DispatchSummaryDto(x.Id, x.Number, x.SalesOrderId, x.WarehouseId, x.DispatchedAt, x.Status, x.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(dispatches);
    }

    [HttpPost]
    public async Task<ActionResult<DispatchDto>> Create(CreateDispatchRequest request, CancellationToken cancellationToken)
    {
        var id = await salesService.CreateDispatchAsync(request.SalesOrderId, request.WarehouseId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DispatchDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dispatch = await dbContext.DispatchNotes.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (dispatch is null)
        {
            return NotFound();
        }

        return Ok(new DispatchDto(
            dispatch.Id,
            dispatch.Number,
            dispatch.SalesOrderId,
            dispatch.WarehouseId,
            dispatch.DispatchedAt,
            dispatch.Status,
            dispatch.Lines.Select(l => new DispatchLineDto(l.Id, l.ItemId, l.Quantity, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.DispatchNote, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddDispatchLineRequest request, CancellationToken cancellationToken)
    {
        await salesService.AddDispatchLineAsync(id, request.ItemId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateDispatchLineRequest request, CancellationToken cancellationToken)
    {
        await salesService.UpdateDispatchLineAsync(id, lineId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        await salesService.RemoveDispatchLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await salesService.PostDispatchAsync(id, cancellationToken);
        return NoContent();
    }
}
