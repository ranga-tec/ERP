using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class ItemAttachment : AuditableEntity
{
    private ItemAttachment() { }

    public ItemAttachment(
        Guid itemId,
        string fileName,
        string url,
        bool isImage,
        string? contentType,
        long? sizeBytes,
        string? notes,
        string? storagePath = null)
    {
        ItemId = itemId;
        FileName = Guard.NotNullOrWhiteSpace(fileName, nameof(FileName), maxLength: 256);
        Url = Guard.NotNullOrWhiteSpace(url, nameof(Url), maxLength: 2000);
        IsImage = isImage;
        ContentType = string.IsNullOrWhiteSpace(contentType)
            ? null
            : Guard.NotNullOrWhiteSpace(contentType, nameof(ContentType), maxLength: 128);
        if (sizeBytes is < 0)
        {
            throw new DomainValidationException($"{nameof(SizeBytes)} cannot be negative.");
        }

        SizeBytes = sizeBytes;
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : Guard.NotNullOrWhiteSpace(notes, nameof(Notes), maxLength: 1000);
        StoragePath = string.IsNullOrWhiteSpace(storagePath)
            ? null
            : Guard.NotNullOrWhiteSpace(storagePath, nameof(StoragePath), maxLength: 2000);
    }

    public Guid ItemId { get; private set; }
    public Item? Item { get; private set; }
    public string FileName { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public bool IsImage { get; private set; }
    public string? ContentType { get; private set; }
    public long? SizeBytes { get; private set; }
    public string? Notes { get; private set; }
    public string? StoragePath { get; private set; }
}
