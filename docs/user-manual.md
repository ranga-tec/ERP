# User Manual (Quick)

## Sign in and roles

- Use `Login` or `Register` in the web app.
- The first registered user becomes `Admin`.
- Roles used in the system:
  - `Admin`, `Procurement`, `Inventory`, `Sales`, `Service`, `Finance`, `Reporting`
- Admin user and role management is at `Admin -> Users`.

## Master data setup order

Recommended setup sequence:

Note:
- fresh databases auto-seed default currencies, payment types, tax codes, and reference forms so finance/reporting screens can work immediately

1. Warehouses
2. Brands (optional)
3. UoMs
4. UoM conversions
5. Currencies and currency rates
6. Taxes and tax conversions
7. Payment types
8. Reference forms
9. Items
10. Suppliers and customers
11. Reorder settings

## Bulk import (Excel)

- `Admin -> Import`
  - Download template
  - Fill sheets
  - Upload `.xlsx`
- Import runs as one transaction (all-or-nothing).

## Master data maintenance actions

- Master-data list pages support row actions:
  - `Edit` -> `Save/Cancel`
  - `Delete` (with confirmation)
- If a record is referenced by transactions, delete may be blocked and you should set it inactive instead.
- Items are maintained in `Master Data -> Items -> Edit Item` where you can update and delete the selected item.

## Procurement workflows

- procurement list pages show explicit `View` and `Edit` actions so you can open a document from the actions column instead of only clicking the document number
- `Edit` stays available while the procurement document is still `Draft`
- RFQ:
  - create -> add lines -> send
  - while the RFQ is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Purchase requisition:
  - create -> add lines -> submit -> approve/reject/cancel
  - approved PR can be converted to PO
  - while the requisition is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Purchase order:
  - create -> add lines -> approve
  - while the PO is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Goods receipt (GRN):
  - create from PO -> add lines -> post
  - posting increases stock and creates AP entry
  - when a GRN is created from a PO, the receipt page loads all open PO lines into one working grid
  - enter received qty, cost, batch, and serial details directly in that grid, then use `Save receipt plan`
- Direct purchase:
  - create -> add lines -> post
  - posting increases stock without PO/GRN chain
- Supplier invoice:
  - create (linked to PO/GRN or direct purchase) -> post
- Supplier return:
  - create -> add lines -> post
  - posting issues stock and creates supplier credit note
  - while the return is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line

## Inventory workflows

- On hand:
  - query stock by warehouse/item/batch
  - review balances as all together, warehouse wise, batch wise, or warehouse + batch
- Reorder alerts:
  - uses reorder settings and current on-hand
  - supports creating PR draft from reorder results
- Stock adjustment:
  - create -> enter counted quantity -> post/void
  - system shows system quantity and posts only the signed variance to stock history
- Stock transfer:
  - create -> enter move quantity -> post/void
  - source warehouse stock remains visible while adding or editing lines

## Sales workflows

- sales list pages show explicit `View` and `Edit` actions so you can open a document from the actions column instead of only clicking the document number
- `Edit` stays available while the sales document is still `Draft` (invoice edit also follows invoice-role permissions)
- Quote:
  - create -> add lines -> send
  - while the quote is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Sales order:
  - create -> add lines -> confirm
  - while the sales order is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Dispatch:
  - create -> add lines -> post
  - while the dispatch is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Direct dispatch:
  - create -> add lines -> post
  - while the direct dispatch is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Invoice:
  - create -> add lines -> post
  - posting creates AR entry
  - while the invoice is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Customer return:
  - create -> add lines -> post
  - posting returns stock and creates customer credit note
  - while the return is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line

## Service workflows

- Equipment units:
  - register serialized customer equipment
  - maintain purchased date, warranty end date, and warranty coverage scope
  - the list page exposes `View` and `Edit` actions explicitly; both open the unit detail page
  - use the equipment-unit detail page to edit ownership or warranty coverage as the installed base changes
- Service contracts:
  - create `AMC`, `SLA`, or `Warranty Extension` documents against a specific customer-owned equipment unit
  - set the contract coverage window and coverage scope (`Inspection`, `Labor`, `Parts`, or `Labor and Parts`)
  - use this when the unit is covered by a service agreement beyond or instead of manufacturer warranty
  - the list page exposes `View` and `Edit` actions explicitly; both open the contract detail page
  - contracts are edited from the contract detail page after creation
- Service jobs:
  - create -> start -> complete -> close
  - choose `Service` or `Repair` when opening the job
  - the list page exposes `View` and `Edit`; `Edit` is available while the job is still `Open`
  - while status is still `Open`, you can edit the job header (unit, customer, type, problem) from the detail page
  - once the job is started, header editing is locked and execution should continue through work orders, estimates, material issues, and handover
  - entitlement is captured automatically when the job is created by checking active service contracts first, then manufacturer warranty on the equipment unit
  - use `Refresh Entitlement` on the job if warranty/contract data is added after the job was already opened
  - recommended operating flow is: receive equipment -> open the job immediately -> diagnose -> draft/send estimate before continuing additional billable work
- Work orders:
  - create and track work records
  - add labor entries/timesheets with technician, date, hours, cost rate, billing rate, and notes
  - labor entries move through `Draft -> Submitted -> Approved/Rejected -> Invoiced`
  - approved labor feeds job costing; approved billable labor can be billed during handover invoice conversion
  - labor coverage from warranty or contract is shown as reduced effective billing where applicable
- Service estimates:
  - list page exposes `View` and `Edit`; `Edit` is available while the estimate is still `Draft`
  - create -> add lines -> send -> mark customer approved/rejected
  - estimates now support `Part`, `Labor`, and billable `Expense` lines
  - while status is `Draft`, the estimate header (`Valid until`, `Terms`) and lines can be edited directly
  - sending a draft estimate marks `Customer Approval = Pending`
  - if a sent draft is edited, the pending approval is cleared and the estimate must be resent so the customer approves the latest scope
  - if extra findings appear after approval or rejection, use `Create Change Order` instead of overwriting the original estimate
  - covered labor or part lines under the job's warranty or service contract are saved and billed at zero automatically
- Service expense claims:
  - the list page exposes `View` so users can open the claim detail page directly
  - create against a service job for `Out of Pocket` or `Petty Cash`
  - add free-text or item-linked lines
  - submit -> finance approve/reject -> settle
  - billable approved/settled claim lines can be pushed into the latest draft estimate or into an automatic change-order draft revision
  - if an expense-claim line references a spare part item, conversion classifies it as a part line so entitlement rules can still cover it
  - use this for technician reimbursement, emergency cash buys, and petty-cash clearing that should not be hidden as stock or AP workarounds
- Material requisitions:
  - create -> add lines -> post
  - use when issuing stocked spare parts from a warehouse to the job
- Quality checks:
  - record pass/fail and notes
- Service handovers:
  - complete handover and optionally convert to invoice
  - invoice conversion can map expense estimate lines by using the line item when present or a selected fallback expense/service item
  - labor billing can come from estimate labor lines or from approved work-order timesheets when actual labor should drive the invoice
  - warranty/contract coverage still applies during invoice conversion, so covered labor or parts can invoice at zero
- Direct purchases linked to service jobs:
  - use when a required part is bought from a supplier and also needs to be received into stock and/or booked to supplier AP
  - the direct purchase can now be linked back to the service job for traceability
  - use direct purchase instead of expense claim when the purchase should enter inventory and follow procurement-style receiving/accounting

## Finance workflows

- Chart of accounts:
  - create and maintain finance account codes from `Finance -> Chart of Accounts`
  - use `Classic` mode for the dedicated create form and standard row editing
  - use `Priority Grid` mode for dense inline create/edit/delete behavior
- Item and item category account mapping:
  - maintain default income / expense accounts on item categories
  - override or confirm income / expense accounts on individual items when needed
  - accounts support `Asset`, `Liability`, `Equity`, `Revenue`, and `Expense`
  - accounts can be grouped under parent accounts and marked as posting or group-only
- AR and AP:
  - view outstanding entries
- Payments:
  - create incoming/outgoing payments
  - allocate to AR/AP entries
- Petty cash:
  - create fund -> optionally seed opening balance
  - post top-ups and manual adjustments
  - settle approved service expense claims against a petty cash fund when workshop cash was used
  - petty-cash-funded service claims require a petty cash fund at settlement; out-of-pocket claims can still be settled with payment type/reference only
- Credit notes:
  - create and allocate to AR/AP
- Debit notes:
  - create additional AR/AP charge entries
- Service expense claims:
  - finance users approve/reject submitted claims and settle approved claims with a payment method/reference and optional petty cash fund linkage
  - petty-cash and out-of-pocket claims are tracked here rather than hidden as ad-hoc stock or payment workarounds

## Reporting

- Dashboard:
  - the home page is your operational dashboard
  - it shows only the queues, alerts, and shortcuts allowed for your role
  - typical sections include open work, draft transactions, overdue finance balances, reorder pressure, and direct links into the next action screens
  - if no live metrics are available yet, the dashboard still shows quick access links so the page is not empty
- Available reports:
  - dashboard, stock ledger, aging, tax summary, service KPIs, costing
- Costing report:
  - compares default and weighted average cost
  - shows last receipt cost/date
  - shows on-hand valuation in base currency
- Service job costing:
  - each service job detail page now shows quoted revenue, posted invoice revenue, actual job cost, approved/pending labor, pending claim cost, uninvoiced billable labor, and unconverted billable expense-claim totals
  - cost sources are broken down by material requisitions, direct purchases, work-order labor entries, and service expense claims
  - entitlement source, coverage scope, billing treatment, and linked service-contract visibility are shown on the job detail page
  - estimate and invoice snapshots are shown together with quoted and posted gross margin visibility

## Line editing in draft documents

For line-based documents, while status is draft:

- you can `Add` line
- you can `Edit` line (then `Save` or `Cancel`)
- you can `Delete` line
- on the newer grid-based screens, `Tab` moves across the editable cells and `Enter` saves the current row
- GRN receipt planning uses one primary receipt grid and a document-level `Save receipt plan` action instead of row-by-row saving

This applies across procurement, sales, inventory, and service document detail pages.

## PDFs and attachments

- Detail pages provide PDF download links for document outputs.
- Many detail pages include collaboration:
  - comments
  - attachments

## Cross-navigation

- Related transaction numbers on detail, list, finance, and reporting pages open the linked source document when clicked.
- Item references on transaction and reporting pages open `Master Data -> Items` and focus the selected item.

## Notifications

- Notification outbox and retries are available at `Admin -> Notifications`.
- Supplier/customer notifications are generated by configured business events.

## Audit logs

- Transaction and entity changes are recorded in audit logs.
- View at `Audit Logs`.
- the audit screen now shows readable field-by-field old/new values instead of only raw JSON
- technical rows such as document-sequence updates are hidden by default and can be shown when needed
- system-managed fields such as `CreatedAt` and `LastModifiedAt` are hidden by default and can also be shown when needed
