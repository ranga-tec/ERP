using ISS.Domain.Common;
using ISS.Domain.Finance;

namespace ISS.UnitTests.Domain;

public sealed class FinanceTests
{
    [Fact]
    public void PettyCashIouApprovalBatch_Approves_Editable_Breakdown_And_Receives_One_Funding()
    {
        var reviewerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var firstIouId = Guid.NewGuid();
        var secondIouId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var batch = new PettyCashIouApprovalBatch(
            "PCAB000001",
            Guid.NewGuid(),
            reviewerId,
            "Accountant",
            approverId,
            "Approver",
            now);
        var first = batch.AddLine(firstIouId, 100m);
        var second = batch.AddLine(secondIouId, 80m);

        batch.ApproveAssigned(approverId, now, new Dictionary<Guid, decimal>
        {
            [first.Id] = 90m,
            [second.Id] = 80m,
        });
        batch.SubmitToHeadOffice(reviewerId, now);
        batch.ApproveHeadOffice(Guid.NewGuid(), now);
        batch.ReceiveFunding(reviewerId, now, "HO-REM-001");

        Assert.Equal(180m, batch.RequestedTotal);
        Assert.Equal(170m, batch.ApprovedTotal);
        Assert.Equal(PettyCashIouApprovalBatchStatus.FundingReceived, batch.Status);
        Assert.Equal("HO-REM-001", batch.FundingReference);
    }

    [Fact]
    public void Ar_Ap_ApplyPayment_Requires_Positive()
    {
        var ar = new AccountsReceivableEntry(Guid.NewGuid(), "INV", Guid.NewGuid(), 100m, DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => ar.ApplyPayment(0));
        ar.ApplyPayment(40m);
        Assert.Equal(60m, ar.Outstanding);

        var ap = new AccountsPayableEntry(Guid.NewGuid(), "GRN", Guid.NewGuid(), 50m, DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => ap.ApplyPayment(-1));
        ap.ApplyPayment(10m);
        Assert.Equal(40m, ap.Outstanding);
    }

    [Fact]
    public void Payment_Can_Allocate_To_Entries()
    {
        var payment = new Payment("PAY0001", PaymentDirection.Incoming, CounterpartyType.Customer, Guid.NewGuid(), null, "USD", 1m, 100m, DateTimeOffset.UtcNow, null);
        payment.AllocateToAr(Guid.NewGuid(), 25m);
        Assert.Single(payment.Allocations);
    }

    [Fact]
    public void LedgerAccount_Defaults_To_Active_And_Allows_Updates()
    {
        var account = new LedgerAccount("1100", "Cash on Hand", LedgerAccountType.Asset, null, true, "Main cash account");

        Assert.True(account.IsActive);
        Assert.True(account.AllowsPosting);
        Assert.Equal(LedgerAccountType.Asset, account.AccountType);

        account.Update("1101", "Petty Cash", LedgerAccountType.Asset, Guid.NewGuid(), false, "Grouped under cash", false);

        Assert.Equal("1101", account.Code);
        Assert.Equal("Petty Cash", account.Name);
        Assert.False(account.AllowsPosting);
        Assert.False(account.IsActive);
        Assert.Equal("Grouped under cash", account.Description);
        Assert.NotNull(account.ParentAccountId);
    }

    [Fact]
    public void PettyCashIou_Release_Records_Collector_And_Requires_Signed_Slip()
    {
        var requesterId = Guid.NewGuid();
        var collectorId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var assignedApproverId = Guid.NewGuid();
        var fundId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var iou = new PettyCashIou(
            "IOU0001",
            Guid.NewGuid(),
            requesterId,
            "Job supervisor",
            500m,
            "Parts for service job",
            now,
            now.AddDays(2));

        iou.Submit(now);
        iou.AssignForApproval(reviewerId, "Receiver", assignedApproverId, "Operational approver", now);
        iou.ApproveAssigned(assignedApproverId, now);
        iou.SubmitToHeadOffice(reviewerId, now);
        iou.Approve(Guid.NewGuid(), now);

        Assert.Throws<DomainValidationException>(() =>
            iou.Release(fundId, 500m, now, null, "", collectorId, "Technician", Guid.NewGuid()));

        iou.Release(fundId, 200m, now, null, "SLIP-100", collectorId, "Technician", null);

        Assert.Equal(PettyCashIouStatus.Released, iou.Status);
        Assert.Equal(200m, iou.ReleasedAmount);
        Assert.Equal(300m, iou.RemainingReleaseAmount);
        iou.Release(fundId, 300m, now, null, "SLIP-101", collectorId, "Technician", null);
        Assert.Equal(500m, iou.ReleasedAmount);
        Assert.Equal(0m, iou.RemainingReleaseAmount);
        Assert.Equal(requesterId, iou.RequestedByUserId);
        Assert.Equal(collectorId, iou.IssuedToUserId);
        Assert.Equal("Technician", iou.IssuedToName);
        Assert.Equal("SLIP-101", iou.IssueBillNumber);
    }

    [Fact]
    public void PettyCashIou_Can_Be_Edited_While_Submitted_But_Not_After_Approval()
    {
        var now = DateTimeOffset.UtcNow;
        var originalJobId = Guid.NewGuid();
        var updatedJobId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var assignedApproverId = Guid.NewGuid();
        var iou = new PettyCashIou(
            "IOU0002",
            originalJobId,
            requesterId,
            "Requester",
            100m,
            "Original purpose",
            now,
            now.AddDays(1));

        iou.Submit(now);
        iou.UpdateBeforeApproval(updatedJobId, 125.75m, "Updated purpose", now.AddDays(3));

        Assert.Equal(PettyCashIouStatus.Submitted, iou.Status);
        Assert.Equal(updatedJobId, iou.ServiceJobId);
        Assert.Equal(125.75m, iou.Amount);
        Assert.Equal("Updated purpose", iou.Purpose);
        Assert.Equal(now.AddDays(3), iou.ExpectedSettlementAt);

        iou.AssignForApproval(reviewerId, "Receiver", assignedApproverId, "Operational approver", now.AddMinutes(1));
        Assert.Equal(PettyCashIouStatus.AwaitingAssignedApproval, iou.Status);
        Assert.Throws<DomainValidationException>(() => iou.ApproveAssigned(Guid.NewGuid(), now.AddMinutes(2)));

        iou.UpdateBeforeApproval(updatedJobId, 130m, "Approver-adjusted purpose", now.AddDays(4));
        iou.ApproveAssigned(assignedApproverId, now.AddMinutes(2));
        Assert.Equal(PettyCashIouStatus.ReturnedToReviewer, iou.Status);
        Assert.Throws<DomainValidationException>(() => iou.SubmitToHeadOffice(Guid.NewGuid(), now.AddMinutes(3)));

        iou.SubmitToHeadOffice(reviewerId, now.AddMinutes(3));
        iou.Approve(Guid.NewGuid(), now.AddMinutes(4));

        Assert.Throws<DomainValidationException>(() =>
            iou.UpdateBeforeApproval(originalJobId, 150m, "Too late", now.AddDays(4)));
    }

    [Fact]
    public void PettyCashFund_HeadOfficeIouFunding_Allows_Release_From_Zero_Balance()
    {
        var fund = new PettyCashFund("PC-HO", "Head office IOU fund", "LKR", null, null);
        var iouId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var funding = fund.RecordHeadOfficeIouFunding(400m, now, iouId, "SLIP-200", "First instalment");
        var release = fund.RecordIouRelease(400m, now, iouId, "SLIP-200", "Released to employee");

        Assert.Equal(PettyCashTransactionType.HeadOfficeIouFunding, funding.Type);
        Assert.Equal(PettyCashTransactionDirection.In, funding.Direction);
        Assert.Equal(PettyCashTransactionType.IouRelease, release.Type);
        Assert.Equal(PettyCashTransactionDirection.Out, release.Direction);
        Assert.Equal(0m, fund.Balance);
        Assert.Equal(2, fund.Transactions.Count);
    }

    [Fact]
    public void PettyCashReturn_Requires_Category_Lines_And_HeadOffice_Receipt_Evidence()
    {
        var now = DateTimeOffset.UtcNow;
        var preparedBy = Guid.NewGuid();
        var receivedBy = Guid.NewGuid();
        var categoryLineId = Guid.NewGuid();
        var pettyCashReturn = new PettyCashReturn(
            "PCRTN0001",
            Guid.NewGuid(),
            preparedBy,
            "Site accountant",
            now,
            "Month-end return");

        Assert.Throws<DomainValidationException>(() => pettyCashReturn.Submit(now));

        pettyCashReturn.AddLine(categoryLineId, 250m);
        Assert.Throws<DomainValidationException>(() => pettyCashReturn.AddLine(categoryLineId, 1m));
        pettyCashReturn.Submit(now);

        Assert.Equal(PettyCashReturnStatus.Submitted, pettyCashReturn.Status);
        Assert.Equal(250m, pettyCashReturn.TotalAmount);
        Assert.Throws<DomainValidationException>(() => pettyCashReturn.ConfirmReceived(receivedBy, now, ""));

        pettyCashReturn.ConfirmReceived(receivedBy, now, "DEP-100");

        Assert.Equal(PettyCashReturnStatus.Received, pettyCashReturn.Status);
        Assert.Equal("DEP-100", pettyCashReturn.ReceiptReference);
        Assert.Equal(receivedBy, pettyCashReturn.ReceivedByUserId);
    }

    [Fact]
    public void PettyCashFund_HeadOfficeReturn_Reduces_Fund_And_Original_Category()
    {
        var now = DateTimeOffset.UtcNow;
        var fund = new PettyCashFund("SITE", "Site float", "LKR", "Accountant", null);
        var requestId = Guid.NewGuid();
        var categoryLineId = Guid.NewGuid();
        fund.RecordRequestFunding(500m, now, requestId, categoryLineId, "TRF-1", null);

        var transaction = fund.RecordHeadOfficeReturn(
            125m,
            now.AddDays(1),
            Guid.NewGuid(),
            categoryLineId,
            "DEP-100",
            null);

        Assert.Equal(PettyCashTransactionType.HeadOfficeReturn, transaction.Type);
        Assert.Equal(PettyCashTransactionDirection.Out, transaction.Direction);
        Assert.Equal("PCRTN", transaction.ReferenceType);
        Assert.Equal(375m, fund.Balance);
        Assert.Equal(375m, fund.BalanceForRequestLine(categoryLineId));
        Assert.Throws<DomainValidationException>(() =>
            fund.RecordHeadOfficeReturn(376m, now, Guid.NewGuid(), categoryLineId, "DEP-101", null));
    }

    [Fact]
    public void PettyCashReallocation_Requires_Different_Categories_And_Approval_Workflow()
    {
        var now = DateTimeOffset.UtcNow;
        var sourceLineId = Guid.NewGuid();
        var destinationLineId = Guid.NewGuid();
        Assert.Throws<DomainValidationException>(() => new PettyCashReallocation(
            "PCRAL0001",
            Guid.NewGuid(),
            sourceLineId,
            sourceLineId,
            200m,
            "Unexpected expense",
            Guid.NewGuid(),
            "Site accountant",
            now));

        var reallocation = new PettyCashReallocation(
            "PCRAL0001",
            Guid.NewGuid(),
            sourceLineId,
            destinationLineId,
            200m,
            "Unexpected expense",
            Guid.NewGuid(),
            "Site accountant",
            now);
        reallocation.Submit(now.AddMinutes(1));
        reallocation.Approve(Guid.NewGuid(), now.AddMinutes(2));

        Assert.Equal(PettyCashReallocationStatus.Approved, reallocation.Status);
        Assert.Equal(200m, reallocation.Amount);
        Assert.NotNull(reallocation.ApprovedAt);
    }

    [Fact]
    public void PettyCashFund_CategoryReallocation_Changes_SubAccounts_But_Not_Fund_Total()
    {
        var now = DateTimeOffset.UtcNow;
        var fund = new PettyCashFund("SITE", "Site float", "LKR", "Accountant", null);
        var requestId = Guid.NewGuid();
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        fund.RecordRequestFunding(20m, now, requestId, categoryA, "TRF-A", null);
        fund.RecordRequestFunding(200m, now, requestId, categoryB, "TRF-B", null);

        var transactions = fund.RecordCategoryReallocation(
            200m,
            now.AddMinutes(1),
            Guid.NewGuid(),
            categoryB,
            categoryA,
            "PCRAL0001",
            "Move spare transport balance to the urgent category");

        Assert.Equal(2, transactions.Count);
        Assert.Equal(PettyCashTransactionType.CategoryTransferOut, transactions[0].Type);
        Assert.Equal(PettyCashTransactionType.CategoryTransferIn, transactions[1].Type);
        Assert.Equal(220m, fund.Balance);
        Assert.Equal(220m, fund.BalanceForRequestLine(categoryA));
        Assert.Equal(0m, fund.BalanceForRequestLine(categoryB));
        Assert.Throws<DomainValidationException>(() => fund.RecordCategoryReallocation(
            1m,
            now.AddMinutes(2),
            Guid.NewGuid(),
            categoryB,
            categoryA,
            "PCRAL0002",
            "No balance remains"));
    }
}
