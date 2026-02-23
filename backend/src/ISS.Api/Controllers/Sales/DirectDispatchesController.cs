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
[Route("api/sales/direct-dispatches")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Inventory},{Roles.Service}")]
public sealed class DirectDispatchesController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record DirectDispatchSummaryDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        Guid? CustomerId,
        Guid? ServiceJobId,
        DateTimeOffset DispatchedAt,
        DirectDispatchStatus Status,
        string? Reason,
        int LineCount);

    public sealed record DirectDispatchLineDto(Guid Id, Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string> Serials);
    public sealed record DirectDispatchDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        Guid? CustomerId,
        Guid? ServiceJobId,
        DateTimeOffset DispatchedAt,
        DirectDispatchStatus Status,
        string? Reason,
        IReadOnlyList<DirectDispatchLineDto> Lines);

    public sealed record CreateDirectDispatchRequest(Guid WarehouseId, Guid? CustomerId, Guid? ServiceJobId, string? Reason);
    public sealed record AddDirectDispatchLineRequest(Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DirectDispatchSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.DirectDispatches.AsNoTracking()
            .OrderByDescending(x => x.DispatchedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new DirectDispatchSummaryDto(
                x.Id,
                x.Number,
                x.WarehouseId,
                x.CustomerId,
                x.ServiceJobId,
                x.DispatchedAt,
                x.Status,
                x.Reason,
                x.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<DirectDispatchDto>> Create(CreateDirectDispatchRequest request, CancellationToken cancellationToken)
    {
        var id = await salesService.CreateDirectDispatchAsync(
            request.WarehouseId,
            request.CustomerId,
            request.ServiceJobId,
            request.Reason,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DirectDispatchDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dispatch = await dbContext.DirectDispatches.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (dispatch is null)
        {
            return NotFound();
        }

        return Ok(new DirectDispatchDto(
            dispatch.Id,
            dispatch.Number,
            dispatch.WarehouseId,
            dispatch.CustomerId,
            dispatch.ServiceJobId,
            dispatch.DispatchedAt,
            dispatch.Status,
            dispatch.Reason,
            dispatch.Lines.Select(l => new DirectDispatchLineDto(l.Id, l.ItemId, l.Quantity, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.DirectDispatch, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddDirectDispatchLineRequest request, CancellationToken cancellationToken)
    {
        await salesService.AddDirectDispatchLineAsync(id, request.ItemId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await salesService.PostDirectDispatchAsync(id, cancellationToken);
        return NoContent();
    }
}
