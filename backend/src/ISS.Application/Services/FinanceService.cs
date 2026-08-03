using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Domain.Common;
using ISS.Domain.Finance;
using ISS.Domain.Service;
using Microsoft.EntityFrameworkCore;

namespace ISS.Application.Services;

public sealed class FinanceService(
    IIssDbContext dbContext,
    IDocumentNumberService documentNumberService,
    IClock clock)
{
    public async Task<Guid> CreatePettyCashFundAsync(
        string code,
        string name,
        string currencyCode,
        string? custodianName,
        string? notes,
        decimal? openingBalance,
        DateTimeOffset? openedAt,
        string? openingReferenceNumber,
        CancellationToken cancellationToken = default)
    {
        await EnsureCurrencyIsActiveAsync(currencyCode, cancellationToken);

        var fund = new PettyCashFund(code, name, currencyCode, custodianName, notes);
        await dbContext.PettyCashFunds.AddAsync(fund, cancellationToken);

        if (openingBalance is > 0m)
        {
            var openingTransaction = fund.AddOpeningBalance(
                openingBalance.Value,
                openedAt ?? clock.UtcNow,
                openingReferenceNumber,
                "Opening balance");
            dbContext.DbContext.Add(openingTransaction);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return fund.Id;
    }

    public async Task UpdatePettyCashFundAsync(
        Guid pettyCashFundId,
        string code,
        string name,
        string currencyCode,
        string? custodianName,
        string? notes,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var fund = await dbContext.PettyCashFunds
            .FirstOrDefaultAsync(x => x.Id == pettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        await EnsureCurrencyIsActiveAsync(currencyCode, cancellationToken);
        fund.Update(code, name, currencyCode, custodianName, notes, isActive);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPettyCashTopUpAsync(
        Guid pettyCashFundId,
        decimal amount,
        DateTimeOffset? occurredAt,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var fund = await dbContext.PettyCashFunds
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == pettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        var transaction = fund.AddTopUp(amount, occurredAt ?? clock.UtcNow, referenceNumber, notes);
        dbContext.DbContext.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPettyCashAdjustmentAsync(
        Guid pettyCashFundId,
        decimal amount,
        PettyCashTransactionDirection direction,
        DateTimeOffset? occurredAt,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var fund = await dbContext.PettyCashFunds
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == pettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        var transaction = fund.AddAdjustment(amount, direction, occurredAt ?? clock.UtcNow, referenceNumber, notes);
        dbContext.DbContext.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreatePettyCashIouAsync(
        Guid serviceJobId,
        Guid requestedByUserId,
        string requestedByName,
        decimal amount,
        string purpose,
        DateTimeOffset? expectedSettlementAt,
        Guid? serviceJobDailySheetId = null,
        CancellationToken cancellationToken = default)
    {
        var jobStatus = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => x.Id == serviceJobId)
            .Select(x => (ServiceJobStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        if (jobStatus == ServiceJobStatus.Closed)
        {
            throw new DomainValidationException("Closed service jobs cannot receive new IOUs.");
        }

        if (serviceJobDailySheetId is not null)
        {
            var dailySheet = await dbContext.ServiceJobDailySheets.AsNoTracking()
                .Where(x => x.Id == serviceJobDailySheetId.Value)
                .Select(x => new { x.ServiceJobId, x.Status })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Service job daily sheet not found.");

            if (dailySheet.ServiceJobId != serviceJobId)
            {
                throw new DomainValidationException("Daily sheet does not belong to this service job.");
            }

            if (dailySheet.Status == ServiceJobDailySheetStatus.Approved)
            {
                throw new DomainValidationException("Approved daily sheets cannot receive new IOUs.");
            }
        }

        var number = await documentNumberService.NextAsync("IOU", "IOU", cancellationToken);
        var iou = new PettyCashIou(
            number,
            serviceJobId,
            requestedByUserId,
            requestedByName,
            amount,
            purpose,
            clock.UtcNow,
            expectedSettlementAt,
            serviceJobDailySheetId);
        await dbContext.PettyCashIous.AddAsync(iou, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return iou.Id;
    }

    public async Task UpdatePettyCashIouBeforeApprovalAsync(
        Guid iouId,
        Guid serviceJobId,
        decimal amount,
        string purpose,
        DateTimeOffset? expectedSettlementAt,
        CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous
            .FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
            ?? throw new NotFoundException("Petty cash IOU not found.");

        var jobStatus = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => x.Id == serviceJobId)
            .Select(x => (ServiceJobStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        if (jobStatus == ServiceJobStatus.Closed)
        {
            throw new DomainValidationException("Closed service jobs cannot be assigned to IOUs.");
        }

        if (iou.ServiceJobDailySheetId is { } dailySheetId)
        {
            var dailySheet = await dbContext.ServiceJobDailySheets.AsNoTracking()
                .Where(x => x.Id == dailySheetId)
                .Select(x => new { x.ServiceJobId, x.Status })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Service job daily sheet not found.");

            if (dailySheet.ServiceJobId != serviceJobId)
            {
                throw new DomainValidationException("The IOU's daily sheet does not belong to the selected service job.");
            }

            if (dailySheet.Status == ServiceJobDailySheetStatus.Approved)
            {
                throw new DomainValidationException("IOUs linked to approved daily sheets cannot be edited.");
            }
        }

        iou.UpdateBeforeApproval(serviceJobId, amount, purpose, expectedSettlementAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreatePettyCashRequestAsync(
        Guid pettyCashFundId,
        Guid requestedByUserId,
        string requestedByName,
        DateTimeOffset? neededByAt,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var fund = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == pettyCashFundId)
            .Select(x => new { x.Id, x.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        if (!fund.IsActive)
        {
            throw new DomainValidationException("Inactive petty cash funds cannot receive new requests.");
        }

        var number = await documentNumberService.NextAsync(ReferenceTypes.PettyCashRequest, "PCR", cancellationToken);
        var request = new PettyCashRequest(
            number,
            pettyCashFundId,
            requestedByUserId,
            requestedByName,
            clock.UtcNow,
            neededByAt,
            notes);

        await dbContext.PettyCashRequests.AddAsync(request, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return request.Id;
    }

    public async Task UpdatePettyCashRequestHeaderAsync(
        Guid requestId,
        DateTimeOffset? neededByAt,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);
        request.UpdateHeader(neededByAt, notes);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddPettyCashRequestLineAsync(
        Guid requestId,
        PettyCashRequestCategory category,
        Guid? serviceJobId,
        string? customCategoryName,
        string purpose,
        decimal requestedAmount,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);
        await EnsureJobIsOpenForCategoryAsync(category, serviceJobId, cancellationToken);

        var line = request.AddLine(category, serviceJobId, customCategoryName, purpose, requestedAmount);
        dbContext.DbContext.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);
        return line.Id;
    }

    public async Task UpdatePettyCashRequestLineAsync(
        Guid requestId,
        Guid lineId,
        PettyCashRequestCategory category,
        Guid? serviceJobId,
        string? customCategoryName,
        string purpose,
        decimal requestedAmount,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);
        await EnsureJobIsOpenForCategoryAsync(category, serviceJobId, cancellationToken);

        request.UpdateLine(lineId, category, serviceJobId, customCategoryName, purpose, requestedAmount);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePettyCashRequestLineAsync(Guid requestId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);
        request.RemoveLine(lineId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitPettyCashRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);
        request.Submit(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApprovePettyCashRequestAsync(
        Guid requestId,
        Guid approvedByUserId,
        IReadOnlyDictionary<Guid, decimal> approvedAmountsByLineId,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);
        request.Approve(approvedByUserId, clock.UtcNow, approvedAmountsByLineId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectPettyCashRequestAsync(Guid requestId, string? reason, CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);
        request.Reject(clock.UtcNow, reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelPettyCashRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);
        request.Cancel();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Head office paying out against approved lines. One call per line even when a single bank
    /// transfer covers several: the caller passes the same payment reference for each, which is how
    /// "funds received separately for each, even in one transfer" is represented. The money reaches
    /// the custodian's float as a fund transaction tagged with the line, so the category sub-balance
    /// and the fund balance come from the same ledger.
    /// </summary>
    public async Task FundPettyCashRequestLineAsync(
        Guid requestId,
        Guid lineId,
        decimal amount,
        DateTimeOffset? fundedAt,
        string? paymentReference,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadPettyCashRequestAsync(requestId, cancellationToken);

        var fund = await dbContext.PettyCashFunds
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == request.PettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        var occurredAt = fundedAt ?? clock.UtcNow;
        var funding = request.RecordFunding(lineId, amount, occurredAt, paymentReference, notes);
        dbContext.DbContext.Add(funding);

        var transaction = fund.RecordRequestFunding(
            amount,
            occurredAt,
            request.Id,
            lineId,
            paymentReference ?? request.Number,
            notes);
        dbContext.DbContext.Add(transaction);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreatePettyCashReturnAsync(
        Guid pettyCashFundId,
        Guid preparedByUserId,
        string preparedByName,
        string? notes,
        IReadOnlyDictionary<Guid, decimal> amountsByRequestLineId,
        CancellationToken cancellationToken = default)
    {
        if (amountsByRequestLineId.Count == 0)
        {
            throw new DomainValidationException("Select at least one funded category to return.");
        }

        var fund = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == pettyCashFundId)
            .Select(x => new { x.Id, x.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        if (!fund.IsActive)
        {
            throw new DomainValidationException("Inactive petty cash funds cannot create new returns.");
        }

        var number = await documentNumberService.NextAsync(ReferenceTypes.PettyCashReturn, "PCRTN", cancellationToken);
        var pettyCashReturn = new PettyCashReturn(
            number,
            pettyCashFundId,
            preparedByUserId,
            preparedByName,
            clock.UtcNow,
            notes);

        foreach (var (lineId, amount) in amountsByRequestLineId)
        {
            pettyCashReturn.AddLine(lineId, amount);
        }

        await dbContext.PettyCashReturns.AddAsync(pettyCashReturn, cancellationToken);
        await ValidatePettyCashReturnAsync(pettyCashReturn, requireReconciledIous: false, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return pettyCashReturn.Id;
    }

    public async Task SubmitPettyCashReturnAsync(Guid pettyCashReturnId, CancellationToken cancellationToken = default)
    {
        var pettyCashReturn = await LoadPettyCashReturnAsync(pettyCashReturnId, cancellationToken);
        await ValidatePettyCashReturnAsync(pettyCashReturn, requireReconciledIous: true, cancellationToken);
        pettyCashReturn.Submit(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReceivePettyCashReturnAsync(
        Guid pettyCashReturnId,
        Guid receivedByUserId,
        DateTimeOffset? receivedAt,
        string receiptReference,
        CancellationToken cancellationToken = default)
    {
        var pettyCashReturn = await LoadPettyCashReturnAsync(pettyCashReturnId, cancellationToken);
        await ValidatePettyCashReturnAsync(pettyCashReturn, requireReconciledIous: true, cancellationToken);

        var fund = await dbContext.PettyCashFunds
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == pettyCashReturn.PettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        var occurredAt = receivedAt ?? clock.UtcNow;
        pettyCashReturn.ConfirmReceived(receivedByUserId, occurredAt, receiptReference);

        foreach (var line in pettyCashReturn.Lines)
        {
            var transaction = fund.RecordHeadOfficeReturn(
                line.Amount,
                occurredAt,
                pettyCashReturn.Id,
                line.PettyCashRequestLineId,
                pettyCashReturn.ReceiptReference!,
                $"Unused petty cash returned to head office on {pettyCashReturn.Number}.");
            dbContext.DbContext.Add(transaction);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectPettyCashReturnAsync(
        Guid pettyCashReturnId,
        Guid rejectedByUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var pettyCashReturn = await LoadPettyCashReturnAsync(pettyCashReturnId, cancellationToken);
        pettyCashReturn.Reject(rejectedByUserId, clock.UtcNow, reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelPettyCashReturnAsync(Guid pettyCashReturnId, CancellationToken cancellationToken = default)
    {
        var pettyCashReturn = await LoadPettyCashReturnAsync(pettyCashReturnId, cancellationToken);
        pettyCashReturn.Cancel(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreatePettyCashReallocationAsync(
        Guid pettyCashFundId,
        Guid sourcePettyCashRequestLineId,
        Guid destinationPettyCashRequestLineId,
        decimal amount,
        string reason,
        Guid requestedByUserId,
        string requestedByName,
        CancellationToken cancellationToken = default)
    {
        var fund = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == pettyCashFundId)
            .Select(x => new { x.Id, x.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        if (!fund.IsActive)
        {
            throw new DomainValidationException("Inactive petty cash funds cannot create new category reallocations.");
        }

        var number = await documentNumberService.NextAsync(
            ReferenceTypes.PettyCashReallocation,
            "PCRAL",
            cancellationToken);
        var reallocation = new PettyCashReallocation(
            number,
            pettyCashFundId,
            sourcePettyCashRequestLineId,
            destinationPettyCashRequestLineId,
            amount,
            reason,
            requestedByUserId,
            requestedByName,
            clock.UtcNow);

        await dbContext.PettyCashReallocations.AddAsync(reallocation, cancellationToken);
        await ValidatePettyCashReallocationAsync(reallocation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return reallocation.Id;
    }

    public async Task SubmitPettyCashReallocationAsync(
        Guid pettyCashReallocationId,
        CancellationToken cancellationToken = default)
    {
        var reallocation = await LoadPettyCashReallocationAsync(pettyCashReallocationId, cancellationToken);
        await ValidatePettyCashReallocationAsync(reallocation, cancellationToken);
        reallocation.Submit(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApprovePettyCashReallocationAsync(
        Guid pettyCashReallocationId,
        Guid approvedByUserId,
        CancellationToken cancellationToken = default)
    {
        var reallocation = await LoadPettyCashReallocationAsync(pettyCashReallocationId, cancellationToken);
        await ValidatePettyCashReallocationAsync(reallocation, cancellationToken);

        var fund = await dbContext.PettyCashFunds
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == reallocation.PettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        var occurredAt = clock.UtcNow;
        reallocation.Approve(approvedByUserId, occurredAt);
        var transactions = fund.RecordCategoryReallocation(
            reallocation.Amount,
            occurredAt,
            reallocation.Id,
            reallocation.SourcePettyCashRequestLineId,
            reallocation.DestinationPettyCashRequestLineId,
            reallocation.Number,
            reallocation.Reason);
        foreach (var transaction in transactions)
        {
            dbContext.DbContext.Add(transaction);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectPettyCashReallocationAsync(
        Guid pettyCashReallocationId,
        Guid rejectedByUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var reallocation = await LoadPettyCashReallocationAsync(pettyCashReallocationId, cancellationToken);
        reallocation.Reject(rejectedByUserId, clock.UtcNow, reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelPettyCashReallocationAsync(
        Guid pettyCashReallocationId,
        CancellationToken cancellationToken = default)
    {
        var reallocation = await LoadPettyCashReallocationAsync(pettyCashReallocationId, cancellationToken);
        reallocation.Cancel(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<PettyCashReallocation> LoadPettyCashReallocationAsync(
        Guid pettyCashReallocationId,
        CancellationToken cancellationToken)
        => await dbContext.PettyCashReallocations
               .FirstOrDefaultAsync(x => x.Id == pettyCashReallocationId, cancellationToken)
           ?? throw new NotFoundException("Petty cash category reallocation not found.");

    private async Task ValidatePettyCashReallocationAsync(
        PettyCashReallocation reallocation,
        CancellationToken cancellationToken)
    {
        var lineIds = new[]
        {
            reallocation.SourcePettyCashRequestLineId,
            reallocation.DestinationPettyCashRequestLineId,
        };
        var categories = await dbContext.PettyCashRequests.AsNoTracking()
            .SelectMany(request => request.Lines.Select(line => new
            {
                request.PettyCashFundId,
                LineId = line.Id,
                line.Purpose,
                FundedAmount = line.Fundings.Sum(x => x.Amount),
            }))
            .Where(x => lineIds.Contains(x.LineId))
            .ToListAsync(cancellationToken);

        if (categories.Count != 2)
        {
            throw new NotFoundException("One or more funded petty cash categories were not found.");
        }

        if (categories.Any(x => x.PettyCashFundId != reallocation.PettyCashFundId))
        {
            throw new DomainValidationException("Both categories must belong to the selected petty cash fund.");
        }

        if (categories.Any(x => x.FundedAmount <= 0m))
        {
            throw new DomainValidationException("Both categories must have received head-office funding before money can be reallocated between them.");
        }

        var sourceBalance = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(fund => fund.Id == reallocation.PettyCashFundId)
            .SelectMany(fund => fund.Transactions)
            .Where(transaction => transaction.PettyCashRequestLineId == reallocation.SourcePettyCashRequestLineId)
            .SumAsync(transaction => transaction.Direction == PettyCashTransactionDirection.In
                ? transaction.Amount
                : -transaction.Amount, cancellationToken);
        var pendingReturns = await dbContext.PettyCashReturns.AsNoTracking()
            .Where(x => x.Status == PettyCashReturnStatus.Submitted)
            .SelectMany(x => x.Lines)
            .Where(x => x.PettyCashRequestLineId == reallocation.SourcePettyCashRequestLineId)
            .SumAsync(x => x.Amount, cancellationToken);
        var pendingReallocations = await dbContext.PettyCashReallocations.AsNoTracking()
            .Where(x => x.Id != reallocation.Id
                        && x.Status == PettyCashReallocationStatus.Submitted
                        && x.SourcePettyCashRequestLineId == reallocation.SourcePettyCashRequestLineId)
            .SumAsync(x => x.Amount, cancellationToken);
        var available = sourceBalance - pendingReturns - pendingReallocations;
        if (reallocation.Amount > available)
        {
            var source = categories.Single(x => x.LineId == reallocation.SourcePettyCashRequestLineId);
            throw new DomainValidationException(
                $"'{source.Purpose}' has only {available:0.00} available after pending returns and reallocations; {reallocation.Amount:0.00} cannot be reallocated.");
        }
    }

    private async Task<PettyCashReturn> LoadPettyCashReturnAsync(Guid pettyCashReturnId, CancellationToken cancellationToken)
        => await dbContext.PettyCashReturns
               .Include(x => x.Lines)
               .FirstOrDefaultAsync(x => x.Id == pettyCashReturnId, cancellationToken)
           ?? throw new NotFoundException("Petty cash return not found.");

    private async Task ValidatePettyCashReturnAsync(
        PettyCashReturn pettyCashReturn,
        bool requireReconciledIous,
        CancellationToken cancellationToken)
    {
        var lineIds = pettyCashReturn.Lines.Select(x => x.PettyCashRequestLineId).Distinct().ToList();
        if (lineIds.Count != pettyCashReturn.Lines.Count)
        {
            throw new DomainValidationException("Each funded category can appear only once on a petty cash return.");
        }

        var categories = await dbContext.PettyCashRequests.AsNoTracking()
            .SelectMany(request => request.Lines.Select(line => new
            {
                request.PettyCashFundId,
                LineId = line.Id,
                line.Purpose,
            }))
            .Where(x => lineIds.Contains(x.LineId))
            .ToListAsync(cancellationToken);

        if (categories.Count != lineIds.Count)
        {
            throw new NotFoundException("One or more funded petty cash categories were not found.");
        }

        if (categories.Any(x => x.PettyCashFundId != pettyCashReturn.PettyCashFundId))
        {
            throw new DomainValidationException("Every return line must belong to the selected petty cash fund.");
        }

        var balances = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(fund => fund.Id == pettyCashReturn.PettyCashFundId)
            .SelectMany(fund => fund.Transactions)
            .Where(transaction => transaction.PettyCashRequestLineId != null
                                  && lineIds.Contains(transaction.PettyCashRequestLineId.Value))
            .GroupBy(transaction => transaction.PettyCashRequestLineId!.Value)
            .Select(group => new
            {
                LineId = group.Key,
                Balance = group.Sum(transaction => transaction.Direction == PettyCashTransactionDirection.In
                    ? transaction.Amount
                    : -transaction.Amount),
            })
            .ToDictionaryAsync(x => x.LineId, x => x.Balance, cancellationToken);

        var reservations = await dbContext.PettyCashReturns.AsNoTracking()
            .Where(x => x.Id != pettyCashReturn.Id && x.Status == PettyCashReturnStatus.Submitted)
            .SelectMany(x => x.Lines)
            .Where(x => lineIds.Contains(x.PettyCashRequestLineId))
            .GroupBy(x => x.PettyCashRequestLineId)
            .Select(group => new { LineId = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, cancellationToken);

        var reallocationReservations = await dbContext.PettyCashReallocations.AsNoTracking()
            .Where(x => x.Status == PettyCashReallocationStatus.Submitted
                        && lineIds.Contains(x.SourcePettyCashRequestLineId))
            .GroupBy(x => x.SourcePettyCashRequestLineId)
            .Select(group => new { LineId = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, cancellationToken);

        foreach (var line in pettyCashReturn.Lines)
        {
            var available = balances.GetValueOrDefault(line.PettyCashRequestLineId)
                            - reservations.GetValueOrDefault(line.PettyCashRequestLineId)
                            - reallocationReservations.GetValueOrDefault(line.PettyCashRequestLineId);
            if (line.Amount > available)
            {
                var category = categories.Single(x => x.LineId == line.PettyCashRequestLineId);
                throw new DomainValidationException(
                    $"'{category.Purpose}' has only {available:0.00} available after pending returns and reallocations; {line.Amount:0.00} cannot be returned.");
            }
        }

        if (!requireReconciledIous)
        {
            return;
        }

        var openIous = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => x.PettyCashRequestLineId != null
                        && lineIds.Contains(x.PettyCashRequestLineId.Value)
                        && (x.Status == PettyCashIouStatus.Released || x.Status == PettyCashIouStatus.Settled))
            .Select(x => new { x.Number, LineId = x.PettyCashRequestLineId!.Value })
            .ToListAsync(cancellationToken);

        if (openIous.Count > 0)
        {
            var numbers = string.Join(", ", openIous.Select(x => x.Number).Distinct().Take(5));
            throw new DomainValidationException(
                $"Settle and obtain head-office approval for the selected categories' open IOUs before returning their balance: {numbers}.");
        }
    }

    private async Task<PettyCashRequest> LoadPettyCashRequestAsync(Guid requestId, CancellationToken cancellationToken)
        => await dbContext.PettyCashRequests
               .Include(x => x.Lines)
               .ThenInclude(line => line.Fundings)
               .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
           ?? throw new NotFoundException("Petty cash request not found.");

    private async Task EnsureJobIsOpenForCategoryAsync(
        PettyCashRequestCategory category,
        Guid? serviceJobId,
        CancellationToken cancellationToken)
    {
        if (category != PettyCashRequestCategory.JobWise || serviceJobId is null)
        {
            return;
        }

        var status = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => x.Id == serviceJobId.Value)
            .Select(x => (ServiceJobStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Service job not found.");

        if (status == ServiceJobStatus.Closed)
        {
            throw new DomainValidationException("Closed service jobs cannot receive new petty cash requests.");
        }
    }

    public async Task SubmitPettyCashIouAsync(Guid iouId, CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous.FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
                  ?? throw new NotFoundException("Petty cash IOU not found.");
        iou.Submit(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApprovePettyCashIouAsync(Guid iouId, Guid approvedByUserId, CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous.FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
                  ?? throw new NotFoundException("Petty cash IOU not found.");
        iou.Approve(approvedByUserId, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectPettyCashIouAsync(Guid iouId, string? reason, CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous.FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
                  ?? throw new NotFoundException("Petty cash IOU not found.");
        iou.Reject(clock.UtcNow, reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleasePettyCashIouAsync(
        Guid iouId,
        Guid pettyCashFundId,
        string? releaseReference,
        string issueBillNumber,
        Guid issuedToUserId,
        string issuedToName,
        Guid? pettyCashRequestLineId = null,
        CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous.FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
                  ?? throw new NotFoundException("Petty cash IOU not found.");

        var trimmedIssueBillNumber = issueBillNumber?.Trim() ?? string.Empty;
        if (trimmedIssueBillNumber.Length == 0)
        {
            throw new DomainValidationException("The signed IOU slip number is required.");
        }

        if (await dbContext.PettyCashIous.AsNoTracking().AnyAsync(
                x => x.Id != iouId
                     && (x.IssueBillNumber == trimmedIssueBillNumber || x.Number == trimmedIssueBillNumber),
                cancellationToken))
        {
            throw new DomainValidationException($"IOU slip {trimmedIssueBillNumber} has already been used.");
        }

        var fund = await dbContext.PettyCashFunds
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == pettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        if (pettyCashRequestLineId is null)
        {
            throw new DomainValidationException("Select the funded job category this advance is being released from.");
        }

        await EnsureRequestLineIsSpendableAsync(
            pettyCashRequestLineId,
            iou.ServiceJobId,
            fund,
            iou.Amount,
            requireJobWise: true,
            cancellationToken);

        iou.Release(
            pettyCashFundId,
            clock.UtcNow,
            releaseReference,
            trimmedIssueBillNumber,
            issuedToUserId,
            issuedToName,
            pettyCashRequestLineId);
        var transaction = fund.RecordIouRelease(
            iou.Amount,
            clock.UtcNow,
            iou.Id,
            iou.Number,
            notes: $"Cash issued to {issuedToName} on IOU slip {trimmedIssueBillNumber}.",
            pettyCashRequestLineId);
        dbContext.DbContext.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Cash handed over on the spot, without a written request. The custodian's proof is the signed
    /// bill, so its number is required - there is no approval trail to fall back on.
    /// </summary>
    public async Task<Guid> IssuePettyCashIouDirectlyAsync(
        Guid? serviceJobId,
        Guid issuedToUserId,
        string issuedToName,
        decimal amount,
        string purpose,
        Guid pettyCashFundId,
        string slipNumber,
        Guid? pettyCashRequestLineId,
        CancellationToken cancellationToken = default)
    {
        var trimmedSlipNumber = slipNumber?.Trim() ?? string.Empty;
        if (trimmedSlipNumber.Length == 0)
        {
            throw new DomainValidationException("The IOU slip number is required - it is the document number for this advance.");
        }

        // The slip book is the source of numbers, so a clash means the same slip is being entered
        // twice. Caught here to say so plainly rather than surface a unique-index violation.
        if (await dbContext.PettyCashIous.AsNoTracking().AnyAsync(x => x.Number == trimmedSlipNumber, cancellationToken))
        {
            throw new DomainValidationException($"IOU slip {trimmedSlipNumber} has already been entered.");
        }

        if (serviceJobId is { } jobId)
        {
            var jobStatus = await dbContext.ServiceJobs.AsNoTracking()
                .Where(x => x.Id == jobId)
                .Select(x => (ServiceJobStatus?)x.Status)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Service job not found.");

            if (jobStatus == ServiceJobStatus.Closed)
            {
                throw new DomainValidationException("Closed service jobs cannot receive new IOUs.");
            }
        }

        var fund = await dbContext.PettyCashFunds
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == pettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        await EnsureRequestLineIsSpendableAsync(
            pettyCashRequestLineId,
            serviceJobId,
            fund,
            amount,
            requireJobWise: false,
            cancellationToken);

        var iou = PettyCashIou.IssueDirectly(
            trimmedSlipNumber,
            serviceJobId,
            issuedToUserId,
            issuedToName,
            amount,
            purpose,
            clock.UtcNow,
            pettyCashFundId,
            pettyCashRequestLineId);

        await dbContext.PettyCashIous.AddAsync(iou, cancellationToken);

        var transaction = fund.RecordIouRelease(
            amount,
            clock.UtcNow,
            iou.Id,
            iou.Number,
            notes: $"Cash issued to {issuedToName} on IOU slip {iou.Number}.",
            pettyCashRequestLineId);
        dbContext.DbContext.Add(transaction);

        await dbContext.SaveChangesAsync(cancellationToken);
        return iou.Id;
    }

    /// <summary>
    /// A bill the holder brought back. The custodian works entirely on the advance; the expense
    /// voucher behind it is found or created here so the spend still reaches job costing and finance
    /// still has a voucher for the books. It stays in draft while bills accumulate - approving the
    /// settlement is what approves these bills.
    /// </summary>
    public async Task AddPettyCashIouBillAsync(
        Guid iouId,
        string description,
        decimal amount,
        bool billableToCustomer,
        string? receiptReference,
        CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
            ?? throw new NotFoundException("Petty cash IOU not found.");

        if (!iou.IsOpenForAccounting)
        {
            throw new DomainValidationException(
                "Bills can only be added to a released advance, and only until head office approves the settlement.");
        }

        var claim = await dbContext.ServiceExpenseClaims
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.PettyCashIouId == iouId && x.Status == ServiceExpenseClaimStatus.Draft, cancellationToken);

        if (claim is null)
        {
            var number = await documentNumberService.NextAsync(ReferenceTypes.ServiceExpenseClaim, "SEC", cancellationToken);
            claim = new ServiceExpenseClaim(
                number,
                iou.ServiceJobId,
                iou.IssuedToUserId ?? iou.RequestedByUserId,
                iou.IssuedToName ?? iou.RequestedByName,
                ServiceExpenseFundingSource.PettyCash,
                clock.UtcNow,
                merchantName: null,
                receiptReference,
                notes: $"Bills accounted against advance {iou.Number}.",
                serviceJobDailySheetId: null,
                pettyCashIouId: iou.Id,
                pettyCashRequestLineId: iou.PettyCashRequestLineId);

            await dbContext.ServiceExpenseClaims.AddAsync(claim, cancellationToken);
        }

        var line = claim.AddLine(null, description, 1m, amount, billableToCustomer);
        dbContext.DbContext.Add(line);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Head office signing off the settlement. This is also what approves the bills gathered against
    /// the advance, so the draft voucher is submitted and approved here and becomes job cost. It is
    /// deliberately never settled: the cash left the box when the advance was released, and settling
    /// it would pay the same money out twice.
    /// </summary>
    public async Task ApprovePettyCashIouSettlementAsync(
        Guid iouId,
        Guid approvedByUserId,
        CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous.FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
                  ?? throw new NotFoundException("Petty cash IOU not found.");

        iou.ApproveSettlement(approvedByUserId, clock.UtcNow);

        var claim = await dbContext.ServiceExpenseClaims
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.PettyCashIouId == iouId && x.Status == ServiceExpenseClaimStatus.Draft, cancellationToken);

        if (claim is { Lines.Count: > 0 })
        {
            claim.Submit(clock.UtcNow);
            claim.Approve(clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// A category can only be spent once head office has actually released money for it, and a
    /// job-wise category only on its own job.
    /// </summary>
    private async Task EnsureRequestLineIsSpendableAsync(
        Guid? pettyCashRequestLineId,
        Guid? serviceJobId,
        PettyCashFund fund,
        decimal amount,
        bool requireJobWise,
        CancellationToken cancellationToken)
    {
        if (pettyCashRequestLineId is null)
        {
            return;
        }

        var line = await dbContext.PettyCashRequests.AsNoTracking()
            .SelectMany(request => request.Lines.Select(line => new
            {
                request.PettyCashFundId,
                line.Id,
                line.Category,
                line.ServiceJobId,
                line.Purpose,
            }))
            .Where(x => x.Id == pettyCashRequestLineId.Value)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Petty cash request line not found.");

        if (line.PettyCashFundId != fund.Id)
        {
            throw new DomainValidationException("The funded category belongs to a different petty cash fund.");
        }

        if (requireJobWise && line.Category != PettyCashRequestCategory.JobWise)
        {
            throw new DomainValidationException("A job advance must be released from a funded Job Wise category.");
        }

        if (line.Category == PettyCashRequestCategory.JobWise && line.ServiceJobId != serviceJobId)
        {
            throw new DomainValidationException($"'{line.Purpose}' was funded for a different job order.");
        }

        var reservedForHeadOfficeReturn = await dbContext.PettyCashReturns.AsNoTracking()
            .Where(x => x.Status == PettyCashReturnStatus.Submitted)
            .SelectMany(x => x.Lines)
            .Where(x => x.PettyCashRequestLineId == pettyCashRequestLineId.Value)
            .SumAsync(x => x.Amount, cancellationToken);
        var reservedForReallocation = await dbContext.PettyCashReallocations.AsNoTracking()
            .Where(x => x.Status == PettyCashReallocationStatus.Submitted
                        && x.SourcePettyCashRequestLineId == pettyCashRequestLineId.Value)
            .SumAsync(x => x.Amount, cancellationToken);
        var available = fund.BalanceForRequestLine(pettyCashRequestLineId.Value)
                        - reservedForHeadOfficeReturn
                        - reservedForReallocation;
        if (available <= 0m)
        {
            throw new DomainValidationException(
                $"No money is available in '{line.Purpose}', so nothing can be issued against it.");
        }

        if (available < amount)
        {
            throw new DomainValidationException(
                $"'{line.Purpose}' has only {available:0.00} available, which is not enough to release {amount:0.00}.");
        }
    }

    /// <summary>
    /// Cash coming back from the holder, in whatever instalments it arrives. It is credited to the
    /// category the advance was drawn from, so the sub-account is restored rather than the float
    /// simply going up - releasing debited that category, and this is the matching credit.
    /// </summary>
    public async Task ReturnPettyCashIouBalanceAsync(
        Guid iouId,
        decimal amount,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous.FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
                  ?? throw new NotFoundException("Petty cash IOU not found.");

        var pettyCashFundId = iou.PettyCashFundId
                              ?? throw new DomainValidationException("This advance was never released from a fund.");

        if (amount > iou.OutstandingAmount)
        {
            throw new DomainValidationException(
                $"Returning {amount} is more than the {iou.OutstandingAmount} still outstanding on this advance.");
        }

        var fund = await dbContext.PettyCashFunds
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == pettyCashFundId, cancellationToken)
            ?? throw new NotFoundException("Petty cash fund not found.");

        iou.AddReturn(amount, clock.UtcNow);

        var transaction = fund.RecordIouSettlement(
            amount,
            clock.UtcNow,
            iou.Id,
            iou.Number,
            reference ?? $"Balance returned on {iou.Number}.",
            iou.PettyCashRequestLineId);
        dbContext.DbContext.Add(transaction);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Closes the accounting on an advance. No amount is passed: what was spent is the advance less
    /// what came back, and the bills behind it are the vouchers already linked to this record.
    /// </summary>
    public async Task SettlePettyCashIouAsync(
        Guid iouId,
        string? settlementReference,
        CancellationToken cancellationToken = default)
    {
        var iou = await dbContext.PettyCashIous.FirstOrDefaultAsync(x => x.Id == iouId, cancellationToken)
                  ?? throw new NotFoundException("Petty cash IOU not found.");

        iou.Settle(clock.UtcNow, settlementReference);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreatePaymentAsync(
        PaymentDirection direction,
        CounterpartyType counterpartyType,
        Guid counterpartyId,
        Guid? paymentTypeId,
        string? currencyCode,
        decimal? exchangeRate,
        decimal amount,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (paymentTypeId is not null)
        {
            var paymentTypeExists = await dbContext.PaymentTypes.AsNoTracking()
                .AnyAsync(x => x.Id == paymentTypeId.Value && x.IsActive, cancellationToken);
            if (!paymentTypeExists)
            {
                throw new DomainValidationException("Selected payment type is invalid or inactive.");
            }
        }

        var baseCurrency = await dbContext.Currencies.AsNoTracking()
            .Where(x => x.IsActive && x.IsBase)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "USD";

        var resolvedCurrencyCode = string.IsNullOrWhiteSpace(currencyCode)
            ? baseCurrency
            : currencyCode.Trim().ToUpperInvariant();

        var currencyExists = await dbContext.Currencies.AsNoTracking()
            .AnyAsync(x => x.Code == resolvedCurrencyCode && x.IsActive, cancellationToken);
        if (!currencyExists)
        {
            throw new DomainValidationException("Selected currency is invalid or inactive.");
        }

        var resolvedExchangeRate = exchangeRate;
        if (resolvedCurrencyCode == baseCurrency)
        {
            resolvedExchangeRate = 1m;
        }
        else if (resolvedExchangeRate is null)
        {
            resolvedExchangeRate = await (
                from rate in dbContext.CurrencyRates.AsNoTracking()
                join fromCurrency in dbContext.Currencies.AsNoTracking() on rate.FromCurrencyId equals fromCurrency.Id
                join toCurrency in dbContext.Currencies.AsNoTracking() on rate.ToCurrencyId equals toCurrency.Id
                where rate.IsActive
                      && fromCurrency.Code == resolvedCurrencyCode
                      && toCurrency.Code == baseCurrency
                orderby rate.EffectiveFrom descending
                select (decimal?)rate.Rate)
                .FirstOrDefaultAsync(cancellationToken);

            if (resolvedExchangeRate is null)
            {
                throw new DomainValidationException($"No active FX rate found from {resolvedCurrencyCode} to {baseCurrency}. Provide exchange rate.");
            }
        }

        var reference = await documentNumberService.NextAsync("PAY", "PAY", cancellationToken);
        var payment = new Payment(reference, direction, counterpartyType, counterpartyId, paymentTypeId, resolvedCurrencyCode, resolvedExchangeRate.Value, amount, clock.UtcNow, notes);
        await dbContext.Payments.AddAsync(payment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }

    public async Task AllocatePaymentToArAsync(Guid paymentId, Guid arEntryId, decimal amount, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
                      ?? throw new NotFoundException("Payment not found.");

        var ar = await dbContext.AccountsReceivableEntries.FirstOrDefaultAsync(x => x.Id == arEntryId, cancellationToken)
                 ?? throw new NotFoundException("AR entry not found.");

        if (amount <= 0)
        {
            throw new DomainValidationException("Allocation amount must be positive.");
        }

        if (amount > ar.Outstanding)
        {
            throw new DomainValidationException("Allocation exceeds outstanding amount.");
        }

        var allocation = payment.AllocateToAr(ar.Id, amount);
        dbContext.DbContext.Add(allocation);
        ar.ApplyPayment(amount);

        await MarkInvoicePaidIfSettledAsync(ar, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AllocatePaymentToApAsync(Guid paymentId, Guid apEntryId, decimal amount, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
                      ?? throw new NotFoundException("Payment not found.");

        var ap = await dbContext.AccountsPayableEntries.FirstOrDefaultAsync(x => x.Id == apEntryId, cancellationToken)
                 ?? throw new NotFoundException("AP entry not found.");

        if (amount <= 0)
        {
            throw new DomainValidationException("Allocation amount must be positive.");
        }

        if (amount > ap.Outstanding)
        {
            throw new DomainValidationException("Allocation exceeds outstanding amount.");
        }

        var allocation = payment.AllocateToAp(ap.Id, amount);
        dbContext.DbContext.Add(allocation);
        ap.ApplyPayment(amount);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateCreditNoteAsync(
        CounterpartyType counterpartyType,
        Guid counterpartyId,
        decimal amount,
        string? notes,
        string? sourceReferenceType = null,
        Guid? sourceReferenceId = null,
        CancellationToken cancellationToken = default)
    {
        var reference = await documentNumberService.NextAsync(ReferenceTypes.CreditNote, ReferenceTypes.CreditNote, cancellationToken);
        var creditNote = new CreditNote(reference, counterpartyType, counterpartyId, amount, clock.UtcNow, notes, sourceReferenceType, sourceReferenceId);
        await dbContext.CreditNotes.AddAsync(creditNote, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return creditNote.Id;
    }

    public async Task<Guid> CreateDebitNoteAsync(
        CounterpartyType counterpartyType,
        Guid counterpartyId,
        decimal amount,
        string? notes,
        string? sourceReferenceType = null,
        Guid? sourceReferenceId = null,
        CancellationToken cancellationToken = default)
    {
        var reference = await documentNumberService.NextAsync(ReferenceTypes.DebitNote, ReferenceTypes.DebitNote, cancellationToken);
        var debitNote = new DebitNote(reference, counterpartyType, counterpartyId, amount, clock.UtcNow, notes, sourceReferenceType, sourceReferenceId);
        await dbContext.DebitNotes.AddAsync(debitNote, cancellationToken);

        if (counterpartyType == CounterpartyType.Customer)
        {
            await dbContext.AccountsReceivableEntries.AddAsync(
                new AccountsReceivableEntry(counterpartyId, ReferenceTypes.DebitNote, debitNote.Id, amount, clock.UtcNow),
                cancellationToken);
        }
        else
        {
            await dbContext.AccountsPayableEntries.AddAsync(
                new AccountsPayableEntry(counterpartyId, ReferenceTypes.DebitNote, debitNote.Id, amount, clock.UtcNow),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return debitNote.Id;
    }

    public async Task AllocateCreditNoteToArAsync(Guid creditNoteId, Guid arEntryId, decimal amount, CancellationToken cancellationToken = default)
    {
        var creditNote = await dbContext.CreditNotes.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == creditNoteId, cancellationToken)
                         ?? throw new NotFoundException("Credit note not found.");

        if (creditNote.CounterpartyType != CounterpartyType.Customer)
        {
            throw new DomainValidationException("Credit note is not for a customer.");
        }

        var ar = await dbContext.AccountsReceivableEntries.FirstOrDefaultAsync(x => x.Id == arEntryId, cancellationToken)
                 ?? throw new NotFoundException("AR entry not found.");

        if (ar.CustomerId != creditNote.CounterpartyId)
        {
            throw new DomainValidationException("AR entry does not belong to this customer.");
        }

        if (amount <= 0)
        {
            throw new DomainValidationException("Allocation amount must be positive.");
        }

        if (amount > ar.Outstanding)
        {
            throw new DomainValidationException("Allocation exceeds outstanding amount.");
        }

        var allocation = creditNote.AllocateToAr(ar.Id, amount);
        dbContext.DbContext.Add(allocation);
        ar.ApplyPayment(amount);
        await MarkInvoicePaidIfSettledAsync(ar, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AllocateCreditNoteToApAsync(Guid creditNoteId, Guid apEntryId, decimal amount, CancellationToken cancellationToken = default)
    {
        var creditNote = await dbContext.CreditNotes.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == creditNoteId, cancellationToken)
                         ?? throw new NotFoundException("Credit note not found.");

        if (creditNote.CounterpartyType != CounterpartyType.Supplier)
        {
            throw new DomainValidationException("Credit note is not for a supplier.");
        }

        var ap = await dbContext.AccountsPayableEntries.FirstOrDefaultAsync(x => x.Id == apEntryId, cancellationToken)
                 ?? throw new NotFoundException("AP entry not found.");

        if (ap.SupplierId != creditNote.CounterpartyId)
        {
            throw new DomainValidationException("AP entry does not belong to this supplier.");
        }

        if (amount <= 0)
        {
            throw new DomainValidationException("Allocation amount must be positive.");
        }

        if (amount > ap.Outstanding)
        {
            throw new DomainValidationException("Allocation exceeds outstanding amount.");
        }

        var allocation = creditNote.AllocateToAp(ap.Id, amount);
        dbContext.DbContext.Add(allocation);
        ap.ApplyPayment(amount);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AutoAllocateCreditNoteAsync(Guid creditNoteId, CancellationToken cancellationToken = default)
    {
        var creditNote = await dbContext.CreditNotes.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == creditNoteId, cancellationToken)
                         ?? throw new NotFoundException("Credit note not found.");

        if (creditNote.RemainingAmount <= 0)
        {
            return;
        }

        if (creditNote.CounterpartyType == CounterpartyType.Customer)
        {
            var arEntries = await dbContext.AccountsReceivableEntries
                .Where(x => x.CustomerId == creditNote.CounterpartyId && x.Outstanding > 0)
                .OrderBy(x => x.PostedAt)
                .ToListAsync(cancellationToken);

            foreach (var ar in arEntries)
            {
                if (creditNote.RemainingAmount <= 0)
                {
                    break;
                }

                var allocate = Math.Min(ar.Outstanding, creditNote.RemainingAmount);
                var allocation = creditNote.AllocateToAr(ar.Id, allocate);
                dbContext.DbContext.Add(allocation);
                ar.ApplyPayment(allocate);
                await MarkInvoicePaidIfSettledAsync(ar, cancellationToken);
            }
        }
        else
        {
            var apEntries = await dbContext.AccountsPayableEntries
                .Where(x => x.SupplierId == creditNote.CounterpartyId && x.Outstanding > 0)
                .OrderBy(x => x.PostedAt)
                .ToListAsync(cancellationToken);

            foreach (var ap in apEntries)
            {
                if (creditNote.RemainingAmount <= 0)
                {
                    break;
                }

                var allocate = Math.Min(ap.Outstanding, creditNote.RemainingAmount);
                var allocation = creditNote.AllocateToAp(ap.Id, allocate);
                dbContext.DbContext.Add(allocation);
                ap.ApplyPayment(allocate);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkInvoicePaidIfSettledAsync(AccountsReceivableEntry ar, CancellationToken cancellationToken)
    {
        if (ar.Outstanding > 0 || ar.ReferenceType != ReferenceTypes.SalesInvoice)
        {
            return;
        }

        var invoice = await dbContext.SalesInvoices.FirstOrDefaultAsync(x => x.Id == ar.ReferenceId, cancellationToken);
        invoice?.MarkPaid();
    }

    private async Task EnsureCurrencyIsActiveAsync(string currencyCode, CancellationToken cancellationToken)
    {
        var resolvedCurrencyCode = string.IsNullOrWhiteSpace(currencyCode)
            ? throw new DomainValidationException("Currency code is required.")
            : currencyCode.Trim().ToUpperInvariant();

        var currencyExists = await dbContext.Currencies.AsNoTracking()
            .AnyAsync(x => x.Code == resolvedCurrencyCode && x.IsActive, cancellationToken);
        if (!currencyExists)
        {
            throw new DomainValidationException("Selected currency is invalid or inactive.");
        }
    }
}
