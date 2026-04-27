# ISS Voiceover Scripts

Use these scripts as spoken narration over clean screen recordings. Record the screen actions first, then record the voiceover against the final edit.

## Delivery Rules

- Speak clearly and slightly slower than normal conversation.
- Pause after any save, post, approve, allocate, or report result.
- If a step is obvious on screen, do not over-explain it.
- Keep product naming consistent:
  - `ISS`
  - `Master Data`
  - `Procurement`
  - `Sales`
  - `Service`
  - `Inventory`
  - `Finance`
  - `Reporting`
  - `Admin`

## 0. Login and App Orientation

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Login screen | `ISS brings operations, stock, service, finance, and reporting into one structured ERP workspace.` |
| 2 | Sign in and land on dashboard | `Users sign in once and land in a role-based workspace designed for day-to-day execution.` |
| 3 | Open sidebar and use search | `The searchable sidebar makes it easy to move quickly between modules without losing context.` |
| 4 | Open settings | `Local operating preferences such as Sri Lanka time zone, locale, and LKR-focused defaults can be standardized for each user.` |

### Guided Tutorial Script

`In this tutorial, we will look at how ISS is laid out and how a new user should move through the system.`

`Start from the login page and sign in with your assigned account. After login, you land in the authenticated shell, which includes the top header, the main content area, and the sidebar navigation.`

`The sidebar is organized by business function. Use it to move between Master Data, Procurement, Sales, Service, Inventory, Finance, Reporting, Audit, and Admin pages. If you are not sure where something is, use the sidebar search to filter the menu.`

`Most ISS pages follow a similar pattern. The page title appears at the top, actions are near the create or filter area, and saved results are shown in a list or detail panel below. When you save or post something, pause and confirm the visible result before moving on.`

`Open Settings to review user preferences. In this environment, the preferred setup is English Sri Lanka, Asia Colombo, and LKR-oriented defaults.`

`Once you understand this layout, the rest of the application becomes much easier to learn because the same operating pattern repeats across modules.`

## 1. Overview

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Dashboard hero metrics | `ISS starts with a dashboard built for operational visibility.` |
| 2 | Scroll KPI panels | `Teams can see stock, receivables, payables, and performance indicators in one place.` |
| 3 | Drill into one metric | `The dashboard is actionable. Users can move from summary metrics directly into the pages behind the numbers.` |

### Guided Tutorial Script

`This tutorial explains how to read the ISS dashboard and use it as a starting point for operational review.`

`Begin on the Overview page. The hero metrics summarize the most important current values, such as outstanding receivables, payables, and inventory-related indicators.`

`As you move down the page, look for trend panels, aging summaries, and business shortcuts. These blocks are not just visual summaries. They help you decide where to go next.`

`When a number looks unusual, drill into the linked working page or reporting page. The dashboard should be treated as a control surface, not just a static homepage.`

`For a manager, this page answers one core question quickly: what needs attention right now, and which module should I open next.`

## 2. Master Data

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Warehouses or Items list | `Strong ERP execution starts with reliable master data.` |
| 2 | Open items and currencies | `ISS keeps warehouses, items, customers, suppliers, currencies, taxes, and payment references consistent across every module.` |
| 3 | Show LKR in currencies and finance references | `Finance-ready defaults, including LKR and seeded reference tables, help new environments become usable quickly.` |

### Guided Tutorial Script

`This tutorial covers the ISS Master Data section and the recommended setup order for a new environment.`

`Start with warehouses, then optional brands, then unit-of-measure records and unit conversions. After that, review currencies and currency rates, taxes and tax conversions, payment types, and reference forms. These finance and reference tables should already contain starter values in a fresh environment, including LKR.`

`Next, maintain items, suppliers, customers, and reorder settings. These records drive the rest of the ERP. If the master data is weak, every downstream document becomes harder to manage.`

`On most master-data pages, you can create a record, edit it inline, save changes, or delete the row if it is not in use. If a record is already referenced by transactions, deletion may be blocked, and the correct action is to mark it inactive instead.`

`For a clean demo or rollout, create one warehouse, one supplier, one customer, and one stock item first. That gives you enough data to demonstrate procurement, inventory, sales, finance, and reporting without switching stories midway through the recording.`

## 3. Procurement

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Purchase order list or detail | `ISS gives procurement teams a controlled purchase-to-receipt flow.` |
| 2 | GRN receipt-plan screen | `Purchase orders feed directly into goods receipt, where open PO lines are loaded into a structured receipt grid.` |
| 3 | Supplier invoice or AP result | `The result is immediate stock visibility and a finance trail that follows the transaction.` |

### Guided Tutorial Script

`This tutorial walks through the main procurement flow in ISS.`

`Start from Purchase Orders. Create a purchase order for supplier SUP1, add the stock item SKU1, enter quantity ten and unit price five, then approve the document.`

`Next, create a goods receipt from that purchase order. ISS loads the open purchase order lines into the receipt-plan grid. Enter the received quantity and confirm the cost details. Save the receipt plan, then post the goods receipt.`

`Once the GRN is posted, stock increases and the procurement event becomes visible to finance as an accounts payable effect.`

`If needed, continue to supplier invoice or supplier return flows. The important concept is that ISS keeps the purchase commitment, physical receipt, and liability chain connected instead of treating them as isolated records.`

`For procurement operators, always end by confirming the posted status and then reviewing the downstream result, either in stock or AP.`

## 4. Sales

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Sales order or direct dispatch page | `ISS supports a clear quote-to-cash path for sales operations.` |
| 2 | Dispatch posting | `Once goods are dispatched, stock movement is reflected immediately.` |
| 3 | Invoice and AR page | `Billing then creates a visible receivable, keeping operations and finance aligned.` |

### Guided Tutorial Script

`This tutorial shows the standard sales flow in ISS.`

`For the demo, use customer CUS1, warehouse MAIN, item SKU1, quantity four, and unit price seven.`

`If you want the fastest path, create a direct dispatch. Add the item line, confirm the quantity, and post the dispatch. This reduces available stock.`

`Next, create a sales invoice for the same customer. Add the same quantity and unit price, then post the invoice. At this point, ISS creates the receivable effect for finance.`

`If your process requires a fuller path, you can also demonstrate quotes, sales orders, and standard dispatches before the invoice.`

`The key operating idea is simple. Dispatch handles the physical stock-out. Invoice handles the financial charge. ISS keeps both effects visible and traceable.`

## 5. Service

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Equipment Units | `ISS also supports service and after-sales execution.` |
| 2 | Service contract and job | `Equipment, warranty or contract coverage, and service jobs stay connected in one operational flow.` |
| 3 | Estimate or handover | `That makes it easier to control labor, parts, customer approval, and final billing handover.` |

### Guided Tutorial Script

`This tutorial introduces the ISS Service section and how the core records fit together.`

`Start with Equipment Units. These represent the installed customer equipment base, including serial identity and warranty-related information.`

`Then move to Service Contracts. Contracts extend or define support coverage for customer-owned equipment.`

`Service Jobs are the operational heart of the module. Open a job when equipment is received for service or repair. From there, estimates, work orders, expense claims, material requisitions, quality checks, and handovers all connect back to the same job context.`

`Use estimates for customer-facing commercial approval. Use work orders and material requisitions for execution. Use expense claims for out-of-pocket or petty-cash-linked service costs. End on the handover when work is complete and billing is ready.`

`The strength of this module is that operational service work, entitlement, and billing preparation are kept in one chain instead of being spread across disconnected tools.`

## 6. Inventory

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | On Hand page | `Inventory visibility in ISS is immediate and operationally useful.` |
| 2 | Filter by item or warehouse | `Users can review stock by warehouse, item, and movement effect.` |
| 3 | Reorder alerts or adjustment | `When shortages or count variances appear, ISS supports direct corrective action.` |

### Guided Tutorial Script

`This tutorial explains the main inventory control pages in ISS.`

`Start with On Hand. After the procurement and sales demo, confirm that stock increased to ten after receipt and then dropped to six after dispatch or sale.`

`Open Reorder Alerts to review items that fall below configured thresholds. These alerts depend on reorder settings from the Master Data section.`

`Use Stock Adjustments when a physical count does not match the system quantity. ISS records the signed variance rather than hiding the correction.`

`Use Stock Transfers when stock needs to move between warehouses while keeping a clear trace of source and destination.`

`Inventory should always be recorded as a result of real activity, and these pages are where users verify, correct, and manage that reality.`

## 7. Finance

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | AP page | `ISS turns operations into visible financial obligations.` |
| 2 | AR page | `Payables and receivables are created from the actual business events behind them.` |
| 3 | Payment allocation | `That means finance teams can review, settle, and trace balances without losing operational context.` |

### Guided Tutorial Script

`This tutorial covers the main finance pages in ISS.`

`Open Accounts Payable after the procurement flow and confirm the supplier balance created by the goods receipt or invoice chain. In the standard demo, the payable value should be fifty.`

`Open Accounts Receivable after the sales invoice and confirm the customer balance. In the standard demo, the receivable value should be twenty-eight before payment allocation.`

`Next, create an incoming payment for that customer and allocate it to the open receivable entry. After allocation, the outstanding amount should fall to zero and the invoice should reflect the settlement result.`

`You can also demonstrate petty cash, credit notes, debit notes, and chart-of-accounts maintenance depending on the audience.`

`The main lesson is that ISS finance pages are driven by live operational activity. Users should not just create balances manually. They should verify the financial effect produced by the underlying business transaction.`

## 8. Audit

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Audit log list | `ISS includes change traceability for operational accountability.` |
| 2 | Open one meaningful change | `Teams can review who changed what, when it changed, and how the values were updated.` |

### Guided Tutorial Script

`This tutorial shows how to use the audit log in ISS.`

`Open the Audit Logs page after making a visible change in another module, such as editing a master-data record or posting a document.`

`Find the relevant row and review the table name, action type, key, and stored change information.`

`This page is especially useful for support teams, testers, and administrators who need to confirm that the system captured the correct operational history.`

`In practice, the audit log is best used as a verification tool. Make a change, then come here to confirm the system recorded it clearly.`

## 9. Reporting

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Reporting overview | `ISS converts transactions into operational and financial insight.` |
| 2 | Costing and aging | `Managers can validate stock value, cash exposure, and performance using live report data.` |
| 3 | Stock ledger or tax summary | `This helps teams move from raw transactions to confident decision-making.` |

### Guided Tutorial Script

`This tutorial explains the main reporting pages in ISS using the same end-to-end demo scenario.`

`Start with the Costing report. After receiving ten units at cost five and selling four units, confirm that on hand is six, weighted average cost is five, and inventory value is thirty.`

`Then open the Aging report to review receivables and payables. In the base scenario, AP should show fifty and AR should show twenty-eight before allocation or zero after settlement.`

`Use Stock Ledger to trace the movement history behind those balances. Depending on the audience, continue to tax summary, service KPIs, sales analysis, purchase analysis, supplier performance, or the reporting overview.`

`The value of reporting in ISS is that it is grounded in recorded business activity. These pages are where users validate that operations, inventory, and finance are all telling the same story.`

## 10. Admin

### Marketing Script

| Scene | Visual | Narration |
| --- | --- | --- |
| 1 | Import page | `ISS includes the admin tooling needed to support rollout and controlled operations.` |
| 2 | Users page | `Teams can manage user access, roles, and system maintenance from inside the product.` |
| 3 | Settings or notifications | `That makes onboarding, governance, and operational support easier to manage.` |

### Guided Tutorial Script

`This tutorial covers the main admin and support functions in ISS.`

`Start with Import. This is where teams can use Excel-driven setup and controlled bulk onboarding.`

`Open Users to review role assignment and user maintenance. This page controls who can access each area of the ERP.`

`Then review Notifications and Settings. Notifications help track support and system communication behavior, while Settings help standardize local operating defaults such as language, time zone, and transaction references.`

`For administrators, this section is the control plane that supports rollout, governance, and day-to-day support around the rest of the application.`

## Standard Outro Lines

Use one of these endings depending on the clip type.

### Marketing Outro Options

- `This is how ISS helps teams move faster with better control.`
- `ISS connects operations, stock, service, finance, and reporting in one working system.`
- `To see the full workflow, continue with the next ISS tutorial in this series.`

### Guided Tutorial Outro Options

- `That completes this section. In the next tutorial, we will continue the flow in the next ISS module.`
- `Before moving on, confirm the saved or posted result on screen so the transaction story stays intact.`
- `Once this step is complete, you can continue with the next module using the same demo data.`
