using ISS.Domain.MasterData;
using Microsoft.EntityFrameworkCore;

namespace ISS.Infrastructure.Persistence;

public static class ReferenceDataSeeder
{
    public static async Task SeedAsync(IssDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var hasChanges = false;

        var currencies = await dbContext.Currencies.ToListAsync(cancellationToken);
        if (currencies.Count == 0)
        {
            currencies =
            [
                new Currency("USD", "US Dollar", "$", 2, isBase: true),
                new Currency("EUR", "Euro", "EUR", 2, isBase: false),
                new Currency("GBP", "Pound Sterling", "GBP", 2, isBase: false)
            ];

            await dbContext.Currencies.AddRangeAsync(currencies, cancellationToken);
            hasChanges = true;
        }
        else if (!currencies.Any(x => x.IsBase && x.IsActive))
        {
            var existingBase = currencies.FirstOrDefault(x => x.IsBase);
            if (existingBase is not null)
            {
                existingBase.Update(
                    existingBase.Code,
                    existingBase.Name,
                    existingBase.Symbol,
                    existingBase.MinorUnits,
                    isBase: true,
                    isActive: true);
            }
            else
            {
                var promoted = currencies.FirstOrDefault(x => x.IsActive) ?? currencies[0];
                promoted.Update(
                    promoted.Code,
                    promoted.Name,
                    promoted.Symbol,
                    promoted.MinorUnits,
                    isBase: true,
                    isActive: true);
            }

            hasChanges = true;
        }

        if (!await dbContext.CurrencyRates.AnyAsync(cancellationToken))
        {
            var usd = currencies.FirstOrDefault(x => x.Code == "USD");
            var eur = currencies.FirstOrDefault(x => x.Code == "EUR");
            var gbp = currencies.FirstOrDefault(x => x.Code == "GBP");
            var seededAt = new DateTimeOffset(2026, 2, 27, 0, 0, 0, TimeSpan.Zero);

            var rates = new List<CurrencyRate>();
            if (usd is not null && eur is not null)
            {
                rates.Add(new CurrencyRate(eur.Id, usd.Id, 1.08m, CurrencyRateType.Spot, seededAt, "Seed default"));
            }

            if (usd is not null && gbp is not null)
            {
                rates.Add(new CurrencyRate(gbp.Id, usd.Id, 1.27m, CurrencyRateType.Spot, seededAt, "Seed default"));
            }

            if (rates.Count > 0)
            {
                await dbContext.CurrencyRates.AddRangeAsync(rates, cancellationToken);
                hasChanges = true;
            }
        }

        if (!await dbContext.PaymentTypes.AnyAsync(cancellationToken))
        {
            await dbContext.PaymentTypes.AddRangeAsync(
            [
                new PaymentType("CASH", "Cash", "Physical cash receipt or disbursement."),
                new PaymentType("BANK_TRANSFER", "Bank Transfer", "Electronic bank-to-bank transfer."),
                new PaymentType("CHEQUE", "Cheque", "Cheque or demand draft payment."),
                new PaymentType("CARD", "Card", "Card swipe or online card payment.")
            ], cancellationToken);

            hasChanges = true;
        }

        if (!await dbContext.TaxCodes.AnyAsync(cancellationToken))
        {
            await dbContext.TaxCodes.AddRangeAsync(
            [
                new TaxCode("VAT0", "Zero Rated", 0m, isInclusive: false, TaxScope.Both, "Zero-rated tax."),
                new TaxCode("VAT5", "VAT 5%", 5m, isInclusive: false, TaxScope.Both, "General reduced VAT rate."),
                new TaxCode("VAT15", "VAT 15%", 15m, isInclusive: false, TaxScope.Both, "General standard VAT rate.")
            ], cancellationToken);

            hasChanges = true;
        }

        var seededReferenceForms = new[]
        {
            new ReferenceForm("PR", "Purchase Requisition", "Procurement", "/procurement/purchase-requisitions/{id}"),
            new ReferenceForm("RFQ", "Request for Quote", "Procurement", "/procurement/rfqs/{id}"),
            new ReferenceForm("PO", "Purchase Order", "Procurement", "/procurement/purchase-orders/{id}"),
            new ReferenceForm("GRN", "Goods Receipt", "Procurement", "/procurement/goods-receipts/{id}"),
            new ReferenceForm("DPR", "Direct Purchase", "Procurement", "/procurement/direct-purchases/{id}"),
            new ReferenceForm("SINV", "Supplier Invoice", "Procurement", "/procurement/supplier-invoices/{id}"),
            new ReferenceForm("SR", "Supplier Return", "Procurement", "/procurement/supplier-returns/{id}"),
            new ReferenceForm("SQ", "Sales Quote", "Sales", "/sales/quotes/{id}"),
            new ReferenceForm("SO", "Sales Order", "Sales", "/sales/orders/{id}"),
            new ReferenceForm("DN", "Dispatch Note", "Sales", "/sales/dispatches/{id}"),
            new ReferenceForm("DDN", "Direct Dispatch", "Sales", "/sales/direct-dispatches/{id}"),
            new ReferenceForm("INV", "Sales Invoice", "Sales", "/sales/invoices/{id}"),
            new ReferenceForm("CRTN", "Customer Return", "Sales", "/sales/customer-returns/{id}"),
            new ReferenceForm("SJ", "Service Job", "Service", "/service/jobs/{id}"),
            new ReferenceForm("SE", "Service Estimate", "Service", "/service/estimates/{id}"),
            new ReferenceForm("SEC", "Service Expense Claim", "Service", "/service/expense-claims/{id}"),
            new ReferenceForm("WO", "Work Order", "Service", "/service/work-orders/{id}"),
            new ReferenceForm("MR", "Material Requisition", "Service", "/service/material-requisitions/{id}"),
            new ReferenceForm("QC", "Quality Check", "Service", "/service/quality-checks/{id}"),
            new ReferenceForm("SH", "Service Handover", "Service", "/service/handovers/{id}"),
            new ReferenceForm("EUNIT", "Equipment Unit", "Service", "/service/equipment-units/{id}"),
            new ReferenceForm("ADJ", "Stock Adjustment", "Inventory", "/inventory/stock-adjustments/{id}"),
            new ReferenceForm("TRF", "Stock Transfer", "Inventory", "/inventory/stock-transfers/{id}"),
            new ReferenceForm("PAY", "Payment", "Finance", "/finance/payments/{id}"),
            new ReferenceForm("PCF", "Petty Cash Fund", "Finance", "/finance/petty-cash/{id}"),
            new ReferenceForm("CN", "Credit Note", "Finance", "/finance/credit-notes/{id}"),
            new ReferenceForm("DBN", "Debit Note", "Finance", "/finance/debit-notes/{id}")
        };

        var existingReferenceCodes = await dbContext.ReferenceForms
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        var missingReferenceForms = seededReferenceForms
            .Where(form => !existingReferenceCodes.Contains(form.Code, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingReferenceForms.Count > 0)
        {
            await dbContext.ReferenceForms.AddRangeAsync(missingReferenceForms, cancellationToken);
            hasChanges = true;
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
