using ISS.Api.Security;
using ISS.Api.Files;
using ISS.Application.Persistence;
using ISS.Domain.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Documents;

[ApiController]
[Route("api/documents")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement},{Roles.Inventory},{Roles.Sales},{Roles.Service},{Roles.Finance}")]
public sealed class DocumentCollaborationController(
    IIssDbContext dbContext,
    IHostEnvironment hostEnvironment) : ControllerBase
{
    public sealed record DocumentCommentDto(
        Guid Id,
        string ReferenceType,
        Guid ReferenceId,
        string Text,
        DateTimeOffset CreatedAt,
        Guid? CreatedBy,
        DateTimeOffset? LastModifiedAt,
        Guid? LastModifiedBy);

    public sealed record CreateDocumentCommentRequest(string Text);

    public sealed record DocumentAttachmentDto(
        Guid Id,
        string ReferenceType,
        Guid ReferenceId,
        string FileName,
        string Url,
        bool IsImage,
        string? ContentType,
        long? SizeBytes,
        string? Notes,
        DateTimeOffset CreatedAt,
        Guid? CreatedBy);

    [HttpGet("{referenceType}/{referenceId:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<DocumentCommentDto>>> ListComments(
        string referenceType,
        Guid referenceId,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeReferenceType(referenceType);

        var comments = await dbContext.DocumentComments.AsNoTracking()
            .Where(x => x.ReferenceType == normalizedType && x.ReferenceId == referenceId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DocumentCommentDto(
                x.Id,
                x.ReferenceType,
                x.ReferenceId,
                x.Text,
                x.CreatedAt,
                x.CreatedBy,
                x.LastModifiedAt,
                x.LastModifiedBy))
            .ToListAsync(cancellationToken);

        return Ok(comments);
    }

    [HttpPost("{referenceType}/{referenceId:guid}/comments")]
    public async Task<ActionResult<DocumentCommentDto>> AddComment(
        string referenceType,
        Guid referenceId,
        CreateDocumentCommentRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeReferenceType(referenceType);
        var comment = new DocumentComment(normalizedType, referenceId, request.Text);
        await dbContext.DocumentComments.AddAsync(comment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToCommentDto(comment));
    }

    [HttpDelete("{referenceType}/{referenceId:guid}/comments/{commentId:guid}")]
    public async Task<ActionResult> DeleteComment(
        string referenceType,
        Guid referenceId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeReferenceType(referenceType);
        var comment = await dbContext.DocumentComments
            .FirstOrDefaultAsync(x => x.Id == commentId && x.ReferenceType == normalizedType && x.ReferenceId == referenceId, cancellationToken);

        if (comment is null)
        {
            return NotFound();
        }

        dbContext.DocumentComments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{referenceType}/{referenceId:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyList<DocumentAttachmentDto>>> ListAttachments(
        string referenceType,
        Guid referenceId,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeReferenceType(referenceType);

        var attachments = await dbContext.DocumentAttachments.AsNoTracking()
            .Where(x => x.ReferenceType == normalizedType && x.ReferenceId == referenceId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DocumentAttachmentDto(
                x.Id,
                x.ReferenceType,
                x.ReferenceId,
                x.FileName,
                x.Url,
                x.IsImage,
                x.ContentType,
                x.SizeBytes,
                x.Notes,
                x.CreatedAt,
                x.CreatedBy))
            .ToListAsync(cancellationToken);

        return Ok(attachments);
    }

    [HttpPost("{referenceType}/{referenceId:guid}/attachments/upload")]
    [RequestSizeLimit(AttachmentUploadPolicy.MaxAttachmentSizeBytes)]
    public async Task<ActionResult<DocumentAttachmentDto>> UploadAttachment(
        string referenceType,
        Guid referenceId,
        [FromForm] IFormFile file,
        [FromForm] string? notes,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeReferenceType(referenceType);

        if (!AttachmentUploadPolicy.TryValidate(file, notes, out var validated, out var validationError))
        {
            return BadRequest(validationError);
        }

        var usage = await dbContext.DocumentAttachments.AsNoTracking()
            .Where(x => x.ReferenceType == normalizedType && x.ReferenceId == referenceId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                TotalBytes = g.Sum(x => x.SizeBytes ?? 0L)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (!AttachmentUploadPolicy.TryValidateQuota(
                usage?.Count ?? 0,
                usage?.TotalBytes ?? 0L,
                validated!.SizeBytes,
                out var quotaError))
        {
            return BadRequest(quotaError);
        }

        var attachmentId = Guid.NewGuid();
        var refTypeFolder = normalizedType.Replace('-', '_');
        var refIdFolder = referenceId.ToString("N");
        var storedFileName = $"{attachmentId:N}{validated!.Extension}";
        var relativePath = Path.Combine("App_Data", "document-attachments", refTypeFolder, refIdFolder, storedFileName);
        var rootPath = hostEnvironment.ContentRootPath;
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        var attachmentsRoot = Path.GetFullPath(Path.Combine(rootPath, "App_Data", "document-attachments"));

        if (!fullPath.StartsWith(attachmentsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid attachment storage path.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var publicUrl = $"/api/documents/{normalizedType}/{referenceId}/attachments/{attachmentId}/content";

        var attachment = new DocumentAttachment(
            normalizedType,
            referenceId,
            validated.FileName,
            publicUrl,
            validated.IsImage,
            validated.ContentType,
            validated.SizeBytes,
            notes,
            relativePath);
        attachment.Id = attachmentId;

        await dbContext.DocumentAttachments.AddAsync(attachment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToAttachmentDto(attachment));
    }

    [HttpGet("{referenceType}/{referenceId:guid}/attachments/{attachmentId:guid}/content")]
    public async Task<ActionResult> GetAttachmentContent(
        string referenceType,
        Guid referenceId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeReferenceType(referenceType);
        var attachment = await dbContext.DocumentAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.ReferenceType == normalizedType && x.ReferenceId == referenceId, cancellationToken);

        if (attachment is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(attachment.StoragePath))
        {
            return BadRequest("Attachment does not have stored file content.");
        }

        var rootPath = hostEnvironment.ContentRootPath;
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, attachment.StoragePath));
        var attachmentsRoot = Path.GetFullPath(Path.Combine(rootPath, "App_Data", "document-attachments"));
        if (!fullPath.StartsWith(attachmentsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        var fileStream = System.IO.File.OpenRead(fullPath);
        return File(fileStream, attachment.ContentType ?? "application/octet-stream", attachment.FileName);
    }

    [HttpDelete("{referenceType}/{referenceId:guid}/attachments/{attachmentId:guid}")]
    public async Task<ActionResult> DeleteAttachment(
        string referenceType,
        Guid referenceId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeReferenceType(referenceType);
        var attachment = await dbContext.DocumentAttachments
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.ReferenceType == normalizedType && x.ReferenceId == referenceId, cancellationToken);

        if (attachment is null)
        {
            return NotFound();
        }

        string? fullPath = null;
        if (!string.IsNullOrWhiteSpace(attachment.StoragePath))
        {
            fullPath = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, attachment.StoragePath));
        }

        dbContext.DocumentAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(fullPath) && System.IO.File.Exists(fullPath))
        {
            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch
            {
                // Keep metadata deletion successful even if filesystem cleanup fails.
            }
        }

        return NoContent();
    }

    private static string NormalizeReferenceType(string referenceType) => DocumentComment.NormalizeReferenceType(referenceType);

    private static DocumentCommentDto ToCommentDto(DocumentComment comment) =>
        new(
            comment.Id,
            comment.ReferenceType,
            comment.ReferenceId,
            comment.Text,
            comment.CreatedAt,
            comment.CreatedBy,
            comment.LastModifiedAt,
            comment.LastModifiedBy);

    private static DocumentAttachmentDto ToAttachmentDto(DocumentAttachment attachment) =>
        new(
            attachment.Id,
            attachment.ReferenceType,
            attachment.ReferenceId,
            attachment.FileName,
            attachment.Url,
            attachment.IsImage,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.Notes,
            attachment.CreatedAt,
            attachment.CreatedBy);
}
