using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/estimates")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
public sealed class ServiceEstimatesController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record ServiceEstimateSummaryDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        DateTimeOffset IssuedAt,
        DateTimeOffset? ValidUntil,
        ServiceEstimateStatus Status,
        decimal Subtotal,
        decimal TaxTotal,
        decimal Total,
        int LineCount);

    public sealed record ServiceEstimateLineDto(
        Guid Id,
        ServiceEstimateLineKind Kind,
        Guid? ItemId,
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal TaxPercent,
        decimal LineSubtotal,
        decimal LineTax,
        decimal LineTotal);

    public sealed record ServiceEstimateDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        DateTimeOffset IssuedAt,
        DateTimeOffset? ValidUntil,
        string? Terms,
        ServiceEstimateStatus Status,
        decimal Subtotal,
        decimal TaxTotal,
        decimal Total,
        IReadOnlyList<ServiceEstimateLineDto> Lines);

    public sealed record CreateServiceEstimateRequest(Guid ServiceJobId, DateTimeOffset? ValidUntil, string? Terms);
    public sealed record AddServiceEstimateLineRequest(
        ServiceEstimateLineKind Kind,
        Guid? ItemId,
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal TaxPercent);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceEstimateSummaryDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.ServiceEstimates.AsNoTracking()
            .OrderByDescending(x => x.IssuedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new ServiceEstimateSummaryDto(
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.IssuedAt,
                x.ValidUntil,
                x.Status,
                x.Lines.Sum(l => l.Quantity * l.UnitPrice),
                x.Lines.Sum(l => (l.Quantity * l.UnitPrice) * (l.TaxPercent / 100m)),
                x.Lines.Sum(l => (l.Quantity * l.UnitPrice) + ((l.Quantity * l.UnitPrice) * (l.TaxPercent / 100m))),
                x.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceEstimateDto>> Create(CreateServiceEstimateRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateServiceEstimateAsync(
            request.ServiceJobId,
            request.ValidUntil,
            request.Terms,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceEstimateDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var estimate = await dbContext.ServiceEstimates.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (estimate is null)
        {
            return NotFound();
        }

        return Ok(new ServiceEstimateDto(
            estimate.Id,
            estimate.Number,
            estimate.ServiceJobId,
            estimate.IssuedAt,
            estimate.ValidUntil,
            estimate.Terms,
            estimate.Status,
            estimate.Subtotal,
            estimate.TaxTotal,
            estimate.Total,
            estimate.Lines.Select(l => new ServiceEstimateLineDto(
                l.Id,
                l.Kind,
                l.ItemId,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.TaxPercent,
                l.LineSubtotal,
                l.LineTax,
                l.LineTotal)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.ServiceEstimate, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddServiceEstimateLineRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.AddServiceEstimateLineAsync(
            id,
            request.Kind,
            request.ItemId,
            request.Description,
            request.Quantity,
            request.UnitPrice,
            request.TaxPercent,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.ApproveServiceEstimateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.RejectServiceEstimateAsync(id, cancellationToken);
        return NoContent();
    }
}
