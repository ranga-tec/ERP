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
[Route("api/service/work-orders")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
public sealed class WorkOrdersController(IIssDbContext dbContext, ServiceManagementService serviceManagementService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record WorkOrderDto(Guid Id, Guid ServiceJobId, string Description, Guid? AssignedToUserId, WorkOrderStatus Status);
    public sealed record CreateWorkOrderRequest(Guid ServiceJobId, string Description, Guid? AssignedToUserId);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkOrderDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var workOrders = await dbContext.WorkOrders.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new WorkOrderDto(x.Id, x.ServiceJobId, x.Description, x.AssignedToUserId, x.Status))
            .ToListAsync(cancellationToken);

        return Ok(workOrders);
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderDto>> Create(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateWorkOrderAsync(request.ServiceJobId, request.Description, request.AssignedToUserId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkOrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var wo = await dbContext.WorkOrders.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new WorkOrderDto(x.Id, x.ServiceJobId, x.Description, x.AssignedToUserId, x.Status))
            .FirstOrDefaultAsync(cancellationToken);

        return wo is null ? NotFound() : Ok(wo);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.WorkOrder, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }
}
