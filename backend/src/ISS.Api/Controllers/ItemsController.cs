using ISS.Api.Security;
using ISS.Api.Files;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class ItemsController(IIssDbContext dbContext, IDocumentPdfService pdfService, IWebHostEnvironment hostEnvironment) : ControllerBase
{
    public sealed record ItemDto(
        Guid Id,
        string Sku,
        string Name,
        ItemType Type,
        TrackingType TrackingType,
        string UnitOfMeasure,
        Guid? BrandId,
        Guid? CategoryId,
        string? CategoryCode,
        string? CategoryName,
        Guid? SubcategoryId,
        string? SubcategoryCode,
        string? SubcategoryName,
        string? Barcode,
        decimal DefaultUnitCost,
        bool IsActive);

    public sealed record CreateItemRequest(
        string Sku,
        string Name,
        ItemType Type,
        TrackingType TrackingType,
        string UnitOfMeasure,
        Guid? BrandId,
        Guid? CategoryId,
        Guid? SubcategoryId,
        string? Barcode,
        decimal DefaultUnitCost);

    public sealed record UpdateItemRequest(
        string Sku,
        string Name,
        ItemType Type,
        TrackingType TrackingType,
        string UnitOfMeasure,
        Guid? BrandId,
        Guid? CategoryId,
        Guid? SubcategoryId,
        string? Barcode,
        decimal DefaultUnitCost,
        bool IsActive);

    public sealed record ItemAttachmentDto(
        Guid Id,
        Guid ItemId,
        string FileName,
        string Url,
        bool IsImage,
        string? ContentType,
        long? SizeBytes,
        string? Notes,
        DateTimeOffset CreatedAt,
        Guid? CreatedBy);

    public sealed record CreateItemAttachmentRequest(
        string FileName,
        string Url,
        bool IsImage,
        string? ContentType,
        long? SizeBytes,
        string? Notes);

    public sealed record ItemPriceHistoryEntryDto(
        Guid AuditLogId,
        DateTimeOffset OccurredAt,
        Guid? UserId,
        decimal? OldDefaultUnitCost,
        decimal NewDefaultUnitCost);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.Items.AsNoTracking()
            .OrderBy(x => x.Sku)
            .Select(x => new ItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.UnitOfMeasure,
                x.BrandId,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.SubcategoryId,
                x.Subcategory != null ? x.Subcategory.Code : null,
                x.Subcategory != null ? x.Subcategory.Name : null,
                x.Barcode,
                x.DefaultUnitCost,
                x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.UnitOfMeasure,
                x.BrandId,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.SubcategoryId,
                x.Subcategory != null ? x.Subcategory.Code : null,
                x.Subcategory != null ? x.Subcategory.Name : null,
                x.Barcode,
                x.DefaultUnitCost,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}/label/pdf")]
    public async Task<ActionResult> LabelPdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.ItemLabel, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<ItemDto>> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        barcode = barcode.Trim();

        var item = await dbContext.Items.AsNoTracking()
            .Where(x => x.Barcode == barcode)
            .Select(x => new ItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.UnitOfMeasure,
                x.BrandId,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.SubcategoryId,
                x.Subcategory != null ? x.Subcategory.Code : null,
                x.Subcategory != null ? x.Subcategory.Name : null,
                x.Barcode,
                x.DefaultUnitCost,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var classificationError = await ValidateClassificationAsync(request.CategoryId, request.SubcategoryId, cancellationToken);
        if (classificationError is not null)
        {
            return BadRequest(classificationError);
        }

        var item = new Item(
            request.Sku,
            request.Name,
            request.Type,
            request.TrackingType,
            request.UnitOfMeasure,
            request.BrandId,
            request.Barcode,
            request.DefaultUnitCost,
            request.CategoryId,
            request.SubcategoryId);

        await dbContext.Items.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.Items.AsNoTracking()
            .Where(x => x.Id == item.Id)
            .Select(x => new ItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.UnitOfMeasure,
                x.BrandId,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.SubcategoryId,
                x.Subcategory != null ? x.Subcategory.Code : null,
                x.Subcategory != null ? x.Subcategory.Name : null,
                x.Barcode,
                x.DefaultUnitCost,
                x.IsActive))
            .FirstAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemDto>> Update(Guid id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var classificationError = await ValidateClassificationAsync(request.CategoryId, request.SubcategoryId, cancellationToken);
        if (classificationError is not null)
        {
            return BadRequest(classificationError);
        }

        var item = await dbContext.Items.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(
            request.Sku,
            request.Name,
            request.Type,
            request.TrackingType,
            request.UnitOfMeasure,
            request.BrandId,
            request.Barcode,
            request.DefaultUnitCost,
            request.IsActive,
            request.CategoryId,
            request.SubcategoryId);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.Items.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.UnitOfMeasure,
                x.BrandId,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.SubcategoryId,
                x.Subcategory != null ? x.Subcategory.Code : null,
                x.Subcategory != null ? x.Subcategory.Name : null,
                x.Barcode,
                x.DefaultUnitCost,
                x.IsActive))
            .FirstAsync(cancellationToken);

        return Ok(updated);
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyList<ItemAttachmentDto>>> ListAttachments(Guid id, CancellationToken cancellationToken)
    {
        var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!itemExists)
        {
            return NotFound();
        }

        var attachments = await dbContext.ItemAttachments.AsNoTracking()
            .Where(x => x.ItemId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ItemAttachmentDto(
                x.Id,
                x.ItemId,
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

    [HttpPost("{id:guid}/attachments")]
    public async Task<ActionResult<ItemAttachmentDto>> AddAttachment(Guid id, CreateItemAttachmentRequest request, CancellationToken cancellationToken)
    {
        var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!itemExists)
        {
            return NotFound();
        }

        var attachment = new ItemAttachment(
            id,
            request.FileName,
            request.Url,
            request.IsImage,
            request.ContentType,
            request.SizeBytes,
            request.Notes);

        await dbContext.ItemAttachments.AddAsync(attachment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(ListAttachments),
            new { id },
            ToAttachmentDto(attachment));
    }

    [HttpPost("{id:guid}/attachments/upload")]
    [RequestSizeLimit(AttachmentUploadPolicy.MaxAttachmentSizeBytes)]
    public async Task<ActionResult<ItemAttachmentDto>> UploadAttachment(
        Guid id,
        [FromForm] IFormFile file,
        [FromForm] string? notes,
        CancellationToken cancellationToken)
    {
        if (!AttachmentUploadPolicy.TryValidate(file, notes, out var validated, out var validationError))
        {
            return BadRequest(validationError);
        }

        var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!itemExists)
        {
            return NotFound();
        }

        var usage = await dbContext.ItemAttachments.AsNoTracking()
            .Where(x => x.ItemId == id)
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
        var itemFolderName = id.ToString("N");
        var storedFileName = $"{attachmentId:N}{validated!.Extension}";
        var relativePath = Path.Combine("App_Data", "item-attachments", itemFolderName, storedFileName);
        var rootPath = hostEnvironment.ContentRootPath;
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        var attachmentsRoot = Path.GetFullPath(Path.Combine(rootPath, "App_Data", "item-attachments"));

        if (!fullPath.StartsWith(attachmentsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid attachment storage path.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var publicUrl = $"/api/items/{id}/attachments/{attachmentId}/content";

        var attachment = new ItemAttachment(
            id,
            validated.FileName,
            publicUrl,
            validated.IsImage,
            validated.ContentType,
            validated.SizeBytes,
            notes,
            relativePath);
        attachment.Id = attachmentId;

        await dbContext.ItemAttachments.AddAsync(attachment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(ListAttachments), new { id }, ToAttachmentDto(attachment));
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/content")]
    public async Task<ActionResult> GetAttachmentContent(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.ItemAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.ItemId == id, cancellationToken);

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
        var attachmentsRoot = Path.GetFullPath(Path.Combine(rootPath, "App_Data", "item-attachments"));
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

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<ActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.ItemAttachments
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.ItemId == id, cancellationToken);

        if (attachment is null)
        {
            return NotFound();
        }

        var fullPath = default(string);
        if (!string.IsNullOrWhiteSpace(attachment.StoragePath))
        {
            fullPath = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, attachment.StoragePath));
        }

        dbContext.ItemAttachments.Remove(attachment);
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

    [HttpGet("{id:guid}/price-history")]
    public async Task<ActionResult<IReadOnlyList<ItemPriceHistoryEntryDto>>> PriceHistory(Guid id, CancellationToken cancellationToken)
    {
        var itemExists = await dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!itemExists)
        {
            return NotFound();
        }

        var itemKey = id.ToString();
        var logs = await dbContext.AuditLogs.AsNoTracking()
            .Where(x => x.TableName == nameof(IIssDbContext.Items) && x.Key == itemKey)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new { x.Id, x.OccurredAt, x.UserId, x.ChangesJson })
            .ToListAsync(cancellationToken);

        var history = new List<ItemPriceHistoryEntryDto>();
        foreach (var log in logs)
        {
            if (!TryExtractDefaultCost(log.ChangesJson, out var oldCost, out var newCost)
                || newCost is not decimal nextDefaultCost)
            {
                continue;
            }

            history.Add(new ItemPriceHistoryEntryDto(log.Id, log.OccurredAt, log.UserId, oldCost, nextDefaultCost));
        }

        return Ok(history);
    }

    private async Task<string?> ValidateClassificationAsync(Guid? categoryId, Guid? subcategoryId, CancellationToken cancellationToken)
    {
        if (categoryId is null && subcategoryId is null)
        {
            return null;
        }

        if (subcategoryId is not null)
        {
            var subcategory = await dbContext.ItemSubcategories.AsNoTracking()
                .Where(x => x.Id == subcategoryId.Value)
                .Select(x => new { x.Id, x.CategoryId })
                .FirstOrDefaultAsync(cancellationToken);

            if (subcategory is null)
            {
                return "Selected subcategory does not exist.";
            }

            if (categoryId is null)
            {
                return "Category is required when subcategory is selected.";
            }

            if (subcategory.CategoryId != categoryId.Value)
            {
                return "Selected subcategory does not belong to the selected category.";
            }
        }

        if (categoryId is not null)
        {
            var categoryExists = await dbContext.ItemCategories.AsNoTracking()
                .AnyAsync(x => x.Id == categoryId.Value, cancellationToken);
            if (!categoryExists)
            {
                return "Selected category does not exist.";
            }
        }

        return null;
    }

    private static bool TryExtractDefaultCost(string changesJson, out decimal? oldValue, out decimal? newValue)
    {
        oldValue = null;
        newValue = null;

        try
        {
            using var doc = JsonDocument.Parse(changesJson);
            if (!doc.RootElement.TryGetProperty(nameof(Item.DefaultUnitCost), out var defaultCostChange))
            {
                return false;
            }

            if (defaultCostChange.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            oldValue = TryReadDecimalProperty(defaultCostChange, "old");
            newValue = TryReadDecimalProperty(defaultCostChange, "new");
            return newValue.HasValue;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static decimal? TryReadDecimalProperty(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    private static ItemAttachmentDto ToAttachmentDto(ItemAttachment attachment)
        => new(
            attachment.Id,
            attachment.ItemId,
            attachment.FileName,
            attachment.Url,
            attachment.IsImage,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.Notes,
            attachment.CreatedAt,
            attachment.CreatedBy);
}
