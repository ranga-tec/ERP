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
[Route("api/service/quality-checks")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
public sealed class QualityChecksController(IIssDbContext dbContext, ServiceManagementService serviceManagementService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record QualityCheckDto(Guid Id, Guid ServiceJobId, DateTimeOffset CheckedAt, bool Passed, string? Notes);
    public sealed record AddQualityCheckRequest(Guid ServiceJobId, bool Passed, string? Notes);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QualityCheckDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var qcs = await dbContext.QualityChecks.AsNoTracking()
            .OrderByDescending(x => x.CheckedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new QualityCheckDto(x.Id, x.ServiceJobId, x.CheckedAt, x.Passed, x.Notes))
            .ToListAsync(cancellationToken);

        return Ok(qcs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QualityCheckDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var qc = await dbContext.QualityChecks.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new QualityCheckDto(x.Id, x.ServiceJobId, x.CheckedAt, x.Passed, x.Notes))
            .FirstOrDefaultAsync(cancellationToken);

        return qc is null ? NotFound() : Ok(qc);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.QualityCheck, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost]
    public async Task<ActionResult<QualityCheckDto>> Create(AddQualityCheckRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.AddQualityCheckAsync(request.ServiceJobId, request.Passed, request.Notes, cancellationToken);
        var qc = await dbContext.QualityChecks.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new QualityCheckDto(x.Id, x.ServiceJobId, x.CheckedAt, x.Passed, x.Notes))
            .FirstAsync(cancellationToken);
        return Ok(qc);
    }
}
