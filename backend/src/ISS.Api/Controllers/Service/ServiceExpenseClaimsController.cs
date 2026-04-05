using System.Security.Claims;
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
[Route("api/service/expense-claims")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Finance}")]
public sealed class ServiceExpenseClaimsController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record ServiceExpenseClaimSummaryDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        Guid? ClaimedByUserId,
        string ClaimedByName,
        ServiceExpenseFundingSource FundingSource,
        DateTimeOffset ExpenseDate,
        string? MerchantName,
        ServiceExpenseClaimStatus Status,
        decimal Total,
        int LineCount,
        int BillableUnconvertedLineCount,
        DateTimeOffset? SettledAt);

    public sealed record ServiceExpenseClaimLineDto(
        Guid Id,
        Guid? ItemId,
        Guid? ExpenseAccountId,
        string? ExpenseAccountCode,
        string? ExpenseAccountName,
        string Description,
        decimal Quantity,
        decimal UnitCost,
        bool BillableToCustomer,
        Guid? ConvertedToServiceEstimateId,
        Guid? ConvertedToServiceEstimateLineId,
        DateTimeOffset? ConvertedToEstimateAt,
        decimal LineTotal);

    public sealed record ServiceExpenseClaimDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        Guid? ClaimedByUserId,
        string ClaimedByName,
        ServiceExpenseFundingSource FundingSource,
        DateTimeOffset ExpenseDate,
        string? MerchantName,
        string? ReceiptReference,
        string? Notes,
        ServiceExpenseClaimStatus Status,
        DateTimeOffset? SubmittedAt,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? RejectedAt,
        string? RejectionReason,
        Guid? SettlementPaymentTypeId,
        Guid? SettlementPettyCashFundId,
        DateTimeOffset? SettledAt,
        string? SettlementReference,
        decimal Total,
        int BillableUnconvertedLineCount,
        IReadOnlyList<ServiceExpenseClaimLineDto> Lines);

    public sealed record CreateServiceExpenseClaimRequest(
        Guid ServiceJobId,
        string? ClaimedByName,
        ServiceExpenseFundingSource FundingSource,
        DateTimeOffset? ExpenseDate,
        string? MerchantName,
        string? ReceiptReference,
        string? Notes);

    public sealed record AddServiceExpenseClaimLineRequest(
        Guid? ItemId,
        string Description,
        decimal Quantity,
        decimal UnitCost,
        bool BillableToCustomer);

    public sealed record UpdateServiceExpenseClaimLineRequest(
        Guid? ItemId,
        string Description,
        decimal Quantity,
        decimal UnitCost,
        bool BillableToCustomer);

    public sealed record RejectServiceExpenseClaimRequest(string? RejectionReason);
    public sealed record SettleServiceExpenseClaimRequest(Guid? SettlementPaymentTypeId, Guid? SettlementPettyCashFundId, string? SettlementReference);
    public sealed record ConvertBillableLinesToEstimateRequest(Guid? ServiceEstimateId, decimal? TaxPercent, DateTimeOffset? ValidUntil, string? Terms);
    public sealed record ConvertBillableLinesToEstimateResponse(Guid ServiceEstimateId, int AddedLineCount);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceExpenseClaimSummaryDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.ServiceExpenseClaims.AsNoTracking()
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new ServiceExpenseClaimSummaryDto(
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.ClaimedByUserId,
                x.ClaimedByName,
                x.FundingSource,
                x.ExpenseDate,
                x.MerchantName,
                x.Status,
                x.Lines.Sum(l => l.Quantity * l.UnitCost),
                x.Lines.Count,
                x.Lines.Count(l => l.BillableToCustomer && l.ConvertedToServiceEstimateLineId == null),
                x.SettledAt))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceExpenseClaimDto>> Create(CreateServiceExpenseClaimRequest request, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var claimedByUserId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : (Guid?)null;
        var fallbackName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
        var resolvedClaimedByName = string.IsNullOrWhiteSpace(request.ClaimedByName) ? fallbackName : request.ClaimedByName.Trim();

        var id = await serviceManagementService.CreateServiceExpenseClaimAsync(
            request.ServiceJobId,
            claimedByUserId,
            resolvedClaimedByName,
            request.FundingSource,
            request.ExpenseDate,
            request.MerchantName,
            request.ReceiptReference,
            request.Notes,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceExpenseClaimDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var claim = await dbContext.ServiceExpenseClaims.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(line => line.ExpenseAccount)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (claim is null)
        {
            return NotFound();
        }

        return Ok(new ServiceExpenseClaimDto(
            claim.Id,
            claim.Number,
            claim.ServiceJobId,
            claim.ClaimedByUserId,
            claim.ClaimedByName,
            claim.FundingSource,
            claim.ExpenseDate,
            claim.MerchantName,
            claim.ReceiptReference,
            claim.Notes,
            claim.Status,
            claim.SubmittedAt,
            claim.ApprovedAt,
            claim.RejectedAt,
            claim.RejectionReason,
            claim.SettlementPaymentTypeId,
            claim.SettlementPettyCashFundId,
            claim.SettledAt,
            claim.SettlementReference,
            claim.Total,
            claim.Lines.Count(line => line.BillableToCustomer && line.ConvertedToServiceEstimateLineId == null),
            claim.Lines.Select(line => new ServiceExpenseClaimLineDto(
                line.Id,
                line.ItemId,
                line.ExpenseAccountId,
                line.ExpenseAccount != null ? line.ExpenseAccount.Code : null,
                line.ExpenseAccount != null ? line.ExpenseAccount.Name : null,
                line.Description,
                line.Quantity,
                line.UnitCost,
                line.BillableToCustomer,
                line.ConvertedToServiceEstimateId,
                line.ConvertedToServiceEstimateLineId,
                line.ConvertedToEstimateAt,
                line.LineTotal)).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.ServiceExpenseClaim, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddServiceExpenseClaimLineRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.AddServiceExpenseClaimLineAsync(
            id,
            request.ItemId,
            request.Description,
            request.Quantity,
            request.UnitCost,
            request.BillableToCustomer,
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateServiceExpenseClaimLineRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.UpdateServiceExpenseClaimLineAsync(
            id,
            lineId,
            request.ItemId,
            request.Description,
            request.Quantity,
            request.UnitCost,
            request.BillableToCustomer,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        await serviceManagementService.RemoveServiceExpenseClaimLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.SubmitServiceExpenseClaimAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.ApproveServiceExpenseClaimAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
    public async Task<ActionResult> Reject(Guid id, RejectServiceExpenseClaimRequest? request, CancellationToken cancellationToken)
    {
        await serviceManagementService.RejectServiceExpenseClaimAsync(id, request?.RejectionReason, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/settle")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
    public async Task<ActionResult> Settle(Guid id, SettleServiceExpenseClaimRequest? request, CancellationToken cancellationToken)
    {
        await serviceManagementService.SettleServiceExpenseClaimAsync(
            id,
            request?.SettlementPaymentTypeId,
            request?.SettlementPettyCashFundId,
            request?.SettlementReference,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/convert-billable-lines-to-estimate")]
    public async Task<ActionResult<ConvertBillableLinesToEstimateResponse>> ConvertBillableLinesToEstimate(
        Guid id,
        ConvertBillableLinesToEstimateRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await serviceManagementService.ConvertBillableExpenseClaimToEstimateAsync(
            id,
            request?.ServiceEstimateId,
            request?.TaxPercent ?? 0m,
            request?.ValidUntil,
            request?.Terms,
            cancellationToken);

        return Ok(new ConvertBillableLinesToEstimateResponse(result.ServiceEstimateId, result.AddedLineCount));
    }
}
