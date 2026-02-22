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
[Route("api/finance/payments")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class PaymentsController(IIssDbContext dbContext, FinanceService financeService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record PaymentDto(Guid Id, string ReferenceNumber, PaymentDirection Direction, CounterpartyType CounterpartyType, Guid CounterpartyId, decimal Amount, DateTimeOffset PaidAt, string? Notes);
    public sealed record PaymentAllocationDto(Guid Id, Guid? AccountsReceivableEntryId, Guid? AccountsPayableEntryId, decimal Amount);
    public sealed record PaymentDetailDto(Guid Id, string ReferenceNumber, PaymentDirection Direction, CounterpartyType CounterpartyType, Guid CounterpartyId, decimal Amount, DateTimeOffset PaidAt, string? Notes, IReadOnlyList<PaymentAllocationDto> Allocations);

    public sealed record CreatePaymentRequest(PaymentDirection Direction, CounterpartyType CounterpartyType, Guid CounterpartyId, decimal Amount, string? Notes);
    public sealed record AllocateRequest(Guid EntryId, decimal Amount);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var payments = await dbContext.Payments.AsNoTracking()
            .OrderByDescending(x => x.PaidAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new PaymentDto(x.Id, x.ReferenceNumber, x.Direction, x.CounterpartyType, x.CounterpartyId, x.Amount, x.PaidAt, x.Notes))
            .ToListAsync(cancellationToken);

        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.AsNoTracking()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(new PaymentDetailDto(
            payment.Id,
            payment.ReferenceNumber,
            payment.Direction,
            payment.CounterpartyType,
            payment.CounterpartyId,
            payment.Amount,
            payment.PaidAt,
            payment.Notes,
            payment.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.AccountsReceivableEntryId, a.AccountsPayableEntryId, a.Amount)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.Payment, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var id = await financeService.CreatePaymentAsync(request.Direction, request.CounterpartyType, request.CounterpartyId, request.Amount, request.Notes, cancellationToken);

        var payment = await dbContext.Payments.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PaymentDto(x.Id, x.ReferenceNumber, x.Direction, x.CounterpartyType, x.CounterpartyId, x.Amount, x.PaidAt, x.Notes))
            .FirstAsync(cancellationToken);

        return Ok(payment);
    }

    [HttpPost("{id:guid}/allocate/ar")]
    public async Task<ActionResult> AllocateToAr(Guid id, AllocateRequest request, CancellationToken cancellationToken)
    {
        await financeService.AllocatePaymentToArAsync(id, request.EntryId, request.Amount, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/allocate/ap")]
    public async Task<ActionResult> AllocateToAp(Guid id, AllocateRequest request, CancellationToken cancellationToken)
    {
        await financeService.AllocatePaymentToApAsync(id, request.EntryId, request.Amount, cancellationToken);
        return NoContent();
    }
}
