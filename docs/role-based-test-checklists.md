# ISS Role-Based Test Checklists

This document provides manual test checklists for these user roles:

- `Admin`
- `Procurement`
- `Sales`
- `Finance`
- `Service`

It is intended for UAT, regression testing, and user training.

Validated against the current backend authorization attributes on March 30, 2026.

## How To Use This Document

1. Create one test user per role.
2. Give each user only the single role being tested.
3. Use a prepared baseline dataset so the role user can focus on business actions instead of system setup.
4. Test both:
   - what the role should be able to do
   - what the role should not be able to do

Important note:

- the frontend sidebar is broadly shared
- backend API authorization is the true enforcement layer
- a restricted user may still see some links, but they must not be able to successfully use restricted pages or actions

## Baseline Data To Prepare Before Role Testing

Prepare this once using an `Admin` user:

- at least one warehouse
- at least one item and UoM
- one supplier
- one customer
- default currencies
- default payment types
- default tax codes
- default reference forms

Recommended sample data:

| Type | Sample |
| --- | --- |
| Warehouse | `MAIN` |
| Supplier | `SUP1` |
| Customer | `CUS1` |
| Item | `SKU1 - Hydraulic Filter` |
| UoM | `PCS` |

## Role Scope Summary

| Role | Main allowed areas | Typical blocked areas |
| --- | --- | --- |
| `Admin` | All modules | None by design |
| `Procurement` | suppliers, taxes, reference forms, PR, RFQ, PO, GRN, direct purchase, supplier invoice, supplier return | users, notifications, currencies, AR/AP, payments, service work order pages |
| `Sales` | customers, taxes, reference forms, quotes, orders, dispatches, direct dispatches, invoices, customer returns, equipment units, jobs, estimates, handovers | users, procurement pages, finance pages, currencies, work orders, QC, material requisitions, expense claims |
| `Finance` | customers, suppliers, currencies, currency rates, payment types, taxes, tax conversions, reference forms, AR/AP, payments, petty cash, credit notes, debit notes, direct purchases, supplier invoices, invoices, service expense claims | users, notifications, audit logs, reporting pages, PO/GRN, service jobs/work orders/estimates/handovers |
| `Service` | customers, taxes, reference forms, equipment units, jobs, estimates, expense claims, work orders, material requisitions, quality checks, handovers, direct dispatches | users, procurement, currencies, payments, AR/AP, petty cash, audit logs, reporting |

## Admin Checklist

Use this role to verify full-system access and role setup.

### Access Smoke

- [ ] Sign in successfully.
- [ ] Open `Dashboard`.
- [ ] Open `Master Data -> Items`.
- [ ] Open `Procurement -> Purchase Orders`.
- [ ] Open `Sales -> Invoices`.
- [ ] Open `Service -> Expense Claims`.
- [ ] Open `Service -> Work Orders`.
- [ ] Open `Finance -> Payments`.
- [ ] Open `Finance -> Petty Cash`.
- [ ] Open `Reporting -> Costing`.
- [ ] Open `Admin -> Users`.
- [ ] Open `Audit Logs`.

Expected result:

- all pages load without authorization errors

### User And Role Management

- [ ] Open `Admin -> Users`.
- [ ] Create one user each for `Procurement`, `Sales`, `Finance`, and `Service`.
- [ ] Verify the user appears in the list.
- [ ] Change one user role assignment.
- [ ] Reset one user's password.
- [ ] Sign in with the updated user to confirm the password reset worked.

Expected result:

- user creation, role update, and password reset all succeed

### Admin Operations

- [ ] Open `Admin -> Import`.
- [ ] Download the Excel import template.
- [ ] Upload either a valid test file or an intentionally invalid file to confirm validation feedback.
- [ ] Open `Admin -> Notifications`.
- [ ] Verify queued notifications can be listed.
- [ ] If a retryable item exists, run the retry action.
- [ ] Open `Settings`.

Expected result:

- import tools work
- notification list loads
- settings page opens

### Cross-Module System Checks

- [ ] Create or edit one master-data record.
- [ ] Create one procurement document.
- [ ] Create one sales document.
- [ ] Create one service document.
- [ ] Create one finance document.
- [ ] Open one PDF from each area touched.
- [ ] Confirm `Audit Logs` records the actions.

Expected result:

- admin can complete full-system operations end to end

### Negative Check

- [ ] Sign in as a non-admin user created by admin.
- [ ] Confirm the non-admin user cannot successfully use `Admin -> Users`.
- [ ] Confirm the non-admin user cannot successfully use `Admin -> Notifications`.

Expected result:

- non-admin user is blocked from admin-only features

## Procurement Checklist

Use a user with only the `Procurement` role.

### Allowed Access Smoke

- [ ] Sign in successfully.
- [ ] Open `Master Data -> Suppliers`.
- [ ] Open `Master Data -> Tax Codes`.
- [ ] Open `Master Data -> Reference Forms`.
- [ ] Open `Procurement -> Purchase Reqs`.
- [ ] Open `Procurement -> RFQs`.
- [ ] Open `Procurement -> Purchase Orders`.
- [ ] Open `Procurement -> Goods Receipts`.
- [ ] Open `Procurement -> Direct Purchases`.
- [ ] Open `Procurement -> Supplier Invoices`.
- [ ] Open `Procurement -> Supplier Returns`.

Expected result:

- all listed pages load and show usable data

### Core Procurement Flow

- [ ] Create a purchase requisition.
- [ ] Add at least one line.
- [ ] Submit and approve the PR if the test path requires both actions.
- [ ] Convert the approved PR to a purchase order.
- [ ] Open the resulting PO and verify the supplier, item, and quantity.
- [ ] Approve the PO.
- [ ] Create a goods receipt from the PO.
- [ ] Use the PO receipt plan to add the delivered quantity into the draft GRN and post it.

Expected result:

- PR, PO, and GRN flow completes without validation or authorization errors

### Additional Procurement Flows

- [ ] Create an RFQ and add a line.
- [ ] Send the RFQ.
- [ ] Create a direct purchase and post it.
- [ ] Create a supplier invoice and post it.
- [ ] Create a supplier return and post it.

Expected result:

- each document follows the expected status transition

### Document Quality Checks

- [ ] Open one procurement detail page in draft status.
- [ ] Add a line.
- [ ] Edit the line.
- [ ] Save the line.
- [ ] Delete the line if the document is still draft.
- [ ] Download at least one procurement PDF.
- [ ] Add one comment or attachment if collaboration is present on the page.

Expected result:

- draft line actions work
- PDF opens
- collaboration actions save successfully where available

### Downstream Impact Checks

- [ ] Confirm a posted GRN creates AP visibility for finance.
- [ ] Confirm a posted direct purchase increases stock.
- [ ] Confirm a supplier return reduces stock or adjusts supplier balance as designed.

Expected result:

- procurement documents cause the expected business impact

### Negative Access Checks

- [ ] Attempt to open `Admin -> Users`.
- [ ] Attempt to open `Finance -> Payments`.
- [ ] Attempt to open `Finance -> AR`.
- [ ] Attempt to open `Master Data -> Currencies`.
- [ ] Attempt to open `Service -> Work Orders`.

Expected result:

- procurement user cannot successfully use those restricted screens

## Sales Checklist

Use a user with only the `Sales` role.

### Allowed Access Smoke

- [ ] Sign in successfully.
- [ ] Open `Master Data -> Customers`.
- [ ] Open `Master Data -> Tax Codes`.
- [ ] Open `Master Data -> Reference Forms`.
- [ ] Open `Sales -> Quotes`.
- [ ] Open `Sales -> Orders`.
- [ ] Open `Sales -> Dispatches`.
- [ ] Open `Sales -> Direct Dispatches`.
- [ ] Open `Sales -> Invoices`.
- [ ] Open `Sales -> Customer Returns`.
- [ ] Open `Service -> Equipment Units`.
- [ ] Open `Service -> Jobs`.
- [ ] Open `Service -> Estimates`.
- [ ] Open `Service -> Handovers`.

Expected result:

- all listed pages load and are usable

### Core Sales Flow

- [ ] Create a quote and add a line.
- [ ] Send the quote.
- [ ] Create a sales order and add a line.
- [ ] Confirm the order.
- [ ] Create a dispatch from the order if the workflow requires it.
- [ ] Add lines and post the dispatch.
- [ ] Create a sales invoice.
- [ ] Add at least one line and post the invoice.

Expected result:

- quote, order, dispatch, and invoice statuses update correctly

### Direct Sales And Returns

- [ ] Create a direct dispatch.
- [ ] Add a line and post it.
- [ ] Create a customer return against the customer or invoice path used by the team.
- [ ] Add a line and post it.

Expected result:

- stock issues and returns behave correctly
- invoice or AR-related downstream records are created where expected

### Shared Sales-Service Checks

- [ ] Create or view an equipment unit.
- [ ] Create a service estimate and add a line.
- [ ] Send or approve the estimate.
- [ ] Create or update a service job.
- [ ] Open a handover and confirm the page works.

Expected result:

- sales user can use the shared customer/service screens allowed by the API

### Document Quality Checks

- [ ] On one draft sales document, test line `Add`, `Edit`, `Save`, `Cancel`, and `Delete`.
- [ ] Download one sales PDF.
- [ ] Add one comment or attachment on a supported sales detail page.

Expected result:

- draft editing, PDF output, and collaboration work correctly

### Negative Access Checks

- [ ] Attempt to open `Admin -> Users`.
- [ ] Attempt to open `Procurement -> Purchase Orders`.
- [ ] Attempt to open `Finance -> Payments`.
- [ ] Attempt to open `Master Data -> Currencies`.
- [ ] Attempt to open `Service -> Work Orders`.
- [ ] Attempt to open `Service -> Quality Checks`.
- [ ] Attempt to open `Service -> Expense Claims`.

Expected result:

- sales user is blocked from those restricted areas

## Finance Checklist

Use a user with only the `Finance` role.

### Allowed Access Smoke

- [ ] Sign in successfully.
- [ ] Open `Master Data -> Customers`.
- [ ] Open `Master Data -> Suppliers`.
- [ ] Open `Master Data -> Currencies`.
- [ ] Open `Master Data -> Currency Rates`.
- [ ] Open `Master Data -> Payment Types`.
- [ ] Open `Master Data -> Tax Codes`.
- [ ] Open `Master Data -> Tax Conversions`.
- [ ] Open `Master Data -> Reference Forms`.
- [ ] Open `Finance -> Accounts Receivable`.
- [ ] Open `Finance -> Accounts Payable`.
- [ ] Open `Finance -> Payments`.
- [ ] Open `Finance -> Petty Cash`.
- [ ] Open `Finance -> Credit Notes`.
- [ ] Open `Finance -> Debit Notes`.
- [ ] Open `Procurement -> Direct Purchases`.
- [ ] Open `Procurement -> Supplier Invoices`.
- [ ] Open `Sales -> Invoices`.
- [ ] Open `Service -> Expense Claims`.

Expected result:

- all listed pages load and show usable data

### AR And AP Checks

- [ ] Open `Finance -> AR` and confirm outstanding invoice balances are visible.
- [ ] Open `Finance -> AP` and confirm outstanding supplier balances are visible.
- [ ] Toggle any available outstanding/all filters.
- [ ] Open at least one linked source document from AR or AP.

Expected result:

- balances and links load correctly

### Payments And Allocations

- [ ] Create an incoming payment for a customer.
- [ ] Allocate it to an AR entry.
- [ ] Confirm the remaining balance decreases correctly.
- [ ] Create an outgoing payment for a supplier if AP test data exists.
- [ ] Allocate it to an AP entry.

Expected result:

- payment totals, allocated amounts, and remaining amounts are accurate

### Credit And Debit Notes

- [ ] Create a credit note.
- [ ] Allocate it where the business flow supports allocation.
- [ ] Create a debit note.
- [ ] Confirm the affected AR or AP values update correctly.

Expected result:

- finance adjustment documents save and affect balances correctly

### Petty Cash And Service Expense Claims

- [ ] Create a petty cash fund or open an existing one.
- [ ] Post one top-up or adjustment entry.
- [ ] Open one submitted service expense claim.
- [ ] Approve or reject the claim.
- [ ] If approved, settle it with payment details and a petty cash fund when the funding source is `Petty Cash`.

Expected result:

- petty cash transactions update the fund balance correctly
- finance users can complete expense-claim approval and settlement without authorization errors

### Shared Finance-Document Checks

- [ ] Open one supplier invoice.
- [ ] Open one sales invoice.
- [ ] Download one finance-related PDF.
- [ ] Verify document numbers and totals match the screen.

Expected result:

- shared document review works from the finance role

### Negative Access Checks

- [ ] Attempt to open `Admin -> Users`.
- [ ] Attempt to open `Admin -> Notifications`.
- [ ] Attempt to open `Audit Logs`.
- [ ] Attempt to open `Reporting -> Costing`.
- [ ] Attempt to open `Procurement -> Purchase Orders`.
- [ ] Attempt to open `Procurement -> Goods Receipts`.
- [ ] Attempt to open `Service -> Jobs`.
- [ ] Attempt to open `Service -> Work Orders`.

Expected result:

- finance user is blocked from those restricted areas

## Service Checklist

Use a user with only the `Service` role.

### Allowed Access Smoke

- [ ] Sign in successfully.
- [ ] Open `Master Data -> Customers`.
- [ ] Open `Master Data -> Tax Codes`.
- [ ] Open `Master Data -> Reference Forms`.
- [ ] Open `Service -> Equipment Units`.
- [ ] Open `Service -> Jobs`.
- [ ] Open `Service -> Expense Claims`.
- [ ] Open `Service -> Work Orders`.
- [ ] Open `Service -> Estimates`.
- [ ] Open `Service -> Material Reqs`.
- [ ] Open `Service -> Quality Checks`.
- [ ] Open `Service -> Handovers`.
- [ ] Open `Sales -> Direct Dispatches`.

Expected result:

- all listed pages load and are usable

### Core Service Flow

- [ ] Create an equipment unit for a customer.
- [ ] Create a service job.
- [ ] Choose `Service` or `Repair` explicitly when creating the job.
- [ ] Update the job through at least one status change.
- [ ] Create a work order.
- [ ] Create a service estimate and add at least one line.
- [ ] Confirm estimate lines can use `Part`, `Labor`, or `Expense` as needed.
- [ ] Send or approve the estimate.
- [ ] Create a service expense claim and submit it.

Expected result:

- equipment, job, estimate, expense-claim, and work-order workflows are usable by the service role

### Parts And Quality Flow

- [ ] Create a material requisition.
- [ ] Add a line and post it.
- [ ] Record a quality check.
- [ ] Create a handover or open an existing handover.
- [ ] If the handover-to-invoice path is used, confirm the conversion action works or is correctly restricted by process.
- [ ] Open one service job detail page and review the costing section.

Expected result:

- service documents move through their intended workflow without authorization failures

### Shared Service-Sales Check

- [ ] Open `Sales -> Direct Dispatches`.
- [ ] Create a direct dispatch if the service team uses it for parts issue.
- [ ] Add a line and post it.

Expected result:

- service role can use the shared direct-dispatch capability

### Document Quality Checks

- [ ] On one draft service document, test line `Add`, `Edit`, `Save`, `Cancel`, and `Delete`.
- [ ] Add one comment or attachment on a supported service detail page.
- [ ] Download one service-related PDF if available on the tested page.

Expected result:

- draft line editing and collaboration features work on service documents

### Negative Access Checks

- [ ] Attempt to open `Admin -> Users`.
- [ ] Attempt to open `Procurement -> Purchase Orders`.
- [ ] Attempt to open `Finance -> Payments`.
- [ ] Attempt to open `Finance -> AR`.
- [ ] Attempt to open `Finance -> Petty Cash`.
- [ ] Attempt to open `Master Data -> Currencies`.
- [ ] Attempt to open `Audit Logs`.
- [ ] Attempt to open `Reporting -> Costing`.

Expected result:

- service user is blocked from those restricted pages

## Role-Test Sign-Off Template

Use one row per role:

| Role | Tester | Date | Allowed flows passed | Negative checks passed | Open defects | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Admin |  |  |  |  |  |  |
| Procurement |  |  |  |  |  |  |
| Sales |  |  |  |  |  |  |
| Finance |  |  |  |  |  |  |
| Service |  |  |  |  |  |  |

## Related Documents

- `docs/iss-tester-trainer-handbook.md`
- `docs/manual-uat-guide.md`
- `docs/user-manual.md`
- `docs/system-technical-maintainer-guide.md`
