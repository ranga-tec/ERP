using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Procurement;

[ApiController]
[Route("api/procurement/direct-purchases")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement},{Roles.Inventory},{Roles.Finance}")]
public sealed class DirectPurchasesController(
    IIssDbContext dbContext,
    ProcurementService procurementService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record DirectPurchaseSummaryDto(
        Guid Id,
        string Number,
        Guid SupplierId,
        Guid WarehouseId,
        DateTimeOffset PurchasedAt,
        DirectPurchaseStatus Status,
        string? Remarks,
        decimal Subtotal,
        decimal TaxTotal,
        decimal GrandTotal);

    public sealed record DirectPurchaseLineDto(
        Guid Id,
        Guid ItemId,
        decimal Quantity,
        decimal UnitPrice,
        decimal TaxPercent,
        string? BatchNumber,
        IReadOnlyList<string> Serials,
        decimal LineSubTotal,
        decimal LineTax,
        decimal LineTotal);

    public sealed record DirectPurchaseDto(
        Guid Id,
        string Number,
        Guid SupplierId,
        Guid WarehouseId,
        DateTimeOffset PurchasedAt,
        DirectPurchaseStatus Status,
        string? Remarks,
        decimal Subtotal,
        decimal TaxTotal,
        decimal GrandTotal,
        IReadOnlyList<DirectPurchaseLineDto> Lines);

    public sealed record CreateDirectPurchaseRequest(Guid SupplierId, Guid WarehouseId, DateTimeOffset? PurchasedAt, string? Remarks);
    public sealed record AddDirectPurchaseLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice, decimal TaxPercent, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DirectPurchaseSummaryDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.DirectPurchases.AsNoTracking()
            .OrderByDescending(x => x.PurchasedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new DirectPurchaseSummaryDto(
                x.Id,
                x.Number,
                x.SupplierId,
                x.WarehouseId,
                x.PurchasedAt,
                x.Status,
                x.Remarks,
                x.Lines.Sum(l => l.Quantity * l.UnitPrice),
                x.Lines.Sum(l => l.Quantity * l.UnitPrice * (l.TaxPercent / 100m)),
                x.Lines.Sum(l => l.Quantity * l.UnitPrice * (1m + (l.TaxPercent / 100m)))))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<DirectPurchaseDto>> Create(CreateDirectPurchaseRequest request, CancellationToken cancellationToken)
    {
        var id = await procurementService.CreateDirectPurchaseAsync(
            request.SupplierId,
            request.WarehouseId,
            request.PurchasedAt,
            request.Remarks,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DirectPurchaseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dp = await dbContext.DirectPurchases.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (dp is null)
        {
            return NotFound();
        }

        var lineDtos = dp.Lines.Select(l => new DirectPurchaseLineDto(
            l.Id,
            l.ItemId,
            l.Quantity,
            l.UnitPrice,
            l.TaxPercent,
            l.BatchNumber,
            l.Serials.Select(s => s.SerialNumber).ToList(),
            l.LineSubTotal,
            l.LineTax,
            l.LineTotal)).ToList();

        return Ok(new DirectPurchaseDto(
            dp.Id,
            dp.Number,
            dp.SupplierId,
            dp.WarehouseId,
            dp.PurchasedAt,
            dp.Status,
            dp.Remarks,
            lineDtos.Sum(x => x.LineSubTotal),
            lineDtos.Sum(x => x.LineTax),
            lineDtos.Sum(x => x.LineTotal),
            lineDtos));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.DirectPurchase, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddDirectPurchaseLineRequest request, CancellationToken cancellationToken)
    {
        await procurementService.AddDirectPurchaseLineAsync(
            id,
            request.ItemId,
            request.Quantity,
            request.UnitPrice,
            request.TaxPercent,
            request.BatchNumber,
            request.Serials,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.PostDirectPurchaseAsync(id, cancellationToken);
        return NoContent();
    }
}
