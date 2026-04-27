using ISS.Domain.MasterData;
using Microsoft.EntityFrameworkCore;

namespace ISS.Infrastructure.Persistence;

public static class ReferenceDataSeeder
{
    private static readonly DateTimeOffset SeededFxEffectiveFrom = new(2026, 2, 27, 0, 0, 0, TimeSpan.Zero);

    private static readonly CurrencySeed[] CurrencySeeds =
    [
        new("USD", "US Dollar", "$", 2, IsBase: true),
        new("EUR", "Euro", "EUR", 2, IsBase: false),
        new("GBP", "Pound Sterling", "GBP", 2, IsBase: false),
        new("LKR", "Sri Lankan Rupee", "LKR", 2, IsBase: false),
        new("AED", "UAE Dirham", "AED", 2, IsBase: false),
        new("SGD", "Singapore Dollar", "SGD", 2, IsBase: false)
    ];

    private static readonly CurrencyRateSeed[] CurrencyRateSeeds =
    [
        new("EUR", "USD", 1.08m, CurrencyRateType.Spot, "Sample seed rate"),
        new("GBP", "USD", 1.27m, CurrencyRateType.Spot, "Sample seed rate"),
        new("LKR", "USD", 0.0033m, CurrencyRateType.Spot, "Sample seed rate"),
        new("AED", "USD", 0.2723m, CurrencyRateType.Spot, "Sample seed rate"),
        new("SGD", "USD", 0.7425m, CurrencyRateType.Spot, "Sample seed rate")
    ];

    private static readonly PaymentTypeSeed[] PaymentTypeSeeds =
    [
        new("CASH", "Cash", "Physical cash receipt or disbursement."),
        new("BANK_TRANSFER", "Bank Transfer", "Electronic bank-to-bank transfer."),
        new("CHEQUE", "Cheque", "Cheque or demand draft payment."),
        new("CARD", "Card", "Card swipe or online card payment."),
        new("MOBILE_PAYMENT", "Mobile Payment", "Wallet, QR, or app-based payment."),
        new("ONLINE_GATEWAY", "Online Gateway", "Payment captured through an online payment gateway.")
    ];

    private static readonly TaxCodeSeed[] TaxCodeSeeds =
    [
        new("VAT0", "Zero Rated", 0m, IsInclusive: false, TaxScope.Both, "Zero-rated tax."),
        new("EXEMPT", "Tax Exempt", 0m, IsInclusive: false, TaxScope.Both, "Exempt supply or purchase."),
        new("VAT5", "VAT 5%", 5m, IsInclusive: false, TaxScope.Both, "Sample reduced VAT rate."),
        new("VAT15", "VAT 15%", 15m, IsInclusive: false, TaxScope.Both, "Sample standard VAT rate."),
        new("VAT15_INC", "VAT 15% Inclusive", 15m, IsInclusive: true, TaxScope.Both, "Sample inclusive VAT rate.")
    ];

    private static readonly TaxConversionSeed[] TaxConversionSeeds =
    [
        new("VAT5", "VAT15", 3m, "Sample multiplier from 5% VAT amount to 15% VAT amount."),
        new("VAT15", "VAT5", 0.33333333m, "Sample multiplier from 15% VAT amount to 5% VAT amount.")
    ];

    private static readonly ReferenceFormSeed[] ReferenceFormSeeds =
    [
        new("PR", "Purchase Requisition", "Procurement", "/procurement/purchase-requisitions/{id}"),
        new("RFQ", "Request for Quote", "Procurement", "/procurement/rfqs/{id}"),
        new("PO", "Purchase Order", "Procurement", "/procurement/purchase-orders/{id}"),
        new("GRN", "Goods Receipt", "Procurement", "/procurement/goods-receipts/{id}"),
        new("DPR", "Direct Purchase", "Procurement", "/procurement/direct-purchases/{id}"),
        new("SINV", "Supplier Invoice", "Procurement", "/procurement/supplier-invoices/{id}"),
        new("SR", "Supplier Return", "Procurement", "/procurement/supplier-returns/{id}"),
        new("SQ", "Sales Quote", "Sales", "/sales/quotes/{id}"),
        new("SO", "Sales Order", "Sales", "/sales/orders/{id}"),
        new("DN", "Dispatch Note", "Sales", "/sales/dispatches/{id}"),
        new("DDN", "Direct Dispatch", "Sales", "/sales/direct-dispatches/{id}"),
        new("INV", "Sales Invoice", "Sales", "/sales/invoices/{id}"),
        new("CRTN", "Customer Return", "Sales", "/sales/customer-returns/{id}"),
        new("SC", "Service Contract", "Service", "/service/contracts/{id}"),
        new("SJ", "Service Job", "Service", "/service/jobs/{id}"),
        new("SE", "Service Estimate", "Service", "/service/estimates/{id}"),
        new("SEC", "Service Expense Claim", "Service", "/service/expense-claims/{id}"),
        new("WO", "Work Order", "Service", "/service/work-orders/{id}"),
        new("MR", "Material Requisition", "Service", "/service/material-requisitions/{id}"),
        new("QC", "Quality Check", "Service", "/service/quality-checks/{id}"),
        new("SH", "Service Handover", "Service", "/service/handovers/{id}"),
        new("EUNIT", "Equipment Unit", "Service", "/service/equipment-units/{id}"),
        new("ADJ", "Stock Adjustment", "Inventory", "/inventory/stock-adjustments/{id}"),
        new("TRF", "Stock Transfer", "Inventory", "/inventory/stock-transfers/{id}"),
        new("PAY", "Payment", "Finance", "/finance/payments/{id}"),
        new("PCF", "Petty Cash Fund", "Finance", "/finance/petty-cash/{id}"),
        new("CN", "Credit Note", "Finance", "/finance/credit-notes/{id}"),
        new("DBN", "Debit Note", "Finance", "/finance/debit-notes/{id}")
    ];

    public static async Task SeedAsync(IssDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var hasChanges = false;

        var currencies = await dbContext.Currencies.ToListAsync(cancellationToken);
        hasChanges |= await SeedCurrenciesAsync(dbContext, currencies, cancellationToken);
        hasChanges |= EnsureActiveBaseCurrency(currencies);

        var currencyByCode = currencies.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        hasChanges |= await SeedCurrencyRatesAsync(dbContext, currencyByCode, cancellationToken);
        hasChanges |= await SeedPaymentTypesAsync(dbContext, cancellationToken);

        var taxCodes = await dbContext.TaxCodes.ToListAsync(cancellationToken);
        hasChanges |= await SeedTaxCodesAsync(dbContext, taxCodes, cancellationToken);

        var taxCodeByCode = taxCodes.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        hasChanges |= await SeedTaxConversionsAsync(dbContext, taxCodeByCode, cancellationToken);
        hasChanges |= await SeedReferenceFormsAsync(dbContext, cancellationToken);

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<bool> SeedCurrenciesAsync(
        IssDbContext dbContext,
        List<Currency> currencies,
        CancellationToken cancellationToken)
    {
        var existingCodes = currencies
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingCurrencies = CurrencySeeds
            .Where(seed => !existingCodes.Contains(seed.Code))
            .Select(seed => new Currency(seed.Code, seed.Name, seed.Symbol, seed.MinorUnits, seed.IsBase))
            .ToList();

        if (missingCurrencies.Count == 0)
        {
            return false;
        }

        await dbContext.Currencies.AddRangeAsync(missingCurrencies, cancellationToken);
        currencies.AddRange(missingCurrencies);
        return true;
    }

    private static bool EnsureActiveBaseCurrency(List<Currency> currencies)
    {
        if (currencies.Any(x => x.IsBase && x.IsActive))
        {
            return false;
        }

        var baseCurrency = currencies.FirstOrDefault(x => x.IsBase)
            ?? currencies.FirstOrDefault(x => x.Code.Equals("USD", StringComparison.OrdinalIgnoreCase))
            ?? currencies.FirstOrDefault(x => x.IsActive)
            ?? currencies.FirstOrDefault();

        if (baseCurrency is null)
        {
            return false;
        }

        baseCurrency.Update(
            baseCurrency.Code,
            baseCurrency.Name,
            baseCurrency.Symbol,
            baseCurrency.MinorUnits,
            isBase: true,
            isActive: true);

        return true;
    }

    private static async Task<bool> SeedCurrencyRatesAsync(
        IssDbContext dbContext,
        IReadOnlyDictionary<string, Currency> currencyByCode,
        CancellationToken cancellationToken)
    {
        var existingRatePairs = (await dbContext.CurrencyRates
            .Select(x => new { x.FromCurrencyId, x.ToCurrencyId, x.RateType })
            .ToListAsync(cancellationToken))
            .Select(x => (x.FromCurrencyId, x.ToCurrencyId, x.RateType))
            .ToHashSet();

        var missingRates = new List<CurrencyRate>();
        foreach (var seed in CurrencyRateSeeds)
        {
            if (!currencyByCode.TryGetValue(seed.FromCode, out var fromCurrency)
                || !currencyByCode.TryGetValue(seed.ToCode, out var toCurrency))
            {
                continue;
            }

            var key = (fromCurrency.Id, toCurrency.Id, seed.RateType);
            if (existingRatePairs.Contains(key))
            {
                continue;
            }

            missingRates.Add(new CurrencyRate(
                fromCurrency.Id,
                toCurrency.Id,
                seed.Rate,
                seed.RateType,
                SeededFxEffectiveFrom,
                seed.Source));

            existingRatePairs.Add(key);
        }

        if (missingRates.Count == 0)
        {
            return false;
        }

        await dbContext.CurrencyRates.AddRangeAsync(missingRates, cancellationToken);
        return true;
    }

    private static async Task<bool> SeedPaymentTypesAsync(IssDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingCodes = (await dbContext.PaymentTypes
            .Select(x => x.Code)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingPaymentTypes = PaymentTypeSeeds
            .Where(seed => !existingCodes.Contains(seed.Code))
            .Select(seed => new PaymentType(seed.Code, seed.Name, seed.Description))
            .ToList();

        if (missingPaymentTypes.Count == 0)
        {
            return false;
        }

        await dbContext.PaymentTypes.AddRangeAsync(missingPaymentTypes, cancellationToken);
        return true;
    }

    private static async Task<bool> SeedTaxCodesAsync(
        IssDbContext dbContext,
        List<TaxCode> taxCodes,
        CancellationToken cancellationToken)
    {
        var existingCodes = taxCodes
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingTaxCodes = TaxCodeSeeds
            .Where(seed => !existingCodes.Contains(seed.Code))
            .Select(seed => new TaxCode(
                seed.Code,
                seed.Name,
                seed.RatePercent,
                seed.IsInclusive,
                seed.Scope,
                seed.Description))
            .ToList();

        if (missingTaxCodes.Count == 0)
        {
            return false;
        }

        await dbContext.TaxCodes.AddRangeAsync(missingTaxCodes, cancellationToken);
        taxCodes.AddRange(missingTaxCodes);
        return true;
    }

    private static async Task<bool> SeedTaxConversionsAsync(
        IssDbContext dbContext,
        IReadOnlyDictionary<string, TaxCode> taxCodeByCode,
        CancellationToken cancellationToken)
    {
        var existingPairs = (await dbContext.TaxConversions
            .Select(x => new { x.SourceTaxCodeId, x.TargetTaxCodeId })
            .ToListAsync(cancellationToken))
            .Select(x => (x.SourceTaxCodeId, x.TargetTaxCodeId))
            .ToHashSet();

        var missingConversions = new List<TaxConversion>();
        foreach (var seed in TaxConversionSeeds)
        {
            if (!taxCodeByCode.TryGetValue(seed.SourceCode, out var sourceTaxCode)
                || !taxCodeByCode.TryGetValue(seed.TargetCode, out var targetTaxCode))
            {
                continue;
            }

            var key = (sourceTaxCode.Id, targetTaxCode.Id);
            if (existingPairs.Contains(key))
            {
                continue;
            }

            missingConversions.Add(new TaxConversion(
                sourceTaxCode.Id,
                targetTaxCode.Id,
                seed.Multiplier,
                seed.Notes));

            existingPairs.Add(key);
        }

        if (missingConversions.Count == 0)
        {
            return false;
        }

        await dbContext.TaxConversions.AddRangeAsync(missingConversions, cancellationToken);
        return true;
    }

    private static async Task<bool> SeedReferenceFormsAsync(IssDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingCodes = (await dbContext.ReferenceForms
            .Select(x => x.Code)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingReferenceForms = ReferenceFormSeeds
            .Where(seed => !existingCodes.Contains(seed.Code))
            .Select(seed => new ReferenceForm(seed.Code, seed.Name, seed.Module, seed.RouteTemplate))
            .ToList();

        if (missingReferenceForms.Count == 0)
        {
            return false;
        }

        await dbContext.ReferenceForms.AddRangeAsync(missingReferenceForms, cancellationToken);
        return true;
    }

    private sealed record CurrencySeed(string Code, string Name, string Symbol, int MinorUnits, bool IsBase);
    private sealed record CurrencyRateSeed(string FromCode, string ToCode, decimal Rate, CurrencyRateType RateType, string Source);
    private sealed record PaymentTypeSeed(string Code, string Name, string Description);
    private sealed record TaxCodeSeed(string Code, string Name, decimal RatePercent, bool IsInclusive, TaxScope Scope, string Description);
    private sealed record TaxConversionSeed(string SourceCode, string TargetCode, decimal Multiplier, string Notes);
    private sealed record ReferenceFormSeed(string Code, string Name, string Module, string RouteTemplate);
}
