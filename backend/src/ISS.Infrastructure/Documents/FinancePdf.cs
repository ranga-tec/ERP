using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ISS.Infrastructure.Documents;

public sealed partial class DocumentPdfService
{
    private async Task<PdfDocument> RenderPaymentAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _dbContext.Payments.AsNoTracking()
                          .Include(x => x.Allocations)
                          .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Payment not found.");

        var customer = payment.CounterpartyType == CounterpartyType.Customer
            ? await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == payment.CounterpartyId, cancellationToken)
            : null;
        var supplier = payment.CounterpartyType == CounterpartyType.Supplier
            ? await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == payment.CounterpartyId, cancellationToken)
            : null;

        var meta = new List<(string Label, string Value)>
        {
            ("Direction", payment.Direction.ToString()),
            ("Counterparty", payment.CounterpartyType == CounterpartyType.Customer ? CustomerLabel(customer, payment.CounterpartyId) : SupplierLabel(supplier, payment.CounterpartyId)),
            ("Paid at", payment.PaidAt.ToString("u")),
            ("Amount", FormatMoney(payment.Amount)),
            ("Notes", payment.Notes ?? "")
        };

        var arIds = payment.Allocations.Where(a => a.AccountsReceivableEntryId != null).Select(a => a.AccountsReceivableEntryId!.Value).Distinct().ToList();
        var apIds = payment.Allocations.Where(a => a.AccountsPayableEntryId != null).Select(a => a.AccountsPayableEntryId!.Value).Distinct().ToList();

        var arById = arIds.Count == 0
            ? new Dictionary<Guid, AccountsReceivableEntry>()
            : await _dbContext.AccountsReceivableEntries.AsNoTracking()
                .Where(x => arIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        var apById = apIds.Count == 0
            ? new Dictionary<Guid, AccountsPayableEntry>()
            : await _dbContext.AccountsPayableEntries.AsNoTracking()
                .Where(x => apIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        return BuildPdf(
            title: "Payment",
            referenceNumber: payment.ReferenceNumber,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Allocations").SemiBold();
                if (payment.Allocations.Count == 0)
                {
                    column.Item().Text("No allocations.");
                    return;
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Type");
                        h.Cell().Element(CellHeader).Text("Reference");
                        h.Cell().Element(CellHeader).AlignRight().Text("Amount");
                    });

                    foreach (var a in payment.Allocations)
                    {
                        if (a.AccountsReceivableEntryId is { } arId && arById.TryGetValue(arId, out var ar))
                        {
                            table.Cell().Element(CellBody).Text("AR");
                            table.Cell().Element(CellBody).Text($"{ar.ReferenceType} {ar.ReferenceId}");
                            table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(a.Amount));
                        }
                        else if (a.AccountsPayableEntryId is { } apId && apById.TryGetValue(apId, out var ap))
                        {
                            table.Cell().Element(CellBody).Text("AP");
                            table.Cell().Element(CellBody).Text($"{ap.ReferenceType} {ap.ReferenceId}");
                            table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(a.Amount));
                        }
                    }
                });
            },
            fileName: $"PAY-{payment.ReferenceNumber}.pdf",
            qrPayload: $"ISS:PAY:{payment.Id}",
            barcodePayload: payment.ReferenceNumber);
    }

    private async Task<PdfDocument> RenderCreditNoteAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await _dbContext.CreditNotes.AsNoTracking()
                       .Include(x => x.Allocations)
                       .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Credit note not found.");

        var customer = note.CounterpartyType == CounterpartyType.Customer
            ? await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == note.CounterpartyId, cancellationToken)
            : null;
        var supplier = note.CounterpartyType == CounterpartyType.Supplier
            ? await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == note.CounterpartyId, cancellationToken)
            : null;

        var meta = new List<(string Label, string Value)>
        {
            ("Counterparty", note.CounterpartyType == CounterpartyType.Customer ? CustomerLabel(customer, note.CounterpartyId) : SupplierLabel(supplier, note.CounterpartyId)),
            ("Issued at", note.IssuedAt.ToString("u")),
            ("Amount", FormatMoney(note.Amount)),
            ("Remaining", FormatMoney(note.RemainingAmount)),
            ("Notes", note.Notes ?? ""),
            ("Source", string.IsNullOrWhiteSpace(note.SourceReferenceType) ? "" : $"{note.SourceReferenceType} {note.SourceReferenceId}")
        };

        return BuildPdf(
            title: "Credit Note",
            referenceNumber: note.ReferenceNumber,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Allocations").SemiBold();
                if (note.Allocations.Count == 0)
                {
                    column.Item().Text("No allocations.");
                    return;
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Type");
                        h.Cell().Element(CellHeader).Text("Entry Id");
                        h.Cell().Element(CellHeader).AlignRight().Text("Amount");
                    });

                    foreach (var a in note.Allocations)
                    {
                        var type = a.AccountsReceivableEntryId is not null ? "AR" : "AP";
                        var entryId = a.AccountsReceivableEntryId?.ToString() ?? a.AccountsPayableEntryId?.ToString() ?? "";
                        table.Cell().Element(CellBody).Text(type);
                        table.Cell().Element(CellBody).Text(entryId);
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(a.Amount));
                    }
                });
            },
            fileName: $"CN-{note.ReferenceNumber}.pdf",
            qrPayload: $"ISS:CN:{note.Id}",
            barcodePayload: note.ReferenceNumber);
    }

    private async Task<PdfDocument> RenderDebitNoteAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await _dbContext.DebitNotes.AsNoTracking()
                       .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Debit note not found.");

        var customer = note.CounterpartyType == CounterpartyType.Customer
            ? await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == note.CounterpartyId, cancellationToken)
            : null;
        var supplier = note.CounterpartyType == CounterpartyType.Supplier
            ? await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == note.CounterpartyId, cancellationToken)
            : null;

        var meta = new List<(string Label, string Value)>
        {
            ("Counterparty", note.CounterpartyType == CounterpartyType.Customer ? CustomerLabel(customer, note.CounterpartyId) : SupplierLabel(supplier, note.CounterpartyId)),
            ("Issued at", note.IssuedAt.ToString("u")),
            ("Amount", FormatMoney(note.Amount)),
            ("Notes", note.Notes ?? ""),
            ("Source", string.IsNullOrWhiteSpace(note.SourceReferenceType) ? "" : $"{note.SourceReferenceType} {note.SourceReferenceId}")
        };

        return BuildPdf(
            title: "Debit Note",
            referenceNumber: note.ReferenceNumber,
            meta: meta,
            content: _ => { },
            fileName: $"DBN-{note.ReferenceNumber}.pdf",
            qrPayload: $"ISS:DBN:{note.Id}",
            barcodePayload: note.ReferenceNumber);
    }

    private async Task<PdfDocument> RenderSupplierInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.SupplierInvoices.AsNoTracking()
                          .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Supplier invoice not found.");

        var supplier = await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoice.SupplierId, cancellationToken);

        string links = "";
        if (invoice.PurchaseOrderId is { } poId)
        {
            var po = await _dbContext.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == poId, cancellationToken);
            links += $"PO:{po?.Number ?? poId.ToString()} ";
        }

        if (invoice.GoodsReceiptId is { } grnId)
        {
            var grn = await _dbContext.GoodsReceipts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == grnId, cancellationToken);
            links += $"GRN:{grn?.Number ?? grnId.ToString()} ";
        }

        if (invoice.DirectPurchaseId is { } dpId)
        {
            var dp = await _dbContext.DirectPurchases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dpId, cancellationToken);
            links += $"DP:{dp?.Number ?? dpId.ToString()}";
        }

        var meta = new List<(string Label, string Value)>
        {
            ("Supplier", SupplierLabel(supplier, invoice.SupplierId)),
            ("Doc No", invoice.Number),
            ("Supplier Invoice No", invoice.InvoiceNumber),
            ("Invoice date", invoice.InvoiceDate.ToString("u")),
            ("Due date", invoice.DueDate?.ToString("u") ?? ""),
            ("Status", invoice.Status.ToString()),
            ("Links", links.Trim()),
            ("Subtotal", FormatMoney(invoice.Subtotal)),
            ("Discount", FormatMoney(invoice.DiscountAmount)),
            ("Tax", FormatMoney(invoice.TaxAmount)),
            ("Freight", FormatMoney(invoice.FreightAmount)),
            ("Rounding", FormatMoney(invoice.RoundingAmount)),
            ("Grand total", FormatMoney(invoice.GrandTotal)),
            ("Notes", invoice.Notes ?? "")
        };

        return BuildPdf(
            title: "Supplier Invoice",
            referenceNumber: invoice.Number,
            meta: meta,
            content: _ => { },
            fileName: $"SINV-{invoice.Number}.pdf",
            qrPayload: $"ISS:SINV:{invoice.Id}",
            barcodePayload: invoice.Number);
    }
}
