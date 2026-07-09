using System.Diagnostics;
using System.Text;

namespace ISS.DocumentIntelligence.ReceiptDocuments;

public sealed class CommandLineReceiptDocumentAnalyzer : IReceiptDocumentAnalyzer
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp",
        ".gif",
        ".jpeg",
        ".jpg",
        ".png",
        ".tif",
        ".tiff",
        ".webp"
    };

    private readonly HeuristicReceiptDocumentAnalyzer _textAnalyzer = new();

    public async Task<ReceiptDocumentExtraction> AnalyzeAsync(ReceiptDocumentInput input, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(input.UserText) || LooksTextual(input.FileName, input.ContentType))
        {
            return await _textAnalyzer.AnalyzeAsync(input, cancellationToken);
        }

        var warnings = new List<string>();
        var extension = Path.GetExtension(input.FileName);
        if (!ImageExtensions.Contains(extension) && !IsPdf(input.FileName, input.ContentType))
        {
            warnings.Add($"Unsupported OCR file type '{extension}'. Upload an image/PDF or paste OCR text.");
            return new ReceiptDocumentExtraction(null, null, null, null, [], null, warnings);
        }

        var workingDirectory = Path.Combine(Path.GetTempPath(), "iss-document-ocr", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var sourcePath = Path.Combine(workingDirectory, $"source{(string.IsNullOrWhiteSpace(extension) ? ".bin" : extension)}");
            await using (var file = File.Create(sourcePath))
            {
                await input.Content.CopyToAsync(file, cancellationToken);
            }

            var rawText = IsPdf(input.FileName, input.ContentType)
                ? await ExtractPdfTextAsync(sourcePath, workingDirectory, cancellationToken)
                : await RunTesseractAsync(sourcePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                warnings.Add("OCR completed but no readable text was found. Try a clearer image or paste recognized text manually.");
                return new ReceiptDocumentExtraction(null, null, null, null, [], null, warnings);
            }

            var textExtraction = await _textAnalyzer.AnalyzeAsync(
                new ReceiptDocumentInput(input.FileName, "text/plain", new MemoryStream(Encoding.UTF8.GetBytes(rawText)), rawText),
                cancellationToken);

            return textExtraction with { Warnings = textExtraction.Warnings.Concat(warnings).ToList() };
        }
        catch (OcrCommandException ex)
        {
            warnings.Add(ex.Message);
            return new ReceiptDocumentExtraction(null, null, null, null, [], null, warnings);
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    private static async Task<string> ExtractPdfTextAsync(string sourcePath, string workingDirectory, CancellationToken cancellationToken)
    {
        var directText = await RunProcessAsync("pdftotext", QuoteArg(sourcePath) + " -", cancellationToken, 20);
        if (!string.IsNullOrWhiteSpace(directText.StdOut))
        {
            return directText.StdOut;
        }

        var pagePrefix = Path.Combine(workingDirectory, "page");
        var pages = await RunProcessAsync("pdftoppm", $"-png -r 200 -f 1 -l 5 {QuoteArg(sourcePath)} {QuoteArg(pagePrefix)}", cancellationToken, 30);
        if (pages.ExitCode != 0)
        {
            throw new OcrCommandException("PDF OCR conversion failed. Ensure poppler-utils is installed in the runtime image.");
        }

        var imageFiles = Directory.GetFiles(workingDirectory, "page-*.png").OrderBy(x => x).ToList();
        var builder = new StringBuilder();
        foreach (var imageFile in imageFiles)
        {
            builder.AppendLine(await RunTesseractAsync(imageFile, cancellationToken));
        }

        return builder.ToString();
    }

    private static async Task<string> RunTesseractAsync(string imagePath, CancellationToken cancellationToken)
    {
        var language = Environment.GetEnvironmentVariable("DOCUMENT_OCR_TESSERACT_LANG");
        var languageArgs = string.IsNullOrWhiteSpace(language) ? string.Empty : $" -l {QuoteArg(language)}";
        var result = await RunProcessAsync("tesseract", $"{QuoteArg(imagePath)} stdout{languageArgs} --psm 6", cancellationToken, 45);

        if (result.ExitCode != 0)
        {
            throw new OcrCommandException("Tesseract OCR failed. Ensure tesseract-ocr is installed and the uploaded image is readable.");
        }

        return result.StdOut;
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken,
        int timeoutSeconds)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);

            return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OcrCommandException($"OCR command '{fileName}' timed out.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new OcrCommandException($"OCR command '{fileName}' was not found. Install Tesseract/Poppler in the runtime image.");
        }
    }

    private static bool LooksTextual(string fileName, string contentType)
        => contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
           || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
           || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
           || fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
           || fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    private static bool IsPdf(string fileName, string contentType)
        => contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
           || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    private static string QuoteArg(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch
        {
            // Temporary OCR files are best-effort cleanup.
        }
    }

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

    private sealed class OcrCommandException(string message) : Exception(message);
}
