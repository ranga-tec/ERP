using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Procurement;

[ApiController]
[Route("api/procurement/purchase-requisitions")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement}")]
public sealed class PurchaseRequisitionsController(IIssDbContext dbContext, ProcurementService procurementService) : ControllerBase
{
    public sealed record PurchaseRequisitionSummaryDto(
        Guid Id,
        string Number,
        DateTimeOffset RequestDate,
        PurchaseRequisitionStatus Status,
        int LineCount,
        string? Notes);

    public sealed record PurchaseRequisitionDto(
        Guid Id,
        string Number,
        DateTimeOffset RequestDate,
        PurchaseRequisitionStatus Status,
        string? Notes,
        IReadOnlyList<PurchaseRequisitionLineDto> Lines);

    public sealed record PurchaseRequisitionLineDto(Guid Id, Guid ItemId, decimal Quantity, string? Notes);

    public sealed record CreatePurchaseRequisitionRequest(string? Notes);
    public sealed record AddPurchaseRequisitionLineRequest(Guid ItemId, decimal Quantity, string? Notes);
    public sealed record ConvertToPurchaseOrderRequest(Guid SupplierId);
    public sealed record PurchaseOrderRefDto(Guid Id, string Number);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseRequisitionSummaryDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var items = await dbContext.PurchaseRequisitions.AsNoTracking()
            .OrderByDescending(x => x.RequestDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new PurchaseRequisitionSummaryDto(
                x.Id,
                x.Number,
                x.RequestDate,
                x.Status,
                x.Lines.Count,
                x.Notes))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseRequisitionDto>> Create(CreatePurchaseRequisitionRequest request, CancellationToken cancellationToken)
    {
        var id = await procurementService.CreatePurchaseRequisitionAsync(request.Notes, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseRequisitionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequisitions.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (pr is null)
        {
            return NotFound();
        }

        return Ok(new PurchaseRequisitionDto(
            pr.Id,
            pr.Number,
            pr.RequestDate,
            pr.Status,
            pr.Notes,
            pr.Lines.Select(x => new PurchaseRequisitionLineDto(x.Id, x.ItemId, x.Quantity, x.Notes)).ToList()));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddPurchaseRequisitionLineRequest request, CancellationToken cancellationToken)
    {
        await procurementService.AddPurchaseRequisitionLineAsync(id, request.ItemId, request.Quantity, request.Notes, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.SubmitPurchaseRequisitionAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.ApprovePurchaseRequisitionAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.RejectPurchaseRequisitionAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await procurementService.CancelPurchaseRequisitionAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/convert-to-po")]
    public async Task<ActionResult<PurchaseOrderRefDto>> ConvertToPo(Guid id, ConvertToPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var purchaseOrderId = await procurementService.CreatePurchaseOrderFromPurchaseRequisitionAsync(id, request.SupplierId, cancellationToken);

        var po = await dbContext.PurchaseOrders.AsNoTracking()
            .Where(x => x.Id == purchaseOrderId)
            .Select(x => new PurchaseOrderRefDto(x.Id, x.Number))
            .FirstAsync(cancellationToken);

        return Ok(po);
    }
}
