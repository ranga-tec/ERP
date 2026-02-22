using ISS.Domain.Common;
using ISS.Domain.Inventory;

namespace ISS.UnitTests.Domain;

public sealed class InventoryTests
{
    [Fact]
    public void StockAdjustment_Post_Requires_Lines()
    {
        var adj = new StockAdjustment("ADJ0001", Guid.NewGuid(), DateTimeOffset.UtcNow, reason: "Count");
        Assert.Throws<DomainValidationException>(() => adj.Post());

        adj.AddLine(Guid.NewGuid(), quantityDelta: 5m, unitCost: 1m, batchNumber: null);
        adj.Post();
        Assert.Equal(StockAdjustmentStatus.Posted, adj.Status);
        Assert.Throws<DomainValidationException>(() => adj.AddLine(Guid.NewGuid(), quantityDelta: 1m, unitCost: 1m, batchNumber: null));
    }

    [Fact]
    public void StockTransfer_From_And_To_Must_Differ()
    {
        var warehouse = Guid.NewGuid();
        Assert.Throws<DomainValidationException>(() => new StockTransfer("TRF0001", warehouse, warehouse, DateTimeOffset.UtcNow, notes: null));
    }

    [Fact]
    public void StockTransfer_Post_Requires_Lines()
    {
        var transfer = new StockTransfer("TRF0001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, notes: null);
        Assert.Throws<DomainValidationException>(() => transfer.Post());

        transfer.AddLine(Guid.NewGuid(), quantity: 2m, unitCost: 1m, batchNumber: null);
        transfer.Post();
        Assert.Equal(StockTransferStatus.Posted, transfer.Status);
    }
}

