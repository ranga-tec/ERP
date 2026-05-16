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
[Route("api/finance/credit-notes")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class CreditNotesController(IIssDbContext dbContext, FinanceService financeService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record CreditNoteDto(
        Guid Id,
        string ReferenceNumber,
        CounterpartyType CounterpartyType,
        Guid CounterpartyId,
        decimal Amount,
        decimal RemainingAmount,
        DateTimeOffset IssuedAt,
        string? Notes,
        string? SourceReferenceType,
        Guid? SourceReferenceId,
        string? SourceReferenceNumber);

    public sealed record CreditNoteAllocationDto(Guid Id, Guid? AccountsReceivableEntryId, Guid? AccountsPayableEntryId, decimal Amount);
    public sealed record CreditNoteDetailDto(CreditNoteDto CreditNote, IReadOnlyList<CreditNoteAllocationDto> Allocations);

    public sealed record CreateCreditNoteRequest(CounterpartyType CounterpartyType, Guid CounterpartyId, decimal Amount, string? Notes);
    public sealed record AllocateRequest(Guid EntryId, decimal Amount);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditNoteDto>>> List(
        [FromQuery] CounterpartyType? counterpartyType = null,
        [FromQuery] Guid? counterpartyId = null,
        [FromQuery] bool remainingOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CreditNotes.AsNoTracking();

        if (counterpartyType is not null)
        {
            query = query.Where(x => x.CounterpartyType == counterpartyType);
        }

        if (counterpartyId is not null)
        {
            query = query.Where(x => x.CounterpartyId == counterpartyId);
        }

        if (remainingOnly)
        {
            query = query.Where(x => x.RemainingAmount > 0);
        }

        var notes = await query
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => new CreditNoteDto(
                x.Id,
                x.ReferenceNumber,
                x.CounterpartyType,
                x.CounterpartyId,
                x.Amount,
                x.RemainingAmount,
                x.IssuedAt,
                x.Notes,
                x.SourceReferenceType,
                x.SourceReferenceId,
                null))
            .ToListAsync(cancellationToken);

        await PopulateSourceReferenceNumbersAsync(notes, cancellationToken);
        return Ok(notes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditNoteDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var creditNote = await dbContext.CreditNotes.AsNoTracking()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (creditNote is null)
        {
            return NotFound();
        }

        var dto = new CreditNoteDto(
            creditNote.Id,
            creditNote.ReferenceNumber,
            creditNote.CounterpartyType,
            creditNote.CounterpartyId,
            creditNote.Amount,
            creditNote.RemainingAmount,
            creditNote.IssuedAt,
            creditNote.Notes,
            creditNote.SourceReferenceType,
            creditNote.SourceReferenceId,
            await ResolveSourceReferenceNumberAsync(creditNote.SourceReferenceType, creditNote.SourceReferenceId, cancellationToken));

        return Ok(new CreditNoteDetailDto(
            dto,
            creditNote.Allocations.Select(a => new CreditNoteAllocationDto(a.Id, a.AccountsReceivableEntryId, a.AccountsPayableEntryId, a.Amount)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.CreditNote, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost]
    public async Task<ActionResult<CreditNoteDto>> Create(CreateCreditNoteRequest request, CancellationToken cancellationToken)
    {
        var id = await financeService.CreateCreditNoteAsync(
            request.CounterpartyType,
            request.CounterpartyId,
            request.Amount,
            request.Notes,
            cancellationToken: cancellationToken);

        var note = await dbContext.CreditNotes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CreditNoteDto(
                x.Id,
                x.ReferenceNumber,
                x.CounterpartyType,
                x.CounterpartyId,
                x.Amount,
                x.RemainingAmount,
                x.IssuedAt,
                x.Notes,
                x.SourceReferenceType,
                x.SourceReferenceId,
                null))
            .FirstAsync(cancellationToken);

        note = note with
        {
            SourceReferenceNumber = await ResolveSourceReferenceNumberAsync(note.SourceReferenceType, note.SourceReferenceId, cancellationToken)
        };

        return Ok(note);
    }

    [HttpPost("{id:guid}/allocate/ar")]
    public async Task<ActionResult> AllocateToAr(Guid id, AllocateRequest request, CancellationToken cancellationToken)
    {
        await financeService.AllocateCreditNoteToArAsync(id, request.EntryId, request.Amount, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/allocate/ap")]
    public async Task<ActionResult> AllocateToAp(Guid id, AllocateRequest request, CancellationToken cancellationToken)
    {
        await financeService.AllocateCreditNoteToApAsync(id, request.EntryId, request.Amount, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/auto-allocate")]
    public async Task<ActionResult> AutoAllocate(Guid id, CancellationToken cancellationToken)
    {
        await financeService.AutoAllocateCreditNoteAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task PopulateSourceReferenceNumbersAsync(List<CreditNoteDto> notes, CancellationToken cancellationToken)
    {
        var customerReturnIds = notes
            .Where(x => x.SourceReferenceType == "CRTN" && x.SourceReferenceId is not null)
            .Select(x => x.SourceReferenceId!.Value)
            .Distinct()
            .ToList();
        var supplierReturnIds = notes
            .Where(x => x.SourceReferenceType == "SR" && x.SourceReferenceId is not null)
            .Select(x => x.SourceReferenceId!.Value)
            .Distinct()
            .ToList();

        var customerReturnNumbers = await dbContext.CustomerReturns.AsNoTracking()
            .Where(x => customerReturnIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number })
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);
        var supplierReturnNumbers = await dbContext.SupplierReturns.AsNoTracking()
            .Where(x => supplierReturnIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number })
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        for (var index = 0; index < notes.Count; index++)
        {
            var note = notes[index];
            notes[index] = note with
            {
                SourceReferenceNumber = ResolveSourceReferenceNumber(
                    note.SourceReferenceType,
                    note.SourceReferenceId,
                    customerReturnNumbers,
                    supplierReturnNumbers)
            };
        }
    }

    private async Task<string?> ResolveSourceReferenceNumberAsync(string? sourceReferenceType, Guid? sourceReferenceId, CancellationToken cancellationToken)
    {
        if (sourceReferenceId is null)
        {
            return null;
        }

        if (sourceReferenceType == "CRTN")
        {
            return await dbContext.CustomerReturns.AsNoTracking()
                .Where(x => x.Id == sourceReferenceId.Value)
                .Select(x => x.Number)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (sourceReferenceType == "SR")
        {
            return await dbContext.SupplierReturns.AsNoTracking()
                .Where(x => x.Id == sourceReferenceId.Value)
                .Select(x => x.Number)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private static string? ResolveSourceReferenceNumber(
        string? sourceReferenceType,
        Guid? sourceReferenceId,
        IReadOnlyDictionary<Guid, string> customerReturnNumbers,
        IReadOnlyDictionary<Guid, string> supplierReturnNumbers)
    {
        if (sourceReferenceId is null)
        {
            return null;
        }

        if (sourceReferenceType == "CRTN" && customerReturnNumbers.TryGetValue(sourceReferenceId.Value, out var customerReturnNumber))
        {
            return customerReturnNumber;
        }

        if (sourceReferenceType == "SR" && supplierReturnNumbers.TryGetValue(sourceReferenceId.Value, out var supplierReturnNumber))
        {
            return supplierReturnNumber;
        }

        return null;
    }
}
