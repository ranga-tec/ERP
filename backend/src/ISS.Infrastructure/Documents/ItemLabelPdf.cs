using ISS.Application.Abstractions;
using ISS.Application.Common;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ISS.Infrastructure.Documents;

public sealed partial class DocumentPdfService
{
    private async Task<PdfDocument> RenderItemLabelAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Item not found.");

        var qr = QrPngBytes($"ISS:ITEM:{item.Id}");
        var barcode = string.IsNullOrWhiteSpace(item.Barcode) ? null : BarcodePngBytes(item.Barcode.Trim(), width: 420, height: 110);

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(12));
                page.Content().Column(col =>
                {
                    col.Item().Text("Item Label").FontSize(18).SemiBold();
                    col.Item().Text($"{item.Sku} — {item.Name}");
                    col.Item().PaddingTop(12).Row(row =>
                    {
                        row.ConstantItem(160).Height(160).Image(qr, ImageScaling.FitArea);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"SKU: {item.Sku}");
                            c.Item().Text($"UOM: {item.UnitOfMeasure}");
                            if (!string.IsNullOrWhiteSpace(item.Barcode))
                            {
                                c.Item().PaddingTop(8).Text($"Barcode: {item.Barcode}");
                            }
                        });
                    });

                    if (barcode is not null)
                    {
                        col.Item().PaddingTop(16).Height(110).Image(barcode, ImageScaling.FitArea);
                    }
                });
            });
        }).GeneratePdf();

        return new PdfDocument($"LABEL-{item.Sku}.pdf", pdfBytes);
    }
}
