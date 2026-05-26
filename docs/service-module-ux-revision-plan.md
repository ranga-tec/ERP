# Service Module UX Revision Plan

Date: 2026-05-25

## Phase 1 Implementation Status

Implemented on 2026-05-26:

- Job-local IOU register added to the job `Expenses -> IOU Advances` view.
- Job-local expense claim registers added to the job `Expenses -> Petty Cash Expenses` and `Expenses -> Out-of-Pocket Claims` views.
- IOU creation now confirms the generated IOU number and tells the user it is waiting for finance approval.
- Daily progress history now appears before the add-progress form.
- Daily sheet rows now expose direct quick links for labour, progress, material issue, IOU request, and expense entry.
- Production frontend build passed after the Phase 1 changes.

## Problem Summary

The service module has the right operational data model, but the current job-detail experience is too document-centric and too scroll-heavy for daily users.

Observed issues:

- `frontend/src/app/(app)/service/jobs/[id]/page.tsx` is a single large page component of about 1,963 lines.
- Job users must understand where each record lives: daily sheet, staff/labour, progress, materials, expenses, job sheet/work order, quotation, service taken, invoice, and costs.
- Daily sheet rows show counts, but the job page does not provide a single visual process view that explains what has happened, what is pending, and what the next action is.
- Staff/labour and progress are hidden behind daily-work sub-tabs. Users must first understand daily sheets before they can find who worked on the job.
- Progress update history appears below the progress form, so users can miss existing updates after entering or reviewing data.
- Job-linked petty-cash IOUs and expense vouchers can be created from the job, but the job detail does not show the created IOU/claim list. The user is forced to know that IOUs are later handled under Finance.
- Backend already supports job-filtered lists for IOUs and claims:
  - `GET /api/finance/petty-cash-ious?serviceJobId=...`
  - `GET /api/service/expense-claims?serviceJobId=...`
- Closeout readiness exists, but it is mostly a blocking checklist, not a guided workflow.

## External Product Patterns

Common field-service systems do not make users discover work through disconnected forms.

- Microsoft Dynamics 365 Field Service models the work order lifecycle as `Create -> Schedule -> Dispatch -> Service -> Review -> Invoice`, and separates work-order status from technician booking status. Booking status changes such as `Traveling`, `In Progress`, and `Completed` update timeline fields and drive the work-order lifecycle. Source: https://learn.microsoft.com/en-us/dynamics365/field-service/work-order-status-booking-status
- Dynamics Field Service uses a schedule board and map views so dispatchers can see work orders, resources, status, and location context instead of relying only on lists. Source: https://learn.microsoft.com/en-us/dynamics365/field-service/work-with-schedule-board and https://learn.microsoft.com/en-us/dynamics365/field-service/field-service-maps-address-locations
- ServiceNow Field Service Management uses work order tasks with clear sequential states: `Draft`, `Pending dispatch`, `Scheduled`, `Assigned`, `Accepted`, `Work in Progress`, `Closed Complete`, `Closed Incomplete`, and `Cancelled`. Source: https://www.servicenow.com/docs/r/field-service-management/work-order-management/work-order-task-states.html
- ServiceNow dispatching can happen from task records, a task map, or Dispatcher Workspace, and Dispatcher Workspace is positioned as the main workspace with tasks, teams, locations, status, filters, search, sorting, and configurable cards. Source: https://www.servicenow.com/docs/r/field-service-management/field-service-scheduling/c_DispatchWorkOrderTasks.html and https://www.servicenow.com/docs/r/IJyAGf92oiTtn3C0u_FeaQ/N9nsYBtQdmIr6c5wbO6BBw
- ServiceNow also provides an overview dashboard with visualizations such as work orders by priority, SLA by stage, tasks by state, and tasks by assignment group. Source: https://www.servicenow.com/docs/bundle/washingtondc-field-service-management/page/use/dashboards/application-content-packs/Use_field_service_management_overview_dashboard.html
- Salesforce Field Service dispatcher console exposes Gantt and map concepts for dispatchers; its map view shows service appointments and resources geographically. Source: https://help.salesforce.com/s/articleView?id=service.pfs_using_map.htm&type=5

## Target Design Direction

ISS should keep its existing service documents and accounting controls, but the user-facing service module should move to a workspace model:

1. Service command center for supervisors and coordinators.
2. Job cockpit for each job.
3. Daily sheet drawer or timeline for daily work.
4. Inline job-local visibility for IOUs, expenses, labour, progress, materials, quotations, invoices, and service taken.
5. Guided next actions based on job status and unresolved records.

## Proposed Information Architecture

### Service Command Center

New page: `Service -> Command Center`

Purpose:

- First screen for service managers, coordinators, and supervisors.
- Show what is currently processing, overdue, blocked, waiting for finance, waiting for customer approval, ready to invoice, and ready to close.

Widgets:

- Jobs by status: Open, In Progress, Completed, Invoiced, Closed, Reopened.
- Jobs by stage: Intake, Plan, Daily Work, Materials, Expenses, Billing, Closeout.
- Priority/age board: overdue expected completion, no activity today, no daily sheet today.
- Financial blockers: pending IOUs, approved unreleased IOUs, unsettled IOUs, submitted claims, approved unsettled claims.
- Billing blockers: approved labour not invoiced, billable claims not converted, service taken not invoiced.
- Cards for active jobs with status, customer, equipment, last update, next action, and pending blockers.

### Job Cockpit

Replace the current "many tabs plus long sections" feel with a cockpit layout:

- Sticky job header:
  - job number, customer, equipment, status, priority, expected completion, responsible officer
  - primary next action button
  - visible pending blocker count
- Process timeline:
  - Intake
  - Plan
  - Daily Work
  - Materials
  - Expenses
  - Quote / Approval
  - Service Taken
  - Invoice
  - Close
- Left or top navigation for sections, but every section starts with summary cards and recent records.
- Activity feed:
  - daily sheet created/submitted/approved
  - labour assigned/approved
  - progress added
  - MRN posted
  - material return/damage posted
  - IOU requested/submitted/approved/released/settled
  - claim created/submitted/approved/settled
  - quotation approved
  - invoice created/posted
- "Next actions" panel:
  - create today daily sheet
  - add labour
  - add progress
  - issue materials
  - request IOU
  - add expense
  - create service taken
  - create invoice
  - close job

### Daily Work Revision

Current issue:

- Users must go into `Daily Work`, understand daily sheets, then use `Staff / Labor` or `Progress`.

Recommended:

- Keep daily sheets, but present each sheet as an expandable daily card.
- Each daily card should show:
  - planned work
  - completed work
  - pending work
  - staff/labour rows
  - progress updates
  - MRNs
  - IOUs
  - expenses
  - status and approval actions
- Add buttons on each daily card:
  - Add labour
  - Add progress
  - Issue material
  - Request IOU
  - Add expense
  - Submit sheet
  - Approve/reject sheet
- Keep sub-tabs for power users, but do not make them the only way to discover labour/progress.

### Expenses And Petty Cash Revision

Current issue:

- Creating IOU from a job submits it, refreshes the page, and clears the form. The created IOU is not shown on the job page.
- Expense claims redirect to the claim detail, but the job page does not provide a job-local expense register.

Recommended:

- Fetch job-linked IOUs and claims on the job page:
  - `/finance/petty-cash-ious?serviceJobId={id}`
  - `/service/expense-claims?serviceJobId={id}`
- Show registers inside the `Expenses` section:
  - IOU number, daily sheet, requested by, amount, status, submitted/approved/released/settled dates, finance action link
  - Claim number, type, daily sheet, claimed by, merchant, amount, status, line count, billable unconverted count, detail link
- After creating an IOU, show a success message: `IOU PCIOU-0001 submitted and waiting for finance approval`.
- Keep the created IOU visible immediately in the table.
- Add an expense status timeline: Requested -> Submitted -> Approved -> Cash Released -> Settled.

### Labour Revision

Current issue:

- Daily staff assignments and work-order time entries are two separate concepts, but users may not understand the split.

Recommended:

- Rename visible labels consistently:
  - `Daily Staff` = who worked/attended that day
  - `Billable Labour / Timesheets` = labour cost and customer billing source
- On the job cockpit, show both:
  - Daily attendance summary by person/day
  - Billable labour summary from job sheet/work-order time entries
- Add cross-links:
  - From a daily sheet labour row to the related job sheet/time entry if linked.
  - From a work-order time entry back to the job and daily sheet if linked.

### Progress Revision

Current issue:

- Progress updates are low visibility and can sit below a long form.

Recommended:

- Show latest progress updates before the add form.
- Add a compact timeline:
  - date/time
  - completed
  - pending
  - problem
  - next required action
- Put `Add Progress` in a drawer/modal or collapsible form below the recent timeline.
- Add "Last progress update" to job list and command center cards.

### Billing Revision

Current issue:

- Billing information is present, but it is too separate from closeout and service taken.

Recommended:

- Billing panel should show:
  - quote/estimate status
  - approved labour not invoiced
  - billable expenses not converted
  - service taken status
  - invoice status
  - final invoice decision
- Manual invoice creation from service taken should be visible as the normal path, not a hidden conversion detail.

## Implementation Phases

### Phase 1: Fix Disappearing And Low-Visibility Records

This is the highest-value quick win.

- Completed: add job-local IOU list to the job `Expenses` tab.
- Completed: add job-local expense claim list to the job `Expenses` tab.
- Completed: show success confirmation after IOU creation.
- Completed: show recent progress updates above the add-progress form.
- Completed: add quick links from daily sheet rows to `Add labour`, `Add progress`, `Request IOU`, `Add expense`, and `Issue material`.

### Phase 2: Job Cockpit Summary

- Add top summary cards for:
  - last progress update
  - assigned staff today
  - pending IOUs/claims
  - uninvoiced labour
  - pending material disposition
  - invoice/service taken status
- Add process timeline below the header.
- Add next-action panel based on closeout checks and current job status.

### Phase 3: Daily Sheet Card Workspace

- Replace the daily-work sub-tab-first experience with expandable daily cards.
- Each daily card shows staff, progress, materials, expenses, and IOUs together.
- Forms open inline inside the selected daily card or in drawers.

### Phase 4: Service Command Center

- New `/service/command-center` route.
- Backend summary endpoint:
  - `GET /api/service/dashboard`
  - status counts, stage counts, blocker counts, overdue jobs, active job cards, financial queues, billing queues
- Frontend dashboard with charts/cards and filtered job lists.

### Phase 5: Dispatch And Technician View

- Add a dispatch board if ISS needs field/service scheduling:
  - unassigned jobs
  - assigned technician/team
  - planned date/time
  - status
  - simple calendar/list view first
- Add technician-friendly job page:
  - today jobs
  - start work
  - add progress
  - request material
  - request IOU/expense
  - complete daily sheet

## Recommended First Build Scope

Start with Phase 1 and Phase 2. They address the user confusion without redesigning the whole database.

Files likely touched first:

- `frontend/src/app/(app)/service/jobs/[id]/page.tsx`
- `frontend/src/app/(app)/service/jobs/ServiceJobDailyIouCreateForm.tsx`
- `frontend/src/app/(app)/service/jobs/ServiceJobDailyExpenseClaimCreateForm.tsx`
- optional new components:
  - `ServiceJobProcessTimeline.tsx`
  - `ServiceJobNextActions.tsx`
  - `ServiceJobExpenseRegister.tsx`
  - `ServiceJobDailySheetCard.tsx`

Backend endpoint work is minimal for Phase 1 because the filtered IOU and claim list endpoints already exist.
