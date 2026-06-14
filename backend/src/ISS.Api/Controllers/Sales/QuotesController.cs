using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
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
public sealed class QuotesController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record SalesQuoteSummaryDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset QuoteDate, DateTimeOffset? ValidUntil, SalesQuoteStatus Status, decimal Total);
    public sealed record SalesQuoteDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset QuoteDate, DateTimeOffset? ValidUntil, SalesQuoteStatus Status, decimal Total, IReadOnlyList<SalesQuoteLineDto> Lines);
    public sealed record SalesQuoteLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal LineTotal);

    public sealed record CreateQuoteRequest(Guid CustomerId, DateTimeOffset? ValidUntil);
    public sealed record AddQuoteLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice);
    public sealed record UpdateQuoteLineRequest(decimal Quantity, decimal UnitPrice);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesQuoteSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesQuoteView, cancellationToken))
        {
            return Forbid();
        }

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
        if (!await HasPermissionAsync(AppPermissions.SalesQuoteCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateQuoteAsync(request.CustomerId, request.ValidUntil, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesQuoteDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesQuoteView, cancellationToken))
        {
            return Forbid();
        }

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
        if (!await HasPermissionAsync(AppPermissions.SalesQuoteView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.SalesQuote, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddQuoteLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesQuoteEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.AddQuoteLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateQuoteLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesQuoteEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.UpdateQuoteLineAsync(id, lineId, request.Quantity, request.UnitPrice, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesQuoteEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.RemoveQuoteLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesQuoteSend, cancellationToken))
        {
            return Forbid();
        }

        await salesService.MarkQuoteSentAsync(id, cancellationToken);
        await NotifyQuoteCreatorAsync(id, "Sales quote sent", "Your sales quote has been sent.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyQuoteCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var quote = await dbContext.SalesQuotes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (quote is null || quote.CreatedBy is null || quote.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            quote.CreatedBy.Value,
            title,
            $"{quote.Number}: {message}",
            $"/sales/quotes/{quote.Id}",
            ReferenceTypes.SalesQuote,
            quote.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
