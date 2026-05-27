# Agent Handover: Service Job Daily Operations

## Current Status

This handover covers the service/job module work completed through:

`2e29358 Link service job closeout checks to workflows`

Local update on 2026-05-25:

- service handover invoice conversion now supports manual invoice lines without requiring an approved estimate
- manual conversion covers labour/work done, additional items, sundries/grease/lubricants, discount percent, and tax selection
- estimate-based conversion remains available when an approved service estimate should drive the invoice
- item category `SUNDRIES` is used for grease, lubricants, consumables, and similar service-repair charges
- warehouse bins/racks moved out of the warehouse master page into `Master Data -> Warehouse Bins`

Local update on 2026-05-26:

- Phase 1 of the service UX revision is implemented.
- Job `Expenses` now separates IOU advances, petty-cash expenses, and out-of-pocket claims.
- Job-linked IOUs and claims remain visible on the job page after creation.
- IOU creation confirms the generated IOU number and pending finance approval state.
- Daily progress history appears before the add-progress form.
- Daily sheet rows expose quick links for labour, progress, material issue, IOU request, and expense entry.
- Phase 2 cockpit work is also implemented: job detail now shows summary metrics, a process timeline, and next-action suggestions above the tab navigation.

Local update on 2026-05-27:

- Phase 3 of the service UX revision is implemented.
- `Daily Work -> Daily Sheets` now shows daily cards instead of a plain row/table-first view.
- Each daily card shows planned work, completed work, pending/issues, latest progress, staff preview, and counts for staff, progress, MRNs, returns, expenses, and IOUs.
- Daily cards link directly to staff/labour, progress, materials, IOU request, and expense entry while the focused sub-tabs remain the detailed entry surfaces.
- Phase 4 command center is implemented at `Service -> Command Center`.
- Backend endpoint `GET /api/service/jobs/dashboard` powers service workload metrics, stage/status bars, active job cards, finance queue, and billing/closeout queue.

GitHub push status:

- Pushed to `origin/main`.
- Recent service/job commits:
  - `9788070 Add service job operations planning`
  - `909f5b1 Fix service job operations actual labor query`
  - `acc3298 Simplify service job detail workflow`
  - `8af1961 Organize service job detail into workflow tabs`
  - `debe9df Refine service job daily work flow`
  - `2e29358 Link service job closeout checks to workflows`

Railway status:

- Latest deployed commit: `2e29358`.
- Deploy command used from a clean detached worktree:
  - `npx @railway/cli@latest up --service ERP --environment production --detach`
- Railway service was verified `Online`.
- Live verification passed:
  - `/login` returned HTTP `200`
  - job detail returned HTTP `200`
  - `?tab=daily-work&dailyView=sheets` returned HTTP `200`
  - `?tab=daily-work&dailyView=labor` returned HTTP `200`
  - `?tab=daily-work&dailyView=progress` returned HTTP `200`
  - daily sheet labor/progress deep links returned HTTP `200`
  - `?tab=billing`, `?tab=materials`, and `?tab=expenses` returned HTTP `200`

Unrelated local changes exist and must not be reverted or staged unless the user explicitly asks:

- `backend/src/ISS.Infrastructure/DependencyInjection.cs`
- `backend/src/ISS.Infrastructure/Persistence/IssDbContextFactory.cs`
- `backend/tests/ISS.UnitTests/ISS.UnitTests.csproj`
- `frontend/src/components/SearchableSelect.tsx`
- untracked `Modification reqs/`
- untracked `backend/src/ISS.Infrastructure/Persistence/DatabaseConnectionStringResolver.cs`
- untracked `backend/tests/ISS.UnitTests/Infrastructure/`
- untracked `data/`
- untracked `docs/testing-input-output-checklist.pdf`
- untracked `frontend/src/lib/attachment-upload.ts`

## User Requirement

The user clarified that the system must replace paper books/notes during real job execution.

The required practical workflow is:

1. Open a service job.
2. While the job is running, create a daily field/job sheet for each working day or site visit.
3. Record daily technicians/workers, progress, petty-cash advances, IOUs, out-of-pocket expenses, material issues, lubricants, consumables, returns, damages, rejected parts, and supplier-return impacts.
4. Show ongoing job costs while the job is still open.
5. Block closeout until daily records, IOUs, expenses, labor, materials, returns, and invoice decisions are resolved.

The user also clarified after implementation:

- The job detail page must not show every service workflow form as one long page.
- Service jobs now use top-level workflow tabs:
  - `Overview`
  - `Plan`
  - `Daily Work`
  - `Materials`
  - `Expenses`
  - `Billing`
  - `Costs`
  - `Files & Notes`
- `Daily Work` now uses sub-tabs:
  - `Daily Sheets`
  - `Staff / Labor`
  - `Progress`
- Closeout readiness tiles are clickable and route users to the relevant tab/sub-tab or supporting module.

## Industry/Standard Pattern Used

The implemented direction follows common field-service ERP practice:

- Field technicians update work orders and submit service reports during the job.
- Expenses are linked to work orders/jobs.
- Job costing combines labor, materials, expenses, and returns while the job is in progress.
- Field inventory includes issued parts and returns of unused, excess, defective, damaged, or wrong parts.

References already documented in `docs/service-job-daily-field-operations-requirement.md`:

- Salesforce Field Service pricing/features: technician users can view/update work orders, create quotes, and submit service reports from the field.
- Salesforce Field Service expenses: expenses are associated with work orders.
- Simpro job costing: job cost should track materials, labor, contractors, equipment, overhead, and live profitability.
- Oracle field service parts returns: technicians return excess, unused, and defective parts to warehouse/return destinations.

## Completed Backend Work

### Service Job Operations / Sub-Parts Plan

Added:

- `backend/src/ISS.Domain/Service/ServiceJobOperation.cs`
- EF mapping and migration for service job operations
- API endpoints under `ServiceJobsController`
- service methods in `ServiceManagementService`

Operation fields:

- step number / sequence
- work step / subassembly name
- description
- planned item/sub-part
- planned quantity
- estimated labor hours
- required date
- notes
- status: planned, in progress, completed, skipped

The operation plan is only a plan. Inventory is still consumed through MRN posting, and labor actuals still come from approved labor/time records.

### Daily Sheet Entity

Added:

- `backend/src/ISS.Domain/Service/ServiceJobDailySheet.cs`

Daily sheet fields:

- number
- service job
- sheet date
- prepared by
- site/location
- shift
- weather/site condition
- planned work
- completed work
- pending work
- problems found
- customer instructions
- technician notes
- supervisor notes
- status

Statuses:

- `Draft`
- `Submitted`
- `Approved`
- `Rejected`

### Daily Sheet Links Added

Existing records now optionally link to `ServiceJobDailySheetId`:

- `ServiceJobAssignment`
- `ServiceJobProgressUpdate`
- `PettyCashIou`
- `ServiceExpenseClaim`
- `MaterialRequisition`
- `ServiceJobMaterialDisposition`

This allows each daily sheet to show staff/progress/material/return/expense/IOU counts.

### Service Methods Added/Changed

In `ServiceManagementService`:

- create daily sheet
- submit daily sheet
- approve daily sheet
- reject daily sheet
- validate daily sheet belongs to job before linking entries
- prevent adding new entries to approved daily sheets
- closeout readiness now blocks open `Draft` or `Submitted` daily sheets

Existing create methods were extended with optional `ServiceJobDailySheetId`:

- assignment creation
- progress update creation
- material requisition creation
- material disposition creation
- service expense claim creation

In `FinanceService`:

- `CreatePettyCashIouAsync` accepts optional `ServiceJobDailySheetId`
- validates daily sheet belongs to the service job
- prevents adding IOUs to approved daily sheets

### API Changes

Updated service job API:

- `GET /api/service/jobs/{id}/daily-sheets`
- `POST /api/service/jobs/{id}/daily-sheets`
- `POST /api/service/jobs/{id}/daily-sheets/{dailySheetId}/submit`
- `POST /api/service/jobs/{id}/daily-sheets/{dailySheetId}/approve`
- `POST /api/service/jobs/{id}/daily-sheets/{dailySheetId}/reject`

Updated existing APIs to carry optional daily sheet IDs:

- service job assignment
- service job progress update
- material requisition
- material disposition
- service expense claim
- petty cash IOU

### Database Migration

Added migration:

- `backend/src/ISS.Infrastructure/Persistence/Migrations/20260514163425_AddServiceJobDailySheets.cs`
- designer file
- updated `IssDbContextModelSnapshot.cs`

## Completed Frontend Work

Service job detail is now organized as a workflow workspace instead of one long form.

Top-level tabs:

- `Overview`
- `Plan`
- `Daily Work`
- `Materials`
- `Expenses`
- `Billing`
- `Costs`
- `Files & Notes`

Overview:

- concise job header remains visible above tabs
- job actions remain visible above tabs
- edit/intake and entitlement are collapsed
- closeout readiness tiles are clickable
- each closeout tile routes to the relevant workflow tab/sub-tab or supporting module

Plan:

- added `Job Operations / Sub-Parts Plan`
- renamed labels to:
  - `Step No.`
  - `Work step / subassembly`
- planned work steps can carry a planned part/sub-part, quantity, estimated labor hours, required date, description, and notes
- operation actions allow start/complete/skip as applicable

Daily Work:

- added `Daily Field Sheets` section
- added create form for daily sheets
- added submit/approve/reject actions
- added daily sheet counts for:
  - staff
  - progress
  - MRN
  - returns/dispositions
  - expenses
  - IOUs
- split daily work into sub-tabs:
  - `Daily Sheets`
  - `Staff / Labor`
  - `Progress`
- daily sheet rows now have direct `Labor` and `Progress` links
- staff/labor and progress forms are driven by the selected daily sheet
- staff/labor and progress tables show records for the selected sheet instead of mixing the whole job together

Added forms:

- `ServiceJobDailySheetCreateForm.tsx`
- `ServiceJobDailySheetActions.tsx`
- `ServiceJobDailyIouCreateForm.tsx`
- `ServiceJobDailyExpenseClaimCreateForm.tsx`
- `ServiceJobDailyMaterialRequisitionCreateForm.tsx`

Updated forms to select a daily sheet:

- `ServiceJobAssignmentAddForm.tsx`
- `ServiceJobProgressUpdateAddForm.tsx`
- `ServiceJobMaterialDispositionAddForm.tsx`

Other sections:

- `Materials` tab owns MRN creation and material disposition/return/damage/rejection
- `Expenses` tab owns IOU/employee advance, petty cash expense, and employee reimbursement claim
- `Billing` tab owns closeout readiness, final invoice/not-billable decision, estimates, and invoices
- `Costs` tab owns cost cards, profitability, and source lines
- `Files & Notes` tab owns comments and attachments

## Completed Documentation

Updated/added:

- `docs/service-job-daily-field-operations-requirement.md`
- `docs/testing-input-output-checklist.md`
- `docs/manual-uat-guide.md`
- `docs/end-to-end-testing-workflow.md`
- `docs/user-manual.md`

The testing checklist now includes:

- service job tab navigation
- clickable closeout tile routing
- service job operation/sub-part plan
- daily sheet creation
- MRN linked to daily sheet
- material disposition/return linked to daily sheet
- technician assignment linked to daily sheet
- progress update linked to daily sheet
- IOU advance linked to daily sheet
- expense voucher linked to daily sheet
- daily sheet approval before closeout

## Verification Already Run

Passed:

- `dotnet test backend\tests\ISS.UnitTests\ISS.UnitTests.csproj`
  - 45 tests passed during service operation backend work
- `dotnet build backend\src\ISS.Api\ISS.Api.csproj`
- `npx tsc --noEmit`
- targeted `npx eslint` for changed service-job frontend files
- `git diff --check`

Deployment:

- latest Railway deployment completed from clean worktree at `2e29358`
- live `/login` returned HTTP `200`
- live job tabs and daily-work sub-tabs returned HTTP `200`
- daily sheet labor/progress deep links returned HTTP `200`

## Pending Finance Refinement

Still pending:

- expense category master
- clearer separation of:
  - petty cash advance / IOU
  - petty-cash-funded expense
  - employee out-of-pocket reimbursement
  - supplier/direct purchase
- IOU outstanding balance reporting per employee/job
- partial IOU settlement details:
  - amount spent
  - cash returned
  - extra reimbursement payable
- attachments/bill upload per expense line or daily sheet
- approval role checks for finance/service supervisor
- stronger audit trail/reporting for employee advances and reimbursement balances

## Pending Phase 4: Reports, Dashboards, PDF, And Final QA

Still pending:

- daily sheet PDF/service report
- job daily sheet report
- technician daily work/time report
- petty cash by job report
- IOU/employee advance report
- employee reimbursement report
- material issued by job report
- material returned/damaged/rejected report
- pending daily sheet approval report
- pending job closeout report
- daily sheet dashboard widget
- mobile-friendly technician screen
- full UAT walkthrough from daily sheet to final invoice

## Recommended Next Agent Steps

1. Treat `origin/main` at `2e29358` or later as the baseline.
2. Preserve unrelated local worktree changes unless the user explicitly asks to stage or revert them.
3. Continue with remaining reporting/PDF/accounting refinements, especially:
   - daily sheet PDF/service report
   - pending closeout report
   - IOU/employee advance report
   - material issued/returned/damaged report
   - direct line entry for MRN/expense from the daily job context
4. Run targeted checks for service-job work:
   - `dotnet test backend\tests\ISS.UnitTests\ISS.UnitTests.csproj`
   - `dotnet build backend\src\ISS.Api\ISS.Api.csproj`
   - `npx tsc --noEmit` from `frontend/`
   - targeted `npx eslint` for changed frontend files
5. Push with `GIT_TERMINAL_PROMPT=0` if the Windows Git credential manager hangs.
6. Deploy to Railway from a clean detached worktree using:
   - `npx @railway/cli@latest up --service ERP --environment production --detach`
7. Verify `/login`, job detail tabs, and daily-work deep links after deploy.
