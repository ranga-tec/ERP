using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ISS.Infrastructure.Documents;

public sealed partial class DocumentPdfService
{
    private async Task<PdfDocument> RenderStockAdjustmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var adj = await _dbContext.StockAdjustments.AsNoTracking()
                      .Include(x => x.Lines)
                      .ThenInclude(l => l.Serials)
                      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                  ?? throw new NotFoundException("Stock adjustment not found.");

        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == adj.WarehouseId, cancellationToken);
        var itemById = await LoadItemMapAsync(adj.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Warehouse", WarehouseLabel(warehouse, adj.WarehouseId)),
            ("Adjusted at", adj.AdjustedAt.ToString("u")),
            ("Status", adj.Status.ToString()),
            ("Reason", adj.Reason ?? "")
        };

        return BuildPdf(
            title: "Stock Adjustment",
            referenceNumber: adj.Number,
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
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty Delta");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit Cost");
                        h.Cell().Element(CellHeader).Text("Batch");
                        h.Cell().Element(CellHeader).Text("Serials");
                    });

                    foreach (var line in adj.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.QuantityDelta));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitCost));
                        table.Cell().Element(CellBody).Text(line.BatchNumber ?? "");
                        table.Cell().Element(CellBody).Text(string.Join(", ", line.Serials.Select(s => s.SerialNumber)));
                    }
                });
            },
            fileName: $"ADJ-{adj.Number}.pdf",
            qrPayload: $"ISS:ADJ:{adj.Id}",
            barcodePayload: adj.Number);
    }

    private async Task<PdfDocument> RenderStockTransferAsync(Guid id, CancellationToken cancellationToken)
    {
        var trf = await _dbContext.StockTransfers.AsNoTracking()
                     .Include(x => x.Lines)
                     .ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                 ?? throw new NotFoundException("Stock transfer not found.");

        var fromWarehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == trf.FromWarehouseId, cancellationToken);
        var toWarehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == trf.ToWarehouseId, cancellationToken);
        var itemById = await LoadItemMapAsync(trf.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("From", WarehouseLabel(fromWarehouse, trf.FromWarehouseId)),
            ("To", WarehouseLabel(toWarehouse, trf.ToWarehouseId)),
            ("Transfer date", trf.TransferDate.ToString("u")),
            ("Status", trf.Status.ToString())
        };

        return BuildPdf(
            title: "Stock Transfer",
            referenceNumber: trf.Number,
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

                    foreach (var line in trf.Lines)
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
            fileName: $"TRF-{trf.Number}.pdf",
            qrPayload: $"ISS:TRF:{trf.Id}",
            barcodePayload: trf.Number);
    }
}
