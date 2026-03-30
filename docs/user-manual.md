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

- RFQ:
  - create -> add lines -> send
- Purchase requisition:
  - create -> add lines -> submit -> approve/reject/cancel
  - approved PR can be converted to PO
- Purchase order:
  - create -> add lines -> approve
- Goods receipt (GRN):
  - create from PO -> add lines -> post
  - posting increases stock and creates AP entry
- Direct purchase:
  - create -> add lines -> post
  - posting increases stock without PO/GRN chain
- Supplier invoice:
  - create (linked to PO/GRN or direct purchase) -> post
- Supplier return:
  - create -> add lines -> post
  - posting issues stock and creates supplier credit note

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

- Quote:
  - create -> add lines -> send
- Sales order:
  - create -> add lines -> confirm
- Dispatch:
  - create -> add lines -> post
- Direct dispatch:
  - create -> add lines -> post
- Invoice:
  - create -> add lines -> post
  - posting creates AR entry
- Customer return:
  - create -> add lines -> post
  - posting returns stock and creates customer credit note

## Service workflows

- Equipment units:
  - register serialized customer equipment
- Service jobs:
  - create -> start -> complete -> close
  - choose `Service` or `Repair` when opening the job
- Work orders:
  - create and track work records
- Service estimates:
  - create -> add lines -> approve/reject/send
  - estimates now support `Part`, `Labor`, and billable `Expense` lines
  - if extra findings appear after approval or rejection, create a revision instead of overwriting the original estimate
- Service expense claims:
  - create against a service job for `Out of Pocket` or `Petty Cash`
  - add free-text or item-linked lines
  - submit -> finance approve/reject -> settle
  - billable approved/settled claim lines can be pushed into the latest draft estimate or into an automatic estimate revision
  - use this for emergency outside buys and technician reimbursement/petty-cash clearing that do not come from stock
- Material requisitions:
  - create -> add lines -> post
  - use when issuing stocked spare parts from a warehouse to the job
- Quality checks:
  - record pass/fail and notes
- Service handovers:
  - complete handover and optionally convert to invoice
  - invoice conversion can now map expense estimate lines by using the line item when present or a selected fallback expense/service item
- Direct purchases linked to service jobs:
  - use when a required part is bought from a supplier and also needs to be received into stock
  - the direct purchase can now be linked back to the service job for traceability

## Finance workflows

- AR and AP:
  - view outstanding entries
- Payments:
  - create incoming/outgoing payments
  - allocate to AR/AP entries
- Petty cash:
  - create fund -> optionally seed opening balance
  - post top-ups and manual adjustments
  - settle approved service expense claims against a petty cash fund when workshop cash was used
- Credit notes:
  - create and allocate to AR/AP
- Debit notes:
  - create additional AR/AP charge entries
- Service expense claims:
  - finance users approve/reject submitted claims and settle approved claims with a payment method/reference and optional petty cash fund linkage
  - petty-cash and out-of-pocket claims are tracked here rather than hidden as ad-hoc stock or payment workarounds

## Reporting

- Available reports:
  - dashboard, stock ledger, aging, tax summary, service KPIs, costing
- Costing report:
  - compares default and weighted average cost
  - shows last receipt cost/date
  - shows on-hand valuation in base currency
- Service job costing:
  - each service job detail page now shows quoted revenue, posted invoice revenue, actual job cost, and unconverted billable expense-claim totals
  - cost sources are broken down by material requisitions, direct purchases, and service expense claims

## Line editing in draft documents

For line-based documents, while status is draft:

- you can `Add` line
- you can `Edit` line (then `Save` or `Cancel`)
- you can `Delete` line

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
