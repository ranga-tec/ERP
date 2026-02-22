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
[Route("api/service/jobs")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
public sealed class ServiceJobsController(IIssDbContext dbContext, ServiceManagementService serviceManagementService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record ServiceJobDto(Guid Id, string Number, Guid EquipmentUnitId, Guid CustomerId, DateTimeOffset OpenedAt, string ProblemDescription, ServiceJobStatus Status, DateTimeOffset? CompletedAt);
    public sealed record CreateServiceJobRequest(Guid EquipmentUnitId, Guid CustomerId, string ProblemDescription);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceJobDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var jobs = await dbContext.ServiceJobs.AsNoTracking()
            .OrderByDescending(x => x.OpenedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new ServiceJobDto(x.Id, x.Number, x.EquipmentUnitId, x.CustomerId, x.OpenedAt, x.ProblemDescription, x.Status, x.CompletedAt))
            .ToListAsync(cancellationToken);

        return Ok(jobs);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceJobDto>> Create(CreateServiceJobRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateServiceJobAsync(request.EquipmentUnitId, request.CustomerId, request.ProblemDescription, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceJobDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ServiceJobDto(x.Id, x.Number, x.EquipmentUnitId, x.CustomerId, x.OpenedAt, x.ProblemDescription, x.Status, x.CompletedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.ServiceJob, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.StartServiceJobAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.CompleteServiceJobAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.CloseServiceJobAsync(id, cancellationToken);
        return NoContent();
    }
}
