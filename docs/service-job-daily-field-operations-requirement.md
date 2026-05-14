# Service Job Daily Field Operations Requirement

## Purpose

The service job must be operated from the system while the work is happening. Users should not keep separate notebooks for daily work, technician attendance, petty cash advances, expenses, material issues, lubricants, unused returns, damaged items, or job closeout notes.

The practical field-service pattern is:

1. Open the service job/work order.
2. Create a daily field sheet for each working day or site visit.
3. Record labor, progress, IOUs, expenses, materials, returns, damages, and attachments against that daily sheet.
4. Approve the daily sheet.
5. Feed approved and pending transactions into job costing and closeout checks.

This matches common field-service systems where technicians update work orders in the field, record expenses against work orders, track actual job costs from labor/materials/expenses, and return excess or defective parts through field inventory return flows.

Reference points used:

- Salesforce Field Service pricing/features: technician users can view and update work orders, create quotes, and submit service reports from the field: https://www.salesforce.com/service/field-service-management/pricing
- Salesforce Field Service expenses: expenses are associated to work orders: https://help.salesforce.com/s/articleView?id=service.fs_expense_fields.htm&type=5
- Simpro job costing: job cost should track materials, labor, contractors, equipment, overhead, and live profitability during work: https://www.simprogroup.com/blog/9-job-costing-tips-for-field-service-businesses
- Oracle field service parts returns: technicians return excess, unused, and defective parts to warehouse or return destinations: https://docs.oracle.com/en/cloud/saas/service-logistics/24c/fasul/route-and-return-field-service-parts.html
- Oracle Field Service Technician Portal: unused/defective parts return and reverse-logistics routing are standard technician workflows: https://docs.oracle.com/cd/E18727_01/doc.121/e12787/T325183T325196.htm

## User Requirement Compared With Current System

| Requirement | Already existed before this change | Gap | Implemented now |
| --- | --- | --- | --- |
| Open service jobs | Yes | Needed stronger daily operating flow | Daily sheets are linked to service jobs |
| Assign technicians/workers | Yes, on job detail | It was not linked to a daily sheet | Assignment can now be linked to a daily sheet |
| Daily work/progress notes | Yes, separate progress updates | It was separate from cash/materials/labor | Progress update can now be linked to a daily sheet |
| Petty cash/IOU advance during running job | Yes, finance IOU linked to job | It was not captured from the job daily workflow | IOU can be created from job detail and linked to a daily sheet |
| Out-of-pocket expense claim | Yes, service expense claim | It was not linked to a daily field sheet | Expense voucher can be created from job detail and linked to a daily sheet |
| Material/lubricant issue | Yes, MRN against job | It was not linked to daily field work | MRN can be created from job detail and linked to a daily sheet |
| Unused/incorrect material return | Yes, material disposition from previous phase | It was not linked to the daily sheet | Material disposition can be linked to a daily sheet |
| Damaged/rejected/supplier return tracking | Yes, material disposition from previous phase | It was not linked to the daily sheet | Damaged/rejected disposition can be linked to a daily sheet |
| Ongoing costing | Yes, job costing shows material, labor, expenses | Needed real-time entry points while job is running | Daily transactions feed the existing costing records |
| Closeout controls | Yes | Needed daily sheet approval as an operational control | Daily sheets have Draft, Submitted, Approved, Rejected status |

## Implemented Workflow

### Daily Field Sheet

A daily sheet records:

- sheet number
- job
- date/time
- prepared by
- site/location
- shift
- site condition
- planned work
- completed work
- pending work
- problems found
- customer instructions
- technician notes
- supervisor notes
- status

Statuses:

- Draft
- Submitted
- Approved
- Rejected

### Linked Operational Entries

The following records can be linked to a daily sheet:

- technician/worker assignment
- daily job progress update
- petty cash IOU advance
- service expense claim
- material requisition
- material disposition/return/damage/rejection

### Accounting And Inventory Treatment

The daily sheet does not bypass accounting or inventory controls.

- IOU advance remains an employee advance until released and settled.
- Out-of-pocket spending remains an employee reimbursement claim.
- Petty-cash spending remains a petty-cash funded expense claim.
- Material issues still post through MRN stock consumption.
- Unused and incorrect returns still create stock receipts.
- Damaged/rejected/supplier return dispositions remain visible in job costing/closeout but do not silently add stock back.

## Remaining Future Improvements

This version gives the practical daily operating structure. Later refinements can add:

- split the current job detail daily action area into separate sections for staff/labor, progress, IOU advances, petty-cash expenses, employee out-of-pocket claims, material/lubricant issues, and material returns/damage/rejection
- photo capture directly per daily sheet section
- mobile/offline technician screen
- daily sheet PDF
- supervisor approval rules by role
- technician trunk stock
- daily sheet dashboard/report
- expense category master
- direct line entry for expense voucher from the daily job page
- direct line entry for MRN from the daily job page

## Handover Note

Detailed agent handover is maintained in `docs/agent-handover-service-job-daily-operations.md`. It lists completed files, deployment status, GitHub push blocker, the requested UI split, and the remaining phase 3 and phase 4 work.
