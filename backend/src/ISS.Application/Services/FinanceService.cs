using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class FinanceService(
    IIssDbContext dbContext,
    IDocumentNumberService documentNumberService,
    IClock clock)
{
    public async Task<Guid> CreatePaymentAsync(
        PaymentDirection direction,
        CounterpartyType counterpartyType,
        Guid counterpartyId,
        Guid? paymentTypeId,
        string? currencyCode,
        decimal? exchangeRate,
        decimal amount,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (paymentTypeId is not null)
        {
            var paymentTypeExists = await dbContext.PaymentTypes.AsNoTracking()
                .AnyAsync(x => x.Id == paymentTypeId.Value && x.IsActive, cancellationToken);
            if (!paymentTypeExists)
            {
                throw new DomainValidationException("Selected payment type is invalid or inactive.");
            }
        }

        var baseCurrency = await dbContext.Currencies.AsNoTracking()
            .Where(x => x.IsActive && x.IsBase)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "USD";

        var resolvedCurrencyCode = string.IsNullOrWhiteSpace(currencyCode)
            ? baseCurrency
            : currencyCode.Trim().ToUpperInvariant();

        var currencyExists = await dbContext.Currencies.AsNoTracking()
            .AnyAsync(x => x.Code == resolvedCurrencyCode && x.IsActive, cancellationToken);
        if (!currencyExists)
        {
            throw new DomainValidationException("Selected currency is invalid or inactive.");
        }

        var resolvedExchangeRate = exchangeRate;
        if (resolvedCurrencyCode == baseCurrency)
        {
            resolvedExchangeRate = 1m;
        }
        else if (resolvedExchangeRate is null)
        {
            resolvedExchangeRate = await (
                from rate in dbContext.CurrencyRates.AsNoTracking()
                join fromCurrency in dbContext.Currencies.AsNoTracking() on rate.FromCurrencyId equals fromCurrency.Id
                join toCurrency in dbContext.Currencies.AsNoTracking() on rate.ToCurrencyId equals toCurrency.Id
                where rate.IsActive
                      && fromCurrency.Code == resolvedCurrencyCode
                      && toCurrency.Code == baseCurrency
                orderby rate.EffectiveFrom descending
                select (decimal?)rate.Rate)
                .FirstOrDefaultAsync(cancellationToken);

            if (resolvedExchangeRate is null)
            {
                throw new DomainValidationException($"No active FX rate found from {resolvedCurrencyCode} to {baseCurrency}. Provide exchange rate.");
            }
        }

        var reference = await documentNumberService.NextAsync("PAY", "PAY", cancellationToken);
        var payment = new Payment(reference, direction, counterpartyType, counterpartyId, paymentTypeId, resolvedCurrencyCode, resolvedExchangeRate.Value, amount, clock.UtcNow, notes);
        await dbContext.Payments.AddAsync(payment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }

    public async Task AllocatePaymentToArAsync(Guid paymentId, Guid arEntryId, decimal amount, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
                      ?? throw new NotFoundException("Payment not found.");

        var ar = await dbContext.AccountsReceivableEntries.FirstOrDefaultAsync(x => x.Id == arEntryId, cancellationToken)
                 ?? throw new NotFoundException("AR entry not found.");

        if (amount <= 0)
        {
            throw new DomainValidationException("Allocation amount must be positive.");
        }

        if (amount > ar.Outstanding)
        {
            throw new DomainValidationException("Allocation exceeds outstanding amount.");
        }

        var allocation = payment.AllocateToAr(ar.Id, amount);
        dbContext.DbContext.Add(allocation);
        ar.ApplyPayment(amount);

        await MarkInvoicePaidIfSettledAsync(ar, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AllocatePaymentToApAsync(Guid paymentId, Guid apEntryId, decimal amount, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
                      ?? throw new NotFoundException("Payment not found.");

        var ap = await dbContext.AccountsPayableEntries.FirstOrDefaultAsync(x => x.Id == apEntryId, cancellationToken)
                 ?? throw new NotFoundException("AP entry not found.");

        if (amount <= 0)
        {
            throw new DomainValidationException("Allocation amount must be positive.");
        }

        if (amount > ap.Outstanding)
        {
            throw new DomainValidationException("Allocation exceeds outstanding amount.");
        }

        var allocation = payment.AllocateToAp(ap.Id, amount);
        dbContext.DbContext.Add(allocation);
        ap.ApplyPayment(amount);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateCreditNoteAsync(
        CounterpartyType counterpartyType,
        Guid counterpartyId,
        decimal amount,
        string? notes,
        string? sourceReferenceType = null,
        Guid? sourceReferenceId = null,
        CancellationToken cancellationToken = default)
    {
        var reference = await documentNumberService.NextAsync(ReferenceTypes.CreditNote, ReferenceTypes.CreditNote, cancellationToken);
        var creditNote = new CreditNote(reference, counterpartyType, counterpartyId, amount, clock.UtcNow, notes, sourceReferenceType, sourceReferenceId);
        await dbContext.CreditNotes.AddAsync(creditNote, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return creditNote.Id;
    }

    public async Task<Guid> CreateDebitNoteAsync(
        CounterpartyType counterpartyType,
        Guid counterpartyId,
        decimal amount,
        string? notes,
        string? sourceReferenceType = null,
        Guid? sourceReferenceId = null,
        CancellationToken cancellationToken = default)
    {
        var reference = await documentNumberService.NextAsync(ReferenceTypes.DebitNote, ReferenceTypes.DebitNote, cancellationToken);
        var debitNote = new DebitNote(reference, counterpartyType, counterpartyId, amount, clock.UtcNow, notes, sourceReferenceType, sourceReferenceId);
        await dbContext.DebitNotes.AddAsync(debitNote, cancellationToken);

        if (counterpartyType == CounterpartyType.Customer)
        {
            await dbContext.AccountsReceivableEntries.AddAsync(
                new AccountsReceivableEntry(counterpartyId, ReferenceTypes.DebitNote, debitNote.Id, amount, clock.UtcNow),
                cancellationToken);
        }
        else
        {
            await dbContext.AccountsPayableEntries.AddAsync(
                new AccountsPayableEntry(counterpartyId, ReferenceTypes.DebitNote, debitNote.Id, amount, clock.UtcNow),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return debitNote.Id;
    }

    public async Task AllocateCreditNoteToArAsync(Guid creditNoteId, Guid arEntryId, decimal amount, CancellationToken cancellationToken = default)
    {
        var creditNote = await dbContext.CreditNotes.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == creditNoteId, cancellationToken)
                         ?? throw new NotFoundException("Credit note not found.");

        if (creditNote.CounterpartyType != CounterpartyType.Customer)
        {
            throw new DomainValidationException("Credit note is not for a customer.");
        }

        var ar = await dbContext.AccountsReceivableEntries.FirstOrDefaultAsync(x => x.Id == arEntryId, cancellationToken)
                 ?? throw new NotFoundException("AR entry not found.");

        if (ar.CustomerId != creditNote.CounterpartyId)
        {
            throw new DomainValidationException("AR entry does not belong to this customer.");
        }

        if (amount <= 0)
        {
            throw new DomainValidationException("Allocation amount must be positive.");
        }

        if (amount > ar.Outstanding)
        {
            throw new DomainValidationException("Allocation exceeds outstanding amount.");
        }

        var allocation = creditNote.AllocateToAr(ar.Id, amount);
        dbContext.DbContext.Add(allocation);
        ar.ApplyPayment(amount);
        await MarkInvoicePaidIfSettledAsync(ar, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AllocateCreditNoteToApAsync(Guid creditNoteId, Guid apEntryId, decimal amount, CancellationToken cancellationToken = default)
    {
        var creditNote = await dbContext.CreditNotes.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == creditNoteId, cancellationToken)
                         ?? throw new NotFoundException("Credit note not found.");

        if (creditNote.CounterpartyType != CounterpartyType.Supplier)
        {
            throw new DomainValidationException("Credit note is not for a supplier.");
        }

        var ap = await dbContext.AccountsPayableEntries.FirstOrDefaultAsync(x => x.Id == apEntryId, cancellationToken)
                 ?? throw new NotFoundException("AP entry not found.");

        if (ap.SupplierId != creditNote.CounterpartyId)
        {
            throw new DomainValidationException("AP entry does not belong to this supplier.");
        }

        if (amount <= 0)
        {
            throw new DomainValidationException("Allocation amount must be positive.");
        }

        if (amount > ap.Outstanding)
        {
            throw new DomainValidationException("Allocation exceeds outstanding amount.");
        }

        var allocation = creditNote.AllocateToAp(ap.Id, amount);
        dbContext.DbContext.Add(allocation);
        ap.ApplyPayment(amount);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AutoAllocateCreditNoteAsync(Guid creditNoteId, CancellationToken cancellationToken = default)
    {
        var creditNote = await dbContext.CreditNotes.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == creditNoteId, cancellationToken)
                         ?? throw new NotFoundException("Credit note not found.");

        if (creditNote.RemainingAmount <= 0)
        {
            return;
        }

        if (creditNote.CounterpartyType == CounterpartyType.Customer)
        {
            var arEntries = await dbContext.AccountsReceivableEntries
                .Where(x => x.CustomerId == creditNote.CounterpartyId && x.Outstanding > 0)
                .OrderBy(x => x.PostedAt)
                .ToListAsync(cancellationToken);

            foreach (var ar in arEntries)
            {
                if (creditNote.RemainingAmount <= 0)
                {
                    break;
                }

                var allocate = Math.Min(ar.Outstanding, creditNote.RemainingAmount);
                var allocation = creditNote.AllocateToAr(ar.Id, allocate);
                dbContext.DbContext.Add(allocation);
                ar.ApplyPayment(allocate);
                await MarkInvoicePaidIfSettledAsync(ar, cancellationToken);
            }
        }
        else
        {
            var apEntries = await dbContext.AccountsPayableEntries
                .Where(x => x.SupplierId == creditNote.CounterpartyId && x.Outstanding > 0)
                .OrderBy(x => x.PostedAt)
                .ToListAsync(cancellationToken);

            foreach (var ap in apEntries)
            {
                if (creditNote.RemainingAmount <= 0)
                {
                    break;
                }

                var allocate = Math.Min(ap.Outstanding, creditNote.RemainingAmount);
                var allocation = creditNote.AllocateToAp(ap.Id, allocate);
                dbContext.DbContext.Add(allocation);
                ap.ApplyPayment(allocate);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkInvoicePaidIfSettledAsync(AccountsReceivableEntry ar, CancellationToken cancellationToken)
    {
        if (ar.Outstanding > 0 || ar.ReferenceType != ReferenceTypes.SalesInvoice)
        {
            return;
        }

        var invoice = await dbContext.SalesInvoices.FirstOrDefaultAsync(x => x.Id == ar.ReferenceId, cancellationToken);
        invoice?.MarkPaid();
    }
}
