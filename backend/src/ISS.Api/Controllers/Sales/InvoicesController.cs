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
[Route("api/sales/invoices")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Finance}")]
public sealed class InvoicesController(IIssDbContext dbContext, SalesService salesService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record InvoiceSummaryDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset InvoiceDate, DateTimeOffset? DueDate, SalesInvoiceStatus Status, decimal Total);
    public sealed record InvoiceDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset InvoiceDate, DateTimeOffset? DueDate, SalesInvoiceStatus Status, decimal Subtotal, decimal TaxTotal, decimal Total, IReadOnlyList<InvoiceLineDto> Lines);
    public sealed record InvoiceLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxPercent, decimal LineTotal);

    public sealed record CreateInvoiceRequest(Guid CustomerId, DateTimeOffset? DueDate);
    public sealed record AddInvoiceLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxPercent);
    public sealed record UpdateInvoiceLineRequest(decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxPercent);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var invoices = await dbContext.SalesInvoices.AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new InvoiceSummaryDto(
                x.Id,
                x.Number,
                x.CustomerId,
                x.InvoiceDate,
                x.DueDate,
                x.Status,
                x.Lines.Sum(l => (l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m)) * (1m + (l.TaxPercent / 100m)))))
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var id = await salesService.CreateInvoiceAsync(request.CustomerId, request.DueDate, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SalesInvoices.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(new InvoiceDto(
            invoice.Id,
            invoice.Number,
            invoice.CustomerId,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.Status,
            invoice.Subtotal,
            invoice.TaxTotal,
            invoice.Total,
            invoice.Lines.Select(l => new InvoiceLineDto(l.Id, l.ItemId, l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxPercent, l.LineTotal)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.SalesInvoice, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddInvoiceLineRequest request, CancellationToken cancellationToken)
    {
        await salesService.AddInvoiceLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, request.DiscountPercent, request.TaxPercent, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateInvoiceLineRequest request, CancellationToken cancellationToken)
    {
        await salesService.UpdateInvoiceLineAsync(
            id,
            lineId,
            request.Quantity,
            request.UnitPrice,
            request.DiscountPercent,
            request.TaxPercent,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        await salesService.RemoveInvoiceLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await salesService.PostInvoiceAsync(id, cancellationToken);
        return NoContent();
    }
}
