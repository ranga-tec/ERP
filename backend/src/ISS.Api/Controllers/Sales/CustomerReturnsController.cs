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
[Route("api/sales/customer-returns")]
[Authorize]
public sealed class CustomerReturnsController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record CustomerReturnSummaryDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        Guid WarehouseId,
        DateTimeOffset ReturnDate,
        CustomerReturnStatus Status,
        Guid? SalesInvoiceId,
        Guid? DispatchNoteId,
        string? Reason);

    public sealed record CustomerReturnLineDto(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, string? BatchNumber, IReadOnlyList<string> Serials);
    public sealed record CustomerReturnDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        Guid WarehouseId,
        DateTimeOffset ReturnDate,
        CustomerReturnStatus Status,
        Guid? SalesInvoiceId,
        Guid? DispatchNoteId,
        string? Reason,
        IReadOnlyList<CustomerReturnLineDto> Lines);

    public sealed record CreateCustomerReturnRequest(Guid CustomerId, Guid WarehouseId, Guid? SalesInvoiceId, Guid? DispatchNoteId, string? Reason);
    public sealed record AddCustomerReturnLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateCustomerReturnLineRequest(decimal Quantity, decimal UnitPrice, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerReturnSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesCustomerReturnView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.CustomerReturns.AsNoTracking()
            .OrderByDescending(x => x.ReturnDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new CustomerReturnSummaryDto(
                x.Id,
                x.Number,
                x.CustomerId,
                x.WarehouseId,
                x.ReturnDate,
                x.Status,
                x.SalesInvoiceId,
                x.DispatchNoteId,
                x.Reason))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerReturnDto>> Create(CreateCustomerReturnRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesCustomerReturnCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateCustomerReturnAsync(
            request.CustomerId,
            request.WarehouseId,
            request.SalesInvoiceId,
            request.DispatchNoteId,
            request.Reason,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerReturnDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesCustomerReturnView, cancellationToken))
        {
            return Forbid();
        }

        var cr = await dbContext.CustomerReturns.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (cr is null)
        {
            return NotFound();
        }

        return Ok(new CustomerReturnDto(
            cr.Id,
            cr.Number,
            cr.CustomerId,
            cr.WarehouseId,
            cr.ReturnDate,
            cr.Status,
            cr.SalesInvoiceId,
            cr.DispatchNoteId,
            cr.Reason,
            cr.Lines.Select(l => new CustomerReturnLineDto(l.Id, l.ItemId, l.Quantity, l.UnitPrice, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesCustomerReturnView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.CustomerReturn, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddCustomerReturnLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesCustomerReturnEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.AddCustomerReturnLineAsync(id, request.ItemId, request.Quantity, request.UnitPrice, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateCustomerReturnLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesCustomerReturnEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.UpdateCustomerReturnLineAsync(
            id,
            lineId,
            request.Quantity,
            request.UnitPrice,
            request.BatchNumber,
            request.Serials,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesCustomerReturnEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.RemoveCustomerReturnLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesCustomerReturnPost, cancellationToken))
        {
            return Forbid();
        }

        await salesService.PostCustomerReturnAsync(id, cancellationToken);
        await NotifyCustomerReturnCreatorAsync(id, "Customer return posted", "Your customer return has been posted.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyCustomerReturnCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var customerReturn = await dbContext.CustomerReturns.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (customerReturn is null || customerReturn.CreatedBy is null || customerReturn.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            customerReturn.CreatedBy.Value,
            title,
            $"{customerReturn.Number}: {message}",
            $"/sales/customer-returns/{customerReturn.Id}",
            ReferenceTypes.CustomerReturn,
            customerReturn.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
