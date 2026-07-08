using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.DocumentIntelligence.ReceiptDocuments;
using ISS.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Procurement;

[ApiController]
[Route("api/procurement/goods-receipts")]
[Authorize]
public sealed class GoodsReceiptsController(
    IIssDbContext dbContext,
    ProcurementService procurementService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService,
    IReceiptDocumentAnalyzer receiptDocumentAnalyzer,
    IReceiptDocumentMatcher receiptDocumentMatcher) : ControllerBase
{
    public sealed record GoodsReceiptSummaryDto(Guid Id, string Number, Guid PurchaseOrderId, Guid WarehouseId, DateTimeOffset ReceivedAt, GoodsReceiptStatus Status);
    public sealed record GoodsReceiptDto(Guid Id, string Number, Guid PurchaseOrderId, Guid WarehouseId, DateTimeOffset ReceivedAt, GoodsReceiptStatus Status, IReadOnlyList<GoodsReceiptLineDto> Lines);
    public sealed record GoodsReceiptLineDto(Guid Id, Guid? PurchaseOrderLineId, Guid ItemId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string> Serials);
    public sealed record GoodsReceiptReceiptPlanDto(IReadOnlyList<GoodsReceiptReceiptPlanLineDto> Lines);
    public sealed record GoodsReceiptReceiptPlanLineDto(
        Guid PurchaseOrderLineId,
        Guid ItemId,
        decimal OrderedQuantity,
        decimal PreviouslyReceivedQuantity,
        decimal ReservedInOtherDraftsQuantity,
        decimal AvailableQuantity,
        Guid? GoodsReceiptLineId,
        decimal CurrentQuantity,
        decimal UnitCost,
        string? BatchNumber,
        IReadOnlyList<string> Serials);

    public sealed record CreateGoodsReceiptRequest(Guid PurchaseOrderId, Guid WarehouseId);
    public sealed record AddGoodsReceiptLineRequest(Guid ItemId, Guid? PurchaseOrderLineId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateGoodsReceiptLineRequest(decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateGoodsReceiptReceiptPlanRequest(IReadOnlyList<UpdateGoodsReceiptReceiptPlanLineRequest> Lines);
    public sealed record UpdateGoodsReceiptReceiptPlanLineRequest(Guid PurchaseOrderLineId, decimal Quantity, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed class AnalyzeGoodsReceiptDocumentRequest
    {
        public IFormFile? File { get; set; }
        public string? Text { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoodsReceiptSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var grns = await dbContext.GoodsReceipts.AsNoTracking()
            .OrderByDescending(x => x.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new GoodsReceiptSummaryDto(x.Id, x.Number, x.PurchaseOrderId, x.WarehouseId, x.ReceivedAt, x.Status))
            .ToListAsync(cancellationToken);

        return Ok(grns);
    }

    [HttpPost]
    public async Task<ActionResult<GoodsReceiptDto>> Create(CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await procurementService.CreateGoodsReceiptAsync(request.PurchaseOrderId, request.WarehouseId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoodsReceiptDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptView, cancellationToken))
        {
            return Forbid();
        }

        var grn = await dbContext.GoodsReceipts.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (grn is null)
        {
            return NotFound();
        }

        return Ok(new GoodsReceiptDto(
            grn.Id,
            grn.Number,
            grn.PurchaseOrderId,
            grn.WarehouseId,
            grn.ReceivedAt,
            grn.Status,
            grn.Lines.Select(l => new GoodsReceiptLineDto(
                l.Id,
                l.PurchaseOrderLineId,
                l.ItemId,
                l.Quantity,
                l.UnitCost,
                l.BatchNumber,
                l.Serials.Select(s => s.SerialNumber).ToList()))
                .ToList()));
    }

    [HttpGet("{id:guid}/receipt-plan")]
    public async Task<ActionResult<GoodsReceiptReceiptPlanDto>> ReceiptPlan(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptView, cancellationToken))
        {
            return Forbid();
        }

        var plan = await BuildReceiptPlanAsync(id, cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.GoodsReceipt, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/document-suggestions")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ReceiptDocumentSuggestion>> AnalyzeDocument(
        Guid id,
        [FromForm] AnalyzeGoodsReceiptDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptEdit, cancellationToken))
        {
            return Forbid();
        }

        if (request.File is null && string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Document or text is required.",
                Detail = "Upload a receipt/invoice file or paste OCR text for analysis."
            });
        }

        var grn = await dbContext.GoodsReceipts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (grn is null)
        {
            return NotFound();
        }

        if (grn.Status != GoodsReceiptStatus.Draft)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Only draft GRNs can be analyzed.",
                Detail = "OCR suggestions can only be applied before the GRN is posted."
            });
        }

        var context = await BuildReceiptDocumentContextAsync(grn, cancellationToken);
        if (context is null)
        {
            return NotFound();
        }

        await using var fileStream = request.File is null ? Stream.Null : request.File.OpenReadStream();
        var extraction = await receiptDocumentAnalyzer.AnalyzeAsync(
            new ReceiptDocumentInput(
                request.File?.FileName ?? "pasted-text.txt",
                request.File?.ContentType ?? "text/plain",
                fileStream,
                request.Text),
            cancellationToken);

        return Ok(receiptDocumentMatcher.Match(extraction, context));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddGoodsReceiptLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.AddGoodsReceiptLineAsync(
            id,
            request.ItemId,
            request.PurchaseOrderLineId,
            request.Quantity,
            request.UnitCost,
            request.BatchNumber,
            request.Serials,
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateGoodsReceiptLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.UpdateGoodsReceiptLineAsync(id, lineId, request.Quantity, request.UnitCost, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> DeleteLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.RemoveGoodsReceiptLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/receipt-plan")]
    public async Task<ActionResult<GoodsReceiptReceiptPlanDto>> UpdateReceiptPlan(
        Guid id,
        UpdateGoodsReceiptReceiptPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.ReplaceGoodsReceiptReceiptPlanAsync(
            id,
            request.Lines.Select(x => new ProcurementService.GoodsReceiptReceiptPlanLineInput(
                x.PurchaseOrderLineId,
                x.Quantity,
                x.UnitCost,
                x.BatchNumber,
                x.Serials)).ToList(),
            cancellationToken);

        var plan = await BuildReceiptPlanAsync(id, cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementGoodsReceiptPost, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.PostGoodsReceiptAsync(id, cancellationToken);
        await NotifyGoodsReceiptCreatorAsync(id, "Goods receipt posted", "Your goods receipt has been posted.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyGoodsReceiptCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var grn = await dbContext.GoodsReceipts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (grn is null || grn.CreatedBy is null || grn.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            grn.CreatedBy.Value,
            title,
            $"{grn.Number}: {message}",
            $"/procurement/goods-receipts/{grn.Id}",
            ReferenceTypes.GoodsReceipt,
            grn.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GoodsReceiptReceiptPlanDto?> BuildReceiptPlanAsync(Guid goodsReceiptId, CancellationToken cancellationToken)
    {
        var grn = await dbContext.GoodsReceipts.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(x => x.Serials)
            .FirstOrDefaultAsync(x => x.Id == goodsReceiptId, cancellationToken);

        if (grn is null)
        {
            return null;
        }

        var po = await dbContext.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken);

        if (po is null)
        {
            return null;
        }

        var reservedInOtherDrafts = await dbContext.GoodsReceipts.AsNoTracking()
            .Where(x => x.PurchaseOrderId == po.Id && x.Status == GoodsReceiptStatus.Draft && x.Id != grn.Id)
            .SelectMany(x => x.Lines)
            .Where(x => x.PurchaseOrderLineId != null)
            .GroupBy(x => x.PurchaseOrderLineId!.Value)
            .Select(x => new { PurchaseOrderLineId = x.Key, Quantity = x.Sum(y => y.Quantity) })
            .ToDictionaryAsync(x => x.PurchaseOrderLineId, x => x.Quantity, cancellationToken);

        var explicitLines = grn.Lines
            .Where(x => x.PurchaseOrderLineId != null)
            .GroupBy(x => x.PurchaseOrderLineId!.Value)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).ToList());
        var fallbackLinesByItem = grn.Lines
            .Where(x => x.PurchaseOrderLineId == null)
            .GroupBy(x => x.ItemId)
            .ToDictionary(x => x.Key, x => new Queue<GoodsReceiptLine>(x.OrderBy(y => y.Id)));

        var lines = new List<GoodsReceiptReceiptPlanLineDto>(po.Lines.Count);
        foreach (var poLine in po.Lines)
        {
            GoodsReceiptLine? currentLine = null;
            if (explicitLines.TryGetValue(poLine.Id, out var mappedLines) && mappedLines.Count > 0)
            {
                currentLine = mappedLines[0];
            }
            else if (fallbackLinesByItem.TryGetValue(poLine.ItemId, out var fallbackLines) && fallbackLines.Count > 0)
            {
                currentLine = fallbackLines.Dequeue();
            }

            var reservedQuantity = reservedInOtherDrafts.GetValueOrDefault(poLine.Id);
            var availableQuantity = poLine.OrderedQuantity - poLine.ReceivedQuantity - reservedQuantity;

            lines.Add(new GoodsReceiptReceiptPlanLineDto(
                poLine.Id,
                poLine.ItemId,
                poLine.OrderedQuantity,
                poLine.ReceivedQuantity,
                reservedQuantity,
                Math.Max(0m, availableQuantity),
                currentLine?.Id,
                currentLine?.Quantity ?? 0m,
                currentLine?.UnitCost ?? poLine.UnitPrice,
                currentLine?.BatchNumber,
                currentLine?.Serials.Select(x => x.SerialNumber).ToList() ?? []));
        }

        return new GoodsReceiptReceiptPlanDto(lines);
    }

    private async Task<ReceiptDocumentContext?> BuildReceiptDocumentContextAsync(GoodsReceipt grn, CancellationToken cancellationToken)
    {
        var po = await dbContext.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == grn.PurchaseOrderId, cancellationToken);
        if (po is null)
        {
            return null;
        }

        var supplierIds = new[] { po.SupplierId };
        var suppliers = await dbContext.Suppliers.AsNoTracking()
            .Where(x => supplierIds.Contains(x.Id))
            .Select(x => new ReceiptSupplierReference(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        var supplier = suppliers.FirstOrDefault(x => x.Id == po.SupplierId);
        var purchaseOrders = new List<ReceiptPurchaseOrderReference>
        {
            new(
                po.Id,
                po.Number,
                po.SupplierId,
                supplier?.Code ?? po.SupplierId.ToString(),
                supplier?.Name ?? po.SupplierId.ToString())
        };

        var itemIds = po.Lines.Select(x => x.ItemId).Distinct().ToList();
        var itemById = await dbContext.Items.AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var reservedInOtherDrafts = await dbContext.GoodsReceipts.AsNoTracking()
            .Where(x => x.PurchaseOrderId == po.Id && x.Status == GoodsReceiptStatus.Draft && x.Id != grn.Id)
            .SelectMany(x => x.Lines)
            .Where(x => x.PurchaseOrderLineId != null)
            .GroupBy(x => x.PurchaseOrderLineId!.Value)
            .Select(x => new { PurchaseOrderLineId = x.Key, Quantity = x.Sum(y => y.Quantity) })
            .ToDictionaryAsync(x => x.PurchaseOrderLineId, x => x.Quantity, cancellationToken);

        var lines = po.Lines.Select(line =>
        {
            var item = itemById.GetValueOrDefault(line.ItemId);
            var reservedQuantity = reservedInOtherDrafts.GetValueOrDefault(line.Id);
            var availableQuantity = Math.Max(0m, line.OrderedQuantity - line.ReceivedQuantity - reservedQuantity);

            return new ReceiptPurchaseOrderLineReference(
                line.Id,
                line.ItemId,
                item?.Sku ?? line.ItemId.ToString(),
                item?.Name ?? line.ItemId.ToString(),
                line.OrderedQuantity,
                line.ReceivedQuantity,
                reservedQuantity,
                availableQuantity,
                line.UnitPrice);
        }).ToList();

        return new ReceiptDocumentContext(purchaseOrders, suppliers, lines);
    }
}
