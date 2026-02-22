using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ISS.Infrastructure.Documents;

public sealed partial class DocumentPdfService
{
    private async Task<PdfDocument> RenderRfqAsync(Guid id, CancellationToken cancellationToken)
    {
        var rfq = await _dbContext.RequestForQuotes.AsNoTracking()
                      .Include(x => x.Lines)
                      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                  ?? throw new NotFoundException("RFQ not found.");

        var supplier = await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rfq.SupplierId, cancellationToken);
        var itemById = await LoadItemMapAsync(rfq.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Supplier", SupplierLabel(supplier, rfq.SupplierId)),
            ("Requested at", rfq.RequestedAt.ToString("u")),
            ("Status", rfq.Status.ToString())
        };

        return BuildPdf(
            title: "Request for Quote",
            referenceNumber: rfq.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Lines").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).Text("Notes");
                    });

                    foreach (var line in rfq.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).Text(line.Notes ?? "");
                    }
                });
            },
            fileName: $"RFQ-{rfq.Number}.pdf",
            qrPayload: $"ISS:RFQ:{rfq.Id}",
            barcodePayload: rfq.Number);
    }

    private async Task<PdfDocument> RenderPurchaseOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var po = await _dbContext.PurchaseOrders.AsNoTracking()
                     .Include(x => x.Lines)
                     .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                 ?? throw new NotFoundException("Purchase order not found.");

        var supplier = await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == po.SupplierId, cancellationToken);
        var itemById = await LoadItemMapAsync(po.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Supplier", SupplierLabel(supplier, po.SupplierId)),
            ("Order date", po.OrderDate.ToString("u")),
            ("Status", po.Status.ToString()),
            ("Total", FormatMoney(po.Total))
        };

        return BuildPdf(
            title: "Purchase Order",
            referenceNumber: po.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Lines").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).AlignRight().Text("Ordered");
                        h.Cell().Element(CellHeader).AlignRight().Text("Received");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit Price");
                        h.Cell().Element(CellHeader).AlignRight().Text("Line Total");
                    });

                    foreach (var line in po.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.OrderedQuantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.ReceivedQuantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitPrice));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.LineTotal));
                    }
                });
            },
            fileName: $"PO-{po.Number}.pdf",
            qrPayload: $"ISS:PO:{po.Id}",
            barcodePayload: po.Number);
    }

    private async Task<PdfDocument> RenderGoodsReceiptAsync(Guid id, CancellationToken cancellationToken)
    {
        var grn = await _dbContext.GoodsReceipts.AsNoTracking()
                      .Include(x => x.Lines)
                      .ThenInclude(l => l.Serials)
                      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                  ?? throw new NotFoundException("Goods receipt not found.");

        var po = await _dbContext.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken);
        var supplier = po is null
            ? null
            : await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == po.SupplierId, cancellationToken);
        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == grn.WarehouseId, cancellationToken);
        var itemById = await LoadItemMapAsync(grn.Lines.Select(l => l.ItemId), cancellationToken);

        var total = grn.Lines.Sum(l => l.Quantity * l.UnitCost);
        var meta = new List<(string Label, string Value)>
        {
            ("Supplier", po is null ? grn.PurchaseOrderId.ToString() : SupplierLabel(supplier, po.SupplierId)),
            ("PO", po?.Number ?? grn.PurchaseOrderId.ToString()),
            ("Warehouse", WarehouseLabel(warehouse, grn.WarehouseId)),
            ("Received at", grn.ReceivedAt.ToString("u")),
            ("Status", grn.Status.ToString()),
            ("Total", FormatMoney(total))
        };

        return BuildPdf(
            title: "Goods Receipt Note",
            referenceNumber: grn.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Lines").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit Cost");
                        h.Cell().Element(CellHeader).Text("Batch");
                        h.Cell().Element(CellHeader).Text("Serials");
                    });

                    foreach (var line in grn.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitCost));
                        table.Cell().Element(CellBody).Text(line.BatchNumber ?? "");
                        table.Cell().Element(CellBody).Text(string.Join(", ", line.Serials.Select(s => s.SerialNumber)));
                    }
                });
            },
            fileName: $"GRN-{grn.Number}.pdf",
            qrPayload: $"ISS:GRN:{grn.Id}",
            barcodePayload: grn.Number);
    }

    private async Task<PdfDocument> RenderSupplierReturnAsync(Guid id, CancellationToken cancellationToken)
    {
        var sr = await _dbContext.SupplierReturns.AsNoTracking()
                     .Include(x => x.Lines)
                     .ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                 ?? throw new NotFoundException("Supplier return not found.");

        var supplier = await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sr.SupplierId, cancellationToken);
        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sr.WarehouseId, cancellationToken);
        var itemById = await LoadItemMapAsync(sr.Lines.Select(l => l.ItemId), cancellationToken);

        var total = sr.Lines.Sum(l => l.Quantity * l.UnitCost);
        var meta = new List<(string Label, string Value)>
        {
            ("Supplier", SupplierLabel(supplier, sr.SupplierId)),
            ("Warehouse", WarehouseLabel(warehouse, sr.WarehouseId)),
            ("Return date", sr.ReturnDate.ToString("u")),
            ("Status", sr.Status.ToString()),
            ("Reason", sr.Reason ?? ""),
            ("Total", FormatMoney(total))
        };

        return BuildPdf(
            title: "Supplier Return",
            referenceNumber: sr.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Lines").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit Cost");
                        h.Cell().Element(CellHeader).Text("Batch");
                        h.Cell().Element(CellHeader).Text("Serials");
                    });

                    foreach (var line in sr.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitCost));
                        table.Cell().Element(CellBody).Text(line.BatchNumber ?? "");
                        table.Cell().Element(CellBody).Text(string.Join(", ", line.Serials.Select(s => s.SerialNumber)));
                    }
                });
            },
            fileName: $"SR-{sr.Number}.pdf",
            qrPayload: $"ISS:SR:{sr.Id}",
            barcodePayload: sr.Number);
    }
}
