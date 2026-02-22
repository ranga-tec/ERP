using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ISS.Infrastructure.Documents;

public sealed partial class DocumentPdfService
{
    private async Task<PdfDocument> RenderSalesQuoteAsync(Guid id, CancellationToken cancellationToken)
    {
        var quote = await _dbContext.SalesQuotes.AsNoTracking()
                        .Include(x => x.Lines)
                        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw new NotFoundException("Sales quote not found.");

        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == quote.CustomerId, cancellationToken);
        var itemById = await LoadItemMapAsync(quote.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Customer", CustomerLabel(customer, quote.CustomerId)),
            ("Quote date", quote.QuoteDate.ToString("u")),
            ("Valid until", quote.ValidUntil?.ToString("u") ?? ""),
            ("Status", quote.Status.ToString()),
            ("Total", FormatMoney(quote.Total))
        };

        return BuildPdf(
            title: "Sales Quotation",
            referenceNumber: quote.Number,
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
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit Price");
                        h.Cell().Element(CellHeader).AlignRight().Text("Line Total");
                    });

                    foreach (var line in quote.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitPrice));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.LineTotal));
                    }
                });
            },
            fileName: $"SQ-{quote.Number}.pdf",
            qrPayload: $"ISS:SQ:{quote.Id}",
            barcodePayload: quote.Number);
    }

    private async Task<PdfDocument> RenderSalesOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.SalesOrders.AsNoTracking()
                        .Include(x => x.Lines)
                        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw new NotFoundException("Sales order not found.");

        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.CustomerId, cancellationToken);
        var itemById = await LoadItemMapAsync(order.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Customer", CustomerLabel(customer, order.CustomerId)),
            ("Order date", order.OrderDate.ToString("u")),
            ("Status", order.Status.ToString()),
            ("Total", FormatMoney(order.Total))
        };

        return BuildPdf(
            title: "Sales Order",
            referenceNumber: order.Number,
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
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit Price");
                        h.Cell().Element(CellHeader).AlignRight().Text("Line Total");
                    });

                    foreach (var line in order.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitPrice));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.LineTotal));
                    }
                });
            },
            fileName: $"SO-{order.Number}.pdf",
            qrPayload: $"ISS:SO:{order.Id}",
            barcodePayload: order.Number);
    }

    private async Task<PdfDocument> RenderDispatchNoteAsync(Guid id, CancellationToken cancellationToken)
    {
        var dispatch = await _dbContext.DispatchNotes.AsNoTracking()
                           .Include(x => x.Lines)
                           .ThenInclude(l => l.Serials)
                           .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                       ?? throw new NotFoundException("Dispatch note not found.");

        var order = await _dbContext.SalesOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dispatch.SalesOrderId, cancellationToken);
        var customer = order is null
            ? null
            : await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.CustomerId, cancellationToken);
        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dispatch.WarehouseId, cancellationToken);
        var itemById = await LoadItemMapAsync(dispatch.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Customer", order is null ? dispatch.SalesOrderId.ToString() : CustomerLabel(customer, order.CustomerId)),
            ("Order", order?.Number ?? dispatch.SalesOrderId.ToString()),
            ("Warehouse", WarehouseLabel(warehouse, dispatch.WarehouseId)),
            ("Dispatched at", dispatch.DispatchedAt.ToString("u")),
            ("Status", dispatch.Status.ToString())
        };

        return BuildPdf(
            title: "Dispatch Note",
            referenceNumber: dispatch.Number,
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
                        cols.RelativeColumn(4);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).Text("Batch");
                        h.Cell().Element(CellHeader).Text("Serials");
                    });

                    foreach (var line in dispatch.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).Text(line.BatchNumber ?? "");
                        table.Cell().Element(CellBody).Text(string.Join(", ", line.Serials.Select(s => s.SerialNumber)));
                    }
                });
            },
            fileName: $"DN-{dispatch.Number}.pdf",
            qrPayload: $"ISS:DN:{dispatch.Id}",
            barcodePayload: dispatch.Number);
    }

    private async Task<PdfDocument> RenderSalesInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.SalesInvoices.AsNoTracking()
                         .Include(x => x.Lines)
                         .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Invoice not found.");

        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoice.CustomerId, cancellationToken);
        var itemById = await LoadItemMapAsync(invoice.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Customer", CustomerLabel(customer, invoice.CustomerId)),
            ("Invoice date", invoice.InvoiceDate.ToString("u")),
            ("Due date", invoice.DueDate?.ToString("u") ?? ""),
            ("Status", invoice.Status.ToString()),
            ("Subtotal", FormatMoney(invoice.Subtotal)),
            ("Tax total", FormatMoney(invoice.TaxTotal)),
            ("Total", FormatMoney(invoice.Total))
        };

        return BuildPdf(
            title: "Sales Invoice",
            referenceNumber: invoice.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Lines").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit Price");
                        h.Cell().Element(CellHeader).AlignRight().Text("Discount %");
                        h.Cell().Element(CellHeader).AlignRight().Text("Tax %");
                        h.Cell().Element(CellHeader).AlignRight().Text("Line Total");
                    });

                    foreach (var line in invoice.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitPrice));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatPercent(line.DiscountPercent));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatPercent(line.TaxPercent));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.LineTotal));
                    }
                });
            },
            fileName: $"INV-{invoice.Number}.pdf",
            qrPayload: $"ISS:INV:{invoice.Id}",
            barcodePayload: invoice.Number);
    }
}
