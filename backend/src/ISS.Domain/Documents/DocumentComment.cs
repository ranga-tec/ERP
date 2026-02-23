using ISS.Domain.Common;

namespace ISS.Domain.Documents;

public sealed class DocumentComment : AuditableEntity
{
    private DocumentComment() { }

    public DocumentComment(string referenceType, Guid referenceId, string text)
    {
        ReferenceType = NormalizeReferenceType(referenceType);
        ReferenceId = referenceId;
        Text = Guard.NotNullOrWhiteSpace(text, nameof(Text), maxLength: 4000);
    }

    public string ReferenceType { get; private set; } = null!;
    public Guid ReferenceId { get; private set; }
    public string Text { get; private set; } = null!;

    public static string NormalizeReferenceType(string referenceType)
    {
        var normalized = Guard.NotNullOrWhiteSpace(referenceType, nameof(referenceType), maxLength: 64).ToUpperInvariant();
        foreach (var ch in normalized)
        {
            var ok = (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch is '-' or '_';
            if (!ok)
            {
                throw new DomainValidationException("ReferenceType may contain only A-Z, 0-9, '-' or '_'.");
            }
        }

        return normalized;
    }
}
