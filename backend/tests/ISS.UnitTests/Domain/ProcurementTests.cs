using ISS.Domain.Common;
using ISS.Domain.Procurement;

namespace ISS.UnitTests.Domain;

public sealed class ProcurementTests
{
    [Fact]
    public void Rfq_Cannot_Be_Sent_Without_Lines()
    {
        var rfq = new RequestForQuote("RFQ0001", Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => rfq.MarkSent());
    }

    [Fact]
    public void PurchaseOrder_Approve_Requires_Lines()
    {
        var po = new PurchaseOrder("PO0001", Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => po.Approve());
        po.AddLine(Guid.NewGuid(), 2m, 100m);
        po.Approve();
        Assert.Equal(PurchaseOrderStatus.Approved, po.Status);
    }

    [Fact]
    public void GoodsReceipt_Post_Requires_Lines()
    {
        var grn = new GoodsReceipt("GRN0001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => grn.Post());
        grn.AddLine(Guid.NewGuid(), 1m, 10m, batchNumber: "B1");
        grn.Post();
        Assert.Equal(GoodsReceiptStatus.Posted, grn.Status);
    }

    [Fact]
    public void SupplierReturn_Post_Requires_Lines()
    {
        var sr = new SupplierReturn("SR0001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, reason: "Damaged");
        Assert.Throws<DomainValidationException>(() => sr.Post());
        sr.AddLine(Guid.NewGuid(), 1m, 10m, batchNumber: "B1");
        sr.Post();
        Assert.Equal(SupplierReturnStatus.Posted, sr.Status);
    }
}

