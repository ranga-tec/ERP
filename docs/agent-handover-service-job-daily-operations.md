# Agent Handover: Service Job Daily Operations

## Current Status

This handover covers the service/job module work completed up to local commit:

`cf69242 Add daily field sheets for service jobs`

GitHub push status:

- Not pushed to GitHub.
- `origin/main` is still at `28db1b5 Add service job material disposition and invoice controls`.
- Push is blocked because the active GitHub credential is `vespertecs`, and GitHub returns:
  - `Permission to ranga-tec/ERP.git denied to vespertecs`
  - HTTP `403`

Railway status:

- The local build from the daily field sheet work was deployed to Railway with `railway up --detach`.
- Live `/login` returned HTTP `200` after deployment.

Unrelated local changes exist and must not be reverted or staged unless the user explicitly asks:

- `Dockerfile`
- `backend/src/ISS.Infrastructure/DependencyInjection.cs`
- `backend/src/ISS.Infrastructure/Persistence/IssDbContextFactory.cs`
- `backend/tests/ISS.UnitTests/ISS.UnitTests.csproj`
- `docs/deployment.md`
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

- IOU, petty cash, expenses, and material actions should not remain crowded together in one job-order section.
- They should be split into clearer separate forms/sections.
- The last two broader phases are still pending and need to be tracked for the next agent.

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

On service job detail page:

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

Important current UI limitation:

- The job detail page now has one grouped section named `Daily Cash, Expense, And Material Actions`.
- The user wants these split into clearer separate forms/sections.
- Treat this as pending UI refinement, not completed.

## Completed Documentation

Updated/added:

- `docs/service-job-daily-field-operations-requirement.md`
- `docs/testing-input-output-checklist.md`
- `docs/manual-uat-guide.md`
- `docs/end-to-end-testing-workflow.md`
- `docs/user-manual.md`

The testing checklist now includes:

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

- `dotnet build ISS.slnx /p:UseSharedCompilation=false`
- `npm run build`
- `dotnet test backend/tests/ISS.UnitTests/ISS.UnitTests.csproj /p:UseSharedCompilation=false --no-build`
  - 43 tests passed
- `git diff --check`

Deployment:

- Railway deployment from local code completed.
- Live `/login` returned HTTP `200`.

## Pending Work: UI Split Requested By User

The user wants the job order/detail page to be less crowded.

Current grouped section:

- `Daily Cash, Expense, And Material Actions`
  - IOU / petty cash advance
  - service expense voucher
  - material requisition

Required split:

1. Daily Staff / Labor section
   - technician assignment
   - work times
   - daily labor descriptions
   - supervisor approval

2. Daily Progress section
   - work completed
   - work pending
   - problems found
   - customer instructions
   - technician/supervisor notes

3. IOU / Employee Advance section
   - issue advance to employee
   - submit/approve/release/settle status visibility
   - outstanding balance visible on the job

4. Petty Cash Expense section
   - expenses paid from company petty cash
   - link to petty cash fund
   - receipt/reference/bill upload later

5. Employee Out-of-Pocket Claim section
   - reimbursement claims
   - approval/settlement flow
   - payable to employee

6. Materials / Lubricants Issue section
   - MRN creation
   - line entry should ideally be possible inline later
   - stock availability/serial/batch validation remains mandatory

7. Material Returns / Damage / Rejection section
   - used
   - unused returned
   - incorrect returned
   - damaged
   - rejected/supplier return

Implementation guidance:

- Keep the backend model as-is; the daily sheet linking is already correct.
- Refactor the frontend job detail page into smaller components and sections.
- Do not duplicate accounting or stock logic.
- Keep IOU as finance IOU, expense claim as service expense claim, MRN as material requisition, and material return/damage as material disposition.

## Pending Phase 3: Accounting And Expense Refinement

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

1. Confirm GitHub credentials are fixed before attempting push.
2. Pull/inspect current local commit `cf69242`.
3. Refactor job detail daily operations UI into separate sections/components listed above.
4. Add daily sheet PDF or at least a printable view.
5. Add reporting queries for IOU, expense, material return, and daily sheet approval status.
6. Run:
   - `dotnet build ISS.slnx /p:UseSharedCompilation=false`
   - `dotnet test backend/tests/ISS.UnitTests/ISS.UnitTests.csproj /p:UseSharedCompilation=false --no-build`
   - `npm run build`
7. Commit only relevant files.
8. Push after GitHub auth is corrected.
9. Deploy to Railway and verify `/login`.
