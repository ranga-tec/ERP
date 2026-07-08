using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ISS.DocumentIntelligence.ReceiptDocuments;

public sealed partial class HeuristicReceiptDocumentAnalyzer : IReceiptDocumentAnalyzer
{
    public async Task<ReceiptDocumentExtraction> AnalyzeAsync(ReceiptDocumentInput input, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var rawText = await ReadTextAsync(input, cancellationToken);

        if (string.IsNullOrWhiteSpace(rawText))
        {
            warnings.Add("No OCR text was available. Configure an OCR/AI provider later, or paste recognized text while reviewing the upload.");
            return new ReceiptDocumentExtraction(null, null, null, null, [], null, warnings);
        }

        var sourceLines = rawText.Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var supplierName = sourceLines.FirstOrDefault(line => !LooksLikeMetadata(line) && !LineHasMoneyOrQuantity(line));
        var poNumber = FindFirstGroup(rawText, PurchaseOrderRegex());
        var documentNumber = FindFirstGroup(rawText, DocumentNumberRegex());
        var documentDate = TryFindDate(rawText);
        var extractedLines = ExtractLines(sourceLines);

        if (extractedLines.Count == 0)
        {
            warnings.Add("No receipt lines were confidently parsed. Review the upload text and enter quantities manually if needed.");
        }

        return new ReceiptDocumentExtraction(supplierName, poNumber, documentNumber, documentDate, extractedLines, rawText, warnings);
    }

    private static async Task<string?> ReadTextAsync(ReceiptDocumentInput input, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(input.UserText))
        {
            return input.UserText;
        }

        if (!LooksTextual(input.FileName, input.ContentType))
        {
            return null;
        }

        using var reader = new StreamReader(input.Content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static bool LooksTextual(string fileName, string contentType)
        => contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
           || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
           || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
           || fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
           || fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    private static List<ReceiptDocumentLineExtraction> ExtractLines(IEnumerable<string> sourceLines)
    {
        var result = new List<ReceiptDocumentLineExtraction>();
        var lineNumber = 0;

        foreach (var sourceLine in sourceLines)
        {
            lineNumber++;
            if (LooksLikeMetadata(sourceLine))
            {
                continue;
            }

            var match = ReceiptLineRegex().Match(sourceLine);
            if (!match.Success)
            {
                continue;
            }

            var itemCode = EmptyToNull(match.Groups["code"].Value);
            var description = EmptyToNull(match.Groups["desc"].Value);
            var quantity = ParseDecimal(match.Groups["qty"].Value);
            var unitCost = ParseDecimal(match.Groups["unit"].Value);

            if (quantity is null || quantity <= 0)
            {
                continue;
            }

            result.Add(new ReceiptDocumentLineExtraction(lineNumber, itemCode, description, quantity, unitCost, null, []));
        }

        return result;
    }

    private static string? FindFirstGroup(string text, Regex regex)
    {
        var match = regex.Match(text);
        return match.Success ? EmptyToNull(match.Groups[1].Value) : null;
    }

    private static DateOnly? TryFindDate(string text)
    {
        foreach (Match match in DateRegex().Matches(text))
        {
            if (DateOnly.TryParse(match.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static decimal? ParseDecimal(string value)
    {
        var cleaned = value.Replace(",", string.Empty).Trim();
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static bool LooksLikeMetadata(string line)
        => PurchaseOrderRegex().IsMatch(line)
           || DocumentNumberRegex().IsMatch(line)
           || DateRegex().IsMatch(line)
           || line.Contains("total", StringComparison.OrdinalIgnoreCase)
           || line.Contains("subtotal", StringComparison.OrdinalIgnoreCase)
           || line.Contains("tax", StringComparison.OrdinalIgnoreCase);

    private static bool LineHasMoneyOrQuantity(string line) => ReceiptLineRegex().IsMatch(line);

    private static string? EmptyToNull(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    [GeneratedRegex(@"(?:P\.?\s*O\.?|purchase\s*order|po\s*no)\s*[:#-]?\s*([A-Z0-9][A-Z0-9\-\/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PurchaseOrderRegex();

    [GeneratedRegex(@"(?:invoice|receipt|bill|doc(?:ument)?)\s*(?:no|number|#)?\s*[:#-]?\s*([A-Z0-9][A-Z0-9\-\/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentNumberRegex();

    [GeneratedRegex(@"\b(?:\d{4}[-/]\d{1,2}[-/]\d{1,2}|\d{1,2}[-/]\d{1,2}[-/]\d{2,4})\b")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"^\s*(?:(?<code>[A-Z0-9][A-Z0-9\-_.\/]{1,})\s+)?(?<desc>.+?)\s+(?<qty>\d+(?:\.\d+)?)\s+(?<unit>\d+(?:,\d{3})*(?:\.\d+)?)\s*(?:\d+(?:,\d{3})*(?:\.\d+)?)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex ReceiptLineRegex();
}
