using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Domain.Service;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ISS.Infrastructure.Documents;

public sealed partial class DocumentPdfService
{
    private async Task<PdfDocument> RenderServiceJobAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await _dbContext.ServiceJobs.AsNoTracking()
                      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");

        var equipment = await _dbContext.EquipmentUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.EquipmentUnitId, cancellationToken);
        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.CustomerId, cancellationToken);
        var equipmentItem = equipment is null
            ? null
            : await _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == equipment.ItemId, cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Customer", CustomerLabel(customer, job.CustomerId)),
            ("Opened at", job.OpenedAt.ToString("u")),
            ("Status", job.Status.ToString()),
            ("Completed at", job.CompletedAt?.ToString("u") ?? ""),
            ("Equipment", equipment is null ? job.EquipmentUnitId.ToString() : $"{ItemLabel(equipmentItem, equipment.ItemId)} / SN: {equipment.SerialNumber}")
        };

        return BuildPdf(
            title: "Service Job",
            referenceNumber: job.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Problem description").SemiBold();
                column.Item().Text(job.ProblemDescription);
            },
            fileName: $"JOB-{job.Number}.pdf",
            qrPayload: $"ISS:JOB:{job.Id}",
            barcodePayload: job.Number);
    }

    private async Task<PdfDocument> RenderWorkOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var wo = await _dbContext.WorkOrders.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                 ?? throw new NotFoundException("Work order not found.");

        var job = await _dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == wo.ServiceJobId, cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Service job", job?.Number ?? wo.ServiceJobId.ToString()),
            ("Status", wo.Status.ToString()),
            ("Assigned to", wo.AssignedToUserId?.ToString() ?? "")
        };

        var shortRef = $"WO-{wo.Id:N}"[..Math.Min(11, $"WO-{wo.Id:N}".Length)];
        return BuildPdf(
            title: "Work Order",
            referenceNumber: shortRef,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Description").SemiBold();
                column.Item().Text(wo.Description);
            },
            fileName: $"WO-{wo.Id:N}.pdf",
            qrPayload: $"ISS:WO:{wo.Id}",
            barcodePayload: wo.Id.ToString("N")[..12].ToUpperInvariant());
    }

    private async Task<PdfDocument> RenderMaterialRequisitionAsync(Guid id, CancellationToken cancellationToken)
    {
        var mr = await _dbContext.MaterialRequisitions.AsNoTracking()
                     .Include(x => x.Lines)
                     .ThenInclude(l => l.Serials)
                     .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                 ?? throw new NotFoundException("Material requisition not found.");

        var job = await _dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == mr.ServiceJobId, cancellationToken);
        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == mr.WarehouseId, cancellationToken);
        var itemById = await LoadItemMapAsync(mr.Lines.Select(l => l.ItemId), cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Service job", job?.Number ?? mr.ServiceJobId.ToString()),
            ("Warehouse", WarehouseLabel(warehouse, mr.WarehouseId)),
            ("Requested at", mr.RequestedAt.ToString("u")),
            ("Status", mr.Status.ToString())
        };

        return BuildPdf(
            title: "Material Requisition",
            referenceNumber: mr.Number,
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

                    foreach (var line in mr.Lines)
                    {
                        var item = itemById.GetValueOrDefault(line.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).Text(line.BatchNumber ?? "");
                        table.Cell().Element(CellBody).Text(string.Join(", ", line.Serials.Select(s => s.SerialNumber)));
                    }
                });
            },
            fileName: $"MR-{mr.Number}.pdf",
            qrPayload: $"ISS:MR:{mr.Id}",
            barcodePayload: mr.Number);
    }

    private async Task<PdfDocument> RenderQualityCheckAsync(Guid id, CancellationToken cancellationToken)
    {
        var qc = await _dbContext.QualityChecks.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                 ?? throw new NotFoundException("Quality check not found.");

        var job = await _dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == qc.ServiceJobId, cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Service job", job?.Number ?? qc.ServiceJobId.ToString()),
            ("Checked at", qc.CheckedAt.ToString("u")),
            ("Passed", qc.Passed ? "Yes" : "No")
        };

        var shortRef = $"QC-{qc.Id:N}"[..Math.Min(11, $"QC-{qc.Id:N}".Length)];
        return BuildPdf(
            title: "Quality Check",
            referenceNumber: shortRef,
            meta: meta,
            content: column =>
            {
                if (!string.IsNullOrWhiteSpace(qc.Notes))
                {
                    column.Item().Text("Notes").SemiBold();
                    column.Item().Text(qc.Notes);
                }
            },
            fileName: $"QC-{qc.Id:N}.pdf",
            qrPayload: $"ISS:QC:{qc.Id}",
            barcodePayload: qc.Id.ToString("N")[..12].ToUpperInvariant());
    }
}
