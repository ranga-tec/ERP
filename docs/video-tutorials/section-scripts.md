# ISS Section Scripts

Use these scripts as the base recording plan for both marketing and training videos.

## 0. Login + App Orientation

### Marketing Cut

- Goal: show that ISS is structured, role-based, and easy to navigate.
- Route flow: `/login` -> `/` -> sidebar search -> `/settings`
- Screen flow:
  1. Show login screen.
  2. Sign in.
  3. Land on dashboard.
  4. Use sidebar search once.
  5. Open settings and briefly show Sri Lanka defaults.
- Voiceover beats:
  - `ISS brings operations, inventory, service, finance, and reporting into one structured workspace.`
  - `Users land in a role-based dashboard and move quickly through modules using the searchable sidebar.`

### Guided Tutorial

- Goal: teach a first-time user how the app is laid out.
- Show:
  - login
  - dashboard shell
  - sidebar sections
  - page headers
  - save-and-refresh behavior
  - settings defaults
- Narration points:
  - how to recognize an authenticated page
  - where to find each major function
  - how section pages generally behave
  - where to manage user preferences

## 1. Overview

### Marketing Cut

- Goal: show decision-ready visibility.
- Route flow: `/`
- Screen flow:
  1. Open dashboard.
  2. Pause on KPI cards.
  3. Scroll through key panels.
  4. Open one drill-down from a metric.
- Voiceover beats:
  - `Managers get one place to see stock, payables, receivables, and operational trends.`
  - `The dashboard is not static; it links directly to the working pages behind the numbers.`

### Guided Tutorial

- Goal: teach how to read the home dashboard.
- Show:
  - KPI cards
  - quick insights
  - drill-down links
  - how dashboard numbers connect to reporting and finance pages
- End state:
  - viewer understands the dashboard as a navigation and monitoring surface, not just a summary page

## 2. Master Data

### Marketing Cut

- Goal: show clean setup and maintainable reference data.
- Route flow:
  - `/master-data/warehouses`
  - `/master-data/items`
  - `/master-data/currencies`
  - `/master-data/taxes`
- Screen flow:
  1. Show warehouse list.
  2. Show item list and open one item.
  3. Show currencies including `LKR`.
  4. Show taxes and payment types.
- Voiceover beats:
  - `ISS keeps operational data consistent through controlled master-data management.`
  - `Warehouses, items, customers, suppliers, taxes, currencies, and payment types stay aligned across every module.`

### Guided Tutorial

- Goal: teach the recommended setup order and maintenance actions.
- Recommended route order:
  1. `/master-data/warehouses`
  2. `/master-data/brands`
  3. `/master-data/uoms`
  4. `/master-data/unit-conversions`
  5. `/master-data/currencies`
  6. `/master-data/currency-rates`
  7. `/master-data/taxes`
  8. `/master-data/tax-conversions`
  9. `/master-data/payment-types`
  10. `/master-data/reference-forms`
  11. `/master-data/items`
  12. `/master-data/suppliers`
  13. `/master-data/customers`
  14. `/master-data/reorder-settings`
- Demo actions:
  - create or edit one warehouse
  - show one item
  - show `LKR` in currencies
  - show one tax conversion and one payment type
- Narration points:
  - fresh systems already seed finance reference tables
  - most pages support `Edit`, `Save/Cancel`, and `Delete`
  - in-use records should be made inactive instead of deleted

## 3. Procurement

### Marketing Cut

- Goal: show controlled purchasing and receiving.
- Route flow:
  - `/procurement/purchase-orders`
  - `/procurement/goods-receipts`
  - `/procurement/supplier-invoices`
- Screen flow:
  1. Open a draft or list of purchase orders.
  2. Show GRN `Receive From PO` style workflow.
  3. End on supplier invoice or AP effect.
- Voiceover beats:
  - `ISS connects purchasing, receipt, and supplier liability in one traceable flow.`
  - `Buying activity becomes stock movement and finance visibility without manual reconciliation.`

### Guided Tutorial

- Goal: teach the standard procurement lifecycle.
- Use the demo values:
  - supplier `SUP1`
  - item `SKU1`
  - quantity `10`
  - unit price `5`
- Core routes:
  - `/procurement/purchase-requisitions`
  - `/procurement/rfqs`
  - `/procurement/purchase-orders`
  - `/procurement/goods-receipts`
  - `/procurement/direct-purchases`
  - `/procurement/supplier-invoices`
  - `/procurement/supplier-returns`
- Demo flow:
  1. Create or open a PO.
  2. Add one line.
  3. Approve it.
  4. Create GRN from PO.
  5. Show receipt-plan grid.
  6. Save and post the GRN.
  7. Open supplier invoice or AP result.
- Narration points:
  - draft documents support direct line editing
  - posting GRN increases stock and creates AP
  - direct purchase exists when a PO/GRN chain is not needed

## 4. Sales

### Marketing Cut

- Goal: show quote-to-cash speed.
- Route flow:
  - `/sales/orders`
  - `/sales/direct-dispatches`
  - `/sales/invoices`
- Screen flow:
  1. Show sales order or quote.
  2. Show dispatch.
  3. Show invoice and final amount.
- Voiceover beats:
  - `ISS turns demand into dispatch and billing with visible stock and receivable impact.`
  - `Sales teams and finance stay aligned from shipment to invoice.`

### Guided Tutorial

- Goal: teach the standard sales lifecycle.
- Use the demo values:
  - customer `CUS1`
  - warehouse `MAIN`
  - item `SKU1`
  - quantity `4`
  - unit price `7`
- Core routes:
  - `/sales/quotes`
  - `/sales/orders`
  - `/sales/dispatches`
  - `/sales/direct-dispatches`
  - `/sales/invoices`
  - `/sales/customer-returns`
- Demo flow:
  1. Create direct dispatch.
  2. Add line for `4` units.
  3. Post dispatch.
  4. Create invoice for same customer.
  5. Add line and post.
  6. Show AR result.
- Narration points:
  - posting dispatch reduces stock
  - posting invoice creates AR
  - return workflows reverse the operational and financial effect

## 5. Service

### Marketing Cut

- Goal: show that ISS is not only inventory and finance; it supports after-sales service execution.
- Route flow:
  - `/service/equipment-units`
  - `/service/contracts`
  - `/service/jobs`
  - `/service/estimates`
  - `/service/handovers`
- Screen flow:
  1. Show one equipment unit.
  2. Show service contract.
  3. Open a service job.
  4. Show estimate and handover.
- Voiceover beats:
  - `ISS supports workshop and field-service operations from installed equipment through billing handover.`
  - `Warranty, contract, labor, expense, and material flows stay connected to the same service record.`

### Guided Tutorial

- Goal: teach the major service entities and how they relate.
- Core routes:
  - `/service/equipment-units`
  - `/service/contracts`
  - `/service/jobs`
  - `/service/estimates`
  - `/service/expense-claims`
  - `/service/work-orders`
  - `/service/material-requisitions`
  - `/service/quality-checks`
  - `/service/handovers`
- Demo flow:
  1. Show equipment registration.
  2. Show service contract.
  3. Create or open service job.
  4. Show estimate and approval flow.
  5. Show expense claim and work order context.
  6. End on handover.
- Narration points:
  - jobs capture operational ownership
  - contracts and warranty influence entitlement
  - estimates, labor, expenses, and material usage all connect back to the job

## 6. Inventory

### Marketing Cut

- Goal: show real-time control of stock.
- Route flow:
  - `/inventory/onhand`
  - `/inventory/reorder-alerts`
  - `/inventory/stock-adjustments`
  - `/inventory/stock-transfers`
- Screen flow:
  1. Show on-hand view.
  2. Filter by warehouse/item.
  3. Show reorder alerts.
  4. Show adjustment or transfer page.
- Voiceover beats:
  - `Inventory visibility is available by warehouse, batch, and movement effect.`
  - `Users can respond to shortages and count variances without leaving the ERP.`

### Guided Tutorial

- Goal: teach how inventory results are reviewed and corrected.
- Demo flow:
  1. Open on-hand after the GRN and sales demo.
  2. Show stock balance moving from `10` to `6`.
  3. Open reorder alerts.
  4. Show stock adjustment.
  5. Show stock transfer.
- Narration points:
  - on-hand is a verification surface
  - reorder alerts depend on master-data settings
  - adjustments post the signed variance
  - transfers move stock between warehouses without losing traceability

## 7. Finance

### Marketing Cut

- Goal: show operational accounting in context.
- Route flow:
  - `/finance/ar`
  - `/finance/ap`
  - `/finance/payments`
  - `/finance/petty-cash`
- Screen flow:
  1. Show AP after GRN.
  2. Show AR after invoice.
  3. Show payments allocation.
  4. Show petty cash or notes.
- Voiceover beats:
  - `ISS connects real operations to receivables, payables, payments, and cash controls.`
  - `The finance team sees the consequence of each transaction, not just a disconnected ledger line.`

### Guided Tutorial

- Goal: teach the finance pages users will verify after transactions.
- Core routes:
  - `/finance/accounts`
  - `/finance/ar`
  - `/finance/ap`
  - `/finance/payments`
  - `/finance/petty-cash`
  - `/finance/credit-notes`
  - `/finance/debit-notes`
- Demo flow:
  1. Show chart of accounts.
  2. Open AP and confirm `50`.
  3. Open AR and confirm `28`.
  4. Create incoming payment for `28`.
  5. Allocate it to AR.
  6. Show AR falling to zero.
- Narration points:
  - AP appears from receipt/purchasing flow
  - AR appears from invoice posting
  - payments can be allocated directly to open entries
  - petty cash and notes cover workshop and adjustment cases

## 8. Audit

### Marketing Cut

- Goal: show accountability.
- Route flow: `/audit-logs`
- Screen flow:
  1. Open audit logs.
  2. Filter or scroll through recent changes.
  3. Pause on one clear before/after change.
- Voiceover beats:
  - `Every important change can be traced.`
  - `ISS helps teams answer who changed what and when.`

### Guided Tutorial

- Goal: teach support or admin users how to verify a change trail.
- Demo flow:
  1. Open audit logs after editing a master record.
  2. Find the changed row.
  3. Show table name, action, key, and change payload.
- Narration points:
  - audit is useful for support, validation, and accountability
  - tie the audit event back to a visible change made in another module

## 9. Reporting

### Marketing Cut

- Goal: show that ISS turns transactions into insight.
- Route flow:
  - `/reporting`
  - `/reporting/costing`
  - `/reporting/aging`
  - `/reporting/stock-ledger`
- Screen flow:
  1. Open reporting overview.
  2. Jump to costing.
  3. Jump to aging.
  4. End on stock ledger or tax summary.
- Voiceover beats:
  - `ISS converts transaction history into actionable operational and financial reporting.`
  - `Teams can verify stock, cash exposure, service performance, and trend data from the same platform.`

### Guided Tutorial

- Goal: teach how to read the main reports after the demo scenario.
- Core routes:
  - `/reporting`
  - `/reporting/stock-ledger`
  - `/reporting/aging`
  - `/reporting/tax-summary`
  - `/reporting/service-kpis`
  - `/reporting/sales-analysis`
  - `/reporting/purchase-analysis`
  - `/reporting/supplier-performance`
  - `/reporting/costing`
- Demo flow:
  1. Open costing and confirm:
     - on hand `6`
     - weighted average cost `5`
     - inventory value `30`
  2. Open aging and show AP `50`, AR `28` or `0` after allocation.
  3. Open stock ledger to tie the movement trail together.
- Narration points:
  - show how reports validate operational reality
  - explain that reports are strongest after a clean end-to-end transaction demo

## 10. Admin

### Marketing Cut

- Goal: show control and maintainability.
- Route flow:
  - `/admin/import`
  - `/admin/users`
  - `/admin/notifications`
  - `/settings`
- Screen flow:
  1. Show Excel import.
  2. Show user management.
  3. Show notifications.
  4. End on settings.
- Voiceover beats:
  - `ISS includes the administration tools needed for rollout, user management, and operational support.`
  - `Teams can onboard data, manage users, and maintain system behavior without leaving the platform.`

### Guided Tutorial

- Goal: teach the support/admin control plane.
- Demo flow:
  1. Open import and show template-driven bulk upload.
  2. Open users and show role management.
  3. Open notifications.
  4. Open settings and show default transaction references with `LKR`.
- Narration points:
  - import is useful for go-live and controlled bulk setup
  - users and roles define access
  - settings help standardize the local operating context

## Closing Guidance

When recording the full library:

- do one clean pass for each section using the guided tutorial script
- cut marketing clips from the same raw footage
- keep the raw transaction order consistent so the data state stays believable
- end every section on a visible business result, not on a half-filled form
