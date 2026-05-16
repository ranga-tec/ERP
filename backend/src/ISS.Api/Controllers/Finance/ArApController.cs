using ISS.Api.Security;
using ISS.Application.Common;
using ISS.Application.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance},{Roles.Reporting}")]
public sealed class ArApController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ArDto(Guid Id, Guid CustomerId, string ReferenceType, Guid ReferenceId, string ReferenceNumber, decimal Amount, decimal Outstanding, DateTimeOffset PostedAt);
    public sealed record ApDto(Guid Id, Guid SupplierId, string ReferenceType, Guid ReferenceId, string ReferenceNumber, decimal Amount, decimal Outstanding, DateTimeOffset PostedAt);

    [HttpGet("ar")]
    public async Task<ActionResult<IReadOnlyList<ArDto>>> ListAr([FromQuery] bool outstandingOnly = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AccountsReceivableEntries.AsNoTracking();
        if (outstandingOnly)
        {
            query = query.Where(x => x.Outstanding > 0);
        }

        var entries = await query
            .OrderByDescending(x => x.PostedAt)
            .Select(x => new { x.Id, x.CustomerId, x.ReferenceType, x.ReferenceId, x.Amount, x.Outstanding, x.PostedAt })
            .ToListAsync(cancellationToken);

        var invoiceIds = entries.Where(x => x.ReferenceType == ReferenceTypes.SalesInvoice).Select(x => x.ReferenceId).Distinct().ToList();
        var debitNoteIds = entries.Where(x => x.ReferenceType == ReferenceTypes.DebitNote).Select(x => x.ReferenceId).Distinct().ToList();

        var invoiceNumbers = await dbContext.SalesInvoices.AsNoTracking()
            .Where(x => invoiceIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number })
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        var debitNoteNumbers = await dbContext.DebitNotes.AsNoTracking()
            .Where(x => debitNoteIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ReferenceNumber })
            .ToDictionaryAsync(x => x.Id, x => x.ReferenceNumber, cancellationToken);

        return Ok(entries.Select(x => new ArDto(
            x.Id,
            x.CustomerId,
            x.ReferenceType,
            x.ReferenceId,
            ResolveReferenceNumber(x.ReferenceType, x.ReferenceId, invoiceNumbers, debitNoteNumbers),
            x.Amount,
            x.Outstanding,
            x.PostedAt)));
    }

    [HttpGet("ap")]
    public async Task<ActionResult<IReadOnlyList<ApDto>>> ListAp([FromQuery] bool outstandingOnly = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AccountsPayableEntries.AsNoTracking();
        if (outstandingOnly)
        {
            query = query.Where(x => x.Outstanding > 0);
        }

        var entries = await query
            .OrderByDescending(x => x.PostedAt)
            .Select(x => new { x.Id, x.SupplierId, x.ReferenceType, x.ReferenceId, x.Amount, x.Outstanding, x.PostedAt })
            .ToListAsync(cancellationToken);

        var goodsReceiptIds = entries.Where(x => x.ReferenceType == ReferenceTypes.GoodsReceipt).Select(x => x.ReferenceId).Distinct().ToList();
        var supplierInvoiceIds = entries.Where(x => x.ReferenceType == ReferenceTypes.SupplierInvoice).Select(x => x.ReferenceId).Distinct().ToList();
        var debitNoteIds = entries.Where(x => x.ReferenceType == ReferenceTypes.DebitNote).Select(x => x.ReferenceId).Distinct().ToList();

        var goodsReceiptNumbers = await dbContext.GoodsReceipts.AsNoTracking()
            .Where(x => goodsReceiptIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number })
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        var supplierInvoiceNumbers = await dbContext.SupplierInvoices.AsNoTracking()
            .Where(x => supplierInvoiceIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number })
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        var debitNoteNumbers = await dbContext.DebitNotes.AsNoTracking()
            .Where(x => debitNoteIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ReferenceNumber })
            .ToDictionaryAsync(x => x.Id, x => x.ReferenceNumber, cancellationToken);

        return Ok(entries.Select(x => new ApDto(
            x.Id,
            x.SupplierId,
            x.ReferenceType,
            x.ReferenceId,
            ResolveReferenceNumber(x.ReferenceType, x.ReferenceId, goodsReceiptNumbers, supplierInvoiceNumbers, debitNoteNumbers),
            x.Amount,
            x.Outstanding,
            x.PostedAt)));
    }

    private static string ResolveReferenceNumber(
        string referenceType,
        Guid referenceId,
        IReadOnlyDictionary<Guid, string> first,
        IReadOnlyDictionary<Guid, string> second,
        IReadOnlyDictionary<Guid, string>? third = null)
    {
        if (first.TryGetValue(referenceId, out var firstNumber))
        {
            return firstNumber;
        }

        if (second.TryGetValue(referenceId, out var secondNumber))
        {
            return secondNumber;
        }

        if (third is not null && third.TryGetValue(referenceId, out var thirdNumber))
        {
            return thirdNumber;
        }

        return $"{referenceType}:{referenceId}";
    }
}
