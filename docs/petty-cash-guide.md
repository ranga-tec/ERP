# Petty cash — the whole process

Every way money enters, moves through and leaves the petty cash float, and what each document
means. Written against the code, not against intent; file references are given so a claim here can
be checked.

---

## Part 0 — The four documents, and why there are four

| Document | Prefix | What it is | Creates |
| --- | --- | --- | --- |
| Petty Cash Fund | `PCF` | The cash box itself, and its ledger | the float |
| Petty Cash Request | `PCR` | Asking head office for money, by category | money **into** the float |
| Petty Cash Advance (IOU) | `IOU` or the slip no. | Cash handed to a **person**, who must account for it | a **receivable** from that person |
| Expense Voucher | `SEC` | Money spent, evidenced by a bill | an **expense** |

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

---

## Part 2 — Getting money out of the float

There are **three** ways, and choosing the right one is the whole of the discipline.

### 2a. Advance against a written request — `Finance → Petty Cash Advances (IOU) → + New IOU`

The classic flow. Someone asks, it is approved, cash is released, they settle later.

```
Draft ─submit─► Submitted ─approve─► Approved ─release─► Released
                    │                                        │
                    └─reject─► Rejected                    settle
                                                             ▼
                                                          Settled ─approve settlement─► SettlementApproved
```

At **release** you record the fund it comes from and, optionally, the **signed bill number** and the
funded category to charge. Release posts `IouRelease` **Out**.

### 2b. Advance on a pre-printed slip — `+ Record IOU Slip`

C-Com issues these from a printed IOU book. The slip carries its own number (`4001`), and **that
number is the advance's number in the system too** — the paper is the original document and the
system records it, rather than minting a second identity for the same thing. Entering the same slip
twice is refused.

The form asks only what the counter asks: **slip number, who took the cash, amount, reason.** The
slip's other fields — Location/Dept, Approved by, Authorized by, Cashier, Received by, Settlement
details — are on the paper and are captured against the recorded advance afterwards, not retyped
while someone waits.

- The IOU is created **already Released**: the money has gone, and walking it back through
  Draft/Submitted/Approved would be a fiction.
- The advance is filed against the **staff member selected**, not whoever typed the form. They hold
  it and they settle it.
- **No job is required.** Cash drawn for workshop or general use has no job order; the slip records
  that as Location/Dept.

Advances raised as a **request** inside the system (2a) keep a generated `IOU…` number, because no
slip exists yet at the moment they are created. So both numbering schemes coexist, and which one a
document has tells you which route it came in by.

### 2c. Direct payment — `+ Issue Cash Now`, with **nobody** selected

Money paid for something already bought — a taxi, a courier. Nobody is left accountable, so this is
not an advance at all: it is an **expense voucher created already Settled**, because the cash has
physically left the box.

- The **bill / receipt number is mandatory**. It is the entire support for the payment.
- The job order is **optional**. Transportation and emergency spend belong to no job; that is
  overhead and it never enters job costing.
- After saving you land on the voucher, which is where the bill image itself is attached.

The authority for paying without approval is the funded category: head office already approved that
money when it released it.

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

The holder returns unspent cash and produces bills. The custodian records the **amount spent**:

```
returned to fund = advance − settled amount
```

The remainder posts `IouSettlement` **In**. The settle field defaults to the total already
documented on linked vouchers, not to the full advance, and warns before committing a gap.

Then **head office approves the settlement** (`SettlementApproved`). Two separate facts: the
custodian saying it adds up, and head office agreeing.

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
| Settled | what the holder declared they spent |
| Returned | advance − settled, back in the box |
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
