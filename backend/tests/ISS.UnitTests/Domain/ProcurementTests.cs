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
    public void PurchaseOrder_Draft_Line_Can_Be_Updated_And_Removed()
    {
        var po = new PurchaseOrder("PO0002", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var line = po.AddLine(Guid.NewGuid(), 2m, 100m);

        po.UpdateLine(line.Id, 3m, 125m);
        Assert.Equal(3m, line.OrderedQuantity);
        Assert.Equal(125m, line.UnitPrice);

        po.RemoveLine(line.Id);
        Assert.Empty(po.Lines);
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
    public void GoodsReceipt_Draft_Line_Can_Be_Updated_And_Removed()
    {
        var grn = new GoodsReceipt("GRN0002", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var line = grn.AddLine(Guid.NewGuid(), 1m, 10m, batchNumber: "B1");
        line.AddSerial("S1");

        grn.UpdateLine(line.Id, 2m, 12m, "B2", new[] { "S2", "S3" });
        Assert.Equal(2m, line.Quantity);
        Assert.Equal(12m, line.UnitCost);
        Assert.Equal("B2", line.BatchNumber);
        Assert.Equal(new[] { "S2", "S3" }, line.Serials.Select(x => x.SerialNumber).ToArray());

        grn.RemoveLine(line.Id);
        Assert.Empty(grn.Lines);
    }

    [Fact]
    public void GoodsReceipt_Cannot_Edit_Lines_After_Post()
    {
        var grn = new GoodsReceipt("GRN0003", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var line = grn.AddLine(Guid.NewGuid(), 1m, 10m, batchNumber: "B1");
        grn.Post();

        Assert.Throws<DomainValidationException>(() => grn.UpdateLine(line.Id, 2m, 10m, "B2", null));
        Assert.Throws<DomainValidationException>(() => grn.RemoveLine(line.Id));
        Assert.Throws<DomainValidationException>(() => grn.AddLine(Guid.NewGuid(), 1m, 10m, batchNumber: "B3"));
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
