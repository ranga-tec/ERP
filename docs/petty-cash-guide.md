# Petty cash — the whole process

Every way money enters, moves through and leaves the petty cash float, and what each document
means. Written against the code, not against intent; file references are given so a claim here can
be checked.

---

## Part 0 — The six documents, and why there are six

| Document | Prefix | What it is | Creates |
| --- | --- | --- | --- |
| Petty Cash Fund | `PCF` | The cash box itself, and its ledger | the float |
| Petty Cash Request | `PCR` | Asking head office for money, by category | money **into** the float |
| Petty Cash Advance (IOU) | `IOU` or the slip no. | Cash handed to a **person**, who must account for it | a **receivable** from that person |
| Expense Voucher | `SEC` | Money spent, evidenced by a bill | an **expense** |
| Petty Cash Return | `PCRTN` | Reconciled unused float handed back to head office, by original funding category | money **out of** the float, with no expense |
| Category Reallocation | `PCRAL` | Head-office-approved transfer of authorization between two funded categories in one float | equal category transfer-out and transfer-in entries; no physical cash movement |

The distinction that matters, and the one people get wrong: **an advance is not an expense.**
Handing someone 1000 does not cost the company 1000; it moves 1000 from the box into that person's
pocket, and they owe it. It becomes cost only when a voucher documents what was bought. This is why
job costing reads vouchers and never reads advances.

The float itself is an **imprest** balance: `PettyCashFund.Balance` is simply the sum of its ledger
(`PettyCashFund.cs`), so every movement below is one row in `PettyCashTransaction` and nothing keeps
a second running total that could drift.

### Transaction types on the fund ledger

| Type | Direction | Written by |
| --- | --- | --- |
| `OpeningBalance` | In | fund created with an opening balance |
| `TopUp` | In | `POST /finance/petty-cash-funds/{id}/top-ups` |
| `RequestFunding` | In | head office funding a request line |
| `IouRelease` | Out | cash handed to a person |
| `IouSettlement` | In | unspent cash returned |
| `ExpenseSettlement` | Out | a voucher paid from the box |
| `Adjustment` | In or Out | `POST /finance/petty-cash-funds/{id}/adjustments` |
| `HeadOfficeReturn` | Out | head office confirms physical receipt of a submitted `PCRTN` |
| `CategoryTransferOut` | Out | head office approves a `PCRAL`; reduces the source category |
| `CategoryTransferIn` | In | the same approval increases the destination category by the same amount |

Outward movements and adjustments check the balance first (`EnsureSufficientBalance`); the box cannot
go negative. An inactive fund refuses every movement **except** opening balance and top-up, which do
not call `EnsureActive` — so a deactivated fund can still be topped up.

---

## Part 1 — Getting money into the float

### 1a. Directly

Opening balance at creation, or a top-up afterwards. Neither is tied to a request, so neither
carries a category. Use this for the initial float and for straight replenishment.

### 1b. By request — the normal route

`Finance → Petty Cash Requests → + New Request`

The site accountant raises one request covering several needs at once. Each need is a **line** with
its own category:

| Category | Names a job? | Notes |
| --- | --- | --- |
| Job Wise | **yes, required** | this is what later lets the spend reach that job's cost |
| Emergency Operation | no | |
| Transportation | no | |
| Custom | no | carries its own name, e.g. "Site refreshments" |

**Status flow** (`PettyCashRequest.cs`):

```
Draft ──submit──► Submitted ──approve──► Approved ──fund──► PartiallyFunded ──fund──► Funded
  │                   │
  └──cancel──►        └──reject──► Rejected
   Cancelled
```

- Only a **Draft** can be edited. Once submitted, lines are frozen.
- Submitting notifies everyone holding `PettyCashRequest.Approve` or `.Fund` — resolved by
  permission, not by role. The submitter is not notified of their own request.
- **Approval is per line.** Head office may approve less than was asked, or zero for a line. It may
  not approve more than was requested. Approving *every* line at zero is refused — that is a
  rejection, and should carry a reason.
- Approval moves no money.

**Funding is per line, and this is the part people ask about.** Head office releases each category
separately. When one bank transfer covers several categories, you fund each line in turn and give
them **the same payment reference** — that is how "funds received separately for each, even in a
single transfer" is represented. The detail page offers the first reference already entered as the
default for the rest.

Each funding posts a `RequestFunding` row **In** on the fund, tagged with the request line.

### Sub-accounts

The custodian keeps categories inside one float. A category's balance is the fund ledger filtered to
that request line — funded in, spent out:

```
PettyCashFund.BalanceForRequestLine(lineId)
  = Σ signed amounts of transactions carrying that line
```

There is no separate sub-ledger to reconcile against the main one. The request detail page shows
`In sub-account` per line.

### Moving an unused balance between categories

Physical cash in the box does not by itself authorize spending from any category. If category A has
20 left and category B has 200 left, a 220 expense that belongs to A must not be posted to B or make
A negative. Use `Finance → Reallocate Category Balance` to request 200 from B to A.

The `PCRAL` starts as Draft, is submitted to head office, and reserves the source balance while it is
pending. Approval posts `CategoryTransferOut` on B and `CategoryTransferIn` on A with the same
document reference. The fund balance is unchanged; A becomes 220 and B becomes 0. Pending returns
and pending reallocations are deducted from availability, so the same balance cannot be promised
twice. Rejected and cancelled reallocations post nothing.

If one bill genuinely contains costs belonging to two categories, allocate its lines by their real
business purpose. Do not split a single-purpose expense merely to bypass the category control.

---

## Part 2 — Getting money out of the float

There are **two** ways, and choosing the right one is the whole of the discipline.

### 2a. Advance against a written request — `Finance → Petty Cash Advances (IOU) → + New IOU`

The classic flow. Someone asks, it is approved, cash is released, they settle later.

```
Draft ─submit─► Submitted ─approve─► Approved ─release─► Released
                    │                                        │
                    └─reject─► Rejected                    settle
                                                             ▼
                                                          Settled ─approve settlement─► SettlementApproved
```

There is no second “Record IOU Slip” creation route. Job staff raise the request because they know
the expected job expense. After approval, the assistant accountant clicks **Release Cash** on that
same IOU and must record:

- the funded **Job Wise** category for the same job;
- the company staff member physically collecting the cash; and
- the number on the signed physical IOU slip.

The selected category determines the fund. Release is refused when the category belongs to another
job or fund, is not Job Wise, is inactive, or has less available than the advance. Release posts
`IouRelease` **Out**. The generated `IOU…` number remains the system request number and the signed
slip number is stored alongside it as the physical handover evidence.

### 2b. Direct payment — expense voucher

Money paid for something already bought — a taxi, a courier. Nobody is left accountable, so this is
not an advance at all. Record it as an **expense voucher**, because there is no employee-held
balance to return later.

- The **bill / receipt number is mandatory**. It is the entire support for the payment.
- The job order is **optional**. Transportation and emergency spend belong to no job; that is
  overhead and it never enters job costing.
- After saving you land on the voucher, which is where the bill image itself is attached.

Use **Not job related (overhead)** for taxis, workshop supplies, emergency operation and similar
general costs. Do not create permanent fake workshop jobs: overhead vouchers are intentionally
excluded from job profitability.

---

## Part 3 — Vouchers (`SEC`)

An expense voucher records money spent. It is the **only** document that reaches job cost.

**Funding source** answers *who gets repaid and from where* — not whether cash was advanced:

| Funding source | Meaning |
| --- | --- |
| Out of Pocket | the claimant used their own money; repaid by payment type |
| Petty Cash Fund | repaid out of the cash box; a fund is mandatory at settlement |

**Status flow** (`ServiceExpenseClaim.cs`):

```
Draft ─submit─► Submitted ─approve─► Approved ─settle─► Settled
                    │
                    └─reject─► Rejected
```

Only a Draft can be edited. Settling a petty-cash voucher posts `ExpenseSettlement` **Out**, tagged
with the funded category if one was chosen.

**Optional links, and what each buys you:**

- **Job order** — omit for overhead. Present means the spend reaches that job's cost.
- **IOU advance** — says this voucher documents money from that advance. Only petty-cash vouchers
  may link, and only to an advance on the same job that has actually been released.
- **Funded category** — charges the spend to that sub-account. Only petty-cash vouchers may link,
  only to a category that has money released, and a Job Wise category only accepts vouchers on its
  own job.

**Attachments:** vouchers support them (`referenceType="SEC"`). Attach the bill on the voucher's
detail page.

---

## Part 4 — Settlement

### Settling an advance

The holder returns unspent cash and produces bills. Search the IOU list by the physical IOU number,
open that advance, and do everything from its detail page:

1. Add each bill amount. The system creates and maintains the hidden draft expense voucher linked to
   the IOU, so the custodian does not raise a second voucher separately.
2. Record returned cash whenever it comes back. Partial returns and bills accumulate against the same
   IOU number.
3. Click **Settle / Account** once the holder has finished. The system calculates spent as
   `advance − total returned`; there is no amount-spent field to enter.
4. Head office reviews the figures and clicks **Approve Settlement**. This submits and approves the
   hidden voucher, sends its job-linked lines to job cost, and closes the IOU.

Each return posts `IouSettlement` **In** and credits the category from which that advance was released.
The IOU remains open for adding bills and returns until head office approves the settlement.

### Overspend

If someone spends more than they were advanced, that excess is **their own money** — it is an
**out-of-pocket voucher**, not part of the advance. An out-of-pocket voucher deliberately cannot be
linked to a funded category, because counting it there would make an overspent advance look
reconciled. The system also refuses a settled amount greater than the advance.

### The reconciliation figures

Shown per advance on the IOU list:

| Column | Meaning |
| --- | --- |
| Advanced | what was handed over |
| Settled | what the holder spent: advance − returned |
| Returned | total cash returned in one or more instalments |
| Claimed | total of vouchers linked to this advance (rejected ones excluded) |
| **Unaccounted** | settled − claimed |

**Unaccounted is the number that matters.** It is cash declared as spent with no bill behind it. It
has left the company and it will never reach job cost. It shows in amber when non-zero. Settlement
warns but does not block — the decision stays with finance.

---

## Part 5 — What reaches job cost

```
TotalActualCost = netMaterialCost + directPurchaseCost + approvedLaborCost + approvedExpenseClaimCost
```

- **Advances never appear.** There is no IOU term. An advance is a cash position, not a cost.
- **Vouchers appear** when `Approved` or `Settled`, regardless of funding source — the job consumed
  the goods either way. Rejected and Draft vouchers do not.
- **Overhead never appears.** A voucher with no job cannot match any job's costing query.
- **Linking a voucher to an advance does not double count.** Costing reads vouchers only; the link
  is for reconciliation.

Settled vouchers stay in cost. Settlement is a cash event, not a cost reversal — otherwise a job's
cost would fall as you reimburse people.

---

## Part 6 — Permissions

| Key | Grants |
| --- | --- |
| `Finance.PettyCashFund.*` | View / Create / Edit / TopUp / Adjust the box |
| `Finance.PettyCashRequest.View/Create/Edit/Submit` | the site accountant's side |
| `Finance.PettyCashRequest.Approve/Reject/Fund` | head office's side |
| `Finance.PettyCashIou.Create/Submit` | asking for an advance |
| `Finance.PettyCashIou.Approve/Reject/Release/Settle` | granting, handing over, accounting |
| `Finance.PettyCashReturn.View/Create/Submit/Cancel` | prepare and submit reconciled unused float |
| `Finance.PettyCashReturn.Receive/Reject` | head office's cash-count and receipt decision |
| `Service.ExpenseClaim.*` | vouchers, including `Settle` which is what direct payment is gated on |

The Finance role gets the whole petty cash set by default, both sides of the request. Narrowing a
specific user to only raise requests, or only approve them, is done with **per-user permission
overrides** rather than by inventing a role.

Two deliberate choices worth knowing:

- **Issuing cash is gated on `Release`, not `Create`** — issuing is releasing, whatever route it
  took.
- **Direct payment is gated on `ExpenseClaim.Settle`, not `Create`** — it is paying money out, not
  filing a claim.

---

## Part 7 — Worked example

Site accountant needs money for a week.

1. **Request** `PCR000001` against fund `001`, four lines: Job Wise on `SJ000014` 5000, Emergency
   3000, Transportation 2000, Custom "Site refreshments" 1000. Total 11000. Submit → head office is
   notified.
2. **Approve**: 4000 / 3000 / 2000 / **0**. The refreshments line is declined. Approved total 9000.
3. **Fund**: one transfer `TRF-9981` covers job and emergency, funded as two separate releases of
   4000 and 3000; transport is funded 1000 now and 1000 later on `TRF-9982`. Request reaches
   `Funded`. The float rises by 9000; sub-accounts hold 4000 / 3000 / 2000 / 0.
4. **Issue** 1500 to a technician on the spot against signed slip `BILL-4471`, charged to the Job
   Wise category. Their IOU is `Released`; the job sub-account falls to 2500.
5. **Pay** a 650 taxi fare with nobody named, receipt `RCP-2291`, charged to Transportation, no job.
   A settled overhead voucher; transport sub-account falls to 1350. The bill is attached to it.
6. **Settle** the technician's advance: they spent 1200 and return 300. The box rises 300. They
   produce a voucher for 1200 linked to the advance, so Claimed 1200 against Settled 1200 and
   Unaccounted is 0.
7. **Head office approves** the settlement. The advance closes at `SettlementApproved`.
8. **Job cost** for `SJ000014` picks up the 1200 voucher. It does not pick up the 1500 advance — that
   was never a cost — and it does not pick up the 650 taxi, which is overhead.

---

## Part 8 — Returning unused money to head office

Use **Finance → Return Money to Head Office**. This is not an IOU return: an IOU return moves cash
from an employee back into the site float; a `PCRTN` moves reconciled cash from the site float back
to head office.

1. Select the petty cash fund. The page lists each original funded request category with its ledger
   balance, any amount already reserved on another submitted return, and its open-IOU count.
2. Select one or more category lines and enter the amount being returned from each. The suggested
   amount is the full balance, but a partial return is allowed.
3. Add the cash-count/reconciliation notes and prepare the return. Comments and deposit slips, cash
   count sheets, or other evidence can be attached to the `PCRTN` detail page.
4. Submit. Submission is refused if a selected category still has a released IOU or an IOU whose
   settlement is waiting for head-office approval. Submitted amounts are reserved, so the system
   will not issue or settle another payment from that reserved category balance.
5. Head office physically counts the cash. It either rejects the document with a reason, or enters
   the mandatory receipt/deposit reference and confirms receipt.
6. Only receipt confirmation posts `HeadOfficeReturn` outflow rows. Each row carries the original
   `PettyCashRequestLineId`, so both the fund balance and every selected category balance fall by
   the same amount. A rejection or draft cancellation never changes the ledger.

The accounting identity is therefore: **funded into category − advances/vouchers + employee cash
returns − confirmed head-office returns = current category balance**. A head-office return is an
asset/custody transfer, not an expense, and never enters job cost.

### Control rationale and external references

This workflow is an internal-control design, not a claim about a particular country's tax law. It
uses the recurring controls in established guidance:

- the US IRS accountable-plan guidance requires expenses to be substantiated and excess advances
  to be returned within a reasonable period: <https://www.irs.gov/publications/p463>;
- Cornell requires receipts/detailed records, periodic reconciliation, and supervisor review:
  <https://finance.cornell.edu/accounting/topics/pettycash>;
- Stanford calls for reconciliation before decreasing a fund, reviewer verification, signatures,
  and retained evidence: <https://fingate.stanford.edu/business-travel-expenses/how-to/reconcile-petty-cash-fund>;
- the University of Colorado's fund-closure procedure counts the fund, resolves variances, deposits
  the cash, and uses a validated cash receipt before releasing the custodian's responsibility:
  <https://www.colorado.edu/controller/policies/cash-control/petty-cash-fund>.

Those controls are represented here by the IOU gate, category-level reconciliation, separate
submit/receive permissions, mandatory receipt reference, attachments/comments, immutable received
document, and a ledger posting only after physical receipt.

---

## Not built, and deliberately so

**A department / cost-centre dimension.** SAP would put ongoing overhead on a permanent cost centre
and take the month from the posting date. C-Com instead creates a job order named "Workshop
Expenses" each month, which forces the period into the object's identity: master data grows twelve
rows a year, year-on-year comparison means summing twelve objects, and the job register fills with
things that are not jobs — including in job-costing reports.

Making a voucher's job optional removes the *need* for those fake jobs. A standing Department list
would be the rest of the fix and is a small table plus one dropdown, but it was left out because the
IOU slip already records Location/Dept on paper.

**A chart of accounts.** `LedgerAccounts` exists in the schema but is empty and unused — head office
keeps the real accounts in QuickBooks. Expense categories here are reference labels that map to
those accounts, not a posting chart.

## Known gaps

- **Advances cannot carry attachments.** `IOU` is not an attachable document type and has no detail
  page, so the signed slip is recorded by number but cannot be attached as an image. Vouchers can.
- **Nothing forces a settlement to be documented.** Unaccounted is surfaced, not enforced. This was
  a deliberate choice — the alternative blocks settlement until vouchers exist.
- **Sub-account balances can go negative** if spending is charged to a category beyond what was
  funded. The fund's own balance is protected; the category's is not.
