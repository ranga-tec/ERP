# Manual UAT Guide

This guide is for a fresh end-to-end manual test of the ISS ERP system.

It is based on a verified walkthrough completed on March 30, 2026 against a fresh database.

## Pre-checks

- Start the backend and frontend.
- Sign in as an `Admin` user.
- Use a fresh database when possible so the baseline is predictable.

Fresh systems now auto-seed the minimum reference data required for core finance/reporting flows:

- currencies: `USD` (base), `EUR`, `GBP`
- payment types: `BANK_TRANSFER`, `CARD`, `CASH`, `CHEQUE`
- tax codes: `VAT0`, `VAT5`, `VAT15`
- reference forms used by document routing

## Smoke Check

Run these first:

1. Open `Master Data -> Currencies`.
Expected:
`USD` exists and is the active base currency.

2. Open `Finance -> Payments`.
Expected:
The payment form has active currency options and does not show the "No active currencies" warning.

3. Open `Reporting -> Costing`.
Expected:
The page loads successfully even on a fresh system.

4. Open `Finance -> Petty Cash`.
Expected:
The petty cash fund list loads successfully.

5. Open `Service -> Jobs`.
Expected:
The job list and create form load without service API errors.

6. Open `Service -> Service Contracts`.
Expected:
The contract list and create form load without service API errors.

## End-to-End Scenario

Use these sample values.

### 1. Create core master data

Create:

- Warehouse
  - Code: `MAIN`
- Supplier
  - Code: `SUP1`
- Customer
  - Code: `CUS1`
- Item
  - SKU: `SKU1`
  - Name: `HydraulicFilter`
  - Type: `Spare Part`
  - UoM: `PCS`
  - Default Unit Cost: `5`

Expected:

- All records save without error.
- The item appears in the item list with default cost `5`.

### 2. Procurement receipt and AP

1. Go to `Procurement -> Purchase Orders`.
2. Create a PO for supplier `SUP1`.
3. Add one line:
   - Item: `SKU1`
   - Qty: `10`
   - Unit Price: `5`
4. Approve the PO.
5. Go to `Procurement -> Goods Receipts`.
6. Create a GRN for:
   - PO: the PO you just approved
   - Warehouse: `MAIN`
7. Confirm the `Receive From PO` table loads the open PO line automatically.
8. Enter received quantity `10` for the PO line and save the receipt plan.
9. Confirm the `Current Draft Lines` table shows the GRN line created from the PO receipt plan.
10. Post the GRN.

Expected:

- PO total = `50`
- GRN posts successfully
- `Finance -> AP` shows one outstanding GRN entry for supplier `SUP1`
- AP Amount = `50`
- AP Outstanding = `50`

### 3. Stock after receipt

1. Go to `Inventory -> On Hand`.
2. Query:
   - Warehouse: `MAIN`
   - Item: `SKU1`

Expected:

- On hand = `10`

3. Go to `Reporting -> Costing`.
4. Filter:
   - Warehouse: `MAIN`
   - Item: `SKU1`

Expected:

- On Hand = `10`
- Default Cost = `5`
- Weighted Avg Cost = `5`
- Last Receipt Cost = `5`
- Inventory Value = `50`

### 4. Sales issue and AR

1. Go to `Sales -> Direct Dispatches`.
2. Create a direct dispatch:
   - Customer: `CUS1`
   - Warehouse: `MAIN`
3. Add one line:
   - Item: `SKU1`
   - Qty: `4`
4. Post the direct dispatch.

Expected:

- Dispatch posts successfully.

5. Go back to `Inventory -> On Hand` and query `MAIN` + `SKU1`.

Expected:

- On hand = `6`

6. Go to `Sales -> Invoices`.
7. Create an invoice for customer `CUS1`.
8. Add one line:
   - Item: `SKU1`
   - Qty: `4`
   - Unit Price: `7`
   - Discount %: `0`
   - Tax %: `0`
9. Post the invoice.

Expected:

- Invoice total = `28`
- `Finance -> AR` shows one outstanding invoice entry for `CUS1`
- AR Amount = `28`
- AR Outstanding = `28`

### 5. Payment and allocation

1. Go to `Finance -> Payments`.
2. Create a payment:
   - Direction: `Incoming`
   - Counterparty Type: `Customer`
   - Counterparty: `CUS1`
   - Currency: `USD`
   - Amount: `28`
3. Open the new payment detail page.
4. Allocate the payment to the outstanding AR invoice for `CUS1`.

Expected:

- Payment detail shows:
  - Amount = `28`
  - Allocated = `28`
  - Remaining = `0`
- The allocation row points to the invoice entry.
- `Finance -> AR` with `Outstanding only` shows no rows.
- The invoice detail page shows `Status: Paid`.

### 6. Stock take / month-end adjustment

1. Go to `Inventory -> Stock Adjustments`.
2. Create an adjustment for warehouse `MAIN`.
3. Add one line:
   - Item: `SKU1`
   - Counted Qty: `5`
   - Unit Cost: `5`
4. Post the adjustment.

Expected:

- Adjustment posts successfully.
- Stock ledger shows a signed adjustment movement of `-1`.

5. Go to `Inventory -> On Hand` and query `MAIN` + `SKU1`.

Expected:

- On hand = `5`

6. Go to `Reporting -> Costing` for `MAIN` + `SKU1`.

Expected:

- On Hand = `5`
- Weighted Avg Cost = `5`
- Inventory Value = `25`

This confirms the stock-take correction is reflected in both inventory balance and valuation.

### 7. Optional Service / Repair Flow

Use this focused scenario when validating the current workshop workflow.

Suggested extra setup:

- one petty cash fund:
  - Code: `WORKSHOP`
  - Name: `Workshop Petty Cash`
  - Currency: `USD`
  - Opening Balance: `100`

Steps:

1. Go to `Service -> Equipment Units` and register one serialized unit for customer `CUS1`.
   - enter a future `Warranty until` date
   - set `Warranty coverage` to `Labor and Parts`
2. Confirm the equipment-unit list shows explicit `View` / `Edit` actions and open the unit from one of them.
3. Go to `Service -> Jobs` and create a job:
   - Kind: `Repair`
   - Problem Description: `Unit does not power on`
4. Confirm the job list shows explicit `View` / `Edit` actions and open the job detail page from the list.
5. While the job is still open, edit the job header once and save it.
6. Confirm the job detail page shows warranty-based entitlement and billing treatment.
7. Go to `Service -> Service Contracts` and create a contract for the same unit:
   - Type: `AMC`
   - Coverage: `Parts Only`
   - Start Date: yesterday
   - End Date: 60 days from now
8. Confirm the contract list shows explicit `View` / `Edit` actions and open the contract detail page from the list.
9. Return to the job and click `Refresh Entitlement`.
10. Confirm the job now shows contract-based entitlement instead of warranty.
11. Start the job.
12. Go to `Service -> Work Orders` and create a work order for the job.
13. Add one billable labor entry on the work order, then submit and approve it.
14. Confirm the approved billable amount reflects coverage-adjusted billing when entitlement covers labor.
15. Go to `Service -> Estimates` and create an estimate for the job.
16. Confirm the estimate list shows explicit `View` / `Edit` actions and open the estimate detail page from the list.
17. While the estimate is still draft, edit `Valid until` or `Terms`, save, and then add at least:
   - one `Part` line using `SKU1`
18. Send the estimate to the customer and confirm `Customer Approval` becomes `Pending`.
19. Edit the sent draft estimate again and confirm the pending approval resets to `Not Sent`.
20. Resend the estimate and then mark it customer approved.
21. Use `Create Change Order` from the approved estimate and confirm a new draft revision opens.
22. Create a service expense claim for the same job:
   - Funding Source: `Petty Cash`
   - one billable line for an emergency outside expense
23. Submit the claim.
24. Approve and settle the claim against petty cash fund `WORKSHOP`.
25. Convert the billable claim line into the working estimate.
26. If the claim line used a spare-part item, confirm the new estimate line is classified as `Part`.
27. Create and complete a service handover.
28. Convert the handover to sales invoice using the labor source that bills approved timesheets.
29. Open the service job detail page and review the costing section.

Expected:

- equipment units accept warranty coverage and the unit detail page allows updates
- the contract can be linked to the same unit and appears on both the contract list and equipment-unit detail page
- service lists now expose explicit `View` / `Edit` entry points instead of relying only on clickable document numbers
- open jobs can be edited, but that header locks after the job is started
- the job first shows warranty entitlement, then changes to contract entitlement after refresh
- the job can be opened as `Repair` and moved to `In Progress`
- draft estimate headers and lines are editable until customer approval or rejection
- sending a draft estimate sets customer approval to `Pending`, editing that pending draft resets it, and resending/restating approval works
- approved estimates stay preserved and `Create Change Order` opens a new draft revision for the changed scope
- the work order accepts labor entries and the approved labor becomes visible on the work order and job costing views
- covered labor or part lines bill at zero where entitlement applies
- the approved/settled billable claim can be converted into an estimate or change-order draft revision
- petty cash balance is reduced by the settled claim amount
- the handover invoice includes both estimate parts and approved actual labor
- job costing reflects labor, material, expense-claim, invoice, and quoted value in one view

## Verified Calculation Trail

This is the tested numeric chain:

- GRN receipt: `10 x 5 = 50`
- After direct dispatch of `4`: on hand `10 - 4 = 6`
- Sales invoice: `4 x 7 = 28`
- Customer payment allocation: `28 - 28 = 0 outstanding`
- Stock take adjustment: `6 - 1 = 5`
- Final valuation: `5 x 5 = 25`

## PDF / Document Check

During UAT, also open at least one PDF from each area you touch:

- PO PDF
- GRN PDF
- Invoice PDF
- Payment PDF
- Stock Adjustment PDF
- one service PDF if the optional service flow is tested

Expected:

- Browser download/open works.
- Document number matches the screen.
- Core totals match the transaction.

## If Something Does Not Match

Check these first:

1. `Master Data -> Currencies`
Expected:
One active base currency exists.

2. `Finance -> Payments`
Expected:
Payment form shows active currencies.

3. `Inventory -> On Hand`
Expected:
Warehouse and item selections match the documents you posted.

4. `Audit Logs`
Expected:
Posted transactions appear in the audit trail.

## Minimum Regression Set

If time is limited, always re-test these pages after changes:

- `Master Data -> Currencies`
- `Procurement -> Goods Receipts`
- `Sales -> Invoices`
- `Finance -> Payments`
- `Finance -> Petty Cash`
- `Service -> Service Contracts`
- `Service -> Jobs`
- `Service -> Expense Claims`
- `Inventory -> On Hand`
- `Reporting -> Costing`
