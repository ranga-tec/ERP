using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;

namespace ISS.Api.Files;

internal static class AttachmentUploadPolicy
{
    public const long MaxAttachmentSizeBytes = 25 * 1024 * 1024;
    public const int MaxAttachmentsPerRecord = 25;
    public const long MaxTotalAttachmentBytesPerRecord = 100 * 1024 * 1024;

    private const int MaxFileNameLength = 256;
    private const int MaxNotesLength = 1000;

    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".txt",
        ".csv",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".webp",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/octet-stream",
        "application/pdf",
        "text/plain",
        "text/csv",
        "application/csv",
        "application/vnd.ms-excel",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp"
    };

    public sealed record ValidatedUpload(
        string FileName,
        string Extension,
        string ContentType,
        bool IsImage,
        long SizeBytes);

    public static bool TryValidate(IFormFile? file, string? notes, out ValidatedUpload? validated, out string? error)
    {
        validated = null;
        error = null;

        if (file is null || file.Length <= 0)
        {
            error = "A non-empty file is required.";
            return false;
        }

        if (file.Length > MaxAttachmentSizeBytes)
        {
            error = $"File is too large. Max {MaxAttachmentSizeBytes} bytes.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > MaxNotesLength)
        {
            error = $"Notes are too long. Max {MaxNotesLength} characters.";
            return false;
        }

        var fileName = SanitizeFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            error = "File name is required.";
            return false;
        }

        if (fileName.Length > MaxFileNameLength)
        {
            error = $"File name is too long. Max {MaxFileNameLength} characters.";
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            error = "File type is not allowed. Allowed extensions: .pdf, .txt, .csv, .png, .jpg, .jpeg, .gif, .webp, .doc, .docx, .xls, .xlsx.";
            return false;
        }

        extension = extension.ToLowerInvariant();

        var contentType = NormalizeContentType(file.ContentType);
        if (string.IsNullOrWhiteSpace(contentType)
            && ContentTypeProvider.TryGetContentType($"file{extension}", out var guessedContentType))
        {
            contentType = guessedContentType;
        }

        contentType ??= "application/octet-stream";
        if (!AllowedContentTypes.Contains(contentType))
        {
            error = $"Content type '{contentType}' is not allowed.";
            return false;
        }

        if (!TryValidateFileSignature(file, extension, out error))
        {
            return false;
        }

        validated = new ValidatedUpload(
            fileName,
            extension,
            contentType,
            contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
            file.Length);
        return true;
    }

    public static bool TryValidateQuota(int existingCount, long existingTotalBytes, long incomingSizeBytes, out string? error)
    {
        error = null;

        if (existingCount >= MaxAttachmentsPerRecord)
        {
            error = $"Attachment limit reached. Max {MaxAttachmentsPerRecord} files per record.";
            return false;
        }

        if (existingTotalBytes + incomingSizeBytes > MaxTotalAttachmentBytesPerRecord)
        {
            error = $"Attachment storage limit exceeded. Max {MaxTotalAttachmentBytesPerRecord} bytes per record.";
            return false;
        }

        return true;
    }

    private static string? NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        var normalized = contentType.Split(';', 2)[0].Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string SanitizeFileName(string? rawFileName)
    {
        var fileName = Path.GetFileName(rawFileName ?? string.Empty);
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        var chars = fileName.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsControl(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return new string(chars).Trim();
    }

    private static bool TryValidateFileSignature(IFormFile file, string extension, out string? error)
    {
        error = null;

        try
        {
            using var stream = file.OpenReadStream();
            var header = new byte[512];
            var read = stream.Read(header, 0, header.Length);
            if (read <= 0)
            {
                error = "Unable to read file content.";
                return false;
            }

            if (IsSignatureValid(extension, header.AsSpan(0, read)))
            {
                return true;
            }

            error = "File content does not match the declared file type.";
            return false;
        }
        catch (Exception)
        {
            error = "Unable to validate uploaded file content.";
            return false;
        }
    }

    private static bool IsSignatureValid(string extension, ReadOnlySpan<byte> header)
        => extension switch
        {
            ".pdf" => HasPrefix(header, "%PDF-"),
            ".png" => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".gif" => HasPrefix(header, "GIF87a") || HasPrefix(header, "GIF89a"),
            ".webp" => HasPrefix(header, "RIFF") && header.Length >= 12 && HasAsciiAt(header, 8, "WEBP"),
            ".docx" or ".xlsx" => IsZipHeader(header),
            ".doc" or ".xls" => IsOleCompoundHeader(header) || IsZipHeader(header),
            ".txt" or ".csv" => LooksLikeText(header),
            _ => false
        };

    private static bool HasPrefix(ReadOnlySpan<byte> data, string ascii)
    {
        var bytes = Encoding.ASCII.GetBytes(ascii);
        return data.StartsWith(bytes);
    }

    private static bool HasAsciiAt(ReadOnlySpan<byte> data, int offset, string ascii)
    {
        var bytes = Encoding.ASCII.GetBytes(ascii);
        return data.Length >= offset + bytes.Length && data.Slice(offset, bytes.Length).SequenceEqual(bytes);
    }

    private static bool IsZipHeader(ReadOnlySpan<byte> data)
        => data.Length >= 4
           && data[0] == (byte)'P'
           && data[1] == (byte)'K'
           && (data[2], data[3]) is ((byte)0x03, (byte)0x04) or ((byte)0x05, (byte)0x06) or ((byte)0x07, (byte)0x08);

    private static bool IsOleCompoundHeader(ReadOnlySpan<byte> data)
        => data.StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });

    private static bool LooksLikeText(ReadOnlySpan<byte> data)
    {
        // Accept UTF-16 BOM-prefixed text files; they contain NUL bytes by design.
        if (data.Length >= 2 && ((data[0] == 0xFF && data[1] == 0xFE) || (data[0] == 0xFE && data[1] == 0xFF)))
        {
            return true;
        }

        var inspected = Math.Min(data.Length, 256);
        for (var i = 0; i < inspected; i++)
        {
            var b = data[i];
            if (b == 0x00)
            {
                return false;
            }

            var isCommonWhitespace = b is 0x09 or 0x0A or 0x0D;
            var isPrintableAscii = b >= 0x20 && b <= 0x7E;
            var isUtf8LeadOrTrail = b >= 0x80;
            if (!(isCommonWhitespace || isPrintableAscii || isUtf8LeadOrTrail))
            {
                return false;
            }
        }

        return true;
    }
}
