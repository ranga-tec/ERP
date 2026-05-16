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

1. Warehouses and bins/racks
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
- for line-grid documents, `Edit` opens the detail page with existing lines already in edit mode; the separate add-line form is hidden in that state so it does not look like a blank saved row
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

- Warehouse bins/racks:
  - maintain under `Master Data -> Warehouses` in the `Bins / Racks` section
  - create bin codes under each warehouse, with optional zone, rack, and shelf values
  - use this structure to represent physical picking/storage locations inside the warehouse
- Inventory availability:
  - go to `Inventory -> Inventory Availability`
  - load all current inventory or filter first by warehouse, bin/rack, item, batch/lot, or serial
  - use the loaded table search to find items, warehouses, bins, racks, batches, or serial numbers
  - rows show item, warehouse, bin/rack, batch/lot, serial, on-hand quantity, unit cost, and inventory value
  - stock that existed before bin tracking, or stock posted without a bin, appears as `Unassigned`
- On hand:
  - query stock by warehouse/item/batch
  - review balances as all together, warehouse wise, batch wise, or warehouse + batch
  - use `Inventory Availability` when you need a full searchable inventory list rather than a single selected-item query
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
- for line-grid documents, `Edit` opens the detail page with existing lines already in edit mode; the separate add-line form is hidden in that state so it does not look like a blank saved row
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
  - when serialized `Equipment` items are posted, the system creates customer equipment units automatically from the dispatched serial numbers
  - warranty end date, coverage, service interval, and next service date entered on the dispatch header become the equipment-unit warranty/service defaults
  - while the dispatch is still `Draft`, existing lines can be edited directly in the line grid
  - use `Tab` to move between editable cells and `Enter` to save the active line
- Direct dispatch:
  - create -> add lines -> post
  - use for AOD/direct delivery; serialized `Equipment` lines also create customer equipment units automatically on post
  - if the AOD is linked to a job order, the equipment unit is assigned to that job's customer when no customer is selected directly
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
  - register serialized customer equipment against an `Equipment` type item from `Master Data -> Items`
  - the item record stores the equipment model/SKU/name; the equipment unit stores the serial number, customer ownership, warranty, and service scheduling fields
  - ISS-sold serialized equipment is normally created automatically when Dispatch/AOD is posted
  - use `Existing item` when the model already exists in Item Master
  - use `Outside equipment` when a customer brings equipment bought elsewhere; this creates an Equipment item and the serialized customer unit in one step
  - outside equipment normally has no ISS warranty unless a service contract or manually entered warranty coverage applies
  - maintain purchased date, warranty end date, and warranty coverage scope
  - the list page exposes `View` and `Edit` actions explicitly; both open the unit detail page
  - use the equipment-unit detail page to edit ownership or warranty coverage as the installed base changes
- Service contracts:
  - create `AMC`, `SLA`, or `Warranty Extension` documents against a specific customer-owned equipment unit
  - select equipment using the searchable equipment-unit picker; it searches the linked item SKU/name, serial number, and customer code
  - set the contract coverage window and coverage scope (`Inspection`, `Labor`, `Parts`, or `Labor and Parts`)
  - use this when the unit is covered by a service agreement beyond or instead of manufacturer warranty
  - the list page exposes `View` and `Edit` actions explicitly; both open the contract detail page
  - contracts are edited from the contract detail page after creation
- Service jobs:
  - create -> start -> complete -> close
  - choose `Service`, `Repair`, `PDI`, `Warranty`, or `Inspection` when opening the job
  - select equipment using the searchable equipment-unit picker; it searches the linked item SKU/name, serial number, and customer code
  - after equipment is selected, the customer defaults from the equipment unit but can still be changed before saving if required
  - the list page exposes `View` and `Edit`; `Edit` is available while the job is still `Open`
  - while status is still `Open`, you can edit the job header (unit, customer, type, problem) from the detail page
  - once the job is started, header editing is locked and execution should continue through work orders, estimates, material issues, and handover
  - entitlement is captured automatically when the job is created by checking active service contracts first, then manufacturer warranty on the equipment unit
  - use `Refresh Entitlement` on the job if warranty/contract data is added after the job was already opened
  - recommended operating flow is: receive equipment -> open the job immediately -> create a daily field sheet for each working day -> capture labor, progress, IOUs, expenses, materials, returns, damages, and notes from that sheet -> approve the sheet -> finish invoicing/closeout
  - `Daily Field Sheets` on the job detail page replace manual daily job cards; each sheet has planned/completed/pending work, site condition, staff/progress/material/return/expense/IOU counts, and approval status
  - daily job detail sections keep the running work separated into `Daily Staff / Labor`, `Daily Progress`, `IOU / Employee Advance`, `Petty Cash Expense`, `Employee Out-of-Pocket Claim`, `Materials / Lubricants Issue`, and `Material Returns / Damage / Rejection`
  - closing a job is blocked while daily sheets are still draft or submitted; approve or reject every sheet before closeout
- Technicians:
  - maintain technician code, name, default cost rate, default billing rate, phone, notes, and active status
  - job detail labor entries select technicians from this master
  - selecting a technician on a job sheet auto-fills default labor cost and billing rates, but the entry can still be adjusted before saving
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
  - using `Edit` from the list opens the existing lines directly in edit mode; use `Switch to Add Line` if you want to return to the add-line form
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
  - when created from a job daily sheet, the claim remains part of the same finance approval/settlement flow but is visible in the daily field record
- Material requisitions:
  - the list page now exposes explicit `View` and `Edit`; `Edit` is available while the requisition is still `Draft`
  - create -> add lines -> post
  - using `Edit` from the list opens the existing draft lines directly in edit mode; use `Switch to Add Line` if you want to return to the add-line form
  - use when issuing stocked spare parts from a warehouse to the job
  - the system validates available stock before a line is saved, so zero-stock or over-requested items are rejected before posting
  - serialized items must be selected from currently available serial numbers in the source warehouse
  - the separate stock lookup opens from a compact `Load stock` button instead of taking permanent space on the requisition screen
  - when created from a job daily sheet, the MRN still follows the same stock validation and posting flow, and material use/return/damage disposition is captured back on the job detail page
- Quality checks:
  - record pass/fail and notes
- Service handovers:
  - the list page now exposes explicit `View` and `Edit`; `Edit` is available while the handover is still `Draft`
  - complete handover and optionally convert to invoice
  - while the handover is still `Draft`, you can edit the returned-items text, post-service warranty months, customer acknowledgement, and notes from the detail page
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
  - create and track IOUs for job-linked cash advances when money is issued before the final expense claim is settled
  - settle approved service expense claims against a petty cash fund when workshop cash was used
  - petty-cash-funded service claims require a petty cash fund at settlement; out-of-pocket claims can still be settled with payment type/reference only
- Credit notes:
  - create and allocate to AR/AP
- Debit notes:
  - create additional AR/AP charge entries
- Service expense claims:
  - finance users approve/reject submitted claims and settle approved claims with a payment method/reference and optional petty cash fund linkage
  - petty-cash and out-of-pocket claims are tracked here rather than hidden as ad-hoc stock or payment workarounds
  - when a technician receives a cash advance, record it as a Petty Cash IOU and clear it through the later claim/settlement process

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
