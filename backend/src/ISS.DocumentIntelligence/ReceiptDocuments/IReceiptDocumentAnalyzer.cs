namespace ISS.DocumentIntelligence.ReceiptDocuments;

public interface IReceiptDocumentAnalyzer
{
    Task<ReceiptDocumentExtraction> AnalyzeAsync(ReceiptDocumentInput input, CancellationToken cancellationToken = default);
}
