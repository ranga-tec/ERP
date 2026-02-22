using ISS.Domain.Common;
using ISS.Domain.Sales;

namespace ISS.UnitTests.Domain;

public sealed class SalesTests
{
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
}

