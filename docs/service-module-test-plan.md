# Service Module Test Plan

A complete, executable test plan for the Service module: equipment, contracts, job orders, planning,
daily field work, materials, expenses, quotations, handover, invoicing, closeout, and reporting.

Every status value, closeout blocker, permission name, and menu path in this document was read from the
code on 2026-07-26 rather than from older documentation.

Related documents:

- `docs/service-job-section-testing-document.md` — the longer narrative walkthrough of the same module
- `docs/testing-input-output-checklist.md` — field-by-field input/output values
- `docs/iss-tester-trainer-handbook.md` — environment setup and evidence rules
- `docs/end-to-end-testing-workflow.md` — the cross-module tally scenario

---

## 1. Before You Start

### 1.1 Environment

Local PostgreSQL on `localhost:5432`, database `iss`, credentials `pgadmin / vesper`. Do **not** use
`docker compose up -d` for this — see `docs/iss-tester-trainer-handbook.md` section 6 for why.

```powershell
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
cd frontend
npm run dev
```

`http://localhost:5257/health` must return `Healthy` before you begin. If it does not, stop and fix the
environment; every failure after this point will be misleading.

### 1.2 Screen conventions

The Service module uses the modal pattern throughout. Create and edit forms open in a dialog over the
list; the list stays the primary view.

| Element | Behaviour |
| --- | --- |
| `+ New ...` at the top right | Opens the create form in a dialog |
| `Edit` in a row | Opens a modal — the job header form, or the document editor for drafts |
| `View` in a row | Opens the full detail page |
| `Delete` | Browser confirmation first |
| Search box above a table | Filters rows **already loaded** on the page, not a server lookup |

There is no `Cancel` button anywhere. Dismiss with `Close`, `Escape`, or a click outside. Dismissing must
never save.

### 1.3 Roles

Run the first full pass as `Admin`. Most service job endpoints accept `Admin`, `Service`, and `Sales`;
`reopen` is restricted to `Admin` and `Service`. Re-run section 12 afterwards with a `Service`-only user.

### 1.4 Service menu

| Menu item | Route |
| --- | --- |
| Command Center | `/service/command-center` |
| Dispatch Board | `/service/dispatch-board` |
| Technician Workbench | `/service/technician-workbench` |
| Equipment Units | `/service/equipment-units` |
| Service Contracts | `/service/contracts` |
| Job Orders | `/service/jobs` |
| Technicians | `/service/technicians` |
| Quotations | `/service/estimates` |
| Petty Cash | `/service/expense-claims` |
| Job Sheets / Work Orders | `/service/work-orders` |
| MRN | `/service/material-requisitions` |
| Inspection / QC | `/service/quality-checks` |
| Service Taken | `/service/handovers` |

Job detail tabs: `Overview`, `Plan`, `Daily Work`, `Materials`, `Expenses`, `Billing`, `Costs`,
`Files & Notes`. Sub-views: Daily Work → `Daily Sheets` / `Staff / Labor` / `Progress`; Materials →
`Issued MRNs` / `Return Materials` / `Damage Material`; Expenses → `IOU Advances` /
`Petty Cash Expenses` / `Out-of-Pocket Claims`.

---

## 2. Reference: Statuses

Testers should assert against these exact values. Anything else on screen is a defect.

**Service job** — `Draft`, `Open`, `Assigned`, `InProgress`, `WaitingForParts`,
`WaitingForCustomerApproval`, `WaitingForSupplier`, `WorkCompleted`, `PendingExpenseSettlement`,
`PendingMaterialReturn`, `ReadyForInvoice`, `Invoiced`, `Closed`, `Reopened`, `Cancelled`

**Job kind** — `Service`, `Repair`, `Pdi`, `Warranty`, `Inspection`

**Entitlement source** — `None`, `ManufacturerWarranty`, `ServiceContract`
**Coverage scope** — `None`, `InspectionOnly`, `LaborOnly`, `PartsOnly`, `LaborAndParts`
**Billing treatment** — `Billable`, `PartiallyCovered`, `CoveredNoCharge`

| Entity | Statuses |
| --- | --- |
| Job operation (Plan) | `Planned`, `InProgress`, `Completed`, `Skipped` |
| Daily field sheet | `Draft`, `Submitted`, `Approved`, `Rejected` |
| Technician assignment | `Pending`, `Approved`, `Rejected` |
| Material requisition (MRN) | `Draft`, `Posted`, `Voided` |
| Material disposition kind | `Used`, `UnusedReturned`, `IncorrectReturned`, `Damaged`, `RejectedSupplierReturn` |
| Material charge-to | `Customer`, `Company`, `Supplier`, `Employee`, `Warranty` |
| Work order | `Open`, `InProgress`, `Done`, `Cancelled` |
| Work order time entry | `Draft`, `Submitted`, `Approved`, `Rejected`, `Invoiced` |
| Expense claim | `Draft`, `Submitted`, `Approved`, `Rejected`, `Settled` |
| Expense funding source | `OutOfPocket`, `PettyCash` |
| Quotation (estimate) | `Draft`, `Approved`, `Rejected` |
| Quotation customer approval | `NotSent`, `Pending`, `Approved`, `Rejected` |
| Quotation line kind | `Part`, `Labor`, `Expense` |
| Service Taken (handover) | `Draft`, `Completed`, `Cancelled` |

---

## 3. Reference: The 11 Closeout Blockers

A job refuses to close while any check is open. The API exposes them at
`GET /api/service/jobs/{id}/closeout-checks`, and the `Billing` tab renders them.

| # | Key | Label | Clears when |
| --- | --- | --- | --- |
| 1 | `daily-field-sheets` | Daily field sheets | Every daily sheet is approved or rejected |
| 2 | `expense-claims` | Expense claims | Every job expense claim is settled or rejected |
| 3 | `petty-cash-ious` | Petty cash IOUs | Every job IOU is settled, rejected, or cancelled |
| 4 | `direct-purchase-bills` | Direct purchase supplier bills | Every posted job-linked direct purchase has a posted supplier invoice |
| 5 | `material-requisitions` | Draft material requisitions | No MRN is left in `Draft` (post or void) |
| 6 | `job-assignments` | Technician assignments | Every assignment is approved or rejected |
| 7 | `labor-entries` | Labor entries | No labor entry is left `Draft` or `Submitted` |
| 8 | `work-orders` | Job detail work orders | Every work order is `Done` or `Cancelled` |
| 9 | `billable-labor` | Uninvoiced billable labor | Approved billable labor is invoiced or marked non-billable |
| 10 | `material-disposition` | Material disposition | Every posted issue line is used/returned/damaged/supplier-returned |
| 11 | `final-invoice` | Final invoice decision | A final invoice exists, or the job is marked not billable with a reason |

**Section 10 is built around deliberately tripping and then clearing all eleven.** That is the highest-value
test in this module and the one most likely to catch a regression.

---

## 4. Test Data

Create this once, **in this order** — later rows depend on earlier ones.

| # | Record | Value |
| --- | --- | --- |
| 1 | Unit of measure | `PCS` — every item requires one, so create it first |
| 2 | Warehouse | `MAIN` — must exist before any stock can be placed |
| 3 | Customer | `CUS-SVC` |
| 4 | Item category | `SUNDRIES` |
| 5 | Equipment item (serial-tracked) | `EQ-GEN01`, type `Equipment`, tracking `Serial` |
| 6 | Spare part item | `SP-FILT`, type `SparePart`, tracking `None`, cost `500` |
| 7 | Sundries item | `SP-GREASE`, type `SparePart`, category `SUNDRIES` |
| 8 | Labour item | `LAB-SVC`, type `Service` |
| 9 | Equipment unit serial | `SN-GEN-0001`, on `EQ-GEN01`, owned by `CUS-SVC` |
| 10 | Technician | `TECH1`, cost rate `10`, billing rate `25` |
| 11 | Opening stock at `MAIN` | `SP-FILT` qty `50`, `SP-GREASE` qty `20` |

Item `Type` accepts only `Equipment`, `SparePart`, `Service`. Tracking accepts only `None`, `Serial`,
`Batch`. There is no "Stock", "Expense", or "Batch + Serial" option.

Create the opening stock as a **posted stock adjustment** at `MAIN` (`Inventory -> Stock Adjustments`,
counted quantity `50` and `20`) rather than by editing the database. That produces a real stock-ledger
movement, which sections 9.1 and 11 later reconcile against.

> **Seed this on a local environment only.** The Railway production database already holds C-COM's real
> item catalogue and supplier list. Adding these test records there pollutes live master data, and the
> opening-stock adjustment creates real inventory value. Never create a shared-password test admin in
> production.

---

## 5. Equipment And Contracts

| # | Step | Expected |
| --- | --- | --- |
| 5.1 | `Service -> Equipment Units`, `+ New`, register `SN-GEN-0001` against `EQ-GEN01` and `CUS-SVC` | Unit appears in list; serial unique |
| 5.2 | Re-register the same serial | Rejected with a clear duplicate message |
| 5.3 | Set warranty-until in the future | Unit shows warranty active |
| 5.4 | `Service -> Service Contracts`, create a contract for `CUS-SVC` covering the unit | Contract appears; coverage scope stored |
| 5.5 | Open the unit detail | Linked customer, contract, and job history visible |

---

## 6. Job Order Lifecycle

| # | Step | Expected |
| --- | --- | --- |
| 6.1 | `Service -> Job Orders`, `+ New Job Order` | Create dialog opens over the list |
| 6.2 | Fill equipment `SN-GEN-0001`, customer `CUS-SVC`, kind `Repair`, problem description, save | Dialog closes, job row appears **without a browser reload** |
| 6.3 | Note the job number | Format `SJ......` |
| 6.4 | `Edit` on the job row | Job header edit modal opens directly from the list, not via the detail page |
| 6.5 | Dismiss with `Escape` after changing a field | Change discarded |
| 6.6 | Open the job, check `Overview` | Cockpit and process timeline render; status `Open` |
| 6.7 | `Start` | Status moves to `InProgress` |
| 6.8 | `Complete` | Confirmation dialog demands the literal word `COMPLETE`; button stays disabled until typed exactly |
| 6.9 | Type `complete` in lowercase | Still disabled — the match is case-sensitive |
| 6.10 | `Refresh entitlement` | Entitlement source/coverage/billing treatment recalculated from warranty and contract |
| 6.11 | `Reopen` a closed job as `Service` role | Allowed |
| 6.12 | `Reopen` as `Sales` role | Denied — reopen is `Admin` and `Service` only |

### Entitlement matrix

Set up three jobs and confirm billing treatment derives correctly:

| Unit state | Expected source | Expected treatment |
| --- | --- | --- |
| In manufacturer warranty | `ManufacturerWarranty` | per coverage scope |
| Covered by active contract | `ServiceContract` | per coverage scope |
| Neither | `None` | `Billable` |

Coverage `LaborAndParts` should give `CoveredNoCharge`; a partial scope (`LaborOnly`, `PartsOnly`,
`InspectionOnly`) should give `PartiallyCovered`.

---

## 7. Plan Tab

| # | Step | Expected |
| --- | --- | --- |
| 7.1 | `Plan` tab | Operations table is the primary content |
| 7.2 | `+ Add Operation` | Opens as a modal, not a passive card |
| 7.3 | Add three operations | All appear in sequence |
| 7.4 | `Start` an operation | `Planned` -> `InProgress` |
| 7.5 | `Complete` it | -> `Completed` |
| 7.6 | `Skip` another | -> `Skipped` |
| 7.7 | Delete an operation | Removed after confirmation |

---

## 8. Daily Work

### 8.1 Daily sheets

| # | Step | Expected |
| --- | --- | --- |
| 8.1.1 | `Daily Work -> Daily Sheets` on an empty job | Clean empty state with `+ Create First Daily Sheet` |
| 8.1.2 | Create a sheet | Modal; card appears with planned/done/pending counts |
| 8.1.3 | With records present | Header shows `+ Add Another Day` |
| 8.1.4 | `Submit` | `Draft` -> `Submitted` |
| 8.1.5 | `Approve` | -> `Approved` |
| 8.1.6 | Create a second sheet, `Reject` it | -> `Rejected`; rejection reason captured |
| 8.1.7 | Daily sheet PDF | Downloads with the C-COM letterhead, header, planned/completed/pending, staff, progress, materials, IOUs, claims |

### 8.2 Staff / Labor

| # | Step | Expected |
| --- | --- | --- |
| 8.2.1 | Open `Staff / Labor` with no sheet selected | Clean no-sheet message plus `Go to Daily Sheets`; **no disabled form** |
| 8.2.2 | Assign `TECH1` to a sheet | Assignment created as `Pending` |
| 8.2.3 | `Approve` | -> `Approved` |
| 8.2.4 | Create a second assignment, `Reject` | -> `Rejected` |
| 8.2.5 | Delete a pending assignment | Removed |

Daily attendance is **not** the same as billable work-order labour. Confirm the two remain separate: a
daily assignment must not create a billable time entry.

### 8.3 Progress

| # | Step | Expected |
| --- | --- | --- |
| 8.3.1 | Open `Progress` with no sheet selected | No-sheet message, no disabled form |
| 8.3.2 | Add a progress update | Appears in history, newest visible; history renders before the add form |
| 8.3.3 | Cockpit | "Last progress" reflects the new entry |

---

## 9. Materials, Expenses, Labour, Quotation, Handover

### 9.1 MRN and stock

| # | Step | Expected |
| --- | --- | --- |
| 9.1.1 | `Materials -> Issued MRNs`, `+ New MRN` | Modal opens |
| 9.1.2 | Add `SP-FILT` qty `4` from `MAIN`, post | MRN `Draft` -> `Posted`; on-hand at `MAIN` drops 50 -> 46 |
| 9.1.3 | Check the stock ledger | An issue movement is recorded against the job |
| 9.1.4 | Try to post more than on hand | Rejected with a clear message |
| 9.1.5 | For a serial item, post without selecting a serial | Rejected by serial validation |
| 9.1.6 | Void a posted MRN | Stock returns; status `Voided` |
| 9.1.7 | Leave one MRN in `Draft` | Needed for closeout blocker 5 in section 10 |

### 9.2 Disposition

Every posted issue line must end as `Used`, `UnusedReturned`, `IncorrectReturned`, `Damaged`, or
`RejectedSupplierReturn`, with a charge-to of `Customer`, `Company`, `Supplier`, `Employee`, or
`Warranty`.

| # | Step | Expected |
| --- | --- | --- |
| 9.2.1 | Mark 2 of 4 `Used`, charge `Customer` | Recorded; billable to the job |
| 9.2.2 | Return 1 as `UnusedReturned` | Stock at `MAIN` increases by 1 |
| 9.2.3 | Mark 1 `Damaged`, charge `Company` | Recorded as a company cost, not customer-billable |
| 9.2.4 | On a separate line use `RejectedSupplierReturn` | Flows toward a supplier return |
| 9.2.5 | Leave one line undisposed | Needed for closeout blocker 10 |

### 9.3 Expenses

Three distinct instruments — confirm they stay distinct:

| Instrument | Where | Funding source |
| --- | --- | --- |
| IOU advance | `Expenses -> IOU Advances` | cash advanced before spend |
| Petty cash expense | `Expenses -> Petty Cash Expenses` | `PettyCash` |
| Out-of-pocket claim | `Expenses -> Out-of-Pocket Claims` | `OutOfPocket` |

| # | Step | Expected |
| --- | --- | --- |
| 9.3.1 | Raise an IOU | IOU number confirmed; state says waiting for finance approval |
| 9.3.2 | `Finance -> Petty Cash IOUs`: approve, release from a fund, settle | Fund balance decreases on release; IOU reaches `Settled` |
| 9.3.3 | Raise an out-of-pocket claim, submit, approve, settle | `Draft` -> `Submitted` -> `Approved` -> `Settled` |
| 9.3.4 | Reject a second claim | -> `Rejected` |
| 9.3.5 | Convert billable claim lines to a quotation | Lines land on a service quotation |
| 9.3.6 | Leave one claim `Submitted` | Needed for closeout blocker 2 |

### 9.4 Work orders and billable labour

| # | Step | Expected |
| --- | --- | --- |
| 9.4.1 | `Service -> Job Sheets / Work Orders`, `+ New Job Sheet` | Modal opens over the register |
| 9.4.2 | Add a time entry for `TECH1`, 4 hours | Entry `Draft`; cost uses `10`/h, billing `25`/h |
| 9.4.3 | Submit then approve | `Draft` -> `Submitted` -> `Approved` |
| 9.4.4 | Reject another entry | -> `Rejected` |
| 9.4.5 | `Start` then `Done` the work order | `Open` -> `InProgress` -> `Done` |
| 9.4.6 | Leave one work order `Open` | Needed for closeout blocker 8 |

### 9.5 Quotation

| # | Step | Expected |
| --- | --- | --- |
| 9.5.1 | `Service -> Quotations`, create for the job | Draft quotation |
| 9.5.2 | Add `Part`, `Labor`, and `Expense` lines | Totals recalculate |
| 9.5.3 | `Send` | Customer approval `NotSent` -> `Pending` |
| 9.5.4 | `Approve` | Quotation `Approved` |
| 9.5.5 | `Revise` an approved quotation | New revision; prior revision retained |
| 9.5.6 | `Reject` another quotation | -> `Rejected` |
| 9.5.7 | PDF | C-COM letterhead, correct lines and totals |

### 9.6 Inspection / QC and Service Taken

| # | Step | Expected |
| --- | --- | --- |
| 9.6.1 | `Inspection / QC`, record a pass | Linked to the job |
| 9.6.2 | Record a fail with notes | Notes retained |
| 9.6.3 | `Service Taken`, create a handover | `Draft` |
| 9.6.4 | `Complete` | -> `Completed` |
| 9.6.5 | Convert to sales invoice **with** an approved quotation | Invoice created from quotation lines |
| 9.6.6 | On a second job, convert **without** an approved quotation, entering manual lines | Manual invoice supported: labour/work done, additional items, sundries, discount %, tax |
| 9.6.7 | Confirm the invoice in `Sales -> Final Invoices` | Present and linked back to the job |
| 9.6.8 | Try to create a handover for a closed job | Blocked with clear validation |
| 9.6.9 | Cancel a draft handover | -> `Cancelled` |

Grease and lubricants should use the `SUNDRIES` item category and must be invoiceable.

---

## 10. Closeout Gauntlet

The core regression test. Deliberately leave all eleven blockers open, then clear them one at a time.

**Setup.** On one job leave open: an unapproved daily sheet, a submitted expense claim, an unsettled IOU,
a job-linked direct purchase without a posted supplier invoice, a draft MRN, a pending assignment, a
draft labor entry, an `Open` work order, approved uninvoiced billable labour, an undisposed material
line, and no final invoice decision.

| # | Step | Expected |
| --- | --- | --- |
| 10.1 | Open `Billing` | All 11 checks listed, each showing a pending count |
| 10.2 | Attempt `Close` | Rejected. Message is `Service job cannot close: <detail of the first open check>` |
| 10.3 | Clear checks 1-10 one at a time, retrying `Close` after each | Each retry fails on the *next* open check, never on one already cleared |
| 10.4 | Clear the final invoice decision by generating the invoice from the completed handover | Check 11 clears |
| 10.5 | `Close` | Succeeds; status `Closed` |
| 10.6 | Alternative for check 11: mark the job not billable with a reason | Check 11 clears without an invoice; reason is stored and visible |
| 10.7 | `Reopen` the closed job | Status `Reopened`; job editable again |
| 10.8 | Re-close | Succeeds if nothing new was opened |

Record which check each `Close` attempt reported. A check that never surfaces, or one that blocks after
being cleared, is a defect worth logging in detail.

---

## 11. Costs, Dashboards, Reports

| # | Step | Expected |
| --- | --- | --- |
| 11.1 | `Costs` tab | Materials, labour, and expenses reconcile line-for-line with the MRNs, approved time entries, and settled claims |
| 11.2 | Compare against `GET /api/service/jobs/{id}/costing` | UI matches the API |
| 11.3 | `Service -> Command Center` | Active jobs, overdue jobs, jobs missing today's daily sheet, jobs missing today's progress, pending sheets, pending IOUs, pending claims, billing-ready handovers |
| 11.4 | `Service -> Dispatch Board` | Unassigned / assigned-active / waiting / completed lanes correct |
| 11.5 | `Service -> Technician Workbench` | Today's assignments and open daily sheets for the technician |
| 11.6 | `Reporting -> Service KPIs` | Counts agree with the jobs created in this run |
| 11.7 | `Reporting -> Costing` | Job costs reflected in valuation |
| 11.8 | Job PDF | C-COM letterhead: logo, address, phone, email, website, and `Page X of Y` in the footer |
| 11.9 | Audit Logs | Status transitions recorded |

---

## 12. Permissions

Re-run with a non-admin `Service` user, then with granular permissions removed. Each permission below
should hide the UI control **and** reject the API call — verify both. A hidden button with a still-open
endpoint is a security defect, not a cosmetic one.

| Permission | Denied user should not be able to |
| --- | --- |
| `Service.DailySheet.Submit` / `.Approve` / `.Reject` | Move a daily sheet out of `Draft` |
| `Service.JobAssignment.Create` / `.Approve` / `.Reject` | Assign or approve technicians |
| `Service.MaterialRequisition.Create` / `.Edit` / `.Post` / `.Void` | Create or post an MRN |
| `Service.ExpenseClaim.Create` / `.Submit` / `.Approve` / `.Reject` / `.Settle` / `.Convert` | Move a claim through its lifecycle |
| `Service.WorkOrderTimeEntry.Submit` / `.Approve` / `.Reject` | Approve billable labour |
| `Service.Estimate.Create` / `.Edit` / `.Approve` / `.Reject` / `.Send` / `.Revise` | Progress a quotation |

Also confirm: a `Sales` user can create and start jobs but **cannot** reopen one.

---

## 13. Negative And Edge Cases

| # | Case | Expected |
| --- | --- | --- |
| 13.1 | Issue material beyond on-hand | Rejected |
| 13.2 | Post a serial item without a serial | Rejected |
| 13.3 | Post the same serial to two jobs | Rejected |
| 13.4 | Close a job with any blocker open | Rejected, naming the blocker |
| 13.5 | Invoice a closed job | Blocked unless an authorised reopen path is used |
| 13.6 | Approve a daily sheet twice | Second attempt rejected or idempotent, never double-counted |
| 13.7 | Settle an IOU for more than advanced | Rejected or explicitly handled as extra reimbursement |
| 13.8 | Negative or zero quantities and hours | Rejected |
| 13.9 | Dismiss any modal mid-entry | Nothing saved |
| 13.10 | Two browser tabs editing the same job | Last write wins without corrupting status |
| 13.11 | Search a job number not on the current page | **Will not be found** — the search box filters loaded rows only. Expected behaviour, not a defect |

---

## 14. Known Gaps

Not yet built. Do not raise these as defects — they are backlog, tracked in
`docs/agent-handover-service-job-daily-operations.md`:

- job daily sheet report, technician daily work/time report, petty cash by job report
- IOU/employee advance report, employee reimbursement report
- material issued / returned / damaged / rejected by job reports
- pending daily sheet approval and pending job closeout reports
- daily sheet dashboard widget, mobile-friendly technician screen
- expense category master; attachments per expense line or daily sheet
- IOU outstanding balance reporting per employee/job

---

## 15. Sign-Off

| Item | Value |
| --- | --- |
| Tester |  |
| Date |  |
| Environment | Local / Railway |
| Frontend commit |  |
| API commit |  |
| Job number used |  |
| Sections passed |  |
| Sections failed |  |
| Closeout gauntlet (section 10) | Pass / Fail |
| Defects raised |  |

A run is only complete when section 10 passes end to end. Everything else can be partially covered; the
closeout gauntlet cannot.
