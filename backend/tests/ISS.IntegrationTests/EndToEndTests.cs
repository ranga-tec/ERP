using ClosedXML.Excel;
using ISS.IntegrationTests.Fixtures;
using ISS.Domain.Finance;
using ISS.Domain.Inventory;
using ISS.Domain.MasterData;
using ISS.Domain.Notifications;
using ISS.Domain.Procurement;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace ISS.IntegrationTests;

public sealed class EndToEndTests(IssApiFixture fixture) : IClassFixture<IssApiFixture>
{
    private readonly HttpClient _client = fixture.Client;

    private static string Code(string prefix, int suffixChars = 8)
        => $"{prefix}{Guid.NewGuid():N}"[..Math.Min(prefix.Length + suffixChars, 32)];

    private sealed record BrandDto(Guid Id, string Code, string Name, bool IsActive);
    private sealed record WarehouseDto(Guid Id, string Code, string Name, string? Address, bool IsActive);
    private sealed record SupplierDto(Guid Id, string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive);
    private sealed record CustomerDto(Guid Id, string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive);
    private sealed record ItemDto(Guid Id, string Sku, string Name, ItemType Type, TrackingType TrackingType, string UnitOfMeasure, Guid? BrandId, string? Barcode, decimal DefaultUnitCost, bool IsActive);

    [Fact]
    public async Task MasterData_Can_Create_Core_Entities()
    {
        var brand = await Post<BrandDto>("/api/brands", new { code = Code("BR"), name = "Bobcat" });
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = "Nugegoda" });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Parts Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Hydraulic Filter",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = brand.Id,
            barcode = (string?)null,
            defaultUnitCost = 10m
        });

        Assert.NotEqual(Guid.Empty, brand.Id);
        Assert.NotEqual(Guid.Empty, warehouse.Id);
        Assert.NotEqual(Guid.Empty, supplier.Id);
        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public async Task Procurement_GRN_Post_Increases_Stock_And_Creates_AP()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = "Nugegoda" });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Oil",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "L",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 5m
        });

        var po = await Post<PurchaseOrderDto>("/api/procurement/purchase-orders", new { supplierId = supplier.Id });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/lines", new { itemId = item.Id, quantity = 10m, unitPrice = 5m });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/approve", new { });

        var grn = await Post<GoodsReceiptDto>("/api/procurement/goods-receipts", new { purchaseOrderId = po.Id, warehouseId = warehouse.Id });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/lines", new { itemId = item.Id, quantity = 10m, unitCost = 5m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/post", new { });

        var onHand = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={warehouse.Id}&itemId={item.Id}");
        Assert.Equal(10m, onHand.OnHand);

        var apEntries = await Get<List<ApDto>>("/api/finance/ap?outstandingOnly=true");
        Assert.Contains(apEntries, e => e.ReferenceType == "GRN" && e.ReferenceId == grn.Id && e.Outstanding == 50m);
    }

    [Fact]
    public async Task Procurement_Rfq_Can_Be_Created_Lined_And_Sent()
    {
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "O-Ring",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 1m
        });

        var rfq = await Post<RfqDto>("/api/procurement/rfqs", new { supplierId = supplier.Id });
        await PostNoContent($"/api/procurement/rfqs/{rfq.Id}/lines", new { itemId = item.Id, quantity = 2m, notes = "Need quote" });
        await PostNoContent($"/api/procurement/rfqs/{rfq.Id}/send", new { });

        var updated = await Get<RfqDto>($"/api/procurement/rfqs/{rfq.Id}");
        Assert.Equal(RequestForQuoteStatus.Sent, updated.Status);
        var line = Assert.Single(updated.Lines);
        Assert.Equal(item.Id, line.ItemId);
        Assert.Equal(2m, line.Quantity);
    }

    [Fact]
    public async Task Procurement_SupplierReturn_Post_Reduces_Stock_And_Creates_AP_Credit()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Oil Filter",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 5m
        });

        // Seed stock via GRN
        var po = await Post<PurchaseOrderDto>("/api/procurement/purchase-orders", new { supplierId = supplier.Id });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/lines", new { itemId = item.Id, quantity = 10m, unitPrice = 5m });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/approve", new { });
        var grn = await Post<GoodsReceiptDto>("/api/procurement/goods-receipts", new { purchaseOrderId = po.Id, warehouseId = warehouse.Id });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/lines", new { itemId = item.Id, quantity = 10m, unitCost = 5m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/post", new { });

        var sr = await Post<SupplierReturnDto>("/api/procurement/supplier-returns", new { supplierId = supplier.Id, warehouseId = warehouse.Id, reason = "Damaged" });
        await PostNoContent($"/api/procurement/supplier-returns/{sr.Id}/lines", new { itemId = item.Id, quantity = 3m, unitCost = 5m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/procurement/supplier-returns/{sr.Id}/post", new { });

        var onHand = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={warehouse.Id}&itemId={item.Id}");
        Assert.Equal(7m, onHand.OnHand);

        var apEntries = await Get<List<ApDto>>("/api/finance/ap?outstandingOnly=false");
        Assert.Contains(apEntries, e => e.ReferenceType == "GRN" && e.ReferenceId == grn.Id && e.Amount == 50m && e.Outstanding == 35m);
        Assert.DoesNotContain(apEntries, e => e.ReferenceType == "SR" && e.ReferenceId == sr.Id);

        var creditNotes = await Get<List<CreditNoteDto>>($"/api/finance/credit-notes?counterpartyType=2&counterpartyId={supplier.Id}");
        Assert.Contains(creditNotes, cn => cn.SourceReferenceType == "SR" && cn.SourceReferenceId == sr.Id && cn.Amount == 15m && cn.RemainingAmount == 0m);
    }

    [Fact]
    public async Task Procurement_DirectPurchase_And_SupplierInvoice_Post_Stock_And_AP()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Bearing",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 10m
        });

        var dp = await Post<DirectPurchaseApiDto>("/api/procurement/direct-purchases", new
        {
            supplierId = supplier.Id,
            warehouseId = warehouse.Id,
            purchasedAt = (DateTimeOffset?)null,
            remarks = "Emergency local buy"
        });
        await PostNoContent($"/api/procurement/direct-purchases/{dp.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 5m,
            unitPrice = 10m,
            taxPercent = 0m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/procurement/direct-purchases/{dp.Id}/post", new { });

        var postedDp = await Get<DirectPurchaseApiDto>($"/api/procurement/direct-purchases/{dp.Id}");
        Assert.Equal(DirectPurchaseStatus.Posted, postedDp.Status);
        Assert.Equal(50m, postedDp.GrandTotal);
        Assert.Single(postedDp.Lines);

        var onHand = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={warehouse.Id}&itemId={item.Id}");
        Assert.Equal(5m, onHand.OnHand);

        var supplierInvoice = await Post<SupplierInvoiceApiDto>("/api/procurement/supplier-invoices", new
        {
            supplierId = supplier.Id,
            invoiceNumber = $"BILL-{Guid.NewGuid():N}"[..16],
            invoiceDate = DateTimeOffset.UtcNow,
            dueDate = (DateTimeOffset?)null,
            purchaseOrderId = (Guid?)null,
            goodsReceiptId = (Guid?)null,
            directPurchaseId = dp.Id,
            subtotal = 50m,
            discountAmount = 0m,
            taxAmount = 0m,
            freightAmount = 0m,
            roundingAmount = 0m,
            notes = "Supplier bill for direct purchase"
        });
        await PostNoContent($"/api/procurement/supplier-invoices/{supplierInvoice.Id}/post", new { });

        var postedInvoice = await Get<SupplierInvoiceApiDto>($"/api/procurement/supplier-invoices/{supplierInvoice.Id}");
        Assert.Equal(SupplierInvoiceStatus.Posted, postedInvoice.Status);
        Assert.Equal(50m, postedInvoice.GrandTotal);
        Assert.NotNull(postedInvoice.AccountsPayableEntryId);

        var apEntries = await Get<List<ApDto>>("/api/finance/ap?outstandingOnly=false");
        Assert.Contains(apEntries, e => e.ReferenceType == "SINV" && e.ReferenceId == supplierInvoice.Id && e.Outstanding == 50m);

        await AssertPdfOkAsync($"/api/procurement/direct-purchases/{dp.Id}/pdf");
        await AssertPdfOkAsync($"/api/procurement/supplier-invoices/{supplierInvoice.Id}/pdf");
    }

    [Fact]
    public async Task Procurement_SupplierInvoice_Create_With_Mixed_DirectPurchase_And_PoRefs_Returns_BadRequest()
    {
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });

        var resp = await _client.PostAsJsonAsync("/api/procurement/supplier-invoices", new
        {
            supplierId = supplier.Id,
            invoiceNumber = $"BILL-{Guid.NewGuid():N}"[..16],
            invoiceDate = DateTimeOffset.UtcNow,
            dueDate = (DateTimeOffset?)null,
            purchaseOrderId = Guid.NewGuid(),
            goodsReceiptId = (Guid?)null,
            directPurchaseId = Guid.NewGuid(),
            subtotal = 100m,
            discountAmount = 0m,
            taxAmount = 0m,
            freightAmount = 0m,
            roundingAmount = 0m,
            notes = "Invalid mixed refs"
        });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("cannot combine direct purchase and PO/GRN references", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Procurement_SupplierInvoice_Post_With_Unposted_DirectPurchase_Returns_BadRequest()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Filter Mesh",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 10m
        });

        var dp = await Post<DirectPurchaseApiDto>("/api/procurement/direct-purchases", new
        {
            supplierId = supplier.Id,
            warehouseId = warehouse.Id,
            purchasedAt = (DateTimeOffset?)null,
            remarks = "Draft DP"
        });
        await PostNoContent($"/api/procurement/direct-purchases/{dp.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 2m,
            unitPrice = 10m,
            taxPercent = 0m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });

        var supplierInvoice = await Post<SupplierInvoiceApiDto>("/api/procurement/supplier-invoices", new
        {
            supplierId = supplier.Id,
            invoiceNumber = $"BILL-{Guid.NewGuid():N}"[..16],
            invoiceDate = DateTimeOffset.UtcNow,
            dueDate = (DateTimeOffset?)null,
            purchaseOrderId = (Guid?)null,
            goodsReceiptId = (Guid?)null,
            directPurchaseId = dp.Id,
            subtotal = 20m,
            discountAmount = 0m,
            taxAmount = 0m,
            freightAmount = 0m,
            roundingAmount = 0m,
            notes = "Should fail until DP posted"
        });

        var resp = await _client.PostAsJsonAsync($"/api/procurement/supplier-invoices/{supplierInvoice.Id}/post", new { });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("direct purchase must be posted", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Procurement_SupplierInvoice_Post_With_Grn_Ap_Variance_Returns_BadRequest()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Pump",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 10m
        });

        var po = await Post<PurchaseOrderDto>("/api/procurement/purchase-orders", new { supplierId = supplier.Id });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/lines", new { itemId = item.Id, quantity = 5m, unitPrice = 10m });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/approve", new { });

        var grn = await Post<GoodsReceiptDto>("/api/procurement/goods-receipts", new { purchaseOrderId = po.Id, warehouseId = warehouse.Id });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/lines", new { itemId = item.Id, quantity = 5m, unitCost = 10m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/post", new { });

        var supplierInvoice = await Post<SupplierInvoiceApiDto>("/api/procurement/supplier-invoices", new
        {
            supplierId = supplier.Id,
            invoiceNumber = $"BILL-{Guid.NewGuid():N}"[..16],
            invoiceDate = DateTimeOffset.UtcNow,
            dueDate = (DateTimeOffset?)null,
            purchaseOrderId = po.Id,
            goodsReceiptId = grn.Id,
            directPurchaseId = (Guid?)null,
            subtotal = 55m,
            discountAmount = 0m,
            taxAmount = 0m,
            freightAmount = 0m,
            roundingAmount = 0m,
            notes = "GRN AP variance test"
        });

        var resp = await _client.PostAsJsonAsync($"/api/procurement/supplier-invoices/{supplierInvoice.Id}/post", new { });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("Use debit/credit notes", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Procurement_SupplierInvoice_Post_Twice_Returns_BadRequest()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Coupling",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 10m
        });

        var dp = await Post<DirectPurchaseApiDto>("/api/procurement/direct-purchases", new
        {
            supplierId = supplier.Id,
            warehouseId = warehouse.Id,
            purchasedAt = (DateTimeOffset?)null,
            remarks = "Duplicate post test"
        });
        await PostNoContent($"/api/procurement/direct-purchases/{dp.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 2m,
            unitPrice = 10m,
            taxPercent = 0m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/procurement/direct-purchases/{dp.Id}/post", new { });

        var supplierInvoice = await Post<SupplierInvoiceApiDto>("/api/procurement/supplier-invoices", new
        {
            supplierId = supplier.Id,
            invoiceNumber = $"BILL-{Guid.NewGuid():N}"[..16],
            invoiceDate = DateTimeOffset.UtcNow,
            dueDate = (DateTimeOffset?)null,
            purchaseOrderId = (Guid?)null,
            goodsReceiptId = (Guid?)null,
            directPurchaseId = dp.Id,
            subtotal = 20m,
            discountAmount = 0m,
            taxAmount = 0m,
            freightAmount = 0m,
            roundingAmount = 0m,
            notes = "Duplicate post test"
        });
        await PostNoContent($"/api/procurement/supplier-invoices/{supplierInvoice.Id}/post", new { });

        var resp = await _client.PostAsJsonAsync($"/api/procurement/supplier-invoices/{supplierInvoice.Id}/post", new { });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("draft", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Procurement_DirectPurchase_Post_Twice_Returns_BadRequest()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Seal Kit",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 10m
        });

        var dp = await Post<DirectPurchaseApiDto>("/api/procurement/direct-purchases", new
        {
            supplierId = supplier.Id,
            warehouseId = warehouse.Id,
            purchasedAt = (DateTimeOffset?)null,
            remarks = "Retry post test"
        });
        await PostNoContent($"/api/procurement/direct-purchases/{dp.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 1m,
            unitPrice = 10m,
            taxPercent = 0m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/procurement/direct-purchases/{dp.Id}/post", new { });

        var resp = await _client.PostAsJsonAsync($"/api/procurement/direct-purchases/{dp.Id}/post", new { });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("draft", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pdf_Export_Endpoints_Return_Pdf_Content()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = "supplier@example.test", address = (string?)null });
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = "customer@example.test", address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Oil Filter",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = "ITEM-0001",
            defaultUnitCost = 5m
        });

        var po = await Post<PurchaseOrderDto>("/api/procurement/purchase-orders", new { supplierId = supplier.Id });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/lines", new { itemId = item.Id, quantity = 10m, unitPrice = 5m });
        await AssertPdfOkAsync($"/api/procurement/purchase-orders/{po.Id}/pdf");

        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/approve", new { });
        var grn = await Post<GoodsReceiptDto>("/api/procurement/goods-receipts", new { purchaseOrderId = po.Id, warehouseId = warehouse.Id });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/lines", new { itemId = item.Id, quantity = 10m, unitCost = 5m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/post", new { });
        await AssertPdfOkAsync($"/api/procurement/goods-receipts/{grn.Id}/pdf");

        var sr = await Post<SupplierReturnDto>("/api/procurement/supplier-returns", new { supplierId = supplier.Id, warehouseId = warehouse.Id, reason = "Damaged" });
        await PostNoContent($"/api/procurement/supplier-returns/{sr.Id}/lines", new { itemId = item.Id, quantity = 3m, unitCost = 5m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/procurement/supplier-returns/{sr.Id}/post", new { });
        await AssertPdfOkAsync($"/api/procurement/supplier-returns/{sr.Id}/pdf");

        var creditNotes = await Get<List<CreditNoteDto>>($"/api/finance/credit-notes?counterpartyType=2&counterpartyId={supplier.Id}");
        var cn = Assert.Single(creditNotes.Where(x => x.SourceReferenceType == "SR" && x.SourceReferenceId == sr.Id));
        await AssertPdfOkAsync($"/api/finance/credit-notes/{cn.Id}/pdf");

        var invoice = await Post<InvoiceDto>("/api/sales/invoices", new { customerId = customer.Id, dueDate = (DateTimeOffset?)null });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 2m,
            unitPrice = 10m,
            discountPercent = 0m,
            taxPercent = 0m
        });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/post", new { });
        await AssertPdfOkAsync($"/api/sales/invoices/{invoice.Id}/pdf");
        var postedInvoice = await Get<InvoiceDto>($"/api/sales/invoices/{invoice.Id}");

        var arEntries = await Get<List<ArDto>>("/api/finance/ar?outstandingOnly=false");
        var invoiceAr = Assert.Single(arEntries.Where(e => e.ReferenceType == "INV" && e.ReferenceId == invoice.Id));

        var payment = await Post<PaymentDto>("/api/finance/payments", new
        {
            direction = PaymentDirection.Incoming,
            counterpartyType = CounterpartyType.Customer,
            counterpartyId = customer.Id,
            amount = postedInvoice.Total,
            notes = (string?)null
        });
        await PostNoContent($"/api/finance/payments/{payment.Id}/allocate/ar", new { entryId = invoiceAr.Id, amount = postedInvoice.Total });
        await AssertPdfOkAsync($"/api/finance/payments/{payment.Id}/pdf");

        await AssertPdfOkAsync($"/api/items/{item.Id}/label/pdf");
    }

    [Fact]
    public async Task Admin_Excel_Import_Can_Create_Master_Data()
    {
        using var wb = new XLWorkbook();

        var brands = wb.AddWorksheet("Brands");
        brands.Cell(1, 1).Value = "Code";
        brands.Cell(1, 2).Value = "Name";
        brands.Cell(1, 3).Value = "IsActive";
        brands.Cell(2, 1).Value = "BR01";
        brands.Cell(2, 2).Value = "Bobcat";
        brands.Cell(2, 3).Value = true;

        var warehouses = wb.AddWorksheet("Warehouses");
        warehouses.Cell(1, 1).Value = "Code";
        warehouses.Cell(1, 2).Value = "Name";
        warehouses.Cell(1, 3).Value = "Address";
        warehouses.Cell(1, 4).Value = "IsActive";
        warehouses.Cell(2, 1).Value = "WH01";
        warehouses.Cell(2, 2).Value = "Main";
        warehouses.Cell(2, 3).Value = "Nugegoda";
        warehouses.Cell(2, 4).Value = true;

        var suppliers = wb.AddWorksheet("Suppliers");
        suppliers.Cell(1, 1).Value = "Code";
        suppliers.Cell(1, 2).Value = "Name";
        suppliers.Cell(1, 3).Value = "Phone";
        suppliers.Cell(1, 4).Value = "Email";
        suppliers.Cell(1, 5).Value = "Address";
        suppliers.Cell(1, 6).Value = "IsActive";
        suppliers.Cell(2, 1).Value = "SUP01";
        suppliers.Cell(2, 2).Value = "Supplier";
        suppliers.Cell(2, 3).Value = "123";
        suppliers.Cell(2, 4).Value = "supplier@example.test";
        suppliers.Cell(2, 5).Value = "Colombo";
        suppliers.Cell(2, 6).Value = true;

        var customers = wb.AddWorksheet("Customers");
        customers.Cell(1, 1).Value = "Code";
        customers.Cell(1, 2).Value = "Name";
        customers.Cell(1, 3).Value = "Phone";
        customers.Cell(1, 4).Value = "Email";
        customers.Cell(1, 5).Value = "Address";
        customers.Cell(1, 6).Value = "IsActive";
        customers.Cell(2, 1).Value = "CUS01";
        customers.Cell(2, 2).Value = "Customer";
        customers.Cell(2, 3).Value = "555";
        customers.Cell(2, 4).Value = "customer@example.test";
        customers.Cell(2, 5).Value = "Galle";
        customers.Cell(2, 6).Value = true;

        var items = wb.AddWorksheet("Items");
        items.Cell(1, 1).Value = "Sku";
        items.Cell(1, 2).Value = "Name";
        items.Cell(1, 3).Value = "Type";
        items.Cell(1, 4).Value = "TrackingType";
        items.Cell(1, 5).Value = "UnitOfMeasure";
        items.Cell(1, 6).Value = "BrandCode";
        items.Cell(1, 7).Value = "Barcode";
        items.Cell(1, 8).Value = "DefaultUnitCost";
        items.Cell(1, 9).Value = "IsActive";

        items.Cell(2, 1).Value = "SKU01";
        items.Cell(2, 2).Value = "Bolt";
        items.Cell(2, 3).Value = "SparePart";
        items.Cell(2, 4).Value = "None";
        items.Cell(2, 5).Value = "PCS";
        items.Cell(2, 6).Value = "BR01";
        items.Cell(2, 7).Value = "B001";
        items.Cell(2, 8).Value = 2.5;
        items.Cell(2, 9).Value = true;

        items.Cell(3, 1).Value = "EQ01";
        items.Cell(3, 2).Value = "Excavator";
        items.Cell(3, 3).Value = "Equipment";
        items.Cell(3, 4).Value = "Serial";
        items.Cell(3, 5).Value = "UNIT";
        items.Cell(3, 6).Value = "BR01";
        items.Cell(3, 7).Value = "EQB001";
        items.Cell(3, 8).Value = 0;
        items.Cell(3, 9).Value = true;

        var reorder = wb.AddWorksheet("ReorderSettings");
        reorder.Cell(1, 1).Value = "WarehouseCode";
        reorder.Cell(1, 2).Value = "ItemSku";
        reorder.Cell(1, 3).Value = "ReorderPoint";
        reorder.Cell(1, 4).Value = "ReorderQuantity";
        reorder.Cell(2, 1).Value = "WH01";
        reorder.Cell(2, 2).Value = "SKU01";
        reorder.Cell(2, 3).Value = 5;
        reorder.Cell(2, 4).Value = 10;

        var equipment = wb.AddWorksheet("EquipmentUnits");
        equipment.Cell(1, 1).Value = "ItemSku";
        equipment.Cell(1, 2).Value = "SerialNumber";
        equipment.Cell(1, 3).Value = "CustomerCode";
        equipment.Cell(1, 4).Value = "PurchasedAt";
        equipment.Cell(1, 5).Value = "WarrantyUntil";
        equipment.Cell(2, 1).Value = "EQ01";
        equipment.Cell(2, 2).Value = "SN001";
        equipment.Cell(2, 3).Value = "CUS01";
        equipment.Cell(2, 4).Value = DateTimeOffset.UtcNow.Date;
        equipment.Cell(2, 5).Value = DateTimeOffset.UtcNow.Date.AddDays(365);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(fileContent, "file", "import.xlsx");

        var resp = await _client.PostAsync("/api/admin/import/excel", form);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"POST /api/admin/import/excel failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {text}");
        }

        var imported = await resp.Content.ReadFromJsonAsync<ImportResultDto>();
        Assert.NotNull(imported);
        Assert.True(imported!.BrandsCreated >= 1);
        Assert.True(imported.WarehousesCreated >= 1);
        Assert.True(imported.ItemsCreated >= 2);
        Assert.True(imported.ReorderSettingsCreated >= 1);
        Assert.True(imported.EquipmentUnitsCreated >= 1);

        var brandsList = await Get<List<BrandDto>>("/api/brands");
        Assert.Contains(brandsList, b => b.Code == "BR01");

        var whList = await Get<List<WarehouseDto>>("/api/warehouses");
        Assert.Contains(whList, w => w.Code == "WH01");

        var itemList = await Get<List<ItemDto>>("/api/items");
        Assert.Contains(itemList, i => i.Sku == "SKU01" && i.Barcode == "B001");
        Assert.Contains(itemList, i => i.Sku == "EQ01");

        var rsList = await Get<List<ReorderSettingDto>>("/api/reorder-settings");
        var wh = Assert.Single(whList.Where(w => w.Code == "WH01"));
        var bolt = Assert.Single(itemList.Where(i => i.Sku == "SKU01"));
        Assert.Contains(rsList, r => r.WarehouseId == wh.Id && r.ItemId == bolt.Id && r.ReorderPoint == 5m && r.ReorderQuantity == 10m);

        var units = await Get<List<EquipmentUnitDto>>("/api/service/equipment-units");
        Assert.Contains(units, u => u.SerialNumber == "SN001");
    }

    [Fact]
    public async Task Notifications_Are_Queued_On_Key_Events()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "+15550001", email = "supplier@example.test", address = (string?)null });
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "+15550002", email = "customer@example.test", address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Bolt",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 1m
        });

        var po = await Post<PurchaseOrderDto>("/api/procurement/purchase-orders", new { supplierId = supplier.Id });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/lines", new { itemId = item.Id, quantity = 1m, unitPrice = 1m });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/approve", new { });

        var afterPo = await Get<List<NotificationOutboxDto>>("/api/admin/notifications?take=200");
        var poNotifications = afterPo.Where(n => n.ReferenceType == "PO" && n.ReferenceId == po.Id).ToList();
        Assert.Contains(poNotifications, n => n.Channel == NotificationChannel.Email && n.Recipient == supplier.Email);
        Assert.Contains(poNotifications, n => n.Channel == NotificationChannel.Sms && n.Recipient == supplier.Phone);

        var invoice = await Post<InvoiceDto>("/api/sales/invoices", new { customerId = customer.Id, dueDate = (DateTimeOffset?)null });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/lines", new { itemId = item.Id, quantity = 1m, unitPrice = 10m, discountPercent = 0m, taxPercent = 0m });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/post", new { });

        var afterInvoice = await Get<List<NotificationOutboxDto>>("/api/admin/notifications?take=200");
        var invNotifications = afterInvoice.Where(n => n.ReferenceType == "INV" && n.ReferenceId == invoice.Id).ToList();
        Assert.Contains(invNotifications, n => n.Channel == NotificationChannel.Email && n.Recipient == customer.Email);
        Assert.Contains(invNotifications, n => n.Channel == NotificationChannel.Sms && n.Recipient == customer.Phone);
    }

    [Fact]
    public async Task Admin_User_Management_Endpoints_Work()
    {
        var email = $"{Guid.NewGuid():N}@local";
        var password = "Passw0rd1";

        var created = await Post<AdminUserDto>("/api/admin/users", new
        {
            email,
            password,
            displayName = "Test User",
            roles = new[] { "Procurement" }
        });

        Assert.Equal(email, created.Email);
        Assert.Contains("Procurement", created.Roles);

        var login = await Post<AuthDto>("/api/auth/login", new { email, password });
        Assert.False(string.IsNullOrWhiteSpace(login.Token));

        var adminAuth = _client.DefaultRequestHeaders.Authorization;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        await Get<List<PurchaseOrderDto>>("/api/procurement/purchase-orders?take=1");
        _client.DefaultRequestHeaders.Authorization = adminAuth;

        var setRolesResp = await _client.PutAsJsonAsync($"/api/admin/users/{created.Id}/roles", new { roles = new[] { "Sales" } });
        if (!setRolesResp.IsSuccessStatusCode)
        {
            var text = await setRolesResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"PUT /api/admin/users/{created.Id}/roles failed: {(int)setRolesResp.StatusCode} {setRolesResp.ReasonPhrase}. Body: {text}");
        }
        var updated = (await setRolesResp.Content.ReadFromJsonAsync<AdminUserDto>())!;
        Assert.Contains("Sales", updated.Roles);
        Assert.DoesNotContain("Procurement", updated.Roles);

        await PostNoContent($"/api/admin/users/{created.Id}/reset-password", new { newPassword = "Passw0rd2" });
        var relogin = await Post<AuthDto>("/api/auth/login", new { email, password = "Passw0rd2" });
        Assert.False(string.IsNullOrWhiteSpace(relogin.Token));
    }

    [Fact]
    public async Task Sales_Dispatch_Reduces_Stock_Invoice_Creates_AR_And_Payment_Marks_Paid()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Bolt",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 2m
        });

        // Seed stock via GRN
        var po = await Post<PurchaseOrderDto>("/api/procurement/purchase-orders", new { supplierId = supplier.Id });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/lines", new { itemId = item.Id, quantity = 10m, unitPrice = 2m });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/approve", new { });
        var grn = await Post<GoodsReceiptDto>("/api/procurement/goods-receipts", new { purchaseOrderId = po.Id, warehouseId = warehouse.Id });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/lines", new { itemId = item.Id, quantity = 10m, unitCost = 2m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/post", new { });

        var order = await Post<SalesOrderDto>("/api/sales/orders", new { customerId = customer.Id });
        await PostNoContent($"/api/sales/orders/{order.Id}/lines", new { itemId = item.Id, quantity = 4m, unitPrice = 3m });
        await PostNoContent($"/api/sales/orders/{order.Id}/confirm", new { });

        var dispatch = await Post<DispatchDto>("/api/sales/dispatches", new { salesOrderId = order.Id, warehouseId = warehouse.Id });
        await PostNoContent($"/api/sales/dispatches/{dispatch.Id}/lines", new { itemId = item.Id, quantity = 4m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/sales/dispatches/{dispatch.Id}/post", new { });

        var afterDispatch = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={warehouse.Id}&itemId={item.Id}");
        Assert.Equal(6m, afterDispatch.OnHand);

        var invoice = await Post<InvoiceDto>("/api/sales/invoices", new { customerId = customer.Id, dueDate = (DateTimeOffset?)null });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/lines", new { itemId = item.Id, quantity = 4m, unitPrice = 3m, discountPercent = 0m, taxPercent = 0m });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/post", new { });

        var arEntries = await Get<List<ArDto>>("/api/finance/ar?outstandingOnly=true");
        var ar = Assert.Single(arEntries, e => e.ReferenceType == "INV" && e.ReferenceId == invoice.Id);
        Assert.Equal(12m, ar.Outstanding);

        var payment = await Post<PaymentDto>("/api/finance/payments", new
        {
            direction = PaymentDirection.Incoming,
            counterpartyType = CounterpartyType.Customer,
            counterpartyId = customer.Id,
            amount = 12m,
            notes = "Paid"
        });

        await PostNoContent($"/api/finance/payments/{payment.Id}/allocate/ar", new { entryId = ar.Id, amount = 12m });

        var arAll = await Get<List<ArDto>>("/api/finance/ar?outstandingOnly=false");
        var updatedAr = Assert.Single(arAll, e => e.Id == ar.Id);
        Assert.Equal(0m, updatedAr.Outstanding);

        var updatedInvoice = await Get<InvoiceDto>($"/api/sales/invoices/{invoice.Id}");
        Assert.Equal(SalesInvoiceStatus.Paid, updatedInvoice.Status);
    }

    [Fact]
    public async Task Sales_Quote_Can_Be_Created_Lined_And_Sent()
    {
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Washer",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 1m
        });

        var quote = await Post<SalesQuoteDto>("/api/sales/quotes", new { customerId = customer.Id, validUntil = (DateTimeOffset?)null });
        await PostNoContent($"/api/sales/quotes/{quote.Id}/lines", new { itemId = item.Id, quantity = 2m, unitPrice = 10m });
        await PostNoContent($"/api/sales/quotes/{quote.Id}/send", new { });

        var updated = await Get<SalesQuoteDto>($"/api/sales/quotes/{quote.Id}");
        Assert.Equal(SalesQuoteStatus.Sent, updated.Status);
        Assert.Equal(20m, updated.Total);
        var line = Assert.Single(updated.Lines);
        Assert.Equal(item.Id, line.ItemId);
        Assert.Equal(2m, line.Quantity);
    }

    [Fact]
    public async Task Sales_DirectDispatch_And_CustomerReturn_Adjust_Stock_And_AR_Credit()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Filter Cartridge",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 5m
        });

        var adj = await Post<StockAdjustmentDto>("/api/inventory/stock-adjustments", new { warehouseId = warehouse.Id, reason = "Seed stock" });
        await PostNoContent($"/api/inventory/stock-adjustments/{adj.Id}/lines", new
        {
            itemId = item.Id,
            quantityDelta = 10m,
            unitCost = 5m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/inventory/stock-adjustments/{adj.Id}/post", new { });

        var directDispatch = await Post<DirectDispatchApiDto>("/api/sales/direct-dispatches", new
        {
            warehouseId = warehouse.Id,
            customerId = customer.Id,
            serviceJobId = (Guid?)null,
            reason = "Counter sale without SO"
        });
        await PostNoContent($"/api/sales/direct-dispatches/{directDispatch.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 4m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/sales/direct-dispatches/{directDispatch.Id}/post", new { });

        var postedDispatch = await Get<DirectDispatchApiDto>($"/api/sales/direct-dispatches/{directDispatch.Id}");
        Assert.Equal(DirectDispatchStatus.Posted, postedDispatch.Status);
        Assert.Single(postedDispatch.Lines);

        var invoice = await Post<InvoiceDto>("/api/sales/invoices", new { customerId = customer.Id, dueDate = (DateTimeOffset?)null });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 4m,
            unitPrice = 10m,
            discountPercent = 0m,
            taxPercent = 0m
        });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/post", new { });

        var arBeforeReturn = await Get<List<ArDto>>("/api/finance/ar?outstandingOnly=false");
        var invoiceArBeforeReturn = Assert.Single(arBeforeReturn, e => e.ReferenceType == "INV" && e.ReferenceId == invoice.Id);
        Assert.Equal(40m, invoiceArBeforeReturn.Outstanding);

        var customerReturn = await Post<CustomerReturnApiDto>("/api/sales/customer-returns", new
        {
            customerId = customer.Id,
            warehouseId = warehouse.Id,
            salesInvoiceId = invoice.Id,
            dispatchNoteId = (Guid?)null,
            reason = "Defective units"
        });
        await PostNoContent($"/api/sales/customer-returns/{customerReturn.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 2m,
            unitPrice = 10m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/sales/customer-returns/{customerReturn.Id}/post", new { });

        var postedReturn = await Get<CustomerReturnApiDto>($"/api/sales/customer-returns/{customerReturn.Id}");
        Assert.Equal(CustomerReturnStatus.Posted, postedReturn.Status);
        Assert.Single(postedReturn.Lines);

        var onHand = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={warehouse.Id}&itemId={item.Id}");
        Assert.Equal(8m, onHand.OnHand); // 10 seed - 4 dispatch + 2 return

        var arAfterReturn = await Get<List<ArDto>>("/api/finance/ar?outstandingOnly=false");
        var invoiceArAfterReturn = Assert.Single(arAfterReturn, e => e.ReferenceType == "INV" && e.ReferenceId == invoice.Id);
        Assert.Equal(20m, invoiceArAfterReturn.Outstanding);

        var creditNotes = await Get<List<CreditNoteDto>>($"/api/finance/credit-notes?counterpartyType=1&counterpartyId={customer.Id}");
        Assert.Contains(creditNotes, cn => cn.SourceReferenceType == "CRTN" && cn.SourceReferenceId == customerReturn.Id && cn.Amount == 20m && cn.RemainingAmount == 0m);

        await AssertPdfOkAsync($"/api/sales/direct-dispatches/{directDispatch.Id}/pdf");
        await AssertPdfOkAsync($"/api/sales/customer-returns/{customerReturn.Id}/pdf");
    }

    [Fact]
    public async Task Sales_DirectDispatch_Post_Twice_Returns_BadRequest()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Return Hose",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 5m
        });

        var adj = await Post<StockAdjustmentDto>("/api/inventory/stock-adjustments", new { warehouseId = warehouse.Id, reason = "Seed stock" });
        await PostNoContent($"/api/inventory/stock-adjustments/{adj.Id}/lines", new
        {
            itemId = item.Id,
            quantityDelta = 2m,
            unitCost = 5m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/inventory/stock-adjustments/{adj.Id}/post", new { });

        var directDispatch = await Post<DirectDispatchApiDto>("/api/sales/direct-dispatches", new
        {
            warehouseId = warehouse.Id,
            customerId = customer.Id,
            serviceJobId = (Guid?)null,
            reason = "Retry post test"
        });
        await PostNoContent($"/api/sales/direct-dispatches/{directDispatch.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 1m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/sales/direct-dispatches/{directDispatch.Id}/post", new { });

        var resp = await _client.PostAsJsonAsync($"/api/sales/direct-dispatches/{directDispatch.Id}/post", new { });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("draft", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sales_CustomerReturn_Post_Twice_Returns_BadRequest()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Pressure Switch",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 5m
        });

        var adj = await Post<StockAdjustmentDto>("/api/inventory/stock-adjustments", new { warehouseId = warehouse.Id, reason = "Seed stock" });
        await PostNoContent($"/api/inventory/stock-adjustments/{adj.Id}/lines", new
        {
            itemId = item.Id,
            quantityDelta = 5m,
            unitCost = 5m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/inventory/stock-adjustments/{adj.Id}/post", new { });

        var invoice = await Post<InvoiceDto>("/api/sales/invoices", new { customerId = customer.Id, dueDate = (DateTimeOffset?)null });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 3m,
            unitPrice = 10m,
            discountPercent = 0m,
            taxPercent = 0m
        });
        await PostNoContent($"/api/sales/invoices/{invoice.Id}/post", new { });

        var customerReturn = await Post<CustomerReturnApiDto>("/api/sales/customer-returns", new
        {
            customerId = customer.Id,
            warehouseId = warehouse.Id,
            salesInvoiceId = invoice.Id,
            dispatchNoteId = (Guid?)null,
            reason = "Duplicate post test"
        });
        await PostNoContent($"/api/sales/customer-returns/{customerReturn.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 1m,
            unitPrice = 10m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/sales/customer-returns/{customerReturn.Id}/post", new { });

        var resp = await _client.PostAsJsonAsync($"/api/sales/customer-returns/{customerReturn.Id}/post", new { });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("draft", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Service_MaterialRequisition_Consumes_Stock()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var supplier = await Post<SupplierDto>("/api/suppliers", new { code = Code("SUP"), name = "Supplier", phone = "123", email = (string?)null, address = (string?)null });
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });

        var equipment = await Post<ItemDto>("/api/items", new
        {
            sku = Code("EQ"),
            name = "Skid Steer Loader",
            type = ItemType.Equipment,
            trackingType = TrackingType.Serial,
            unitOfMeasure = "UNIT",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 0m
        });

        var sparePart = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SP"),
            name = "Belt",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 1m
        });

        // Seed stock for spare part via GRN
        var po = await Post<PurchaseOrderDto>("/api/procurement/purchase-orders", new { supplierId = supplier.Id });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/lines", new { itemId = sparePart.Id, quantity = 5m, unitPrice = 1m });
        await PostNoContent($"/api/procurement/purchase-orders/{po.Id}/approve", new { });
        var grn = await Post<GoodsReceiptDto>("/api/procurement/goods-receipts", new { purchaseOrderId = po.Id, warehouseId = warehouse.Id });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/lines", new { itemId = sparePart.Id, quantity = 5m, unitCost = 1m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/procurement/goods-receipts/{grn.Id}/post", new { });

        var unit = await Post<EquipmentUnitDto>("/api/service/equipment-units", new
        {
            itemId = equipment.Id,
            serialNumber = $"SN-{Guid.NewGuid():N}"[..20],
            customerId = customer.Id,
            purchasedAt = (DateTimeOffset?)null,
            warrantyUntil = (DateTimeOffset?)null
        });

        var job = await Post<ServiceJobDto>("/api/service/jobs", new { equipmentUnitId = unit.Id, customerId = customer.Id, problemDescription = "Routine maintenance" });

        var mr = await Post<MaterialRequisitionDto>("/api/service/material-requisitions", new { serviceJobId = job.Id, warehouseId = warehouse.Id });
        await PostNoContent($"/api/service/material-requisitions/{mr.Id}/lines", new { itemId = sparePart.Id, quantity = 2m, batchNumber = (string?)null, serials = (string[]?)null });
        await PostNoContent($"/api/service/material-requisitions/{mr.Id}/post", new { });

        var onHand = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={warehouse.Id}&itemId={sparePart.Id}");
        Assert.Equal(3m, onHand.OnHand);
    }

    [Fact]
    public async Task Service_WorkOrder_And_QualityCheck_Can_Be_Created()
    {
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var equipment = await Post<ItemDto>("/api/items", new
        {
            sku = Code("EQ"),
            name = "Generator",
            type = ItemType.Equipment,
            trackingType = TrackingType.Serial,
            unitOfMeasure = "UNIT",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 0m
        });

        var unit = await Post<EquipmentUnitDto>("/api/service/equipment-units", new
        {
            itemId = equipment.Id,
            serialNumber = $"SN-{Guid.NewGuid():N}"[..20],
            customerId = customer.Id,
            purchasedAt = (DateTimeOffset?)null,
            warrantyUntil = (DateTimeOffset?)null
        });

        var job = await Post<ServiceJobDto>("/api/service/jobs", new { equipmentUnitId = unit.Id, customerId = customer.Id, problemDescription = "Check output" });

        var workOrder = await Post<WorkOrderDto>("/api/service/work-orders", new { serviceJobId = job.Id, description = "Inspect and test", assignedToUserId = (Guid?)null });
        Assert.Equal(job.Id, workOrder.ServiceJobId);
        Assert.Equal(WorkOrderStatus.Open, workOrder.Status);

        var qc = await Post<QualityCheckDto>("/api/service/quality-checks", new { serviceJobId = job.Id, passed = true, notes = "OK" });
        Assert.Equal(job.Id, qc.ServiceJobId);
        Assert.True(qc.Passed);
    }

    [Fact]
    public async Task Service_Estimate_And_Handover_Can_Be_Processed_And_Exported()
    {
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var equipment = await Post<ItemDto>("/api/items", new
        {
            sku = Code("EQ"),
            name = "Compressor",
            type = ItemType.Equipment,
            trackingType = TrackingType.Serial,
            unitOfMeasure = "UNIT",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 0m
        });

        var sparePart = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SP"),
            name = "Seal Kit",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 25m
        });

        var unit = await Post<EquipmentUnitDto>("/api/service/equipment-units", new
        {
            itemId = equipment.Id,
            serialNumber = $"SN-{Guid.NewGuid():N}"[..20],
            customerId = customer.Id,
            purchasedAt = (DateTimeOffset?)null,
            warrantyUntil = (DateTimeOffset?)null
        });

        var job = await Post<ServiceJobDto>("/api/service/jobs", new { equipmentUnitId = unit.Id, customerId = customer.Id, problemDescription = "Leakage issue" });

        var estimate = await Post<ServiceEstimateApiDto>("/api/service/estimates", new
        {
            serviceJobId = job.Id,
            validUntil = (DateTimeOffset?)null,
            terms = "Estimate valid for 7 days"
        });

        await PostNoContent($"/api/service/estimates/{estimate.Id}/lines", new
        {
            kind = ServiceEstimateLineKind.Part,
            itemId = sparePart.Id,
            description = "Seal kit replacement",
            quantity = 1m,
            unitPrice = 25m,
            taxPercent = 0m
        });
        await PostNoContent($"/api/service/estimates/{estimate.Id}/lines", new
        {
            kind = ServiceEstimateLineKind.Labor,
            itemId = (Guid?)null,
            description = "Repair labor",
            quantity = 2m,
            unitPrice = 30m,
            taxPercent = 10m
        });
        await PostNoContent($"/api/service/estimates/{estimate.Id}/approve", new { });

        var approvedEstimate = await Get<ServiceEstimateApiDto>($"/api/service/estimates/{estimate.Id}");
        Assert.Equal(ServiceEstimateStatus.Approved, approvedEstimate.Status);
        Assert.Equal(85m, approvedEstimate.Subtotal);
        Assert.Equal(6m, approvedEstimate.TaxTotal);
        Assert.Equal(91m, approvedEstimate.Total);
        Assert.Equal(2, approvedEstimate.Lines.Count);

        await AssertPdfOkAsync($"/api/service/estimates/{estimate.Id}/pdf");

        var handover = await Post<ServiceHandoverApiDto>("/api/service/handovers", new
        {
            serviceJobId = job.Id,
            itemsReturned = "Compressor unit, power cable",
            postServiceWarrantyMonths = 3,
            customerAcknowledgement = "Received",
            notes = "Customer informed on operating precautions"
        });

        await PostNoContent($"/api/service/handovers/{handover.Id}/complete", new { });

        var completedHandover = await Get<ServiceHandoverApiDto>($"/api/service/handovers/{handover.Id}");
        Assert.Equal(ServiceHandoverStatus.Completed, completedHandover.Status);
        Assert.Equal(3, completedHandover.PostServiceWarrantyMonths);
        Assert.Contains("power cable", completedHandover.ItemsReturned);

        await AssertPdfOkAsync($"/api/service/handovers/{handover.Id}/pdf");
    }

    [Fact]
    public async Task Service_Estimate_Send_And_Handover_ConvertToInvoice_Queue_Notifications_And_Link_Invoice()
    {
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "+15550123", email = "svc-customer@example.test", address = (string?)null });
        var equipment = await Post<ItemDto>("/api/items", new
        {
            sku = Code("EQ"),
            name = "Water Pump",
            type = ItemType.Equipment,
            trackingType = TrackingType.Serial,
            unitOfMeasure = "UNIT",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 0m
        });
        var partItem = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SP"),
            name = "Impeller",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 20m
        });
        var laborItem = await Post<ItemDto>("/api/items", new
        {
            sku = Code("LAB"),
            name = "Service Labor",
            type = ItemType.Service,
            trackingType = TrackingType.None,
            unitOfMeasure = "HRS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 0m
        });

        var unit = await Post<EquipmentUnitDto>("/api/service/equipment-units", new
        {
            itemId = equipment.Id,
            serialNumber = $"SN-{Guid.NewGuid():N}"[..20],
            customerId = customer.Id,
            purchasedAt = (DateTimeOffset?)null,
            warrantyUntil = (DateTimeOffset?)null
        });
        var job = await Post<ServiceJobDto>("/api/service/jobs", new { equipmentUnitId = unit.Id, customerId = customer.Id, problemDescription = "No pressure output" });

        var estimate = await Post<ServiceEstimateApiDto>("/api/service/estimates", new
        {
            serviceJobId = job.Id,
            validUntil = (DateTimeOffset?)null,
            terms = "Advance approval required"
        });
        await PostNoContent($"/api/service/estimates/{estimate.Id}/lines", new
        {
            kind = ServiceEstimateLineKind.Part,
            itemId = partItem.Id,
            description = "Replace impeller",
            quantity = 1m,
            unitPrice = 50m,
            taxPercent = 0m
        });
        await PostNoContent($"/api/service/estimates/{estimate.Id}/lines", new
        {
            kind = ServiceEstimateLineKind.Labor,
            itemId = (Guid?)null,
            description = "Repair labor",
            quantity = 2m,
            unitPrice = 30m,
            taxPercent = 0m
        });
        await PostNoContent($"/api/service/estimates/{estimate.Id}/send", new { appBaseUrl = "http://localhost:3000" });

        var notificationsAfterEstimateSend = await Get<List<NotificationOutboxDto>>("/api/admin/notifications?take=500");
        var estimateNotifications = notificationsAfterEstimateSend.Where(n => n.ReferenceType == "SE" && n.ReferenceId == estimate.Id).ToList();
        Assert.Contains(estimateNotifications, n => n.Channel == NotificationChannel.Email && n.Recipient == customer.Email);
        Assert.Contains(estimateNotifications, n => n.Channel == NotificationChannel.Sms && n.Recipient == customer.Phone);

        await PostNoContent($"/api/service/estimates/{estimate.Id}/approve", new { });

        var handover = await Post<ServiceHandoverApiDto>("/api/service/handovers", new
        {
            serviceJobId = job.Id,
            itemsReturned = "Pump body, hose clamp",
            postServiceWarrantyMonths = 1,
            customerAcknowledgement = "Ready to collect",
            notes = (string?)null
        });
        await PostNoContent($"/api/service/handovers/{handover.Id}/complete", new { });

        var notificationsAfterHandoverComplete = await Get<List<NotificationOutboxDto>>("/api/admin/notifications?take=500");
        var handoverNotifications = notificationsAfterHandoverComplete.Where(n => n.ReferenceType == "SH" && n.ReferenceId == handover.Id).ToList();
        Assert.Contains(handoverNotifications, n => n.Channel == NotificationChannel.Email && n.Recipient == customer.Email);
        Assert.Contains(handoverNotifications, n => n.Channel == NotificationChannel.Sms && n.Recipient == customer.Phone);

        var convert = await Post<ConvertToSalesInvoiceResponseDto>($"/api/service/handovers/{handover.Id}/convert-to-sales-invoice", new
        {
            serviceEstimateId = estimate.Id,
            laborItemId = laborItem.Id,
            dueDate = (DateTimeOffset?)null
        });

        var linkedHandover = await Get<ServiceHandoverApiDto>($"/api/service/handovers/{handover.Id}");
        Assert.Equal(convert.SalesInvoiceId, linkedHandover.SalesInvoiceId);
        Assert.NotNull(linkedHandover.ConvertedToInvoiceAt);

        var invoice = await Get<InvoiceDetailDto>($"/api/sales/invoices/{convert.SalesInvoiceId}");
        Assert.Equal(SalesInvoiceStatus.Draft, invoice.Status);
        Assert.Equal(customer.Id, invoice.CustomerId);
        Assert.Equal(2, invoice.Lines.Count);
        Assert.Equal(110m, invoice.Total);

        var convertAgain = await Post<ConvertToSalesInvoiceResponseDto>($"/api/service/handovers/{handover.Id}/convert-to-sales-invoice", new
        {
            serviceEstimateId = estimate.Id,
            laborItemId = laborItem.Id,
            dueDate = (DateTimeOffset?)null
        });
        Assert.Equal(convert.SalesInvoiceId, convertAgain.SalesInvoiceId);
    }

    [Fact]
    public async Task Service_Handover_ConvertToInvoice_WithLaborLines_WithoutLaborItem_Returns_BadRequest()
    {
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "+15550124", email = "svc-fail@example.test", address = (string?)null });
        var equipment = await Post<ItemDto>("/api/items", new
        {
            sku = Code("EQ"),
            name = "Hydraulic Pump",
            type = ItemType.Equipment,
            trackingType = TrackingType.Serial,
            unitOfMeasure = "UNIT",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 0m
        });

        var unit = await Post<EquipmentUnitDto>("/api/service/equipment-units", new
        {
            itemId = equipment.Id,
            serialNumber = $"SN-{Guid.NewGuid():N}"[..20],
            customerId = customer.Id,
            purchasedAt = (DateTimeOffset?)null,
            warrantyUntil = (DateTimeOffset?)null
        });
        var job = await Post<ServiceJobDto>("/api/service/jobs", new { equipmentUnitId = unit.Id, customerId = customer.Id, problemDescription = "Overheating" });

        var estimate = await Post<ServiceEstimateApiDto>("/api/service/estimates", new
        {
            serviceJobId = job.Id,
            validUntil = (DateTimeOffset?)null,
            terms = (string?)null
        });
        await PostNoContent($"/api/service/estimates/{estimate.Id}/lines", new
        {
            kind = ServiceEstimateLineKind.Labor,
            itemId = (Guid?)null,
            description = "Inspection labor",
            quantity = 1m,
            unitPrice = 40m,
            taxPercent = 0m
        });
        await PostNoContent($"/api/service/estimates/{estimate.Id}/approve", new { });

        var handover = await Post<ServiceHandoverApiDto>("/api/service/handovers", new
        {
            serviceJobId = job.Id,
            itemsReturned = "Pump unit",
            postServiceWarrantyMonths = (int?)null,
            customerAcknowledgement = (string?)null,
            notes = (string?)null
        });
        await PostNoContent($"/api/service/handovers/{handover.Id}/complete", new { });

        var resp = await _client.PostAsJsonAsync($"/api/service/handovers/{handover.Id}/convert-to-sales-invoice", new
        {
            serviceEstimateId = estimate.Id,
            laborItemId = (Guid?)null,
            dueDate = (DateTimeOffset?)null
        });

        Assert.Equal(400, (int)resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Labor item is required", body);
    }

    [Fact]
    public async Task Document_Collaboration_Comments_And_Attachments_Work_For_ServiceEstimate()
    {
        var customer = await Post<CustomerDto>("/api/customers", new { code = Code("CUS"), name = "Customer A", phone = "555", email = (string?)null, address = (string?)null });
        var equipment = await Post<ItemDto>("/api/items", new
        {
            sku = Code("EQ"),
            name = "Alternator",
            type = ItemType.Equipment,
            trackingType = TrackingType.Serial,
            unitOfMeasure = "UNIT",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 0m
        });
        var unit = await Post<EquipmentUnitDto>("/api/service/equipment-units", new
        {
            itemId = equipment.Id,
            serialNumber = $"SN-{Guid.NewGuid():N}"[..20],
            customerId = customer.Id,
            purchasedAt = (DateTimeOffset?)null,
            warrantyUntil = (DateTimeOffset?)null
        });
        var job = await Post<ServiceJobDto>("/api/service/jobs", new { equipmentUnitId = unit.Id, customerId = customer.Id, problemDescription = "No output" });
        var estimate = await Post<ServiceEstimateApiDto>("/api/service/estimates", new
        {
            serviceJobId = job.Id,
            validUntil = (DateTimeOffset?)null,
            terms = "Check notes"
        });

        var comment = await Post<DocumentCommentApiDto>($"/api/documents/SE/{estimate.Id}/comments", new { text = "Customer called and approved verbal estimate." });
        Assert.Equal("SE", comment.ReferenceType);
        Assert.Equal(estimate.Id, comment.ReferenceId);

        var comments = await Get<List<DocumentCommentApiDto>>($"/api/documents/SE/{estimate.Id}/comments");
        Assert.Contains(comments, x => x.Id == comment.Id && x.Text.Contains("approved verbal"));

        using (var multipart = new MultipartFormDataContent())
        {
            var bytes = Encoding.UTF8.GetBytes("sample attachment");
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            multipart.Add(fileContent, "file", "note.txt");
            multipart.Add(new StringContent("Photo from technician"), "notes");

            var uploadResp = await _client.PostAsync($"/api/documents/SE/{estimate.Id}/attachments/upload", multipart);
            if (!uploadResp.IsSuccessStatusCode)
            {
                var text = await uploadResp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"POST attachment failed: {(int)uploadResp.StatusCode} {uploadResp.ReasonPhrase}. Body: {text}");
            }

            var attachment = (await uploadResp.Content.ReadFromJsonAsync<DocumentAttachmentApiDto>())!;
            Assert.Equal("SE", attachment.ReferenceType);
            Assert.Equal(estimate.Id, attachment.ReferenceId);
            Assert.Equal("note.txt", attachment.FileName);

            var attachments = await Get<List<DocumentAttachmentApiDto>>($"/api/documents/SE/{estimate.Id}/attachments");
            var savedAttachment = Assert.Single(attachments, a => a.Id == attachment.Id);
            Assert.Equal("Photo from technician", savedAttachment.Notes);

            var contentResp = await _client.GetAsync(savedAttachment.Url);
            contentResp.EnsureSuccessStatusCode();
            var downloaded = await contentResp.Content.ReadAsByteArrayAsync();
            Assert.Equal(bytes, downloaded);

            await Delete($"/api/documents/SE/{estimate.Id}/attachments/{savedAttachment.Id}");
        }

        await Delete($"/api/documents/SE/{estimate.Id}/comments/{comment.Id}");
        var commentsAfterDelete = await Get<List<DocumentCommentApiDto>>($"/api/documents/SE/{estimate.Id}/comments");
        Assert.DoesNotContain(commentsAfterDelete, x => x.Id == comment.Id);
    }

    [Fact]
    public async Task Document_Collaboration_Invalid_ReferenceType_Returns_BadRequest()
    {
        var resp = await _client.PostAsJsonAsync($"/api/documents/bad.type/{Guid.NewGuid()}/comments", new { text = "invalid ref type" });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("ReferenceType", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inventory_ReorderAlerts_Returns_Items_Below_ReorderPoint()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Grease",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "KG",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 1m
        });

        await Post<ReorderSettingDto>("/api/reorder-settings", new { warehouseId = warehouse.Id, itemId = item.Id, reorderPoint = 1m, reorderQuantity = 5m });

        var alerts = await Get<List<ReorderAlertDto>>($"/api/inventory/reorder-alerts?warehouseId={warehouse.Id}");
        Assert.Contains(alerts, a => a.WarehouseId == warehouse.Id && a.ItemId == item.Id && a.ReorderPoint == 1m && a.OnHand == 0m);
    }

    [Fact]
    public async Task Inventory_StockAdjustment_Post_Changes_OnHand()
    {
        var warehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "Main", address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Hydraulic Oil",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "L",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 1m
        });

        var adjIn = await Post<StockAdjustmentDto>("/api/inventory/stock-adjustments", new { warehouseId = warehouse.Id, reason = "Initial count" });
        await PostNoContent($"/api/inventory/stock-adjustments/{adjIn.Id}/lines", new
        {
            itemId = item.Id,
            quantityDelta = 5m,
            unitCost = 1m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/inventory/stock-adjustments/{adjIn.Id}/post", new { });

        var onHandAfterIn = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={warehouse.Id}&itemId={item.Id}");
        Assert.Equal(5m, onHandAfterIn.OnHand);

        var adjOut = await Post<StockAdjustmentDto>("/api/inventory/stock-adjustments", new { warehouseId = warehouse.Id, reason = "Damage" });
        await PostNoContent($"/api/inventory/stock-adjustments/{adjOut.Id}/lines", new
        {
            itemId = item.Id,
            quantityDelta = -2m,
            unitCost = 1m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/inventory/stock-adjustments/{adjOut.Id}/post", new { });

        var onHandAfterOut = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={warehouse.Id}&itemId={item.Id}");
        Assert.Equal(3m, onHandAfterOut.OnHand);
    }

    [Fact]
    public async Task Inventory_StockTransfer_Post_Moves_Stock_Between_Warehouses()
    {
        var fromWarehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "From", address = (string?)null });
        var toWarehouse = await Post<WarehouseDto>("/api/warehouses", new { code = Code("WH"), name = "To", address = (string?)null });
        var item = await Post<ItemDto>("/api/items", new
        {
            sku = Code("SKU"),
            name = "Rags",
            type = ItemType.SparePart,
            trackingType = TrackingType.None,
            unitOfMeasure = "PCS",
            brandId = (Guid?)null,
            barcode = (string?)null,
            defaultUnitCost = 1m
        });

        // Seed stock in From via stock adjustment
        var adj = await Post<StockAdjustmentDto>("/api/inventory/stock-adjustments", new { warehouseId = fromWarehouse.Id, reason = "Seed" });
        await PostNoContent($"/api/inventory/stock-adjustments/{adj.Id}/lines", new
        {
            itemId = item.Id,
            quantityDelta = 5m,
            unitCost = 1m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/inventory/stock-adjustments/{adj.Id}/post", new { });

        var transfer = await Post<StockTransferDto>("/api/inventory/stock-transfers", new { fromWarehouseId = fromWarehouse.Id, toWarehouseId = toWarehouse.Id, notes = "Move stock" });
        await PostNoContent($"/api/inventory/stock-transfers/{transfer.Id}/lines", new
        {
            itemId = item.Id,
            quantity = 3m,
            unitCost = 1m,
            batchNumber = (string?)null,
            serials = (string[]?)null
        });
        await PostNoContent($"/api/inventory/stock-transfers/{transfer.Id}/post", new { });

        var fromOnHand = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={fromWarehouse.Id}&itemId={item.Id}");
        var toOnHand = await Get<OnHandDto>($"/api/inventory/onhand?warehouseId={toWarehouse.Id}&itemId={item.Id}");
        Assert.Equal(2m, fromOnHand.OnHand);
        Assert.Equal(3m, toOnHand.OnHand);
    }

    [Fact]
    public async Task Audit_And_Reporting_Endpoints_Work()
    {
        var dashboard = await Get<DashboardDto>("/api/reporting/dashboard");
        Assert.True(dashboard.OpenServiceJobs >= 0);

        var logs = await Get<List<AuditLogDto>>("/api/audit-logs?take=20");
        Assert.NotNull(logs);
    }

    [Fact]
    public async Task Health_Endpoint_Returns_Healthy()
    {
        var resp = await _client.GetAsync("/health");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record PurchaseOrderDto(Guid Id, string Number, Guid SupplierId, DateTimeOffset OrderDate, PurchaseOrderStatus Status, decimal Total);
    private sealed record GoodsReceiptDto(Guid Id, string Number, Guid PurchaseOrderId, Guid WarehouseId, DateTimeOffset ReceivedAt, GoodsReceiptStatus Status);
    private sealed record OnHandDto(Guid WarehouseId, Guid ItemId, string? BatchNumber, decimal OnHand);
    private sealed record ApDto(Guid Id, Guid SupplierId, string ReferenceType, Guid ReferenceId, decimal Amount, decimal Outstanding, DateTimeOffset PostedAt);
    private sealed record CreditNoteDto(Guid Id, string ReferenceNumber, CounterpartyType CounterpartyType, Guid CounterpartyId, decimal Amount, decimal RemainingAmount, DateTimeOffset IssuedAt, string? Notes, string? SourceReferenceType, Guid? SourceReferenceId);

    private sealed record StockAdjustmentDto(Guid Id, string Number, Guid WarehouseId, DateTimeOffset AdjustedAt, StockAdjustmentStatus Status);
    private sealed record StockTransferDto(Guid Id, string Number, Guid FromWarehouseId, Guid ToWarehouseId, DateTimeOffset TransferDate, StockTransferStatus Status);

    private sealed record RfqDto(Guid Id, string Number, Guid SupplierId, DateTimeOffset RequestedAt, RequestForQuoteStatus Status, IReadOnlyList<RfqLineDto> Lines);
    private sealed record RfqLineDto(Guid Id, Guid ItemId, decimal Quantity, string? Notes);

    private sealed record SupplierReturnDto(Guid Id, string Number, Guid SupplierId, Guid WarehouseId, DateTimeOffset ReturnDate, SupplierReturnStatus Status, string? Reason);
    private sealed record DirectPurchaseLineApiDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal TaxPercent, string? BatchNumber, IReadOnlyList<string> Serials, decimal LineSubTotal, decimal LineTax, decimal LineTotal);
    private sealed record DirectPurchaseApiDto(Guid Id, string Number, Guid SupplierId, Guid WarehouseId, DateTimeOffset PurchasedAt, DirectPurchaseStatus Status, string? Remarks, decimal Subtotal, decimal TaxTotal, decimal GrandTotal, IReadOnlyList<DirectPurchaseLineApiDto> Lines);
    private sealed record SupplierInvoiceApiDto(Guid Id, string Number, Guid SupplierId, string InvoiceNumber, DateTimeOffset InvoiceDate, DateTimeOffset? DueDate, Guid? PurchaseOrderId, Guid? GoodsReceiptId, Guid? DirectPurchaseId, decimal Subtotal, decimal DiscountAmount, decimal TaxAmount, decimal FreightAmount, decimal RoundingAmount, decimal GrandTotal, SupplierInvoiceStatus Status, DateTimeOffset? PostedAt, Guid? AccountsPayableEntryId, string? Notes);

    private sealed record SalesOrderDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset OrderDate, SalesOrderStatus Status, decimal Total);
    private sealed record DispatchDto(Guid Id, string Number, Guid SalesOrderId, Guid WarehouseId, DateTimeOffset DispatchedAt, DispatchStatus Status);
    private sealed record DirectDispatchLineApiDto(Guid Id, Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string> Serials);
    private sealed record DirectDispatchApiDto(Guid Id, string Number, Guid WarehouseId, Guid? CustomerId, Guid? ServiceJobId, DateTimeOffset DispatchedAt, DirectDispatchStatus Status, string? Reason, IReadOnlyList<DirectDispatchLineApiDto> Lines);
    private sealed record CustomerReturnLineApiDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, string? BatchNumber, IReadOnlyList<string> Serials);
    private sealed record CustomerReturnApiDto(Guid Id, string Number, Guid CustomerId, Guid WarehouseId, DateTimeOffset ReturnDate, CustomerReturnStatus Status, Guid? SalesInvoiceId, Guid? DispatchNoteId, string? Reason, IReadOnlyList<CustomerReturnLineApiDto> Lines);
    private sealed record InvoiceDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset InvoiceDate, DateTimeOffset? DueDate, SalesInvoiceStatus Status, decimal Subtotal, decimal TaxTotal, decimal Total);
    private sealed record InvoiceLineDetailDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxPercent, decimal LineTotal);
    private sealed record InvoiceDetailDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset InvoiceDate, DateTimeOffset? DueDate, SalesInvoiceStatus Status, decimal Subtotal, decimal TaxTotal, decimal Total, IReadOnlyList<InvoiceLineDetailDto> Lines);
    private sealed record ArDto(Guid Id, Guid CustomerId, string ReferenceType, Guid ReferenceId, decimal Amount, decimal Outstanding, DateTimeOffset PostedAt);
    private sealed record PaymentDto(Guid Id, string ReferenceNumber, PaymentDirection Direction, CounterpartyType CounterpartyType, Guid CounterpartyId, decimal Amount, DateTimeOffset PaidAt, string? Notes);

    private sealed record SalesQuoteDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset QuoteDate, DateTimeOffset? ValidUntil, SalesQuoteStatus Status, decimal Total, IReadOnlyList<SalesQuoteLineDto> Lines);
    private sealed record SalesQuoteLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal LineTotal);

    private sealed record EquipmentUnitDto(Guid Id, Guid ItemId, string SerialNumber, Guid CustomerId, DateTimeOffset? PurchasedAt, DateTimeOffset? WarrantyUntil);
    private sealed record ServiceJobDto(Guid Id, string Number, Guid EquipmentUnitId, Guid CustomerId, DateTimeOffset OpenedAt, string ProblemDescription, ServiceJobStatus Status, DateTimeOffset? CompletedAt);
    private sealed record ServiceEstimateLineApiDto(Guid Id, ServiceEstimateLineKind Kind, Guid? ItemId, string Description, decimal Quantity, decimal UnitPrice, decimal TaxPercent, decimal LineSubtotal, decimal LineTax, decimal LineTotal);
    private sealed record ServiceEstimateApiDto(Guid Id, string Number, Guid ServiceJobId, DateTimeOffset IssuedAt, DateTimeOffset? ValidUntil, string? Terms, ServiceEstimateStatus Status, decimal Subtotal, decimal TaxTotal, decimal Total, IReadOnlyList<ServiceEstimateLineApiDto> Lines);
    private sealed record ServiceHandoverApiDto(Guid Id, string Number, Guid ServiceJobId, DateTimeOffset HandoverDate, string ItemsReturned, int? PostServiceWarrantyMonths, string? CustomerAcknowledgement, string? Notes, ServiceHandoverStatus Status, Guid? SalesInvoiceId = null, DateTimeOffset? ConvertedToInvoiceAt = null);
    private sealed record ConvertToSalesInvoiceResponseDto(Guid SalesInvoiceId);
    private sealed record DocumentCommentApiDto(Guid Id, string ReferenceType, Guid ReferenceId, string Text, DateTimeOffset CreatedAt, Guid? CreatedBy, DateTimeOffset? LastModifiedAt, Guid? LastModifiedBy);
    private sealed record DocumentAttachmentApiDto(Guid Id, string ReferenceType, Guid ReferenceId, string FileName, string Url, bool IsImage, string? ContentType, long? SizeBytes, string? Notes, DateTimeOffset CreatedAt, Guid? CreatedBy);
    private sealed record MaterialRequisitionDto(Guid Id, string Number, Guid ServiceJobId, Guid WarehouseId, DateTimeOffset RequestedAt, MaterialRequisitionStatus Status);

    private sealed record WorkOrderDto(Guid Id, Guid ServiceJobId, string Description, Guid? AssignedToUserId, WorkOrderStatus Status);
    private sealed record QualityCheckDto(Guid Id, Guid ServiceJobId, DateTimeOffset CheckedAt, bool Passed, string? Notes);

    private sealed record ReorderSettingDto(Guid Id, Guid WarehouseId, Guid ItemId, decimal ReorderPoint, decimal ReorderQuantity);
    private sealed record ReorderAlertDto(Guid WarehouseId, Guid ItemId, decimal ReorderPoint, decimal ReorderQuantity, decimal OnHand);

    private sealed record DashboardDto(int OpenServiceJobs, decimal ArOutstanding, decimal ApOutstanding, int ReorderAlerts);
    private sealed record AuditLogDto(Guid Id, DateTimeOffset OccurredAt, Guid? UserId, string TableName, int Action, string Key, string ChangesJson);
    private sealed record ImportResultDto(int BrandsCreated, int BrandsUpdated, int WarehousesCreated, int WarehousesUpdated, int SuppliersCreated, int SuppliersUpdated, int CustomersCreated, int CustomersUpdated, int ItemsCreated, int ItemsUpdated, int ReorderSettingsCreated, int ReorderSettingsUpdated, int EquipmentUnitsCreated, int EquipmentUnitsUpdated);
    private sealed record NotificationOutboxDto(Guid Id, NotificationChannel Channel, string Recipient, string? Subject, string Body, NotificationStatus Status, int Attempts, DateTimeOffset NextAttemptAt, DateTimeOffset? LastAttemptAt, DateTimeOffset? SentAt, string? LastError, string? ReferenceType, Guid? ReferenceId, DateTimeOffset CreatedAt);
    private sealed record AdminUserDto(Guid Id, string Email, string? DisplayName, bool IsLocked, DateTimeOffset? LockoutEnd, IReadOnlyList<string> Roles);
    private sealed record AuthDto(string Token, Guid UserId, string Email, IReadOnlyList<string> Roles);

    private async Task<T> Post<T>(string url, object body)
    {
        var resp = await _client.PostAsJsonAsync(url, body);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"POST {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {text}");
        }
        return (await resp.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task PostNoContent(string url, object body)
    {
        var resp = await _client.PostAsJsonAsync(url, body);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"POST {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {text}");
        }
    }

    private async Task<T> Get<T>(string url)
    {
        var resp = await _client.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"GET {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {text}");
        }
        return (await resp.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task Delete(string url)
    {
        var resp = await _client.DeleteAsync(url);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"DELETE {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {text}");
        }
    }

    private async Task AssertPdfOkAsync(string url)
    {
        var resp = await _client.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"GET {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {text}");
        }

        Assert.Equal("application/pdf", resp.Content.Headers.ContentType?.MediaType);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 256);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, Math.Min(4, bytes.Length)));
    }
}
