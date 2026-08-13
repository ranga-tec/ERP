# Petty cash rework — handover

## Update — 2026-08-13

The current workflow now has an accountant approval cover sheet (`PCAB`) for submitted IOUs. An
accountant selects one active petty-cash fund, one assigned approver, and one or more submitted IOU
requests. The approver sees the combined total plus job number, IOU number, requester, description,
requested amount and editable approved amount for every line. The accountant is notified when it is
returned, submits the same batch to head office, and records the remittance reference when the cash
is physically received.

Fund accounting is event-based: batch funding receipt is `HeadOfficeIouFunding` **In**, signed-slip
cash release is `IouRelease` **Out**, and unused cash returned by a holder is `IouSettlement` **In**.
The old behavior that credited and debited the fund inside one release request was removed because
it hid both the received balance and subsequent reduction.

IOU settlement promotes its hidden bill voucher to Approved immediately. Billable lines therefore
enter job cost and the service handover billing selector at settlement. The handover billing builder
can now reopen an already-linked **Draft** invoice and add only new, uninvoiced charges; posted
invoices remain immutable. This is deliberately scoped to the invoice/expense handoff and does not
change material, labour, MRN/AOD, estimate, or job-status rules.

Migration: `20260812173542_AddPettyCashIouApprovalBatches`.

Validation added:

- domain test for editable batch approval and one remittance receipt;
- integration scenario covering two IOUs, reduced approval, fund receipt, release, bill, cash
  return, settlement, fund balance and job-cost visibility;
- migration script generation and backend/frontend production builds.

The integration scenario requires Docker/PostgreSQL Testcontainers locally. If Docker is not
running, execute the same scenario against the deployed authenticated API after rollout.

## Update — 2026-07-30

The duplicate IOU creation routes described below have been consolidated locally. Job staff now
create the request; after approval, finance releases from that same record and must select the
matching funded Job Wise category, the employee collecting the cash, and the signed physical slip
number. Requester and collector are stored separately by migration
`20260730195255_AddPettyCashIouCollector`. The direct `Record IOU Slip` button is no longer shown.
Non-job taxi, courier, workshop and similar payments remain overhead expense vouchers, not fake
jobs. Grid settlement remains removed; bills, attachments, returns and settlement stay on the IOU
detail page.

The 2026-07-30 consolidation was deployed to Railway production on 2026-07-31 as deployment
`0c1c3b8c-d6bb-48e7-ba2b-23695d75f04e`. Railway reported `SUCCESS`; startup logs confirm migration
`20260730195255_AddPettyCashIouCollector` was applied and the application started.

The remainder is the historical handover from the earlier rework.

State at handover: local `HEAD` = `0ae0000`, which is also `origin/c-com-erp` and Railway
deployment `3f13b8e2`. The frontend completion work described below is now implemented locally;
the only remaining item is running the end-to-end verification against a live API (see §4.4).

---

## 1. The requirement, in full

From the client (C-Com), in their words, restructured but not reinterpreted:

1. The assistant accountant requests petty cash from head office **with a breakdown by category**.
   Head office approves and releases, and the money lands in the petty cash fund.
2. Categories are **job-wise, emergency operation, transportation, and custom**.
3. The assistant then **releases cash from one of those funded categories**, mostly to workers on
   jobs.
4. **Workers are company staff, so selecting them from a dropdown is mandatory.** If the money goes
   to an outsider, the user types a name and purpose instead.
5. The issue form asks for **only four things: bill number, staff member, amount, purpose.**
   Everything else on the paper slip is captured later. (This was explicitly narrowed twice — do not
   add fields back.)
6. When workers return with bills, the assistant **searches by the physical IOU number** from the
   issue note, opens that one record, and does everything there: **attach bill copies/images, add
   the bill amounts, add any returned balance, and settle**.
7. **One place.** No grid-line settle. The client's words: *"no need to have a grid line-wise setoff
   which will be double work as users has to attach somewhere and settle from somewhere."*
8. **Partial balances.** People come back with part of it. Bills and returned cash keep accumulating
   **against the same IOU number until head office approves the settlement.**
9. Bills must still reach job cost. Agreed approach (**option A**): the assistant only ever sees the
   IOU; the system quietly creates and maintains an expense voucher behind it.
10. **Returned cash must credit back the category it was released from**, so debits and credits tally
    per sub-account.

Also settled during the discussion, and already implemented:

- The **pre-printed slip number is the IOU number** (`4001`), not a second generated one. The slip
  book issues numbers; the system records them. Duplicates are refused.
- Advances raised as a request inside the system keep a generated `IOU…` number, because no slip
  exists when they are created. Both schemes coexist deliberately.
- **No department/cost-centre dimension and no chart of accounts.** Head office uses QuickBooks;
  categories here are reference labels. Do not build a chart of accounts.

---

## 2. What is done and deployed

All of this is on `origin/c-com-erp` and live on Railway.

**Domain** (`backend/src/ISS.Domain/Finance/PettyCashFund.cs`)

- `PettyCashIou.ReturnedAmount` accumulates across instalments; `AddReturn` appends rather than
  replaces.
- `OutstandingAmount => Amount - ReturnedAmount`.
- `IsOpenForAccounting => Status is Released or Settled` — this is the gate that keeps an advance
  open for more bills and more cash until head office approves.
- `Settle(settledAt, reference)` **takes no amount.** What was spent is the advance less what came
  back. The old typed field was what let 600 be settled against 100 of bills.
- `PettyCashIouStatus.SettlementApproved = 7` closes it.
- `ServiceJobId` is nullable — cash drawn for workshop/general has no job.

**Application** (`backend/src/ISS.Application/Services/FinanceService.cs`)

- `ReturnPettyCashIouBalanceAsync` — credits the fund **carrying `iou.PettyCashRequestLineId`**, so
  the category is restored. This was a real bug: releasing debited the category, the return credited
  only the fund, so every settled advance left its category permanently understated.
- `AddPettyCashIouBillAsync` — finds or creates the **draft** voucher linked by `PettyCashIouId` and
  appends a line. This is option A: the custodian never sees a voucher.
- `ApprovePettyCashIouSettlementAsync` — closes the advance **and** submits + approves the draft
  voucher so it reaches job cost. It deliberately **never settles that voucher**: the cash left the
  box when the advance was released, and settling would pay the same money out twice.

**API** (`backend/src/ISS.Api/Controllers/Finance/PettyCashIousController.cs`)

| Endpoint | Purpose |
| --- | --- |
| `GET  /finance/petty-cash-ious/staff` | active users for the mandatory dropdown |
| `POST /finance/petty-cash-ious/issue-directly` | record a slip; `SlipNumber` becomes `Number` |
| `GET  /finance/petty-cash-ious/{id}/bills` | bills gathered against the advance |
| `POST /finance/petty-cash-ious/{id}/bills` | add a bill |
| `POST /finance/petty-cash-ious/{id}/return-balance` | record returned cash |
| `POST /finance/petty-cash-ious/{id}/settle` | close; body is `{ }` or `{ settlementReference }` |
| `POST /finance/petty-cash-ious/{id}/approve-settlement` | head office signs off |

`PettyCashIouDto` carries `returnedAmount`, `outstandingAmount`, `isOpenForAccounting`,
`claimedAmount`, `claimCount`, `unaccountedAmount`, `issueBillNumber`, `serviceJobNumber`.

**Migrations applied locally and on Railway:** `AddPettyCashRequests`,
`AddPettyCashIssueTrackingAndOverheadVouchers`, `MakePettyCashIouJobOptional`,
`AddPettyCashIouReturns`.

---

## 3. Uncommitted work in the tree

Do not delete these; they are most of task 4.1 below and they compile.

```
?? frontend/src/app/(app)/finance/petty-cash-ious/PettyCashIouAccountingForms.tsx
?? frontend/src/app/(app)/finance/petty-cash-ious/[id]/page.tsx
 M backend/src/ISS.Api/Controllers/Finance/PettyCashIousController.cs
```

- **`PettyCashIouAccountingForms.tsx`** — three client components, all pointing at live endpoints:
  `PettyCashIouBillAddForm`, `PettyCashIouReturnForm`, `PettyCashIouSettleActions`.
- **`[id]/page.tsx`** — the detail page. Header, four figure tiles (Advanced / Cash returned / Bills
  against it / Unaccounted), bills table + add form, returns form, settlement card, and
  `DocumentCollaborationPanel` with `referenceType="IOU"` for the bill images.
- The controller edit resolves `ServiceJobNumber` server-side instead of returning hardcoded `null`
  (it was the same raw-GUID defect written up as Part C of the UoM guide).

`npx tsc --noEmit` and `npx eslint` both pass with these in place. **They have not been tested
against a running server** — that is step 4.4.

---

## 4. What remains

Implementation status: 4.1 (detail-page link and IOU search), 4.2 (grid settlement removal),
4.3 (mandatory staff/outsider holder selection), and 4.5 (the user-facing guides) are complete.
The instructions below are retained as an audit trail; only 4.4 still needs execution.

### 4.1 Finish and verify the detail page

Mostly written (§3). To finish:

1. Make the list link to it. `frontend/src/app/(app)/finance/petty-cash-ious/page.tsx` renders the
   number as plain text at roughly line 105 — wrap it in
   `<Link href={`/finance/petty-cash-ious/${iou.id}`}>`.
2. Confirm `TableSearchInput` on that page filters by number, so searching `4001` finds the row.
   This is requirement 6 — "search the bill information using the physical IOU number".
3. Add `{ prefix: "/finance/petty-cash-ious", permissions: ["Finance.PettyCashIou.View"] }` already
   exists in `src/lib/route-access.ts`; the `[id]` route inherits it. Verify, do not duplicate.

### 4.2 Remove the grid settle

`frontend/src/app/(app)/finance/petty-cash-ious/PettyCashIouActions.tsx`

The `status === 3 && canSettle` block (around line 144) still renders a "Cash returned" input and a
**Settle / Account** button inline in the list. That is exactly the double-work the client rejected.
Delete that block and its confirm dialog, along with the now-unused `returnedNow` state,
`settleWithReturn`, and the `claimedAmount` / `claimCount` props if nothing else uses them. Settling
happens only on the detail page.

Keep Approve, Reject and Release in the grid — those are not the complaint.

### 4.3 Make the staff selection mandatory

`frontend/src/app/(app)/finance/petty-cash-ious/PettyCashIssueForm.tsx`

Currently `Issued to` is optional with a "Not a named person" option. Requirement 4 says a staff
member is **mandatory**, with a typed name only for outsiders. Change to:

- a required dropdown of staff, plus an explicit **"Someone else (not staff)"** option;
- choosing that reveals a required free-text name field, posted as `issuedToName`;
- otherwise post `issuedToUserId`.

The backend already accepts both (`IssuedToUserId`, `IssuedToName` on
`IssuePettyCashIouDirectlyRequest`) and falls back to resolving the user's display name when no name
is typed. **Keep the form to the four fields** — bill number, staff member, amount, purpose. Do not
re-add job or category pickers.

### 4.4 Verify against a running server

Nothing in §3 has been exercised end to end. Run the full path and check the numbers:

```bash
# API in Development so self-registration works (Production disables it, by design)
cd backend/src/ISS.Api && ASPNETCORE_ENVIRONMENT=Development \
  ASPNETCORE_URLS=http://localhost:5257 dotnet run --no-build --no-launch-profile
```

Then: issue a slip → add two bills → record two partial returns → settle → add one more bill →
approve. Expect advance − returned = spent, `unaccounted` reaching 0, the hidden voucher moving to
Approved with **no** `ExpenseSettlement` row against it, and the fund back where it started.

The equivalent backend-only run has already passed: 3000 on slip 5001, bills 1200 + 500, returns
800 + 500 → 1300 returned, 1700 outstanding, 1700 claimed, 0 unaccounted; voucher `SEC000014`
Approved; float 4300 → 2600 → 4300.

### 4.5 Update the two user-facing guides

Both currently describe the old two-screen settlement and will be wrong once 4.2 lands:

- `docs/petty-cash-guide.md` — Part 4, Settlement.
- `frontend/src/app/(app)/help/petty-cash/page.tsx` — scenarios 6 and 7 tell the user to raise the
  voucher separately and settle from the list. Rewrite as one scenario on the detail page.

---

## 5. Things that will bite you

- **`--no-build` with `dotnet ef`.** `dotnet ef migrations add … --no-build` writes the file but
  `database update --no-build` then runs against the *stale* assembly and reports "up to date"
  without applying anything. Always `dotnet build` in between.
- **Stop the API before building.** Otherwise `MSB3027` file locks. `netstat -ano | grep :5257`.
- **Unknown JSON fields are ignored silently.** `System.Text.Json` drops fields it does not know, so
  a frontend posting a removed field does not error — it quietly sends nothing. That is why 4.2
  matters: the old grid posted `settledAmount` and would have settled at the full advance.
- **A 307 from the live app proves nothing.** The auth middleware redirects every path, existing or
  not, so `/help/zzz-not-a-page` also returns 307. To confirm a deploy, compare the Railway
  deployment id, or probe an authenticated API route for `401` (not `404`).
- **Frontend checks must both exit 0**, and check the exit code rather than piping through `head`:
  `npx tsc --noEmit` and `npx eslint "src/app/(app)" src/components src/lib`.
- **Test data must be removed after verification.** The local database is the client's working data.
  Delete in FK order: transactions → voucher lines → vouchers → IOUs → request lines → requests →
  user roles → users. Then confirm fund `001` is back to **4300.00** and there are **3** users.
- **Pushing is unreliable on this machine.** Git Credential Manager hangs; a `git push` may need two
  attempts with a 240s timeout. Deploys are manual CLI uploads from a clean worktree — the Railway
  service is **not** connected to the GitHub repo, so pushing does not deploy.

## 6. Deploying

Latest production hotfix (2026-07-31): Railway deployment
`a1bbc150-73d6-4631-82aa-1cfc21e5f622` completed with `SUCCESS`. It fixes the PostgreSQL/EF query
translation failure during IOU release and applies the idempotent
`20260731045642_RepairMissingAssistantSettingsTables` migration. The hotfix passed a clean backend
build, all 51 unit tests, migration-script generation, and the post-deployment 5xx log check. A
fresh authenticated IOU release and the complete accounting flow in section 4.4 remain to be
executed manually; do not claim the full end-to-end test as complete yet.

```bash
git worktree add --detach ../ISS-deploy-<sha> <sha>
cd ../ISS-deploy-<sha>
npx @railway/cli@latest link --project 499206bc-390f-4751-baed-92ebab06debd \
  --environment production --service ERP
npx @railway/cli@latest up --service ERP --environment production --detach
```

Then poll `npx @railway/cli@latest status` until the deployment id matches the one the upload
printed. Remove the worktree afterwards. Migrations apply on startup —
`Database__InitializationMode=Migrate` is baked into the Dockerfile — so a healthy API means they
landed.
