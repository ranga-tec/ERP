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
[Route("api/service/material-requisitions")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Inventory}")]
public sealed class MaterialRequisitionsController(IIssDbContext dbContext, ServiceManagementService serviceManagementService, IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record MaterialRequisitionSummaryDto(Guid Id, string Number, Guid ServiceJobId, Guid WarehouseId, DateTimeOffset RequestedAt, MaterialRequisitionStatus Status, int LineCount);
    public sealed record MaterialRequisitionDto(Guid Id, string Number, Guid ServiceJobId, Guid WarehouseId, DateTimeOffset RequestedAt, MaterialRequisitionStatus Status, IReadOnlyList<MaterialRequisitionLineDto> Lines);
    public sealed record MaterialRequisitionLineDto(Guid Id, Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string> Serials);

    public sealed record CreateMaterialRequisitionRequest(Guid ServiceJobId, Guid WarehouseId);
    public sealed record AddMaterialRequisitionLineRequest(Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialRequisitionSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var requisitions = await dbContext.MaterialRequisitions.AsNoTracking()
            .OrderByDescending(x => x.RequestedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new MaterialRequisitionSummaryDto(x.Id, x.Number, x.ServiceJobId, x.WarehouseId, x.RequestedAt, x.Status, x.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(requisitions);
    }

    [HttpPost]
    public async Task<ActionResult<MaterialRequisitionDto>> Create(CreateMaterialRequisitionRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateMaterialRequisitionAsync(request.ServiceJobId, request.WarehouseId, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaterialRequisitionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var mr = await dbContext.MaterialRequisitions.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (mr is null)
        {
            return NotFound();
        }

        return Ok(new MaterialRequisitionDto(
            mr.Id,
            mr.Number,
            mr.ServiceJobId,
            mr.WarehouseId,
            mr.RequestedAt,
            mr.Status,
            mr.Lines.Select(l => new MaterialRequisitionLineDto(l.Id, l.ItemId, l.Quantity, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.MaterialRequisition, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddMaterialRequisitionLineRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.AddMaterialRequisitionLineAsync(id, request.ItemId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.PostMaterialRequisitionAsync(id, cancellationToken);
        return NoContent();
    }
}
