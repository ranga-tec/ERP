using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Sales;

[ApiController]
[Route("api/sales/invoices")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Finance},{Roles.Inventory}")]
public sealed class InvoicesController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record InvoiceSummaryDto(Guid Id, string Number, Guid CustomerId, DateTimeOffset InvoiceDate, DateTimeOffset? DueDate, SalesInvoiceStatus Status, decimal Total);
    public sealed record InvoiceDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        DateTimeOffset InvoiceDate,
        DateTimeOffset? DueDate,
        SalesInvoiceStatus Status,
        decimal Subtotal,
        decimal TaxTotal,
        decimal Total,
        Guid? ServiceJobId,
        string? ServiceJobNumber,
        IReadOnlyList<InvoiceLineDto> Lines);
    public sealed record InvoiceLineDto(
        Guid Id,
        Guid ItemId,
        Guid? RevenueAccountId,
        string? RevenueAccountCode,
        string? RevenueAccountName,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountPercent,
        decimal TaxPercent,
        decimal LineTotal);

    public sealed record CreateInvoiceRequest(Guid CustomerId, DateTimeOffset? DueDate);
    public sealed record CreateInvoiceFromDispatchRequest(Guid DispatchId, DateTimeOffset? DueDate);
    public sealed record CreateInvoiceFromDirectDispatchRequest(Guid DirectDispatchId, DateTimeOffset? DueDate);
    public sealed record AddInvoiceLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxPercent);
    public sealed record UpdateInvoiceLineRequest(decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxPercent);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var invoiceHeaders = await dbContext.SalesInvoices.AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new InvoiceSummaryDto(
                x.Id,
                x.Number,
                x.CustomerId,
                x.InvoiceDate,
                x.DueDate,
                x.Status,
                0m))
            .ToListAsync(cancellationToken);

        if (invoiceHeaders.Count == 0)
        {
            return Ok(invoiceHeaders);
        }

        var invoiceIds = invoiceHeaders.Select(x => x.Id).ToList();

        var totalsByInvoiceId = (await dbContext.Set<SalesInvoiceLine>()
                .AsNoTracking()
                .Where(x => invoiceIds.Contains(x.SalesInvoiceId))
                .Select(x => new
                {
                    x.SalesInvoiceId,
                    x.Quantity,
                    x.UnitPrice,
                    x.DiscountPercent,
                    x.TaxPercent
                })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.SalesInvoiceId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(line =>
                {
                    var gross = line.Quantity * line.UnitPrice;
                    var discount = gross * (line.DiscountPercent / 100m);
                    var subtotal = gross - discount;
                    var tax = subtotal * (line.TaxPercent / 100m);
                    return subtotal + tax;
                }));

        var invoices = invoiceHeaders
            .Select(invoice => new InvoiceSummaryDto(
                invoice.Id,
                invoice.Number,
                invoice.CustomerId,
                invoice.InvoiceDate,
                invoice.DueDate,
                invoice.Status,
                totalsByInvoiceId.TryGetValue(invoice.Id, out var total) ? total : 0m))
            .ToList();

        return Ok(invoices);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateInvoiceAsync(request.CustomerId, request.DueDate, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("from-dispatch")]
    public async Task<ActionResult<InvoiceDto>> CreateFromDispatch(CreateInvoiceFromDispatchRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateInvoiceFromDispatchAsync(request.DispatchId, request.DueDate, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("from-direct-dispatch")]
    public async Task<ActionResult<InvoiceDto>> CreateFromDirectDispatch(CreateInvoiceFromDirectDispatchRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateInvoiceFromDirectDispatchAsync(request.DirectDispatchId, request.DueDate, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceView, cancellationToken))
        {
            return Forbid();
        }

        var invoice = await dbContext.SalesInvoices.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(x => x.RevenueAccount)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (invoice is null)
        {
            return NotFound();
        }

        var serviceJobLink = await dbContext.ServiceHandovers.AsNoTracking()
            .Where(x => x.SalesInvoiceId == invoice.Id)
            .Select(x => new
            {
                x.ServiceJobId,
                ServiceJobNumber = dbContext.ServiceJobs
                    .Where(job => job.Id == x.ServiceJobId)
                    .Select(job => job.Number)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new InvoiceDto(
            invoice.Id,
            invoice.Number,
            invoice.CustomerId,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.Status,
            invoice.Subtotal,
            invoice.TaxTotal,
            invoice.Total,
            serviceJobLink?.ServiceJobId,
            serviceJobLink?.ServiceJobNumber,
            invoice.Lines.Select(l => new InvoiceLineDto(
                l.Id,
                l.ItemId,
                l.RevenueAccountId,
                l.RevenueAccount != null ? l.RevenueAccount.Code : null,
                l.RevenueAccount != null ? l.RevenueAccount.Name : null,
                l.Quantity,
                l.UnitPrice,
                l.DiscountPercent,
                l.TaxPercent,
                l.LineTotal)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.SalesInvoice, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddInvoiceLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.AddInvoiceLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, request.DiscountPercent, request.TaxPercent, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateInvoiceLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.UpdateInvoiceLineAsync(
            id,
            lineId,
            request.Quantity,
            request.UnitPrice,
            request.DiscountPercent,
            request.TaxPercent,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoiceEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.RemoveInvoiceLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesInvoicePost, cancellationToken))
        {
            return Forbid();
        }

        await salesService.PostInvoiceAsync(id, cancellationToken);
        await NotifyInvoiceCreatorAsync(id, "Sales invoice posted", "Your sales invoice has been posted.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyInvoiceCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SalesInvoices.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null || invoice.CreatedBy is null || invoice.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            invoice.CreatedBy.Value,
            title,
            $"{invoice.Number}: {message}",
            $"/sales/invoices/{invoice.Id}",
            ReferenceTypes.SalesInvoice,
            invoice.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
