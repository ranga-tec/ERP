using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Procurement;

[ApiController]
[Route("api/procurement/rfqs")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement}")]
public sealed class RfqsController(
    IIssDbContext dbContext,
    ProcurementService procurementService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record RfqSummaryDto(Guid Id, string Number, Guid SupplierId, DateTimeOffset RequestedAt, RequestForQuoteStatus Status, int LineCount);
    public sealed record RfqDto(Guid Id, string Number, Guid SupplierId, DateTimeOffset RequestedAt, RequestForQuoteStatus Status, IReadOnlyList<RfqLineDto> Lines);
    public sealed record RfqLineDto(Guid Id, Guid ItemId, decimal Quantity, string? Notes);

    public sealed record CreateRfqRequest(Guid SupplierId);
    public sealed record AddRfqLineRequest(Guid ItemId, decimal Quantity, string? Notes);
    public sealed record UpdateRfqLineRequest(decimal Quantity, string? Notes);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RfqSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementRfqView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rfqs = await dbContext.RequestForQuotes.AsNoTracking()
            .OrderByDescending(x => x.RequestedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new RfqSummaryDto(x.Id, x.Number, x.SupplierId, x.RequestedAt, x.Status, x.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(rfqs);
    }

    [HttpPost]
    public async Task<ActionResult<RfqDto>> Create(CreateRfqRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementRfqCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await procurementService.CreateRfqAsync(request.SupplierId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RfqDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementRfqView, cancellationToken))
        {
            return Forbid();
        }

        var rfq = await dbContext.RequestForQuotes.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (rfq is null)
        {
            return NotFound();
        }

        return Ok(new RfqDto(
            rfq.Id,
            rfq.Number,
            rfq.SupplierId,
            rfq.RequestedAt,
            rfq.Status,
            rfq.Lines.Select(l => new RfqLineDto(l.Id, l.ItemId, l.Quantity, l.Notes)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementRfqView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.RequestForQuote, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddRfqLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementRfqEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.AddRfqLineAsync(id, request.ItemId, request.Quantity, request.Notes, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateRfqLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementRfqEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.UpdateRfqLineAsync(id, lineId, request.Quantity, request.Notes, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementRfqEdit, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.RemoveRfqLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.ProcurementRfqSend, cancellationToken))
        {
            return Forbid();
        }

        await procurementService.MarkRfqSentAsync(id, cancellationToken);
        await NotifyRfqCreatorAsync(id, "RFQ sent", "Your request for quotation has been sent.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyRfqCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var rfq = await dbContext.RequestForQuotes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (rfq is null || rfq.CreatedBy is null || rfq.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            rfq.CreatedBy.Value,
            title,
            $"{rfq.Number}: {message}",
            $"/procurement/rfqs/{rfq.Id}",
            ReferenceTypes.RequestForQuote,
            rfq.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
