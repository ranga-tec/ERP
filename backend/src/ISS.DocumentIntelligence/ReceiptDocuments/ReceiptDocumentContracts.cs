namespace ISS.DocumentIntelligence.ReceiptDocuments;

public sealed record ReceiptDocumentInput(
    string FileName,
    string ContentType,
    Stream Content,
    string? UserText);

public sealed record ReceiptDocumentExtraction(
    string? SupplierName,
    string? PurchaseOrderNumber,
    string? DocumentNumber,
    DateOnly? DocumentDate,
    IReadOnlyList<ReceiptDocumentLineExtraction> Lines,
    string? RawText,
    IReadOnlyList<string> Warnings);

public sealed record ReceiptDocumentLineExtraction(
    int LineNumber,
    string? ItemCode,
    string? Description,
    decimal? Quantity,
    decimal? UnitCost,
    string? BatchNumber,
    IReadOnlyList<string> Serials);

public sealed record ReceiptDocumentContext(
    IReadOnlyList<ReceiptPurchaseOrderReference> PurchaseOrders,
    IReadOnlyList<ReceiptSupplierReference> Suppliers,
    IReadOnlyList<ReceiptPurchaseOrderLineReference> PurchaseOrderLines);

public sealed record ReceiptPurchaseOrderReference(
    Guid Id,
    string Number,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName);

public sealed record ReceiptSupplierReference(Guid Id, string Code, string Name);

public sealed record ReceiptPurchaseOrderLineReference(
    Guid PurchaseOrderLineId,
    Guid ItemId,
    string ItemSku,
    string ItemName,
    decimal OrderedQuantity,
    decimal PreviouslyReceivedQuantity,
    decimal ReservedInOtherDraftsQuantity,
    decimal AvailableQuantity,
    decimal UnitCost);

public sealed record ReceiptDocumentSuggestion(
    ReceiptDocumentExtraction Extraction,
    ReceiptMatchedPurchaseOrder? PurchaseOrder,
    ReceiptMatchedSupplier? Supplier,
    IReadOnlyList<ReceiptDocumentLineSuggestion> Lines,
    IReadOnlyList<string> Warnings);

public sealed record ReceiptMatchedPurchaseOrder(Guid Id, string Number, decimal Confidence, string Reason);

public sealed record ReceiptMatchedSupplier(Guid Id, string Code, string Name, decimal Confidence, string Reason);

public sealed record ReceiptDocumentLineSuggestion(
    int SourceLineNumber,
    string? ExtractedItemCode,
    string? ExtractedDescription,
    decimal? ExtractedQuantity,
    decimal? ExtractedUnitCost,
    Guid? PurchaseOrderLineId,
    Guid? ItemId,
    string? ItemSku,
    string? ItemName,
    decimal SuggestedQuantity,
    decimal? SuggestedUnitCost,
    decimal Confidence,
    string Status,
    string Reason);
