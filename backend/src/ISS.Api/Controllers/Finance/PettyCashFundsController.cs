using ISS.Api.Security;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/petty-cash-funds")]
[Authorize]
public sealed class PettyCashFundsController(
    IIssDbContext dbContext,
    FinanceService financeService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record PettyCashTransactionDto(
        Guid Id,
        DateTimeOffset OccurredAt,
        PettyCashTransactionType Type,
        PettyCashTransactionDirection Direction,
        decimal Amount,
        decimal SignedAmount,
        string? ReferenceType,
        Guid? ReferenceId,
        string? ReferenceNumber,
        string? Notes,
        Guid? PettyCashRequestLineId);

    public sealed record PettyCashFundSummaryDto(
        Guid Id,
        string Code,
        string Name,
        string CurrencyCode,
        string? CustodianName,
        string? Location,
        decimal AuthorizedFloat,
        decimal TransactionLimit,
        decimal AdvanceLimit,
        bool RequireReceipt,
        bool BlockOverdueAdvances,
        PettyCashCashCountFrequency CashCountFrequency,
        DateTimeOffset? LastCashCountAt,
        DateTimeOffset? NextCashCountDueAt,
        bool IsActive,
        decimal Balance,
        int TransactionCount,
        DateTimeOffset? LastActivityAt);

    public sealed record PettyCashFundDto(
        Guid Id,
        string Code,
        string Name,
        string CurrencyCode,
        string? CustodianName,
        string? Location,
        string? Notes,
        decimal AuthorizedFloat,
        decimal TransactionLimit,
        decimal AdvanceLimit,
        bool RequireReceipt,
        bool BlockOverdueAdvances,
        Guid? SettlementShortageExpenseAccountId,
        string? SettlementShortageExpenseAccountCode,
        string? SettlementShortageExpenseAccountName,
        string? SettlementShortageCostCenterCode,
        PettyCashCashCountFrequency CashCountFrequency,
        DateTimeOffset? LastCashCountAt,
        DateTimeOffset? NextCashCountDueAt,
        bool IsActive,
        decimal Balance,
        IReadOnlyList<PettyCashTransactionDto> Transactions,
        IReadOnlyList<PettyCashCashCountDto> CashCounts);

    public sealed record PettyCashCashCountDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        DateTimeOffset CountedAt,
        Guid CountedByUserId,
        string CountedByName,
        decimal PhysicalCash,
        decimal OutstandingAdvances,
        decimal SupportedExpenseVouchers,
        decimal AuthorizedFloatSnapshot,
        decimal Accountability,
        decimal Variance,
        string? Notes,
        PettyCashCashCountStatus Status,
        DateTimeOffset? ApprovedAt,
        Guid? ApprovedByUserId,
        DateTimeOffset? RejectedAt,
        Guid? RejectedByUserId,
        string? RejectionReason);

    public sealed record CreatePettyCashFundRequest(
        string Code,
        string Name,
        string CurrencyCode,
        string? CustodianName,
        string? Notes,
        decimal? OpeningBalance,
        DateTimeOffset? OpenedAt,
        string? OpeningReferenceNumber,
        string? Location,
        decimal AuthorizedFloat,
        decimal TransactionLimit,
        decimal AdvanceLimit,
        bool? RequireReceipt,
        bool? BlockOverdueAdvances,
        Guid? SettlementShortageExpenseAccountId,
        string? SettlementShortageCostCenterCode,
        PettyCashCashCountFrequency? CashCountFrequency,
        DateTimeOffset? NextCashCountDueAt);

    public sealed record UpdatePettyCashFundRequest(
        string Code,
        string Name,
        string CurrencyCode,
        string? CustodianName,
        string? Notes,
        bool IsActive,
        string? Location,
        decimal AuthorizedFloat,
        decimal TransactionLimit,
        decimal AdvanceLimit,
        bool? RequireReceipt,
        bool? BlockOverdueAdvances,
        Guid? SettlementShortageExpenseAccountId,
        string? SettlementShortageCostCenterCode,
        PettyCashCashCountFrequency? CashCountFrequency,
        DateTimeOffset? NextCashCountDueAt);

    public sealed record CreatePettyCashCashCountRequest(
        DateTimeOffset? CountedAt,
        decimal PhysicalCash,
        decimal SupportedExpenseVouchers,
        string? Notes);

    public sealed record RejectPettyCashCashCountRequest(string Reason);

    public sealed record AddPettyCashTopUpRequest(
        decimal Amount,
        DateTimeOffset? OccurredAt,
        string? ReferenceNumber,
        string? Notes);

    public sealed record AddPettyCashAdjustmentRequest(
        decimal Amount,
        PettyCashTransactionDirection Direction,
        DateTimeOffset? OccurredAt,
        string? ReferenceNumber,
        string? Notes);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PettyCashFundSummaryDto>>> List(CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashFundView, cancellationToken))
        {
            return Forbid();
        }

        var funds = await dbContext.PettyCashFunds.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new PettyCashFundSummaryDto(
                x.Id,
                x.Code,
                x.Name,
                x.CurrencyCode,
                x.CustodianName,
                x.Location,
                x.AuthorizedFloat,
                x.TransactionLimit,
                x.AdvanceLimit,
                x.RequireReceipt,
                x.BlockOverdueAdvances,
                x.CashCountFrequency,
                x.LastCashCountAt,
                x.NextCashCountDueAt,
                x.IsActive,
                x.Transactions.Sum(t => t.Direction == PettyCashTransactionDirection.In ? t.Amount : -t.Amount),
                x.Transactions.Count,
                x.Transactions.OrderByDescending(t => t.OccurredAt).Select(t => (DateTimeOffset?)t.OccurredAt).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Ok(funds);
    }

    [HttpPost]
    public async Task<ActionResult<PettyCashFundDto>> Create(CreatePettyCashFundRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashFundCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await financeService.CreatePettyCashFundAsync(
            request.Code,
            request.Name,
            request.CurrencyCode,
            request.CustodianName,
            request.Notes,
            request.OpeningBalance,
            request.OpenedAt,
            request.OpeningReferenceNumber,
            request.Location,
            request.AuthorizedFloat,
            request.TransactionLimit,
            request.AdvanceLimit,
            request.RequireReceipt ?? true,
            request.BlockOverdueAdvances ?? true,
            request.SettlementShortageExpenseAccountId,
            request.SettlementShortageCostCenterCode,
            request.CashCountFrequency ?? PettyCashCashCountFrequency.Weekly,
            request.NextCashCountDueAt,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PettyCashFundDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashFundView, cancellationToken))
        {
            return Forbid();
        }

        var fund = await dbContext.PettyCashFunds.AsNoTracking()
            .Include(x => x.Transactions)
            .Include(x => x.CashCounts)
            .Include(x => x.SettlementShortageExpenseAccount)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (fund is null)
        {
            return NotFound();
        }

        return Ok(new PettyCashFundDto(
            fund.Id,
            fund.Code,
            fund.Name,
            fund.CurrencyCode,
            fund.CustodianName,
            fund.Location,
            fund.Notes,
            fund.AuthorizedFloat,
            fund.TransactionLimit,
            fund.AdvanceLimit,
            fund.RequireReceipt,
            fund.BlockOverdueAdvances,
            fund.SettlementShortageExpenseAccountId,
            fund.SettlementShortageExpenseAccount?.Code,
            fund.SettlementShortageExpenseAccount?.Name,
            fund.SettlementShortageCostCenterCode,
            fund.CashCountFrequency,
            fund.LastCashCountAt,
            fund.NextCashCountDueAt,
            fund.IsActive,
            fund.Balance,
            fund.Transactions
                .OrderByDescending(x => x.OccurredAt)
                .Select(x => new PettyCashTransactionDto(
                    x.Id,
                    x.OccurredAt,
                    x.Type,
                    x.Direction,
                    x.Amount,
                    x.SignedAmount,
                    x.ReferenceType,
                    x.ReferenceId,
                    x.ReferenceNumber,
                    x.Notes,
                    x.PettyCashRequestLineId))
                .ToList(),
            fund.CashCounts
                .OrderByDescending(x => x.CountedAt)
                .Select(ToCashCountDto)
                .ToList()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PettyCashFundDto>> Update(Guid id, UpdatePettyCashFundRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashFundEdit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.UpdatePettyCashFundAsync(
            id,
            request.Code,
            request.Name,
            request.CurrencyCode,
            request.CustodianName,
            request.Notes,
            request.IsActive,
            request.Location,
            request.AuthorizedFloat,
            request.TransactionLimit,
            request.AdvanceLimit,
            request.RequireReceipt ?? true,
            request.BlockOverdueAdvances ?? true,
            request.SettlementShortageExpenseAccountId,
            request.SettlementShortageCostCenterCode,
            request.CashCountFrequency ?? PettyCashCashCountFrequency.Weekly,
            request.NextCashCountDueAt,
            cancellationToken);

        await NotifyPettyCashFundCreatorAsync(id, "Petty cash fund updated", "Your petty cash fund has been updated.", cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("{id:guid}/top-ups")]
    public async Task<ActionResult> TopUp(Guid id, AddPettyCashTopUpRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashFundTopUp, cancellationToken))
        {
            return Forbid();
        }

        await financeService.AddPettyCashTopUpAsync(
            id,
            request.Amount,
            request.OccurredAt,
            request.ReferenceNumber,
            request.Notes,
            cancellationToken);
        await NotifyPettyCashFundCreatorAsync(id, "Petty cash fund topped up", "A top-up was added to your petty cash fund.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cash-counts")]
    public async Task<ActionResult<PettyCashCashCountDto>> CreateCashCount(
        Guid id,
        CreatePettyCashCashCountRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashCashCountCreate, cancellationToken))
        {
            return Forbid();
        }

        var countedByUserId = CurrentUserId()
                              ?? throw new UnauthorizedAccessException("Signed-in user id is unavailable.");
        var countedByName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? "Unknown user";
        var cashCountId = await financeService.CreatePettyCashCashCountAsync(
            id,
            request.CountedAt,
            countedByUserId,
            countedByName,
            request.PhysicalCash,
            request.SupportedExpenseVouchers,
            request.Notes,
            cancellationToken);

        var cashCount = await dbContext.PettyCashCashCounts.AsNoTracking()
            .FirstAsync(x => x.Id == cashCountId, cancellationToken);
        var approvers = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.FinancePettyCashCashCountApprove],
            excludeUserId: countedByUserId,
            cancellationToken);
        notificationService.EnqueueInAppForUsers(
            approvers,
            "Petty cash count waiting",
            $"{cashCount.Number} requires independent approval. Variance: {cashCount.Variance:0.00}.",
            $"/finance/petty-cash/{id}",
            ReferenceTypes.PettyCashCashCount,
            cashCount.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToCashCountDto(cashCount));
    }

    [HttpPost("cash-counts/{cashCountId:guid}/approve")]
    public async Task<ActionResult> ApproveCashCount(Guid cashCountId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashCashCountApprove, cancellationToken))
        {
            return Forbid();
        }

        await financeService.ApprovePettyCashCashCountAsync(
            cashCountId,
            CurrentUserId() ?? Guid.Empty,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("cash-counts/{cashCountId:guid}/reject")]
    public async Task<ActionResult> RejectCashCount(
        Guid cashCountId,
        RejectPettyCashCashCountRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashCashCountApprove, cancellationToken))
        {
            return Forbid();
        }

        await financeService.RejectPettyCashCashCountAsync(
            cashCountId,
            CurrentUserId() ?? Guid.Empty,
            request.Reason,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/adjustments")]
    public async Task<ActionResult> Adjust(Guid id, AddPettyCashAdjustmentRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.FinancePettyCashFundAdjust, cancellationToken))
        {
            return Forbid();
        }

        await financeService.AddPettyCashAdjustmentAsync(
            id,
            request.Amount,
            request.Direction,
            request.OccurredAt,
            request.ReferenceNumber,
            request.Notes,
            cancellationToken);
        await NotifyPettyCashFundCreatorAsync(id, "Petty cash fund adjusted", "An adjustment was added to your petty cash fund.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private Guid? CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    private static PettyCashCashCountDto ToCashCountDto(PettyCashCashCount cashCount) => new(
        cashCount.Id,
        cashCount.Number,
        cashCount.PettyCashFundId,
        cashCount.CountedAt,
        cashCount.CountedByUserId,
        cashCount.CountedByName,
        cashCount.PhysicalCash,
        cashCount.OutstandingAdvances,
        cashCount.SupportedExpenseVouchers,
        cashCount.AuthorizedFloatSnapshot,
        cashCount.Accountability,
        cashCount.Variance,
        cashCount.Notes,
        cashCount.Status,
        cashCount.ApprovedAt,
        cashCount.ApprovedByUserId,
        cashCount.RejectedAt,
        cashCount.RejectedByUserId,
        cashCount.RejectionReason);

    private async Task NotifyPettyCashFundCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var fund = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Code, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (fund?.CreatedBy is null)
        {
            return;
        }

        notificationService.EnqueueInApp(
            fund.CreatedBy.Value,
            title,
            $"{fund.Code}: {message}",
            $"/finance/petty-cash/{fund.Id}",
            ReferenceTypes.PettyCashFund,
            fund.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
