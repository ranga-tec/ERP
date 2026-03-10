# Manual UAT Guide

This guide is for a fresh end-to-end manual test of the ISS ERP system.

It is based on a verified walkthrough completed on March 10, 2026 against a fresh database.

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
7. Add one GRN line:
   - Item: `SKU1`
   - Qty: `10`
   - Unit Cost: `5`
8. Post the GRN.

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
   - Qty Delta: `-1`
   - Unit Cost: `5`
4. Post the adjustment.

Expected:

- Adjustment posts successfully.

5. Go to `Inventory -> On Hand` and query `MAIN` + `SKU1`.

Expected:

- On hand = `5`

6. Go to `Reporting -> Costing` for `MAIN` + `SKU1`.

Expected:

- On Hand = `5`
- Weighted Avg Cost = `5`
- Inventory Value = `25`

This confirms the stock-take correction is reflected in both inventory balance and valuation.

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
- `Inventory -> On Hand`
- `Reporting -> Costing`
