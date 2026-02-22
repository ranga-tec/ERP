using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/debit-notes")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class DebitNotesController(IIssDbContext dbContext, FinanceService financeService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record DebitNoteDto(
        Guid Id,
        string ReferenceNumber,
        CounterpartyType CounterpartyType,
        Guid CounterpartyId,
        decimal Amount,
        DateTimeOffset IssuedAt,
        string? Notes,
        string? SourceReferenceType,
        Guid? SourceReferenceId);

    public sealed record CreateDebitNoteRequest(CounterpartyType CounterpartyType, Guid CounterpartyId, decimal Amount, string? Notes);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DebitNoteDto>>> List(
        [FromQuery] CounterpartyType? counterpartyType = null,
        [FromQuery] Guid? counterpartyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.DebitNotes.AsNoTracking();

        if (counterpartyType is not null)
        {
            query = query.Where(x => x.CounterpartyType == counterpartyType);
        }

        if (counterpartyId is not null)
        {
            query = query.Where(x => x.CounterpartyId == counterpartyId);
        }

        var notes = await query
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => new DebitNoteDto(
                x.Id,
                x.ReferenceNumber,
                x.CounterpartyType,
                x.CounterpartyId,
                x.Amount,
                x.IssuedAt,
                x.Notes,
                x.SourceReferenceType,
                x.SourceReferenceId))
            .ToListAsync(cancellationToken);

        return Ok(notes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DebitNoteDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var note = await dbContext.DebitNotes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new DebitNoteDto(
                x.Id,
                x.ReferenceNumber,
                x.CounterpartyType,
                x.CounterpartyId,
                x.Amount,
                x.IssuedAt,
                x.Notes,
                x.SourceReferenceType,
                x.SourceReferenceId))
            .FirstOrDefaultAsync(cancellationToken);

        return note is null ? NotFound() : Ok(note);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.DebitNote, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost]
    public async Task<ActionResult<DebitNoteDto>> Create(CreateDebitNoteRequest request, CancellationToken cancellationToken)
    {
        var id = await financeService.CreateDebitNoteAsync(
            request.CounterpartyType,
            request.CounterpartyId,
            request.Amount,
            request.Notes,
            cancellationToken: cancellationToken);

        return await Get(id, cancellationToken);
    }
}
