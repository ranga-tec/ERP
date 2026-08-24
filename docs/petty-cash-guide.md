# Petty cash V2 — current implementation

**Status:** implemented in the current `c-com-erp` branch as of 2026-08-24.
**Purpose:** this is the single authoritative description of the petty-cash process. Historical handover notes, category-funding guidance, and incomplete test-status notes are not operating instructions.

## 1. The rule that explains the design

Petty cash uses three separate records:

| Record | What it answers | What it does not do |
|---|---|---|
| Petty-cash fund (`PCF`) | Where is the physical cash, and who controls it? | It does not earmark cash by job or expense category. |
| Employee IOU (`IOU`) | Who received an advance, for how much, and how was it accounted for? | Releasing an advance does not recognize an expense. |
| Expense voucher (`SEC`) | Why was money spent, where should it post, and is it billable? | It does not create a separate cash sub-ledger. |

Jobs are controlled through a job-level petty-cash authorization. Actual costs are classified on expense-voucher lines. New replenishments, returns, IOUs, and vouchers do not use category balances.

## 2. Current documents and records

| Code | Name | Current purpose |
|---|---|---|
| `PCF` | Petty-cash fund | Physical float, controls, ledger, and cash counts. |
| `PCR` | Replenishment request | Requests one fund-level amount using a reconciliation snapshot. |
| `PCAB` | IOU approval batch | Groups submitted IOUs for assigned and head-office approval. It does not fund category balances. |
| `IOU` | Employee advance | Approval, release, bills, returned cash, and final settlement. |
| `SEC` | Expense voucher | Expense account, job/cost centre, receipt evidence, and billable classification. |
| `PCRTN` | Fund return | Physical cash returned from a fund to head office. |
| `PCCC` | Cash count | Physical cash and accountability reconciliation with independent review. |

## 3. Configure a fund

Go to **Finance → Petty Cash Funds**. A fund records:

- code, name, currency, location, custodian, notes, and active status;
- authorized float: the management-approved accountability ceiling;
- transaction limit: the maximum value of one direct petty-cash expense;
- advance limit: the maximum value of one employee IOU;
- receipt requirement and whether overdue advances block another release;
- settlement-shortage expense account and cost centre;
- cash-count frequency (`None`, `Daily`, `Weekly`, or `Shift close`) and next due date.

`0` means a monetary ceiling is not configured; it does not create a zero-value fund.

### Posting expense account

The posting account selected on a voucher line, including the configured shortage account, must be an active posting **Expense** account. It is not Accounts Receivable, Accounts Payable, Asset, Liability, or Equity. The fund is the cash-custody record; the expense account classifies what the business consumed.

For a non-job overhead expense, enter a cost centre. For a job expense, select the service job. The voucher line can also state whether the cost is billable to the customer.

## 4. Replenish a fund (`PCR`)

### Custodian or site accountant

1. Count the fund's physical cash.
2. Total open employee advances.
3. Total reconciled expenses awaiting replenishment.
4. Create **Finance → Petty Cash Replenishment → New Replenishment** and enter those figures plus one requested amount.
5. Attach the cash count and voucher summary, then submit the request to head office.

“Requested replenishment” is the amount of cash head office is being asked to send back into the fund. It is not the authorized float and it is not an expense-category allocation.

The request is rejected when:

```text
cash on hand + outstanding advances + requested replenishment
    > authorized float
```

when the authorized float is configured.

### Head office

1. Review the reconciliation snapshot and attachments.
2. Approve one amount no greater than the request, or reject it with a reason.
3. After money is actually transferred, record the received amount, date, payment reference, and notes.

Approval authorizes the transfer. Recording receipt is the event that adds a `FundReplenishment` inflow to the fund ledger. Partial receipts produce `PartiallyFunded`; the request becomes `Funded` after the approved amount has been received.

At receipt time the system also prevents:

```text
current fund balance + open employee advances + receipt
    > authorized float
```

## 5. Request, approve, and release an employee advance (`IOU`)

### Request and approval

1. The requester creates an IOU with the employee, purpose, amount, expected settlement date, and job when applicable.
2. The requester submits it.
3. A reviewer creates or updates a `PCAB`, selects one fund and assigned approver, and adds eligible submitted IOUs.
4. The assigned approver reviews the batch and may reduce approved amounts; an approved amount cannot exceed the requested amount.
5. The reviewer forwards the returned batch to head office for final approval.

An IOU cannot belong to two active batches. Batch approval does not add cash to a category and does not need a category-funded balance.

### Release

After head-office approval, the custodian releases each IOU from the selected fund. The custodian records the employee who physically collected the cash, the signed issue-slip number, and the release reference.

Release is blocked when applicable if:

- the fund does not have enough cash;
- the amount exceeds the fund's advance limit;
- the employee has an overdue unsettled advance and the fund blocks overdue advances;
- the job's actual petty-cash expenses plus open advance commitments would exceed its authorization.

Cash release creates an `IouRelease` outflow and an employee accountability. It does not create a job expense. The former “issue directly” route is retired and returns HTTP `410 Gone`.

## 6. Record bills, receipt evidence, and returned cash

Open the released IOU and account for it using bills and cash returns.

### Bill requirements

Each bill becomes an expense-voucher line and records:

- description and amount;
- an active posting Expense account;
- job and/or cost centre as applicable;
- billable-to-customer flag;
- receipt reference;
- a receipt file attached to that exact line.

Line evidence is stored against the voucher line (`SEC-LINE`), not only as a general IOU attachment.

### Missing receipt

If a receipt is genuinely unavailable:

1. Mark that specific line as missing receipt.
2. Enter the reason.
3. Obtain approval from a user with `Finance.PettyCash.ReceiptExceptionApprove` while the voucher is still Draft.

The claimant cannot approve their own missing-receipt exception. A normal line without its receipt reference and line attachment cannot pass submission.

### Returned cash

Record every cash-return instalment against the IOU. Each return adds an `IouSettlement` inflow to the same fund and reduces the employee's outstanding amount.

## 7. Settle and approve an IOU

Normal settlement uses this equality:

```text
released cash = accepted expense bills + returned cash
```

The custodian can settle only when the amount is fully explained, unless a separately authorized shortage exception has been approved. Head office then reviews the expense classifications, evidence, cash returns, and job/cost impact before approving the settlement. `Settlement Approved` is the final closed state.

### Exact shortage exception

If money or support remains unexplained:

1. Attach the explanation/evidence to the IOU.
2. Enter the exception reason.
3. A different higher-level user with the receipt-exception permission approves the exact unaccounted amount.

The system requires the fund's shortage Expense account and cost centre. It creates a linked, approved, non-billable `SEC` voucher for exactly the shortage and links it back to the IOU. The exception cannot be posted twice.

This is an exception path, not a way to round or force settlements to zero.

## 8. Record a petty-cash expense voucher (`SEC`)

Use **Service → Expense Vouchers** when a purchase has already happened. Use an IOU when cash is needed before the purchase.

1. Create the voucher and select `Petty Cash Fund` or `Out of Pocket` as its funding source.
2. Select the job when the expense belongs to a job; otherwise provide the relevant cost centre for petty-cash overhead.
3. Add every line with its Expense account, receipt reference, line receipt attachment, and billable flag.
4. Resolve and independently approve any missing-receipt lines before submission.
5. Submit, approve, and settle the voucher through its controlled workflow.

The previous immediate “pay and post” endpoint is retired and returns HTTP `410 Gone`. A petty-cash category/request line supplied by an old client is rejected for new vouchers.

## 9. Job-level spending control

On the service-job Overview tab, management can set **Petty-cash authorization**. At head-office IOU approval, the system compares:

```text
approved petty-cash expense already charged to the job
+ open/released advance commitments for the job
+ proposed approval
```

against that job authorization. This limit controls spending directly; it does not reserve or move physical fund cash.

## 10. Cash counts and reminders (`PCCC`)

The fund page calculates open advances and supported vouchers. The counter enters physical cash and records a cash count containing snapshots of:

```text
accountability = physical cash + outstanding advances + supported vouchers
variance       = accountability - authorized float
```

The count starts as `Submitted`. A different user with cash-count approval permission must approve or reject it; the person who counted cannot approve their own count. An approval updates the last-count time and calculates the next due time from the fund frequency.

The hosted reminder service checks active funds for due or overdue counts and sends notifications to users who can record or approve counts. A restart does not remove the stored due date.

## 11. Return cash to head office (`PCRTN`)

1. Reconcile the fund and confirm the cash is physically available.
2. Create one fund-level return with the amount, preparer, notes, and evidence.
3. Submit it.
4. Head office counts the cash and records the receipt/deposit reference, or rejects it with a reason.

Receipt creates one `FundReturn` outflow from the fund ledger. It does not reverse a job cost or expense category. New returns do not contain category lines.

## 12. Permissions and segregation

Petty cash uses separate permissions for viewing, creating, editing, submitting, reviewing, assigned approval, head-office approval, rejection, release, settlement, receipt exceptions, fund maintenance, replenishment, return, and cash-count review.

The built-in Finance role is not automatically granted the entire petty-cash permission set. Assign only the permissions required for the person's duty. The important enforced separations are:

- assigned approver versus reviewer/head-office workflow;
- claimant versus missing-receipt approver;
- cash counter versus cash-count approver;
- custodian cash release versus later head-office settlement approval.

All documents use the shared audit, attachment, notification, and reference-number infrastructure. Submitted or approved records are state-controlled rather than silently rewritten.

## 13. Legacy records

Historical category-based PCR lines, PCRTN lines, PCRAL reallocations, category-linked ledger movements, and old directly issued IOUs remain readable for audit and data integrity.

They are not the current process:

- new PCRs are fund-level;
- new PCRTNs are fund-level;
- category reallocations are disabled;
- category-linked voucher creation is rejected;
- direct IOU issue is disabled;
- immediate pay-and-post is disabled.

Do not use historical screens or data shapes as guidance for a new transaction.

## 14. Worked examples

### Example A — requested replenishment

An authorized float is LKR 100,000. Physical cash is LKR 25,000 and open advances are LKR 15,000. Supported expenses are LKR 60,000.

The custodian requests LKR 60,000. The request-time ceiling check is:

```text
25,000 cash + 15,000 advances + 60,000 request = 100,000
```

The request is within the float. The LKR 60,000 supported-expense figure explains the need; it is not added to the ceiling formula because those vouchers represent cash already spent.

### Example B — normal IOU settlement

An employee receives LKR 20,000. Accepted bills total LKR 16,500 and the employee returns LKR 3,500.

```text
20,000 released = 16,500 expenses + 3,500 returned cash
```

The IOU can be settled. LKR 16,500 reaches expense/job cost; the LKR 20,000 release itself is not an expense.

### Example C — documented shortage

An employee receives LKR 20,000, supplies valid bills for LKR 18,000, and returns LKR 1,500. The unaccounted amount is LKR 500.

After evidence and independent approval, the system creates one linked LKR 500 shortage voucher using the configured shortage Expense account and cost centre. It does not post an arbitrary amount.

### Example D — cash-count variance

The authorized float is LKR 100,000. The count records LKR 30,000 cash, LKR 20,000 open advances, and LKR 49,000 supported vouchers.

```text
accountability = 30,000 + 20,000 + 49,000 = 99,000
variance       = 99,000 - 100,000 = -1,000
```

The count stays Submitted until an independent reviewer approves or rejects the LKR 1,000 shortage.

## 15. Implementation map for maintainers

Backend workflow entry points:

- `PettyCashFundsController` — fund controls, ledger, and cash counts;
- `PettyCashRequestsController` — fund replenishment;
- `PettyCashIouApprovalBatchesController` and `PettyCashIousController` — advance approval, release, and settlement;
- `PettyCashReturnsController` — fund returns;
- `ServiceExpenseClaimsController` — expense vouchers, line evidence, and missing-receipt approval.

Core rules are in `FinanceService`, `ServiceManagementService`, and the finance/service domain entities. The two migrations completing this version are:

```text
20260823075344_PettyCashV2FundReplenishment
20260823114447_CompletePettyCashPrdSuggestions
```

User-facing guidance is also available at `/help/petty-cash`. If code behavior and this document diverge, treat that as a defect and update both in the same change.
