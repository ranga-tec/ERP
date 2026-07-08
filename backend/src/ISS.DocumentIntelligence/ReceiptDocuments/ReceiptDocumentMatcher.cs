using System.Text.RegularExpressions;

namespace ISS.DocumentIntelligence.ReceiptDocuments;

public sealed partial class ReceiptDocumentMatcher : IReceiptDocumentMatcher
{
    public ReceiptDocumentSuggestion Match(ReceiptDocumentExtraction extraction, ReceiptDocumentContext context)
    {
        var warnings = new List<string>(extraction.Warnings);
        var matchedPurchaseOrder = MatchPurchaseOrder(extraction, context.PurchaseOrders);
        var matchedSupplier = MatchSupplier(extraction, context.Suppliers, matchedPurchaseOrder, context.PurchaseOrders);
        var lineSuggestions = extraction.Lines.Select(line => MatchLine(line, context.PurchaseOrderLines)).ToList();

        if (matchedPurchaseOrder is null)
        {
            warnings.Add("No purchase order number could be matched. Suggestions are limited to the open GRN purchase order.");
        }

        if (matchedSupplier is null)
        {
            warnings.Add("No supplier could be confidently matched from the document text.");
        }

        if (lineSuggestions.Any(line => line.PurchaseOrderLineId is null))
        {
            warnings.Add("Some extracted lines could not be matched to PO lines and were left as review-only suggestions.");
        }

        return new ReceiptDocumentSuggestion(extraction, matchedPurchaseOrder, matchedSupplier, lineSuggestions, warnings);
    }

    private static ReceiptMatchedPurchaseOrder? MatchPurchaseOrder(
        ReceiptDocumentExtraction extraction,
        IReadOnlyList<ReceiptPurchaseOrderReference> purchaseOrders)
    {
        if (string.IsNullOrWhiteSpace(extraction.PurchaseOrderNumber))
        {
            return null;
        }

        var normalized = NormalizeCode(extraction.PurchaseOrderNumber);
        var match = purchaseOrders.FirstOrDefault(po => NormalizeCode(po.Number) == normalized)
                    ?? purchaseOrders.FirstOrDefault(po => NormalizeCode(po.Number).Contains(normalized) || normalized.Contains(NormalizeCode(po.Number)));

        return match is null ? null : new ReceiptMatchedPurchaseOrder(match.Id, match.Number, 0.98m, "Matched by purchase order number.");
    }

    private static ReceiptMatchedSupplier? MatchSupplier(
        ReceiptDocumentExtraction extraction,
        IReadOnlyList<ReceiptSupplierReference> suppliers,
        ReceiptMatchedPurchaseOrder? matchedPurchaseOrder,
        IReadOnlyList<ReceiptPurchaseOrderReference> purchaseOrders)
    {
        if (matchedPurchaseOrder is not null)
        {
            var purchaseOrder = purchaseOrders.FirstOrDefault(po => po.Id == matchedPurchaseOrder.Id);
            var supplier = purchaseOrder is null ? null : suppliers.FirstOrDefault(candidate => candidate.Id == purchaseOrder.SupplierId);
            if (supplier is not null)
            {
                return new ReceiptMatchedSupplier(supplier.Id, supplier.Code, supplier.Name, 0.9m, "Matched through purchase order supplier.");
            }
        }

        var supplierText = NormalizeText(extraction.SupplierName ?? extraction.RawText ?? string.Empty);
        if (supplierText.Length == 0)
        {
            return null;
        }

        var best = suppliers
            .Select(supplier => new
            {
                Supplier = supplier,
                Score = ScoreTextMatch(supplierText, $"{supplier.Code} {supplier.Name}")
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return best is null || best.Score < 0.45m
            ? null
            : new ReceiptMatchedSupplier(best.Supplier.Id, best.Supplier.Code, best.Supplier.Name, best.Score, "Matched by supplier text.");
    }

    private static ReceiptDocumentLineSuggestion MatchLine(
        ReceiptDocumentLineExtraction line,
        IReadOnlyList<ReceiptPurchaseOrderLineReference> candidates)
    {
        var best = candidates
            .Select(candidate => new
            {
                Line = candidate,
                Score = ScoreLine(line, candidate)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        var quantity = Math.Max(0m, line.Quantity ?? 0m);
        if (best is null || best.Score < 0.35m)
        {
            return new ReceiptDocumentLineSuggestion(
                line.LineNumber,
                line.ItemCode,
                line.Description,
                line.Quantity,
                line.UnitCost,
                null,
                null,
                null,
                null,
                quantity,
                line.UnitCost,
                0m,
                "Unmatched",
                "No confident PO line match.");
        }

        var suggestedQuantity = Math.Min(quantity, Math.Max(0m, best.Line.AvailableQuantity));
        return new ReceiptDocumentLineSuggestion(
            line.LineNumber,
            line.ItemCode,
            line.Description,
            line.Quantity,
            line.UnitCost,
            best.Line.PurchaseOrderLineId,
            best.Line.ItemId,
            best.Line.ItemSku,
            best.Line.ItemName,
            suggestedQuantity,
            line.UnitCost ?? best.Line.UnitCost,
            best.Score,
            suggestedQuantity <= 0 ? "MatchedNoAvailableQuantity" : "Matched",
            suggestedQuantity <= 0
                ? "Matched item, but this PO line has no available quantity."
                : "Matched by item code or description.");
    }

    private static decimal ScoreLine(ReceiptDocumentLineExtraction extracted, ReceiptPurchaseOrderLineReference candidate)
    {
        var score = 0m;
        var extractedCode = NormalizeCode(extracted.ItemCode ?? string.Empty);
        var candidateCode = NormalizeCode(candidate.ItemSku);
        if (extractedCode.Length > 0 && candidateCode.Length > 0)
        {
            if (extractedCode == candidateCode)
            {
                score += 0.75m;
            }
            else if (extractedCode.Contains(candidateCode) || candidateCode.Contains(extractedCode))
            {
                score += 0.55m;
            }
        }

        score += ScoreTextMatch(NormalizeText(extracted.Description ?? string.Empty), candidate.ItemName) * 0.45m;
        return Math.Min(1m, score);
    }

    private static decimal ScoreTextMatch(string source, string target)
    {
        var sourceTokens = Tokenize(source);
        var targetTokens = Tokenize(target);
        if (sourceTokens.Count == 0 || targetTokens.Count == 0)
        {
            return 0m;
        }

        var overlap = sourceTokens.Intersect(targetTokens).Count();
        return (decimal)overlap / Math.Max(sourceTokens.Count, targetTokens.Count);
    }

    private static HashSet<string> Tokenize(string value)
        => WordRegex().Matches(NormalizeText(value))
            .Select(match => match.Value)
            .Where(token => token.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeCode(string value)
        => NonCodeRegex().Replace(value.ToUpperInvariant(), string.Empty);

    private static string NormalizeText(string value)
        => value.Trim().ToLowerInvariant();

    [GeneratedRegex(@"[^A-Z0-9]")]
    private static partial Regex NonCodeRegex();

    [GeneratedRegex(@"[a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex WordRegex();
}
