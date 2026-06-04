# Service Job Section User Manual

This manual explains the service job section in simple user language. It covers equipment units, command center, dispatch board, technician workbench, job orders, daily field sheets, job sheets/work orders, materials, expenses, estimates, service handover, billing, costs, files, and closeout.

The main rule in this section is: **look at the list or status first, then open a form only when you need to add or edit something**. Create and edit forms open in modal dialogs so users do not lose the page they are working on.

## 1. Main Service Menu Areas

The Service module contains several screens. Each screen has a different purpose.

| Screen | Purpose |
| --- | --- |
| `Command Center` | Supervisor view of active jobs, overdue work, missing daily sheets, missing progress, finance blockers, billing queues, and closeout blockers. |
| `Dispatch Board` | Operational lane view for unassigned, assigned/active, waiting, and completed jobs. |
| `Technician Workbench` | Technician daily view for today's assignments, open daily sheets, and quick actions. |
| `Equipment Units` | Customer-owned machines/equipment that can receive service jobs. |
| `Service Contracts` | Contract coverage, billing entitlement, and service agreement information. |
| `Job Orders` | Main job record: intake, daily work, materials, expenses, billing, costs, and closeout. |
| `Technicians` | Technician master records and default labour rates. |
| `Job Sheets / Work Orders` | Billable work records and time entries used for labour costing and invoicing. |
| `MRN / Material Requisitions` | Stock issue documents used to consume spare parts/materials for jobs. |
| `Quotations / Estimates` | Customer quotation and change-order process. |
| `Service Taken / Handovers` | Customer handover, final service confirmation, and invoice conversion path. |
| `Quality Checks` | Inspection or QC records linked to service work. |

## 2. Equipment Units

Use `Service -> Equipment Units` to register customer equipment before opening a job.

An equipment unit normally contains:

- Serial number
- Linked item/machine model
- Customer
- Site/location information
- Warranty coverage where applicable

When a job is opened, select the equipment unit first. The system uses the equipment unit to default or validate the customer and to check warranty/contract entitlement.

Use equipment units when:

- A customer sends a machine for repair.
- A technician visits installed customer equipment.
- Warranty or service-contract coverage must be checked.

## 3. Command Center

Use `Service -> Command Center` as the supervisor or coordinator first screen.

The command center is not for entering job details. It is for seeing what needs attention.

Use it to find:

- Active jobs
- Overdue jobs
- Jobs without today's daily sheet
- Jobs without today's progress update
- Pending daily sheets
- Pending IOUs and expense claims
- Billing-ready jobs
- Jobs blocked from closeout

From command center cards or queue rows, open the related job or working area.

## 4. Dispatch Board

Use `Service -> Dispatch Board` to view jobs by operational lane.

Typical lanes are:

- Unassigned
- Assigned / Active
- Waiting
- Completed

Use this page when a coordinator needs to see which jobs are not assigned, which jobs are being worked on, and which jobs are waiting for parts, customer approval, supplier response, or another blocker.

## 5. Technician Workbench

Use `Service -> Technician Workbench` for technician daily work.

It shows:

- Today's assignments
- Open daily sheets
- Active jobs
- Quick links for progress, material requests, IOU requests, and expenses

Technicians should normally work from this screen or from the relevant job's `Daily Work` tab.

## 6. Job Orders List

Go to `Service -> Job Orders`.

The job list is the first screen. Existing jobs appear immediately without any form blocking the view.

- Click the job number or `View` to open the full job detail page.
- Click `Edit` in a row to edit the job header in a modal dialog without leaving the list.
- Click `+ New Job Order` (top right) to open the job creation form in a modal dialog.
- Jobs are editable while they are `Draft`, `Open`, or `Reopened`.
- Once execution starts, the job header locks. Continue through daily sheets, work orders, materials, expenses, handover, and billing.

## 7. Create A New Job Order

From the job list, click `+ New Job Order`.

A modal dialog opens. Enter:

- Equipment unit
- Customer
- Job type: `Service`, `Repair`, `PDI`, `Warranty`, or `Inspection`
- Site/location
- Responsible officer
- Customer complaint or service requirement
- Job description and internal remarks if needed

When the job is created, the system checks service contract and warranty entitlement. If contract or warranty data is added later, open the job and click `Refresh Entitlement`.

## 8. Job Detail — Header And Navigation

Open a job to see the compact header, cockpit, and process timeline.

The header shows:

- Breadcrumb: `Job Orders / SJ000001`
- Job number, coloured **status badge** (blue for active, green for closed, red for cancelled), and job type badge
- Essential fields: Equipment, Customer, Site, Responsible — all on one compact line
- Click **`Show dates & details ▾`** to expand full date fields (Opened, Est. start, Actual start, Expected, Completed, Invoice required)
- Inline action buttons: `PDF`, `Start`, `Complete`, `Close`, `Reopen`, `Refresh Entitlement`

Below the header is the **tab navigation bar**. Clicking any tab or Process Timeline card scrolls the browser directly to the relevant work area — you do not need to scroll manually after clicking.

## 9. Edit Job Header

There are two ways to edit a job header:

- From `Service -> Job Orders`, click `Edit` in the row → opens in a modal dialog.
- From job detail, click `Edit Job` (visible only while the job is `Draft`, `Open`, or `Reopened`).

Both open the same edit form in a modal dialog. The page behind the modal stays intact.

Use this only for intake/header information:

- Equipment, Customer, Job type
- Expected dates, Site/location, Responsible officer
- Customer complaint, Problem/intake note, Internal remarks

Do not use header editing to record daily work, parts, labour, or billing. Those belong in their own tabs.

## 10. Job Cockpit (Overview Tab)

The **Job Cockpit** shows eight summary cards:

| Card | What it shows |
| --- | --- |
| Last Progress | Date and description of the most recent progress update. |
| Staff Today | Number of staff assigned today on this job. |
| Cash / Expenses | Pending IOU advances and expense claims. |
| Uninvoiced Labour | Approved billable labour not yet converted to a final invoice. |
| Material Disposition | Issued materials that still need final disposition (used, returned, or damaged). |
| Service Taken | Whether a handover/service-taken record exists and its status. |
| Invoice | Draft or posted invoice status for the job. |
| Job Cost | Total actual cost posted to the job so far. |

Each card is clickable and goes directly to the relevant working area.

## 11. Process Timeline (Overview Tab)

The **Process Timeline** shows 11 stages from intake to close:

`Intake` → `Plan` → `Daily Sheets` → `Labour` → `Progress` → `Materials` → `Expenses` → `Quote` → `Service Taken` → `Invoice` → `Close`

Each stage shows:

- Status badge: `Done`, `Active`, `Blocked`, or `Pending`
- Count badges (sheets, entries, etc.)
- Click the card to jump to that working area

Use the Process Timeline to navigate the job without scrolling through the full page.

## 12. Plan Job Operations

Open the `Plan` tab.

Use this tab to plan major repair stages or sub-parts before doing the actual work.

Click **`+ Add Operation`** to open the add-operation form in a panel.

Enter:

- Step number
- Work step / subassembly name
- Planned part (optional)
- Planned quantity
- Estimated labour hours
- Required date (optional)
- Description and notes

Important:

- Planning does not reduce stock.
- Planning does not create billable labour.
- Actual parts are issued through MRNs.
- Actual billable labour is entered through job sheets/work orders.

## 13. Daily Field Sheets

Open `Daily Work -> Daily Sheets`.

A daily field sheet is the daily record of what happened on a job.

Create one daily sheet for each working day or site visit.

**When no daily sheets exist:** A prominent `+ Create First Daily Sheet` button appears. Click it to open the creation form directly on the page. Fill in the date and work details and submit.

**When daily sheets exist:** A list of sheet cards appears. Click `+ Add Another Day` (top-right of the card section) to create another sheet.

Each daily sheet card shows:

- Sheet date, prepared by, and approval status
- Planned work / Completed work / Pending/Issues panels
- Staff count, Progress count, MRN count, Returns count, Expenses count, IOU count
- Quick links: `Staff / labour`, `Progress`, `Materials`, `Request IOU`, `Add expense`

Daily sheet approval states:

- `Draft` — created, not yet submitted
- `Submitted` — sent for supervisor review
- `Approved` — locked for editing
- `Rejected` — returned for corrections

## 14. Daily Staff / Labour

Open `Daily Work -> Staff / Labor`.

**Requires a daily sheet.** If no daily sheet exists, a clear message appears with a `Go to Daily Sheets` button. Create a daily sheet first, then return here.

This area records who attended the job on a particular daily sheet.

Click `+ Add Staff / Labor` to open the assignment form in a modal dialog.

Use it for:

- Attendance record
- Daily assignment
- What the person did that day
- Normal and overtime hours for daily tracking
- Supervisor review of who worked on site

This is a daily operational record. It differs from job sheets/work orders (see section 16).

## 15. Daily Progress

Open `Daily Work -> Progress`.

**Requires a daily sheet.** If no daily sheet exists, a message appears with a `Go to Daily Sheets` button.

Click `+ Add Progress Update` to open the progress form in a modal dialog.

Use progress updates to record:

- Work completed
- Work pending
- Problems found
- Additional parts required
- Additional labour required
- Customer instructions
- Site issues
- Technician notes
- Supervisor notes

Progress updates help supervisors understand the current job situation without calling the technician.

## 16. Job Sheets / Work Orders Labour

Use `Service -> Job Sheets / Work Orders` for billable labour, time entries, and job-sheet labour costing.

This is different from daily staff/labour.

| Daily Staff / Labour | Job Sheets / Work Orders Labour |
| --- | --- |
| Shows who attended a daily field sheet. | Shows billable or costed labour time entries. |
| Used for daily job supervision. | Used for costing, approval, and customer billing. |
| Linked to a daily sheet. | Linked to a work order/job sheet and service job. |
| Helps answer: "Who worked today?" | Helps answer: "What labour cost/billing should be posted?" |
| Does not by itself create final billable labour. | Approved billable entries can feed invoices. |

Simple example:

- Technician A attends the site today → Add in `Daily Staff / Labor`.
- Technician A performs 3 billable repair hours → Add a time entry in `Job Sheets / Work Orders`.
- The daily sheet shows attendance. The work order time entry supports costing and billing.

## 17. Materials And MRNs

Open the `Materials` tab.

Materials are handled through MRNs and material disposition.

Sub-tabs:

- `Issued MRNs` — shows all posted material requisitions issued to this job
- `Return Materials` — record not-needed, wrongly-issued, or supplier-rejected returns
- `Damage Material` — record damaged or unusable issued material

**To create an MRN:** On `Issued MRNs`, click **`+ New MRN`** to open the creation panel. Then open the MRN document, add item lines, and post it.

Important:

- Draft MRNs do not reduce stock.
- Posted MRNs reduce stock and appear in the job under `Issued MRNs`.
- Unused, wrong, rejected, or damaged materials must be recorded through return/damage disposition before closeout.

## 18. IOUs And Expenses

Open the `Expenses` tab.

There are three separate expense workflows selectable by sub-tab buttons.

### IOU / Employee Advance

Click `+ Request IOU` to open the IOU form in a modal dialog.

Use when a person needs a cash advance before expenses are finalized.

Example: technician needs cash for emergency job-related transport or a small purchase.

The IOU is created and submitted. Finance then approves, releases cash, and settles the advance after receipts are accounted.

The requester is the signed-in system user. After creation, the IOU remains visible in the job IOU register.

### Petty Cash Expenses

Click `+ Petty Cash Voucher` to open the voucher form in a modal dialog.

Use when company petty cash was already used for the job.

### Out-Of-Pocket Claims

Click `+ Reimbursement Claim` to open the claim form in a modal dialog.

Use when an employee paid personally and needs reimbursement.

The claim follows finance approval and settlement.

## 19. Service Estimates / Quotations

Use `Service -> Quotations` to manage service estimates.

Use estimates when the customer must approve a quoted repair or service amount before work continues.

Estimate lines can include parts, labour, and billable expenses.

Draft estimates can be edited. Once approved, use change-order rules.

## 20. Service Taken / Handover

Use `Service -> Service Taken` / handover when repair or service is handed back to the customer.

The handover records:

- Handover date
- Customer acknowledgement
- Returned items or notes
- Post-service warranty if applicable
- Final service confirmation

The handover is also part of the final invoice path where applicable.

## 21. Billing And Closeout

Open the `Billing` tab.

Billing includes three sections:

### Closeout Readiness

Shows what is blocking job closure. Each item is a clickable card that takes you to the relevant working area to resolve it.

Common blockers:

- Draft or submitted daily sheets
- Pending IOUs
- Pending expense claims
- Draft MRNs
- Open labour entries
- Unresolved material disposition
- Missing final invoice decision

Clear the blockers before closing the job.

### Warranty / Billing Entitlement

Shows the entitlement source (manufacturer warranty or service contract), coverage type, and billing treatment (Billable, Partially Covered, or Covered No Charge).

Click `Refresh Entitlement` in the job header to recalculate if contract data was updated.

### Quotations And Final Invoices

Shows linked service estimates and final sales invoices with their status and totals.

## 22. Costs

Open the `Costs` tab.

The cost view shows:

- Actual cost (materials + direct purchase + approved labour + approved claims)
- Quoted revenue (from approved estimate or latest draft)
- Posted invoice revenue
- Uninvoiced billable labour
- Actual Cost Breakdown: Materials, Direct Purchases, Approved Labour, Approved Claims

Use this tab before billing or closing to understand job profitability.

## 23. Files And Notes

Open `Files & Notes`.

Use this area for:

- Customer communication
- Internal comments
- Attachments
- Approval notes
- Supporting documents

## 24. Recommended End-To-End Job Flow

1. Create or confirm the equipment unit.
2. Open the job order (`+ New Job Order` from the list).
3. Review entitlement or click `Refresh Entitlement` if needed.
4. Start the job.
5. Plan operations if the work has multiple stages (`Plan` tab, `+ Add Operation`).
6. Create a daily field sheet for each working day (`Daily Work -> Daily Sheets`, `+ Create First Daily Sheet`).
7. Record daily staff against the daily sheet (`Daily Work -> Staff / Labor`, `+ Add Staff / Labor`).
8. Record progress against the daily sheet (`Daily Work -> Progress`, `+ Add Progress Update`).
9. Issue materials through MRNs (`Materials -> Issued MRNs`, `+ New MRN`), add lines, and post.
10. Record unused/damaged/rejected material disposition (`Materials -> Return Materials` or `Damage Material`).
11. Record IOUs and expenses where needed (`Expenses` tab).
12. Record billable labour through job sheets/work orders (`Service -> Job Sheets / Work Orders`).
13. Prepare estimate or change order if customer approval is needed.
14. Complete the job when work is finished.
15. Prepare service taken/handover (`Service -> Service Taken`).
16. Review `Billing` tab: closeout readiness, entitlement, invoices, and costs.
17. Clear all closeout blockers.
18. Close the job.

## 25. Common User Questions

| Question | Simple Answer |
| --- | --- |
| Where do I create a new job? | Click `+ New Job Order` (top right of the Job Orders list). A modal opens — do not leave the page. |
| Should I create a daily sheet or a work order? | Create a daily sheet for each working day. Use a work order/job sheet for billable labour/time entries. |
| Does daily staff labour create an invoice? | No. It records attendance/work for the day. Invoice labour comes from approved billable job sheet/work-order time entries. |
| Does planning a part reduce stock? | No. Stock reduces only when an MRN is posted. |
| Why is my IOU still visible after requesting it? | Correct — it stays visible so the requester and supervisor know it was sent and can track finance status. |
| Why can't I close the job? | Open `Billing -> Closeout Readiness` and clear all listed blockers. |
| Why can't I edit the job header? | The job may already be started. Use the tabs for operational work. Click `Refresh Entitlement` for entitlement changes. |
| Why does an expense claim show zero total? | Open the claim detail and add expense lines. |
| Where do I check job profit? | Open the job `Costs` tab. |
| Where do technicians work daily? | Use `Technician Workbench` or the job `Daily Work` tab. |
| I can't add staff or progress — there's a message. | Create a daily field sheet first. Go to `Daily Work -> Daily Sheets` and click `+ Create First Daily Sheet`. |
| The `Staff / Labor` form shows "No daily sheet selected". | Select or create a daily sheet in `Daily Sheets`, then return to `Staff / Labor`. |
| How do I jump to a specific working area quickly? | Use the **Process Timeline** cards on the `Overview` tab — each card is clickable and navigates you directly. |
