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
[Route("api/sales/customer-returns")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Inventory},{Roles.Finance}")]
public sealed class CustomerReturnsController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record CustomerReturnSummaryDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        Guid WarehouseId,
        DateTimeOffset ReturnDate,
        CustomerReturnStatus Status,
        Guid? SalesInvoiceId,
        Guid? DispatchNoteId,
        string? Reason);

    public sealed record CustomerReturnLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, string? BatchNumber, IReadOnlyList<string> Serials);
    public sealed record CustomerReturnDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        Guid WarehouseId,
        DateTimeOffset ReturnDate,
        CustomerReturnStatus Status,
        Guid? SalesInvoiceId,
        Guid? DispatchNoteId,
        string? Reason,
        IReadOnlyList<CustomerReturnLineDto> Lines);

    public sealed record CreateCustomerReturnRequest(Guid CustomerId, Guid WarehouseId, Guid? SalesInvoiceId, Guid? DispatchNoteId, string? Reason);
    public sealed record AddCustomerReturnLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerReturnSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.CustomerReturns.AsNoTracking()
            .OrderByDescending(x => x.ReturnDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new CustomerReturnSummaryDto(
                x.Id,
                x.Number,
                x.CustomerId,
                x.WarehouseId,
                x.ReturnDate,
                x.Status,
                x.SalesInvoiceId,
                x.DispatchNoteId,
                x.Reason))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerReturnDto>> Create(CreateCustomerReturnRequest request, CancellationToken cancellationToken)
    {
        var id = await salesService.CreateCustomerReturnAsync(
            request.CustomerId,
            request.WarehouseId,
            request.SalesInvoiceId,
            request.DispatchNoteId,
            request.Reason,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerReturnDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var cr = await dbContext.CustomerReturns.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (cr is null)
        {
            return NotFound();
        }

        return Ok(new CustomerReturnDto(
            cr.Id,
            cr.Number,
            cr.CustomerId,
            cr.WarehouseId,
            cr.ReturnDate,
            cr.Status,
            cr.SalesInvoiceId,
            cr.DispatchNoteId,
            cr.Reason,
            cr.Lines.Select(l => new CustomerReturnLineDto(l.Id, l.ItemId, l.Quantity, l.UnitPrice, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.CustomerReturn, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddCustomerReturnLineRequest request, CancellationToken cancellationToken)
    {
        await salesService.AddCustomerReturnLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await salesService.PostCustomerReturnAsync(id, cancellationToken);
        return NoContent();
    }
}
