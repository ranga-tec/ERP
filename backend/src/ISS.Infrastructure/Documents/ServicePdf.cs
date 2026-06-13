using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Domain.Finance;
using ISS.Domain.Service;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ISS.Infrastructure.Documents;

public sealed partial class DocumentPdfService
{
    private async Task<PdfDocument> RenderServiceJobDailySheetAsync(Guid id, CancellationToken cancellationToken)
    {
        var sheet = await _dbContext.ServiceJobDailySheets.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw new NotFoundException("Service job daily sheet not found.");

        var job = await _dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sheet.ServiceJobId, cancellationToken)
                  ?? throw new NotFoundException("Service job not found.");
        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.CustomerId, cancellationToken);
        var equipment = await _dbContext.EquipmentUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.EquipmentUnitId, cancellationToken);
        var equipmentItem = equipment is null
            ? null
            : await _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == equipment.ItemId, cancellationToken);

        var assignments = await _dbContext.ServiceJobAssignments.AsNoTracking()
            .Where(x => x.ServiceJobDailySheetId == sheet.Id)
            .OrderBy(x => x.AssignedDate)
            .ThenBy(x => x.EmployeeName)
            .ToListAsync(cancellationToken);
        var progressUpdates = await _dbContext.ServiceJobProgressUpdates.AsNoTracking()
            .Where(x => x.ServiceJobDailySheetId == sheet.Id)
            .OrderBy(x => x.ProgressDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var materialRequisitions = await _dbContext.MaterialRequisitions.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(x => x.Serials)
            .Where(x => x.ServiceJobDailySheetId == sheet.Id)
            .OrderBy(x => x.RequestedAt)
            .ToListAsync(cancellationToken);
        var materialDispositions = await _dbContext.ServiceJobMaterialDispositions.AsNoTracking()
            .Include(x => x.Serials)
            .Where(x => x.ServiceJobDailySheetId == sheet.Id && !x.IsVoided)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var expenseClaims = await _dbContext.ServiceExpenseClaims.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.ServiceJobDailySheetId == sheet.Id)
            .OrderBy(x => x.ExpenseDate)
            .ToListAsync(cancellationToken);
        var pettyCashIous = await _dbContext.PettyCashIous.AsNoTracking()
            .Where(x => x.ServiceJobDailySheetId == sheet.Id)
            .OrderBy(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

        var itemIds = materialRequisitions.SelectMany(x => x.Lines.Select(line => line.ItemId))
            .Concat(materialDispositions.Select(x => x.ItemId))
            .Concat(expenseClaims.SelectMany(x => x.Lines.Where(line => line.ItemId.HasValue).Select(line => line.ItemId!.Value)))
            .ToList();
        var itemById = await LoadItemMapAsync(itemIds, cancellationToken);

        var warehouseIds = materialRequisitions.Select(x => x.WarehouseId)
            .Concat(materialDispositions.Select(x => x.WarehouseId))
            .Distinct()
            .ToList();
        var warehouseById = warehouseIds.Count == 0
            ? new Dictionary<Guid, ISS.Domain.MasterData.Warehouse>()
            : await _dbContext.Warehouses.AsNoTracking()
                .Where(x => warehouseIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        var totalNormalHours = assignments.Sum(x => x.NormalHours);
        var totalOvertimeHours = assignments.Sum(x => x.OvertimeHours);
        var materialLineCount = materialRequisitions.Sum(x => x.Lines.Count);
        var expenseTotal = expenseClaims.Sum(x => x.Total);
        var iouTotal = pettyCashIous.Sum(x => x.Amount);

        var meta = new List<(string Label, string Value)>
        {
            ("Service job", job.Number),
            ("Customer", CustomerLabel(customer, job.CustomerId)),
            ("Sheet date", sheet.SheetDate.ToString("u")),
            ("Prepared by", sheet.PreparedByName),
            ("Status", sheet.Status.ToString()),
            ("Site", sheet.SiteLocation ?? job.SiteLocation ?? ""),
            ("Shift", sheet.ShiftName ?? ""),
            ("Weather / condition", sheet.WeatherOrSiteCondition ?? ""),
            ("Staff / hours", $"{assignments.Count} staff / {FormatQty(totalNormalHours)} normal / {FormatQty(totalOvertimeHours)} OT"),
            ("Materials / cash", $"{materialLineCount} material lines / IOU {FormatMoney(iouTotal)} / Expenses {FormatMoney(expenseTotal)}")
        };

        if (equipment is not null)
        {
            meta.Add(("Equipment", $"{ItemLabel(equipmentItem, equipment.ItemId)} / SN: {equipment.SerialNumber}"));
        }

        return BuildPdf(
            title: "Daily Service Report",
            referenceNumber: sheet.Number,
            meta: meta,
            content: column =>
            {
                AddNarrativeSection(column, "Work planned", sheet.WorkPlanned);
                AddNarrativeSection(column, "Work completed", sheet.WorkCompleted);
                AddNarrativeSection(column, "Pending work", sheet.WorkPending);
                AddNarrativeSection(column, "Problems found", sheet.ProblemsFound);
                AddNarrativeSection(column, "Customer instructions", sheet.CustomerInstructions);
                AddNarrativeSection(column, "Technician notes", sheet.TechnicianNotes);
                AddNarrativeSection(column, "Supervisor notes", sheet.SupervisorNotes);

                column.Item().PaddingTop(10).Text("Staff / labor").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2.5f);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(1.4f);
                        cols.RelativeColumn(1.4f);
                        cols.RelativeColumn(1.8f);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Employee");
                        h.Cell().Element(CellHeader).Text("Role");
                        h.Cell().Element(CellHeader).Text("Task");
                        h.Cell().Element(CellHeader).AlignRight().Text("Normal");
                        h.Cell().Element(CellHeader).AlignRight().Text("OT");
                        h.Cell().Element(CellHeader).Text("Status");
                    });

                    foreach (var assignment in assignments)
                    {
                        table.Cell().Element(CellBody).Text(assignment.EmployeeName);
                        table.Cell().Element(CellBody).Text(assignment.Role);
                        table.Cell().Element(CellBody).Text(assignment.AssignedTask);
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(assignment.NormalHours));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(assignment.OvertimeHours));
                        table.Cell().Element(CellBody).Text(assignment.ApprovalStatus.ToString());
                    }

                    if (assignments.Count == 0)
                    {
                        table.Cell().ColumnSpan(6).Element(CellBody).Text("No staff or labor recorded.");
                    }
                });

                column.Item().PaddingTop(10).Text("Progress updates").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(4);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Date");
                        h.Cell().Element(CellHeader).Text("Completed");
                        h.Cell().Element(CellHeader).Text("Pending");
                        h.Cell().Element(CellHeader).Text("Issues / notes");
                    });

                    foreach (var update in progressUpdates)
                    {
                        var notes = string.Join(" / ", new[] { update.ProblemsFound, update.SiteIssues, update.TechnicianNotes }
                            .Where(x => !string.IsNullOrWhiteSpace(x)));
                        table.Cell().Element(CellBody).Text(update.ProgressDate.ToString("yyyy-MM-dd HH:mm"));
                        table.Cell().Element(CellBody).Text(update.WorkCompleted);
                        table.Cell().Element(CellBody).Text(update.WorkPending ?? "-");
                        table.Cell().Element(CellBody).Text(string.IsNullOrWhiteSpace(notes) ? "-" : notes);
                    }

                    if (progressUpdates.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).Element(CellBody).Text("No progress updates recorded.");
                    }
                });

                column.Item().PaddingTop(10).Text("Material issues").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(1.4f);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("MRN");
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).Text("Warehouse");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).Text("Batch");
                        h.Cell().Element(CellHeader).Text("Status");
                    });

                    var renderedLines = 0;
                    foreach (var requisition in materialRequisitions)
                    {
                        foreach (var line in requisition.Lines)
                        {
                            renderedLines++;
                            var item = itemById.GetValueOrDefault(line.ItemId);
                            var warehouse = warehouseById.GetValueOrDefault(requisition.WarehouseId);
                            table.Cell().Element(CellBody).Text(requisition.Number);
                            table.Cell().Element(CellBody).Text(ItemLabel(item, line.ItemId));
                            table.Cell().Element(CellBody).Text(warehouse?.Code ?? requisition.WarehouseId.ToString());
                            table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                            table.Cell().Element(CellBody).Text(line.BatchNumber ?? "");
                            table.Cell().Element(CellBody).Text(requisition.Status.ToString());
                        }
                    }

                    if (renderedLines == 0)
                    {
                        table.Cell().ColumnSpan(6).Element(CellBody).Text("No material issues recorded.");
                    }
                });

                column.Item().PaddingTop(10).Text("Returns / damage / supplier rejection").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(1.5f);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).Text("Kind");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).Text("Charge to");
                        h.Cell().Element(CellHeader).Text("Status");
                        h.Cell().Element(CellHeader).Text("Reason");
                    });

                    foreach (var disposition in materialDispositions)
                    {
                        var item = itemById.GetValueOrDefault(disposition.ItemId);
                        table.Cell().Element(CellBody).Text(ItemLabel(item, disposition.ItemId));
                        table.Cell().Element(CellBody).Text(disposition.Kind.ToString());
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(disposition.Quantity));
                        table.Cell().Element(CellBody).Text(disposition.ChargeTo.ToString());
                        table.Cell().Element(CellBody).Text(disposition.Status.ToString());
                        table.Cell().Element(CellBody).Text(disposition.Reason);
                    }

                    if (materialDispositions.Count == 0)
                    {
                        table.Cell().ColumnSpan(6).Element(CellBody).Text("No material returns, damage, or rejection records.");
                    }
                });

                column.Item().PaddingTop(10).Text("IOU / advances").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2.5f);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(1.8f);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("IOU");
                        h.Cell().Element(CellHeader).Text("Requested by");
                        h.Cell().Element(CellHeader).Text("Purpose");
                        h.Cell().Element(CellHeader).AlignRight().Text("Amount");
                        h.Cell().Element(CellHeader).Text("Status");
                    });

                    foreach (var iou in pettyCashIous)
                    {
                        table.Cell().Element(CellBody).Text(iou.Number);
                        table.Cell().Element(CellBody).Text(iou.RequestedByName);
                        table.Cell().Element(CellBody).Text(iou.Purpose);
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(iou.Amount));
                        table.Cell().Element(CellBody).Text(iou.Status == PettyCashIouStatus.Released ? "Cash Released" : iou.Status.ToString());
                    }

                    if (pettyCashIous.Count == 0)
                    {
                        table.Cell().ColumnSpan(5).Element(CellBody).Text("No IOU or advance records.");
                    }
                });

                column.Item().PaddingTop(10).Text("Expense claims").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2.5f);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(1.8f);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Claim");
                        h.Cell().Element(CellHeader).Text("Claimed by");
                        h.Cell().Element(CellHeader).Text("Funding");
                        h.Cell().Element(CellHeader).Text("Merchant");
                        h.Cell().Element(CellHeader).AlignRight().Text("Total");
                        h.Cell().Element(CellHeader).Text("Status");
                    });

                    foreach (var claim in expenseClaims)
                    {
                        table.Cell().Element(CellBody).Text(claim.Number);
                        table.Cell().Element(CellBody).Text(claim.ClaimedByName);
                        table.Cell().Element(CellBody).Text(claim.FundingSource.ToString());
                        table.Cell().Element(CellBody).Text(claim.MerchantName ?? "-");
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(claim.Total));
                        table.Cell().Element(CellBody).Text(claim.Status.ToString());
                    }

                    if (expenseClaims.Count == 0)
                    {
                        table.Cell().ColumnSpan(6).Element(CellBody).Text("No expense claims recorded.");
                    }
                });
            },
            fileName: $"SJDS-{sheet.Number}.pdf",
            qrPayload: $"ISS:SJDS:{sheet.Id}",
            barcodePayload: sheet.Number);
    }

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
            ("Kind", job.Kind.ToString()),
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
                    .Include(x => x.TimeEntries)
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                 ?? throw new NotFoundException("Work order not found.");

        var job = await _dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == wo.ServiceJobId, cancellationToken);
        var approvedHours = wo.TimeEntries
            .Where(x => x.Status is WorkOrderTimeEntryStatus.Approved or WorkOrderTimeEntryStatus.Invoiced)
            .Sum(x => x.HoursWorked);
        var approvedLaborCost = wo.TimeEntries
            .Where(x => x.Status is WorkOrderTimeEntryStatus.Approved or WorkOrderTimeEntryStatus.Invoiced)
            .Sum(x => x.LaborCost);
        var pendingLaborCost = wo.TimeEntries
            .Where(x => x.Status == WorkOrderTimeEntryStatus.Submitted)
            .Sum(x => x.LaborCost);
        var billableApprovedAmount = wo.TimeEntries
            .Where(x => x.BillableToCustomer && (x.Status is WorkOrderTimeEntryStatus.Approved or WorkOrderTimeEntryStatus.Invoiced))
            .Sum(x => x.BillableTotal);

        var meta = new List<(string Label, string Value)>
        {
            ("Service job", job?.Number ?? wo.ServiceJobId.ToString()),
            ("Status", wo.Status.ToString()),
            ("Assigned to", wo.AssignedToUserId?.ToString() ?? ""),
            ("Approved hours", FormatQty(approvedHours)),
            ("Approved labor cost", FormatMoney(approvedLaborCost)),
            ("Pending labor cost", FormatMoney(pendingLaborCost)),
            ("Approved billable labor", FormatMoney(billableApprovedAmount))
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

                column.Item().PaddingTop(8).Text("Labor entries").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(1.5f);
                        cols.RelativeColumn(1.7f);
                        cols.RelativeColumn(1.7f);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Date");
                        h.Cell().Element(CellHeader).Text("Technician");
                        h.Cell().Element(CellHeader).Text("Work");
                        h.Cell().Element(CellHeader).AlignRight().Text("Hours");
                        h.Cell().Element(CellHeader).AlignRight().Text("Cost");
                        h.Cell().Element(CellHeader).AlignRight().Text("Billable");
                        h.Cell().Element(CellHeader).Text("Status");
                    });

                    foreach (var line in wo.TimeEntries.OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.Id))
                    {
                        table.Cell().Element(CellBody).Text(line.WorkDate.ToString("yyyy-MM-dd"));
                        table.Cell().Element(CellBody).Text(line.TechnicianName);
                        table.Cell().Element(CellBody).Text(line.WorkDescription);
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.HoursWorked));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.LaborCost));
                        table.Cell().Element(CellBody).AlignRight().Text(line.BillableToCustomer ? FormatMoney(line.BillableTotal) : "-");
                        table.Cell().Element(CellBody).Text(line.Status.ToString());
                    }

                    if (wo.TimeEntries.Count == 0)
                    {
                        table.Cell().ColumnSpan(7).Element(CellBody).Text("No labor entries recorded.");
                    }
                });
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

    private async Task<PdfDocument> RenderServiceEstimateAsync(Guid id, CancellationToken cancellationToken)
    {
        var estimate = await _dbContext.ServiceEstimates.AsNoTracking()
                           .Include(x => x.Lines)
                           .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                       ?? throw new NotFoundException("Service estimate not found.");

        var job = await _dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == estimate.ServiceJobId, cancellationToken);
        var customer = job is null
            ? null
            : await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.CustomerId, cancellationToken);
        var itemById = await LoadItemMapAsync(
            estimate.Lines.Where(l => l.ItemId.HasValue).Select(l => l.ItemId!.Value),
            cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Service job", job?.Number ?? estimate.ServiceJobId.ToString()),
            ("Customer", job is null ? "" : CustomerLabel(customer, job.CustomerId)),
            ("Revision", estimate.RevisionNumber.ToString()),
            ("Issued at", estimate.IssuedAt.ToString("u")),
            ("Valid until", estimate.ValidUntil?.ToString("u") ?? ""),
            ("Status", estimate.Status.ToString())
        };

        return BuildPdf(
            title: "Service Estimate",
            referenceNumber: estimate.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Lines").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(1.5f);
                        cols.RelativeColumn(1.8f);
                        cols.RelativeColumn(1.5f);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Kind");
                        h.Cell().Element(CellHeader).Text("Description");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit");
                        h.Cell().Element(CellHeader).AlignRight().Text("Tax%");
                        h.Cell().Element(CellHeader).AlignRight().Text("Line Total");
                    });

                    foreach (var line in estimate.Lines)
                    {
                        var item = line.ItemId.HasValue ? itemById.GetValueOrDefault(line.ItemId.Value) : null;
                        var description = line.Kind switch
                        {
                            ServiceEstimateLineKind.Part or ServiceEstimateLineKind.Expense when item is not null
                                => $"{ItemLabel(item, line.ItemId ?? Guid.Empty)} ({line.Description})",
                            _ => line.Description
                        };

                        table.Cell().Element(CellBody).Text(line.Kind.ToString());
                        table.Cell().Element(CellBody).Text(description);
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitPrice));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatPercent(line.TaxPercent));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.LineTotal));
                    }
                });

                if (!string.IsNullOrWhiteSpace(estimate.Terms))
                {
                    column.Item().PaddingTop(8).Text("Terms").SemiBold();
                    column.Item().Text(estimate.Terms);
                }

                column.Item().PaddingTop(8).AlignRight().Text($"Subtotal: {FormatMoney(estimate.Subtotal)}");
                column.Item().AlignRight().Text($"Tax: {FormatMoney(estimate.TaxTotal)}");
                column.Item().AlignRight().Text($"Total: {FormatMoney(estimate.Total)}").SemiBold();
            },
            fileName: $"SE-{estimate.Number}.pdf",
            qrPayload: $"ISS:SE:{estimate.Id}",
            barcodePayload: estimate.Number);
    }

    private async Task<PdfDocument> RenderServiceHandoverAsync(Guid id, CancellationToken cancellationToken)
    {
        var handover = await _dbContext.ServiceHandovers.AsNoTracking()
                           .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                       ?? throw new NotFoundException("Service handover not found.");

        var job = await _dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == handover.ServiceJobId, cancellationToken);
        var customer = job is null
            ? null
            : await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.CustomerId, cancellationToken);
        var equipment = job is null
            ? null
            : await _dbContext.EquipmentUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.EquipmentUnitId, cancellationToken);
        var equipmentItem = equipment is null
            ? null
            : await _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == equipment.ItemId, cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Service job", job?.Number ?? handover.ServiceJobId.ToString()),
            ("Customer", job is null ? "" : CustomerLabel(customer, job.CustomerId)),
            ("Handover date", handover.HandoverDate.ToString("u")),
            ("Status", handover.Status.ToString()),
            ("Post-service warranty", handover.PostServiceWarrantyMonths is int m ? $"{m} month(s)" : "")
        };

        if (equipment is not null)
        {
            meta.Add(("Equipment", $"{ItemLabel(equipmentItem, equipment.ItemId)} / SN: {equipment.SerialNumber}"));
        }

        if (handover.SalesInvoiceId is { } invoiceId)
        {
            var invoice = await _dbContext.SalesInvoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
            meta.Add(("Sales invoice", invoice?.Number ?? invoiceId.ToString()));
        }

        return BuildPdf(
            title: "Service Handover",
            referenceNumber: handover.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Items returned to customer").SemiBold();
                column.Item().Text(handover.ItemsReturned);

                if (!string.IsNullOrWhiteSpace(handover.CustomerAcknowledgement))
                {
                    column.Item().PaddingTop(8).Text("Customer acknowledgement").SemiBold();
                    column.Item().Text(handover.CustomerAcknowledgement);
                }

                if (!string.IsNullOrWhiteSpace(handover.Notes))
                {
                    column.Item().PaddingTop(8).Text("Notes").SemiBold();
                    column.Item().Text(handover.Notes);
                }
            },
            fileName: $"SH-{handover.Number}.pdf",
            qrPayload: $"ISS:SH:{handover.Id}",
            barcodePayload: handover.Number);
    }

    private async Task<PdfDocument> RenderServiceExpenseClaimAsync(Guid id, CancellationToken cancellationToken)
    {
        var claim = await _dbContext.ServiceExpenseClaims.AsNoTracking()
                         .Include(x => x.Lines)
                         .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Service expense claim not found.");

        var job = await _dbContext.ServiceJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == claim.ServiceJobId, cancellationToken);
        var settlementPaymentType = claim.SettlementPaymentTypeId is null
            ? null
            : await _dbContext.PaymentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == claim.SettlementPaymentTypeId.Value, cancellationToken);
        var settlementPettyCashFund = claim.SettlementPettyCashFundId is null
            ? null
            : await _dbContext.PettyCashFunds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == claim.SettlementPettyCashFundId.Value, cancellationToken);
        var itemById = await LoadItemMapAsync(
            claim.Lines.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value),
            cancellationToken);

        var meta = new List<(string Label, string Value)>
        {
            ("Service job", job?.Number ?? claim.ServiceJobId.ToString()),
            ("Claimed by", claim.ClaimedByName),
            ("Funding source", claim.FundingSource.ToString()),
            ("Expense date", claim.ExpenseDate.ToString("u")),
            ("Merchant", claim.MerchantName ?? ""),
            ("Receipt ref", claim.ReceiptReference ?? ""),
            ("Status", claim.Status.ToString()),
            ("Settled at", claim.SettledAt?.ToString("u") ?? "")
        };

        if (settlementPaymentType is not null)
        {
            meta.Add(("Settlement method", $"{settlementPaymentType.Code} - {settlementPaymentType.Name}"));
        }

        if (settlementPettyCashFund is not null)
        {
            meta.Add(("Petty cash fund", $"{settlementPettyCashFund.Code} - {settlementPettyCashFund.Name}"));
        }

        return BuildPdf(
            title: "Service Expense Claim",
            referenceNumber: claim.Number,
            meta: meta,
            content: column =>
            {
                column.Item().Text("Lines").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(1.5f);
                        cols.RelativeColumn(1.8f);
                        cols.RelativeColumn(1.5f);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(CellHeader).Text("Item");
                        h.Cell().Element(CellHeader).Text("Description");
                        h.Cell().Element(CellHeader).AlignRight().Text("Qty");
                        h.Cell().Element(CellHeader).AlignRight().Text("Unit Cost");
                        h.Cell().Element(CellHeader).Text("Billable");
                        h.Cell().Element(CellHeader).AlignRight().Text("Line Total");
                    });

                    foreach (var line in claim.Lines)
                    {
                        var item = line.ItemId.HasValue ? itemById.GetValueOrDefault(line.ItemId.Value) : null;
                        table.Cell().Element(CellBody).Text(line.ItemId.HasValue ? ItemLabel(item, line.ItemId.Value) : "Ad-hoc / outside buy");
                        table.Cell().Element(CellBody).Text(line.Description);
                        table.Cell().Element(CellBody).AlignRight().Text(FormatQty(line.Quantity));
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.UnitCost));
                        table.Cell().Element(CellBody).Text(line.BillableToCustomer ? "Yes" : "No");
                        table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(line.LineTotal));
                    }
                });

                if (!string.IsNullOrWhiteSpace(claim.Notes))
                {
                    column.Item().PaddingTop(8).Text("Notes").SemiBold();
                    column.Item().Text(claim.Notes);
                }

                if (!string.IsNullOrWhiteSpace(claim.RejectionReason))
                {
                    column.Item().PaddingTop(8).Text("Rejection reason").SemiBold();
                    column.Item().Text(claim.RejectionReason);
                }

                if (!string.IsNullOrWhiteSpace(claim.SettlementReference))
                {
                    column.Item().PaddingTop(8).Text("Settlement reference").SemiBold();
                    column.Item().Text(claim.SettlementReference);
                }

                column.Item().PaddingTop(8).AlignRight().Text($"Total: {FormatMoney(claim.Total)}").SemiBold();
            },
            fileName: $"SEC-{claim.Number}.pdf",
            qrPayload: $"ISS:SEC:{claim.Id}",
            barcodePayload: claim.Number);
    }

    private static void AddNarrativeSection(ColumnDescriptor column, string title, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        column.Item().PaddingTop(8).Text(title).SemiBold();
        column.Item().Text(value);
    }
}
