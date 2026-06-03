# Service Job Section User Manual

This manual explains the service job section in simple user language. It covers equipment units, command center, dispatch board, technician workbench, job orders, daily field sheets, job sheets/work orders, materials, expenses, estimates, service handover, billing, costs, files, and closeout.

The main rule in this section is: **look at the list or status first, then open a form only when you need to add or edit something**. Create and edit forms open in modal dialogs where possible so users do not lose the page they are working on.

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

![Job Orders list](images/job-orders/01-jobs-list.png)

The job list is the main place to open, view, or edit jobs.

- Click the job number or `View` to open the full job detail page.
- Click `+ New Job Order` to create a new job in a modal dialog.
- Click `Edit` on an editable job to open the job header edit modal directly from the list.
- Jobs are normally editable while they are `Draft`, `Open`, or `Reopened`.
- Once execution starts, the job header is locked and users should continue through daily sheets, work orders, materials, expenses, handover, and billing.

## 7. Create A New Job Order

From the job list, click `+ New Job Order`.

Enter:

- Equipment unit
- Customer
- Job type: `Service`, `Repair`, `PDI`, `Warranty`, or `Inspection`
- Site/location
- Responsible officer
- Customer complaint or service requirement
- Job description and internal remarks if needed

When the job is created, the system checks service contract and warranty entitlement. If contract or warranty data is added later, open the job and click `Refresh Entitlement`.

## 8. Job Overview

Open a job to see the compact header, cockpit, and process timeline.

![Job overview](images/job-orders/02-job-overview.png)

The overview shows:

- Job number, status, type, equipment, customer, and responsible officer
- Main job actions such as `Start`, `Complete`, `Close`, `Reopen`, and `Refresh Entitlement`
- Job Cockpit summary cards
- Process Timeline from intake to closeout

Use the process timeline to jump to the correct work area instead of scrolling through the full page.

## 9. Edit Job Header

There are two ways to edit a job header:

- From `Service -> Job Orders`, click `Edit` in the row.
- From job detail `Overview`, click `Edit Job`.

Both open the same edit modal.

Use this only for intake/header information, such as:

- Equipment
- Customer
- Job type
- Expected dates
- Site/location
- Responsible officer
- Customer complaint
- Problem/intake note
- Internal remarks

Do not use header editing to record daily work, parts, labour, or billing. Those belong in their own tabs.

## 10. Plan Job Operations

Open the `Plan` tab.

![Plan tab](images/job-orders/03-plan.png)

Use this tab to plan major repair stages or sub-parts before doing the actual work.

Examples:

- Diagnose hydraulic leak
- Remove and inspect pump
- Replace filter
- Test under load

Important:

- Planning does not reduce stock.
- Planning does not create billable labour.
- Actual parts are issued through MRNs.
- Actual billable labour is entered through job sheets/work orders.

## 11. Daily Field Sheets

Open `Daily Work -> Daily Sheets`.

![Daily sheets](images/job-orders/04-daily-sheets.png)

A daily field sheet is the daily record of what happened on a job.

Create one daily sheet for each working day.

Each daily sheet can show:

- Date
- Work planned
- Work completed
- Work pending
- Site or weather condition
- Staff count
- Progress count
- Material/MRN count
- Return/damage count
- Expense count
- IOU count
- Approval status

Use daily sheets for daily control and supervisor review.

## 12. Daily Staff / Labour

Open `Daily Work -> Staff / Labor`.

![Daily labour](images/job-orders/05-daily-labor.png)

This area records who attended the job on a particular daily sheet.

Use it for:

- Attendance
- Daily assignment
- What the person did that day
- Normal and overtime hours for daily tracking
- Supervisor review of who worked on site

This is a daily operational record. It helps users understand who worked on a job on each day.

## 13. Job Sheets / Work Orders Labour

Use `Service -> Job Sheets / Work Orders` for billable labour, time entries, and job-sheet labour costing.

This is different from daily staff/labour.

| Daily Staff / Labour | Job Sheets / Work Orders Labour |
| --- | --- |
| Shows who attended a daily field sheet. | Shows billable or costed labour time entries. |
| Used for daily job supervision. | Used for costing, approval, and customer billing. |
| Linked to a daily sheet. | Linked to a work order/job sheet and service job. |
| Helps answer: “Who worked today?” | Helps answer: “What labour cost/billing should be posted?” |
| Does not by itself create final billable labour. | Approved billable entries can feed invoices. |

Simple example:

- Technician A attends the site today. Add Technician A in `Daily Staff / Labor`.
- Technician A performs 3 billable repair hours. Add a time entry in `Job Sheets / Work Orders`.
- The daily sheet shows attendance. The work order time entry supports costing and billing.

Use both when both daily attendance and billable labour are required.

## 14. Daily Progress

Open `Daily Work -> Progress`.

Progress updates are recorded against a daily field sheet.

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

## 15. Materials And MRNs

Open the `Materials` tab.

![Materials tab](images/job-orders/06-materials.png)

Materials are handled through MRNs and material disposition.

Tabs:

- `Issued MRNs`
- `Return Materials`
- `Damage Material`

Use `+ New MRN` to create a draft material requisition for the job. Then open the MRN document, add item lines, and post it.

Important:

- Draft MRNs do not reduce stock.
- Posted MRNs reduce stock.
- Posted MRNs appear in the job under `Issued MRNs`.
- Unused, wrong, rejected, or damaged materials should be recorded through return/damage disposition.

Use material disposition before job closeout so the system knows what happened to every issued item.

## 16. IOUs And Expenses

Open the `Expenses` tab.

![Expenses tab](images/job-orders/07-expenses.png)

There are three separate expense workflows.

### IOU Advances

Use `+ Request IOU` when a person needs a cash advance before expenses are finalized.

Example:

- Technician needs cash for emergency job-related transport or a small purchase.
- The IOU is requested from the job.
- Finance approves, releases, and later settles the IOU.

The requester is the signed-in system user. After creation, the IOU remains visible in the job IOU register.

### Petty Cash Expenses

Use `+ Petty Cash Voucher` when company petty cash was used for the job.

Record:

- Daily sheet
- Voucher date
- Merchant/vendor
- Bill number issued by the accountant
- Payment handover method: cash handover, bank deposit, or other
- Notes

### Out-Of-Pocket Claims

Use `+ Reimbursement Claim` when an employee paid personally and needs reimbursement.

The claim remains visible in the job expense register and follows finance approval and settlement.

## 17. Service Estimates / Quotations

Use `Service -> Quotations` or the job `Billing` area to manage service estimates.

Use estimates when the customer must approve a quoted repair or service amount before work continues.

Estimate lines can include:

- Parts
- Labour
- Billable expenses

Draft estimates can be edited. Once sent or approved, use change-order rules instead of silently overwriting approved scope.

## 18. Service Taken / Handover

Use `Service -> Service Taken` / service handover when the repair or service is handed back to the customer.

The handover records:

- Handover date
- Customer acknowledgement
- Returned items or notes
- Post-service warranty if applicable
- Final service confirmation

The handover is also part of the final invoice path where applicable.

## 19. Billing And Closeout

Open the `Billing` tab.

![Billing tab](images/job-orders/08-billing.png)

Billing includes:

- Closeout readiness
- Warranty/billing entitlement
- Quotations and final invoices

Closeout readiness tells users what is blocking job closure.

Common blockers:

- Draft or submitted daily sheets
- Pending IOUs
- Pending expense claims
- Draft MRNs
- Open labour entries
- Unresolved material disposition
- Missing final invoice decision

Clear the blockers before closing the job.

## 20. Costs

Open the `Costs` tab.

![Costs tab](images/job-orders/09-costs.png)

The cost view shows:

- Actual cost
- Quoted revenue
- Posted invoice revenue
- Uninvoiced billable labour
- Material cost
- Direct purchase cost
- Approved labour cost
- Approved claim cost

Use this tab before billing or closing to understand job profitability.

## 21. Files And Notes

Open `Files & Notes`.

![Files and notes](images/job-orders/10-files-notes.png)

Use this area for:

- Customer communication
- Internal comments
- Attachments
- Approval notes
- Supporting documents

## 22. Recommended End-To-End Job Flow

1. Create or confirm the equipment unit.
2. Open the job order.
3. Review entitlement or refresh entitlement if needed.
4. Start the job.
5. Plan operations if the work has multiple stages.
6. Create a daily field sheet for each working day.
7. Record daily staff and progress against the daily sheet.
8. Issue materials through MRNs and post them.
9. Record unused/damaged/rejected material disposition.
10. Record IOUs and expenses where needed.
11. Record billable labour through job sheets/work orders.
12. Prepare estimate or change order if customer approval is needed.
13. Complete the job when work is finished.
14. Prepare service taken/handover.
15. Review billing, invoices, costs, and closeout readiness.
16. Clear all blockers.
17. Close the job.

## 23. Common User Questions

| Question | Simple Answer |
| --- | --- |
| Should I create a daily sheet or a work order? | Create a daily sheet for daily site/work record. Use a work order/job sheet for billable labour/time entries. |
| Does daily staff labour create an invoice? | No. It records attendance/work for the day. Invoice labour comes from approved billable job sheet/work-order time entries. |
| Does planning a part reduce stock? | No. Stock reduces only when an MRN is posted. |
| Why is my IOU still visible after requesting it? | That is correct. It stays visible so the requester and supervisor know it was sent and can track finance status. |
| Why can’t I close the job? | Open `Billing -> Closeout Readiness` and clear the listed blockers. |
| Why can’t I edit the job header? | The job may already be started, completed, invoiced, closed, or cancelled. Continue through operational tabs instead. |
| Why does an expense claim show zero total? | Open the claim detail and add expense lines. |
| Where do I check job profit? | Open the job `Costs` tab. |
| Where do technicians work daily? | Use `Technician Workbench` or the job `Daily Work` tab. |
