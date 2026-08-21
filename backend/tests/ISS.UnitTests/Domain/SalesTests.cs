using ISS.Domain.Common;
using ISS.Domain.Sales;

namespace ISS.UnitTests.Domain;

public sealed class SalesTests
{
    [Fact]
    public void SalesInvoice_Line_Discount_Cannot_Exceed_One_Hundred_Percent()
    {
        var invoice = new SalesInvoice("INV-DISC", Guid.NewGuid(), DateTimeOffset.UtcNow, null);

        Assert.Throws<DomainValidationException>(() =>
            invoice.AddLine(Guid.NewGuid(), 1m, 100m, 100.0001m, 0m));

        var line = invoice.AddLine(Guid.NewGuid(), 1m, 100m, 10m, 0m);
        Assert.Throws<DomainValidationException>(() =>
            invoice.UpdateLine(line.Id, 1m, 100m, 100.0001m, 0m));
    }

    [Fact]
    public void Quote_Send_Requires_Lines()
    {
        var quote = new SalesQuote("SQ0001", Guid.NewGuid(), DateTimeOffset.UtcNow, validUntil: null);
        Assert.Throws<DomainValidationException>(() => quote.MarkSent());
        quote.AddLine(Guid.NewGuid(), 1m, 100m);
        quote.MarkSent();
        Assert.Equal(SalesQuoteStatus.Sent, quote.Status);
    }

    [Fact]
    public void Order_Confirm_Requires_Lines()
    {
        var order = new SalesOrder("SO0001", Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => order.Confirm());
        order.AddLine(Guid.NewGuid(), 2m, 10m);
        order.Confirm();
        Assert.Equal(SalesOrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Invoice_Totals_Are_Calculated()
    {
        var invoice = new SalesInvoice("INV0001", Guid.NewGuid(), DateTimeOffset.UtcNow, dueDate: null);
        invoice.AddLine(Guid.NewGuid(), quantity: 2m, unitPrice: 100m, discountPercent: 10m, taxPercent: 15m);

        Assert.Equal(180m, invoice.Subtotal); // 200 - 10%
        Assert.Equal(27m, invoice.TaxTotal);  // 15% of 180
        Assert.Equal(207m, invoice.Total);
    }

    [Fact]
    public void Invoice_Line_Can_Store_Revenue_Account()
    {
        var revenueAccountId = Guid.NewGuid();
        var invoice = new SalesInvoice("INV0002", Guid.NewGuid(), DateTimeOffset.UtcNow, dueDate: null);
        var line = invoice.AddLine(Guid.NewGuid(), quantity: 1m, unitPrice: 100m, discountPercent: 0m, taxPercent: 0m, revenueAccountId: revenueAccountId);

        invoice.UpdateLine(line.Id, quantity: 2m, unitPrice: 120m, discountPercent: 0m, taxPercent: 0m, revenueAccountId: revenueAccountId);

        Assert.Equal(revenueAccountId, line.RevenueAccountId);
    }

    [Fact]
    public void Header_Discount_Is_Prorated_So_Each_Line_Is_Taxed_On_What_Is_Paid()
    {
        var invoice = new SalesInvoice("INV0003", Guid.NewGuid(), DateTimeOffset.UtcNow, dueDate: null);
        invoice.AddLine(Guid.NewGuid(), quantity: 1m, unitPrice: 1000m, discountPercent: 0m, taxPercent: 15m);
        invoice.AddLine(Guid.NewGuid(), quantity: 1m, unitPrice: 1000m, discountPercent: 0m, taxPercent: 0m);

        invoice.SetHeaderDiscount(discountPercent: 20m, discountAmount: 0m);

        Assert.Equal(2000m, invoice.LinesSubtotal);
        Assert.Equal(400m, invoice.DiscountTotal);
        Assert.Equal(1600m, invoice.Subtotal);
        // half the discount lands on each line, so the taxed line is charged 15% of 800, not 1000
        Assert.Equal(120m, invoice.TaxTotal);
        Assert.Equal(1720m, invoice.Total);
    }

    [Fact]
    public void Header_Discount_Allocation_Adds_Up_Exactly_Despite_Rounding()
    {
        var invoice = new SalesInvoice("INV0004", Guid.NewGuid(), DateTimeOffset.UtcNow, dueDate: null);
        // three equal lines against a discount that does not divide evenly into thirds
        invoice.AddLine(Guid.NewGuid(), quantity: 1m, unitPrice: 10m, discountPercent: 0m, taxPercent: 0m);
        invoice.AddLine(Guid.NewGuid(), quantity: 1m, unitPrice: 10m, discountPercent: 0m, taxPercent: 0m);
        invoice.AddLine(Guid.NewGuid(), quantity: 1m, unitPrice: 10m, discountPercent: 0m, taxPercent: 0m);

        invoice.SetHeaderDiscount(discountPercent: 0m, discountAmount: 10m);

        var allocation = invoice.AllocateDiscount();
        Assert.Equal(10m, allocation.Values.Sum());
        Assert.Equal(20m, invoice.Subtotal);
        Assert.Equal(20m, invoice.Total);
    }

    [Fact]
    public void Header_Discount_Amount_Cannot_Push_The_Invoice_Negative()
    {
        var invoice = new SalesInvoice("INV0005", Guid.NewGuid(), DateTimeOffset.UtcNow, dueDate: null);
        invoice.AddLine(Guid.NewGuid(), quantity: 1m, unitPrice: 100m, discountPercent: 0m, taxPercent: 0m);

        invoice.SetHeaderDiscount(discountPercent: 0m, discountAmount: 500m);

        Assert.Equal(100m, invoice.DiscountTotal);
        Assert.Equal(0m, invoice.Subtotal);
        Assert.Equal(0m, invoice.Total);
    }

    [Fact]
    public void Header_Discount_Rejects_Percent_And_Amount_Together()
    {
        var invoice = new SalesInvoice("INV0006", Guid.NewGuid(), DateTimeOffset.UtcNow, dueDate: null);
        invoice.AddLine(Guid.NewGuid(), quantity: 1m, unitPrice: 100m, discountPercent: 0m, taxPercent: 0m);

        Assert.Throws<DomainValidationException>(() => invoice.SetHeaderDiscount(10m, 10m));
        Assert.Throws<DomainValidationException>(() => invoice.SetHeaderDiscount(101m, 0m));
    }

    [Fact]
    public void Invoice_Without_Discount_Totals_Exactly_As_Before()
    {
        var invoice = new SalesInvoice("INV0007", Guid.NewGuid(), DateTimeOffset.UtcNow, dueDate: null);
        invoice.AddLine(Guid.NewGuid(), quantity: 2m, unitPrice: 100m, discountPercent: 10m, taxPercent: 15m);

        Assert.Equal(0m, invoice.DiscountTotal);
        Assert.Equal(180m, invoice.Subtotal);
        Assert.Equal(27m, invoice.TaxTotal);
        Assert.Equal(207m, invoice.Total);
    }
}
