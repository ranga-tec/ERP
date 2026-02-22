using ISS.Domain.Common;

namespace ISS.Domain.Sequences;

public sealed class DocumentSequence : Entity
{
    private DocumentSequence() { }

    public DocumentSequence(string documentType, string prefix, long nextNumber)
    {
        DocumentType = Guard.NotNullOrWhiteSpace(documentType, nameof(documentType), maxLength: 64);
        Prefix = Guard.NotNullOrWhiteSpace(prefix, nameof(prefix), maxLength: 16);
        NextNumber = nextNumber < 1 ? 1 : nextNumber;
    }

    public string DocumentType { get; private set; } = null!;
    public string Prefix { get; private set; } = null!;
    public long NextNumber { get; private set; }

    public string Peek() => $"{Prefix}{NextNumber:000000}";

    public string Next()
    {
        var current = Peek();
        NextNumber++;
        return current;
    }
}

