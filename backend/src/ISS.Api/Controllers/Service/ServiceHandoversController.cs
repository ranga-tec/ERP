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
[Route("api/service/handovers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
public sealed class ServiceHandoversController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record ServiceHandoverDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        DateTimeOffset HandoverDate,
        string ItemsReturned,
        int? PostServiceWarrantyMonths,
        string? CustomerAcknowledgement,
        string? Notes,
        ServiceHandoverStatus Status);

    public sealed record CreateServiceHandoverRequest(
        Guid ServiceJobId,
        string ItemsReturned,
        int? PostServiceWarrantyMonths,
        string? CustomerAcknowledgement,
        string? Notes);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceHandoverDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.ServiceHandovers.AsNoTracking()
            .OrderByDescending(x => x.HandoverDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new ServiceHandoverDto(
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.HandoverDate,
                x.ItemsReturned,
                x.PostServiceWarrantyMonths,
                x.CustomerAcknowledgement,
                x.Notes,
                x.Status))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceHandoverDto>> Create(CreateServiceHandoverRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateServiceHandoverAsync(
            request.ServiceJobId,
            request.ItemsReturned,
            request.PostServiceWarrantyMonths,
            request.CustomerAcknowledgement,
            request.Notes,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceHandoverDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var handover = await dbContext.ServiceHandovers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ServiceHandoverDto(
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.HandoverDate,
                x.ItemsReturned,
                x.PostServiceWarrantyMonths,
                x.CustomerAcknowledgement,
                x.Notes,
                x.Status))
            .FirstOrDefaultAsync(cancellationToken);

        return handover is null ? NotFound() : Ok(handover);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.ServiceHandover, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.CompleteServiceHandoverAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.CancelServiceHandoverAsync(id, cancellationToken);
        return NoContent();
    }
}
