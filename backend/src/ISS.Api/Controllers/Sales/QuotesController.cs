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
[Route("api/sales/quotes")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales}")]
public sealed class QuotesController(IIssDbContext dbContext, SalesService salesService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record SalesQuoteSummaryDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset QuoteDate, DateTimeOffset? ValidUntil, SalesQuoteStatus Status, decimal Total);
    public sealed record SalesQuoteDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset QuoteDate, DateTimeOffset? ValidUntil, SalesQuoteStatus Status, decimal Total, IReadOnlyList<SalesQuoteLineDto> Lines);
    public sealed record SalesQuoteLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal LineTotal);

    public sealed record CreateQuoteRequest(Guid CustomerId, DateTimeOffset? ValidUntil);
    public sealed record AddQuoteLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesQuoteSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var quotes = await dbContext.SalesQuotes.AsNoTracking()
            .OrderByDescending(x => x.QuoteDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new SalesQuoteSummaryDto(
                x.Id,
                x.Number,
                x.CustomerId,
                x.QuoteDate,
                x.ValidUntil,
                x.Status,
                x.Lines.Sum(l => l.Quantity * l.UnitPrice)))
            .ToListAsync(cancellationToken);

        return Ok(quotes);
    }

    [HttpPost]
    public async Task<ActionResult<SalesQuoteDto>> Create(CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var id = await salesService.CreateQuoteAsync(request.CustomerId, request.ValidUntil, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesQuoteDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var quote = await dbContext.SalesQuotes.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (quote is null)
        {
            return NotFound();
        }

        return Ok(new SalesQuoteDto(
            quote.Id,
            quote.Number,
            quote.CustomerId,
            quote.QuoteDate,
            quote.ValidUntil,
            quote.Status,
            quote.Total,
            quote.Lines.Select(l => new SalesQuoteLineDto(l.Id, l.ItemId, l.Quantity, l.UnitPrice, l.LineTotal)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.SalesQuote, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddQuoteLineRequest request, CancellationToken cancellationToken)
    {
        await salesService.AddQuoteLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        await salesService.MarkQuoteSentAsync(id, cancellationToken);
        return NoContent();
    }
}
