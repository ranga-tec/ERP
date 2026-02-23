using ISS.Domain.Common;

namespace ISS.Domain.Procurement;

public enum SupplierInvoiceStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class SupplierInvoice : AuditableEntity
{
    private SupplierInvoice() { }

    public SupplierInvoice(
        string number,
        Guid supplierId,
        string invoiceNumber,
        DateTimeOffset invoiceDate,
        DateTimeOffset? dueDate,
        Guid? purchaseOrderId,
        Guid? goodsReceiptId,
        Guid? directPurchaseId,
        decimal subtotal,
        decimal discountAmount,
        decimal taxAmount,
        decimal freightAmount,
        decimal roundingAmount,
        string? notes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        SupplierId = supplierId;
        InvoiceNumber = Guard.NotNullOrWhiteSpace(invoiceNumber, nameof(invoiceNumber), maxLength: 64);
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        PurchaseOrderId = purchaseOrderId;
        GoodsReceiptId = goodsReceiptId;
        DirectPurchaseId = directPurchaseId;
        Subtotal = Guard.NotNegative(subtotal, nameof(subtotal));
        DiscountAmount = Guard.NotNegative(discountAmount, nameof(discountAmount));
        TaxAmount = Guard.NotNegative(taxAmount, nameof(taxAmount));
        FreightAmount = Guard.NotNegative(freightAmount, nameof(freightAmount));
        RoundingAmount = roundingAmount;
        Notes = notes?.Trim();

        if (GrandTotal <= 0)
        {
            throw new DomainValidationException("Supplier invoice grand total must be positive.");
        }

        Status = SupplierInvoiceStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;
    public DateTimeOffset InvoiceDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public Guid? PurchaseOrderId { get; private set; }
    public Guid? GoodsReceiptId { get; private set; }
    public Guid? DirectPurchaseId { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal FreightAmount { get; private set; }
    public decimal RoundingAmount { get; private set; }
    public string? Notes { get; private set; }
    public SupplierInvoiceStatus Status { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public Guid? AccountsPayableEntryId { get; private set; }

    public decimal GrandTotal => Subtotal - DiscountAmount + TaxAmount + FreightAmount + RoundingAmount;

    public void Post(Guid? accountsPayableEntryId, DateTimeOffset postedAt)
    {
        if (Status != SupplierInvoiceStatus.Draft)
        {
            throw new DomainValidationException("Only draft supplier invoices can be posted.");
        }

        if (GrandTotal <= 0)
        {
            throw new DomainValidationException("Supplier invoice grand total must be positive.");
        }

        AccountsPayableEntryId = accountsPayableEntryId;
        PostedAt = postedAt;
        Status = SupplierInvoiceStatus.Posted;
    }

    public void Void()
    {
        if (Status == SupplierInvoiceStatus.Voided)
        {
            return;
        }

        if (Status != SupplierInvoiceStatus.Draft)
        {
            throw new DomainValidationException("Only draft supplier invoices can be voided.");
        }

        Status = SupplierInvoiceStatus.Voided;
    }
}
