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
[Route("api/procurement/supplier-invoices")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement},{Roles.Finance}")]
public sealed class SupplierInvoicesController(
    IIssDbContext dbContext,
    ProcurementService procurementService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record SupplierInvoiceDto(
        Guid Id,
        string Number,
        Guid SupplierId,
        string InvoiceNumber,
        DateTimeOffset InvoiceDate,
        DateTimeOffset? DueDate,
        Guid? PurchaseOrderId,
        Guid? GoodsReceiptId,
        Guid? DirectPurchaseId,
        decimal Subtotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal FreightAmount,
        decimal RoundingAmount,
        decimal GrandTotal,
        SupplierInvoiceStatus Status,
        DateTimeOffset? PostedAt,
        Guid? AccountsPayableEntryId,
        string? Notes);

    public sealed record CreateSupplierInvoiceRequest(
        Guid SupplierId,
        string InvoiceNumber,
        DateTimeOffset InvoiceDate,
        DateTimeOffset? DueDate,
        Guid? PurchaseOrderId,
        Guid? GoodsReceiptId,
        Guid? DirectPurchaseId,
        decimal Subtotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal FreightAmount,
        decimal RoundingAmount,
        string? Notes);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierInvoiceDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var invoices = await dbContext.SupplierInvoices.AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new SupplierInvoiceDto(
                x.Id,
                x.Number,
                x.SupplierId,
                x.InvoiceNumber,
                x.InvoiceDate,
                x.DueDate,
                x.PurchaseOrderId,
                x.GoodsReceiptId,
                x.DirectPurchaseId,
                x.Subtotal,
                x.DiscountAmount,
                x.TaxAmount,
                x.FreightAmount,
                x.RoundingAmount,
                x.Subtotal - x.DiscountAmount + x.TaxAmount + x.FreightAmount + x.RoundingAmount,
                x.Status,
                x.PostedAt,
                x.AccountsPayableEntryId,
                x.Notes))
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierInvoiceDto>> Create(CreateSupplierInvoiceRequest request, CancellationToken cancellationToken)
    {
        var id = await procurementService.CreateSupplierInvoiceAsync(
            request.SupplierId,
            request.InvoiceNumber,
            request.InvoiceDate,
            request.DueDate,
            request.PurchaseOrderId,
            request.GoodsReceiptId,
            request.DirectPurchaseId,
            request.Subtotal,
            request.DiscountAmount,
            request.TaxAmount,
            request.FreightAmount,
            request.RoundingAmount,
            request.Notes,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierInvoiceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SupplierInvoices.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SupplierInvoiceDto(
                x.Id,
                x.Number,
                x.SupplierId,
                x.InvoiceNumber,
                x.InvoiceDate,
                x.DueDate,
                x.PurchaseOrderId,
                x.GoodsReceiptId,
                x.DirectPurchaseId,
                x.Subtotal,
                x.DiscountAmount,
                x.TaxAmount,
                x.FreightAmount,
                x.RoundingAmount,
                x.Subtotal - x.DiscountAmount + x.TaxAmount + x.FreightAmount + x.RoundingAmount,
                x.Status,
                x.PostedAt,
                x.AccountsPayableEntryId,
                x.Notes))
            .FirstOrDefaultAsync(cancellationToken);

        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.SupplierInvoice, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.PostSupplierInvoiceAsync(id, cancellationToken);
        return NoContent();
    }
}
