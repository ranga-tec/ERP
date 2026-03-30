using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/petty-cash-funds")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class PettyCashFundsController(
    IIssDbContext dbContext,
    FinanceService financeService) : ControllerBase
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
        string? Notes);

    public sealed record PettyCashFundSummaryDto(
        Guid Id,
        string Code,
        string Name,
        string CurrencyCode,
        string? CustodianName,
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
        string? Notes,
        bool IsActive,
        decimal Balance,
        IReadOnlyList<PettyCashTransactionDto> Transactions);

    public sealed record CreatePettyCashFundRequest(
        string Code,
        string Name,
        string CurrencyCode,
        string? CustodianName,
        string? Notes,
        decimal? OpeningBalance,
        DateTimeOffset? OpenedAt,
        string? OpeningReferenceNumber);

    public sealed record UpdatePettyCashFundRequest(
        string Code,
        string Name,
        string CurrencyCode,
        string? CustodianName,
        string? Notes,
        bool IsActive);

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
        var funds = await dbContext.PettyCashFunds.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new PettyCashFundSummaryDto(
                x.Id,
                x.Code,
                x.Name,
                x.CurrencyCode,
                x.CustodianName,
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
        var id = await financeService.CreatePettyCashFundAsync(
            request.Code,
            request.Name,
            request.CurrencyCode,
            request.CustodianName,
            request.Notes,
            request.OpeningBalance,
            request.OpenedAt,
            request.OpeningReferenceNumber,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PettyCashFundDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var fund = await dbContext.PettyCashFunds.AsNoTracking()
            .Include(x => x.Transactions)
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
            fund.Notes,
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
                    x.Notes))
                .ToList()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PettyCashFundDto>> Update(Guid id, UpdatePettyCashFundRequest request, CancellationToken cancellationToken)
    {
        await financeService.UpdatePettyCashFundAsync(
            id,
            request.Code,
            request.Name,
            request.CurrencyCode,
            request.CustodianName,
            request.Notes,
            request.IsActive,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpPost("{id:guid}/top-ups")]
    public async Task<ActionResult> TopUp(Guid id, AddPettyCashTopUpRequest request, CancellationToken cancellationToken)
    {
        await financeService.AddPettyCashTopUpAsync(
            id,
            request.Amount,
            request.OccurredAt,
            request.ReferenceNumber,
            request.Notes,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/adjustments")]
    public async Task<ActionResult> Adjust(Guid id, AddPettyCashAdjustmentRequest request, CancellationToken cancellationToken)
    {
        await financeService.AddPettyCashAdjustmentAsync(
            id,
            request.Amount,
            request.Direction,
            request.OccurredAt,
            request.ReferenceNumber,
            request.Notes,
            cancellationToken);
        return NoContent();
    }
}
