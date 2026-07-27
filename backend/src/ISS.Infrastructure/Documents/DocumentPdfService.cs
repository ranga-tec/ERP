using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.MasterData;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace ISS.Infrastructure.Documents;

public sealed partial class DocumentPdfService(IIssDbContext dbContext) : IDocumentPdfService
{
    private readonly IIssDbContext _dbContext = dbContext;

    public async Task<PdfDocument> RenderAsync(PdfDocumentType documentType, Guid id, CancellationToken cancellationToken = default)
    {
        return documentType switch
        {
            PdfDocumentType.RequestForQuote => await RenderRfqAsync(id, cancellationToken),
            PdfDocumentType.PurchaseOrder => await RenderPurchaseOrderAsync(id, cancellationToken),
            PdfDocumentType.GoodsReceipt => await RenderGoodsReceiptAsync(id, cancellationToken),
            PdfDocumentType.SupplierReturn => await RenderSupplierReturnAsync(id, cancellationToken),
            PdfDocumentType.SalesQuote => await RenderSalesQuoteAsync(id, cancellationToken),
            PdfDocumentType.SalesOrder => await RenderSalesOrderAsync(id, cancellationToken),
            PdfDocumentType.DispatchNote => await RenderDispatchNoteAsync(id, cancellationToken),
            PdfDocumentType.SalesInvoice => await RenderSalesInvoiceAsync(id, cancellationToken),
            PdfDocumentType.ServiceJob => await RenderServiceJobAsync(id, cancellationToken),
            PdfDocumentType.WorkOrder => await RenderWorkOrderAsync(id, cancellationToken),
            PdfDocumentType.MaterialRequisition => await RenderMaterialRequisitionAsync(id, cancellationToken),
            PdfDocumentType.QualityCheck => await RenderQualityCheckAsync(id, cancellationToken),
            PdfDocumentType.StockAdjustment => await RenderStockAdjustmentAsync(id, cancellationToken),
            PdfDocumentType.StockTransfer => await RenderStockTransferAsync(id, cancellationToken),
            PdfDocumentType.Payment => await RenderPaymentAsync(id, cancellationToken),
            PdfDocumentType.CreditNote => await RenderCreditNoteAsync(id, cancellationToken),
            PdfDocumentType.DebitNote => await RenderDebitNoteAsync(id, cancellationToken),
            PdfDocumentType.ItemLabel => await RenderItemLabelAsync(id, cancellationToken),
            PdfDocumentType.DirectPurchase => await RenderDirectPurchaseAsync(id, cancellationToken),
            PdfDocumentType.SupplierInvoice => await RenderSupplierInvoiceAsync(id, cancellationToken),
            PdfDocumentType.DirectDispatch => await RenderDirectDispatchAsync(id, cancellationToken),
            PdfDocumentType.CustomerReturn => await RenderCustomerReturnAsync(id, cancellationToken),
            PdfDocumentType.ServiceEstimate => await RenderServiceEstimateAsync(id, cancellationToken),
            PdfDocumentType.ServiceHandover => await RenderServiceHandoverAsync(id, cancellationToken),
            PdfDocumentType.ServiceExpenseClaim => await RenderServiceExpenseClaimAsync(id, cancellationToken),
            PdfDocumentType.ServiceJobDailySheet => await RenderServiceJobDailySheetAsync(id, cancellationToken),
            _ => throw new DomainValidationException("Unsupported PDF document type.")
        };
    }

    private static PdfDocument BuildPdf(
        string title,
        string referenceNumber,
        IReadOnlyList<(string Label, string Value)> meta,
        Action<ColumnDescriptor> content,
        string fileName,
        string qrPayload,
        string barcodePayload)
    {
        var qr = QrPngBytes(qrPayload);
        var barcode = BarcodePngBytes(barcodePayload, width: 520, height: 120);

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Element(Letterhead);

                    header.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(title).FontSize(17).SemiBold().FontColor(DocumentBranding.Ink);
                            col.Item().Text(referenceNumber).FontSize(11).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(10).Element(c => MetaTable(c, meta));
                        });

                        row.ConstantItem(170).Column(col =>
                        {
                            col.Item().AlignRight().Width(110).Height(110).Image(qr, ImageScaling.FitArea);
                            col.Item().PaddingTop(8).AlignRight().Height(45).Image(barcode, ImageScaling.FitWidth);
                        });
                    });
                });

                page.Content().PaddingTop(16).Column(content);

                page.Footer().Element(Footer);
            });
        }).GeneratePdf();

        return new PdfDocument(fileName, pdfBytes);
    }

    /// <summary>Company logo on the left, registered contact details on the right, closed by a brand rule.</summary>
    private static void Letterhead(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(150).Height(52).AlignLeft().AlignMiddle()
                    .Image(DocumentBranding.Logo, ImageScaling.FitArea);

                row.RelativeItem().PaddingLeft(14).AlignRight().AlignMiddle().Column(info =>
                {
                    info.Item().AlignRight().Text(DocumentBranding.CompanyName)
                        .FontSize(12).SemiBold().FontColor(DocumentBranding.Ink);
                    info.Item().AlignRight().Text(DocumentBranding.AddressLine)
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    info.Item().AlignRight().Text(DocumentBranding.ContactLine)
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(DocumentBranding.Accent);
        });
    }

    /// <summary>Repeated on every page: brand rule, contact strip, then generation stamp and page numbers.</summary>
    private static void Footer(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(4).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            col.Item().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                t.Span(DocumentBranding.CompanyName).SemiBold().FontColor(DocumentBranding.Ink);
                t.Span($"  ·  {DocumentBranding.AddressLine}  ·  {DocumentBranding.ContactLine}");
            });

            col.Item().PaddingTop(2).AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                t.Span("Generated ");
                t.Span(DateTimeOffset.UtcNow.ToString("u"));
                t.Span("  ·  Page ");
                t.CurrentPageNumber();
                t.Span(" of ");
                t.TotalPages();
            });
        });
    }

    private static void MetaTable(IContainer container, IReadOnlyList<(string Label, string Value)> meta)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(110);
                cols.RelativeColumn();
            });

            foreach (var (label, value) in meta)
            {
                table.Cell().Element(CellMeta).Text(label);
                table.Cell().Element(CellMeta).Text(value);
            }
        });
    }

    private static IContainer CellMeta(IContainer container) =>
        container.PaddingBottom(2).DefaultTextStyle(x => x.FontSize(9));

    // PaddingRight keeps a right-aligned column (Qty, Unit Cost) from touching the
    // left-aligned one beside it, which printed headers like "Unit CostBatch".
    private static IContainer CellHeader(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingRight(6)
            .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.Grey.Darken2).FontSize(9));

    private static IContainer CellBody(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten4)
            .PaddingVertical(3)
            .PaddingRight(6)
            .DefaultTextStyle(x => x.FontSize(9));

    private static string SupplierLabel(Supplier? supplier, Guid fallbackId)
        => supplier is null ? fallbackId.ToString() : $"{supplier.Code} — {supplier.Name}";

    private static string CustomerLabel(Customer? customer, Guid fallbackId)
        => customer is null ? fallbackId.ToString() : $"{customer.Code} — {customer.Name}";

    private static string WarehouseLabel(Warehouse? warehouse, Guid fallbackId)
        => warehouse is null ? fallbackId.ToString() : $"{warehouse.Code} — {warehouse.Name}";

    private static string ItemLabel(ISS.Domain.MasterData.Item? item, Guid fallbackId)
        => item is null ? fallbackId.ToString() : $"{item.Sku} — {item.Name}";

    private static string FormatMoney(decimal amount) => amount.ToString("0.00");
    private static string FormatQty(decimal qty) => qty.ToString("0.####");

    /// <summary>
    /// Quantity with the item's unit of measure, so a document reads "12 L" rather than a bare
    /// number. Falls back to the plain quantity when the item is unknown.
    /// </summary>
    private static string FormatQty(decimal qty, ISS.Domain.MasterData.Item? item)
        => item is null || string.IsNullOrWhiteSpace(item.UnitOfMeasure)
            ? FormatQty(qty)
            : $"{FormatQty(qty)} {item.UnitOfMeasure}";
    private static string FormatPercent(decimal percent) => percent.ToString("0.##");

    private static byte[] QrPngBytes(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(pixelsPerModule: 8);
    }

    private static byte[] BarcodePngBytes(string payload, int width, int height)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Height = height,
                Width = width,
                Margin = 2,
                PureBarcode = true
            }
        };

        var pixelData = writer.Write(payload);
        return EncodePng(pixelData);
    }

    private static byte[] EncodePng(PixelData pixelData)
    {
        var info = new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        Marshal.Copy(pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encoded.ToArray();
    }

    private async Task<Dictionary<Guid, ISS.Domain.MasterData.Item>> LoadItemMapAsync(IEnumerable<Guid> itemIds, CancellationToken cancellationToken)
    {
        var ids = itemIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, ISS.Domain.MasterData.Item>();
        }

        return await _dbContext.Items.AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i, cancellationToken);
    }
}
