namespace ISS.DocumentIntelligence.ReceiptDocuments;

public interface IReceiptDocumentMatcher
{
    ReceiptDocumentSuggestion Match(ReceiptDocumentExtraction extraction, ReceiptDocumentContext context);
}
