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
        var revenueAccountId = Guid.NewGuid();
        var expenseAccountId = Guid.NewGuid();

        var item = new Item(
            "SKU-1",
            "Bolt",
            ItemType.SparePart,
            TrackingType.None,
            "PCS",
            null,
            "123",
            10m,
            revenueAccountId: revenueAccountId,
            expenseAccountId: expenseAccountId);
        item.Update(
            "SKU-2",
            "Bolt M8",
            ItemType.SparePart,
            TrackingType.Batch,
            "PCS",
            null,
            "456",
            12m,
            isActive: false,
            revenueAccountId: revenueAccountId,
            expenseAccountId: expenseAccountId);

        Assert.Equal("SKU-2", item.Sku);
        Assert.Equal("Bolt M8", item.Name);
        Assert.Equal(TrackingType.Batch, item.TrackingType);
        Assert.Equal("456", item.Barcode);
        Assert.Equal(revenueAccountId, item.RevenueAccountId);
        Assert.Equal(expenseAccountId, item.ExpenseAccountId);
        Assert.False(item.IsActive);
    }

    [Fact]
    public void ItemCategory_Can_Store_Default_Accounts()
    {
        var revenueAccountId = Guid.NewGuid();
        var expenseAccountId = Guid.NewGuid();

        var category = new ItemCategory("CAT-1", "Parts", revenueAccountId, expenseAccountId);
        category.Update("CAT-2", "Service Parts", isActive: false, revenueAccountId, expenseAccountId);

        Assert.Equal("CAT-2", category.Code);
        Assert.Equal("Service Parts", category.Name);
        Assert.Equal(revenueAccountId, category.RevenueAccountId);
        Assert.Equal(expenseAccountId, category.ExpenseAccountId);
        Assert.False(category.IsActive);
    }
}
