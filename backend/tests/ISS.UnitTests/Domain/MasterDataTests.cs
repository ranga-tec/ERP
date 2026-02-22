using ISS.Domain.Common;
using ISS.Domain.MasterData;

namespace ISS.UnitTests.Domain;

public sealed class MasterDataTests
{
    [Fact]
    public void Brand_Requires_Code_And_Name()
    {
        Assert.Throws<DomainValidationException>(() => new Brand("", "X"));
        Assert.Throws<DomainValidationException>(() => new Brand("B", ""));
    }

    [Fact]
    public void Item_Can_Be_Created_And_Updated()
    {
        var item = new Item("SKU-1", "Bolt", ItemType.SparePart, TrackingType.None, "PCS", null, "123", 10m);
        item.Update("SKU-2", "Bolt M8", ItemType.SparePart, TrackingType.Batch, "PCS", null, "456", 12m, isActive: false);

        Assert.Equal("SKU-2", item.Sku);
        Assert.Equal("Bolt M8", item.Name);
        Assert.Equal(TrackingType.Batch, item.TrackingType);
        Assert.Equal("456", item.Barcode);
        Assert.False(item.IsActive);
    }
}

