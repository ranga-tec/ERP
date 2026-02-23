namespace ISS.Application.Abstractions;

public enum PdfDocumentType
{
    RequestForQuote = 1,
    PurchaseOrder = 2,
    GoodsReceipt = 3,
    SupplierReturn = 4,
    SalesQuote = 5,
    SalesOrder = 6,
    DispatchNote = 7,
    SalesInvoice = 8,
    ServiceJob = 9,
    WorkOrder = 10,
    MaterialRequisition = 11,
    QualityCheck = 12,
    StockAdjustment = 13,
    StockTransfer = 14,
    Payment = 15,
    CreditNote = 16,
    DebitNote = 17,
    ItemLabel = 18,
    DirectPurchase = 19,
    SupplierInvoice = 20,
    DirectDispatch = 21,
    CustomerReturn = 22,
    ServiceEstimate = 23,
    ServiceHandover = 24
}

public sealed record PdfDocument(string FileName, byte[] Content, string ContentType = "application/pdf");

public interface IDocumentPdfService
{
    Task<PdfDocument> RenderAsync(PdfDocumentType documentType, Guid id, CancellationToken cancellationToken = default);
}
