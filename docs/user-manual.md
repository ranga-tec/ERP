# User Manual (Quick)

## Sign in / roles

- Go to the web app and use **Login / Register**.
- The **first registered user becomes Admin**.
- Roles used in the system:
  - `Admin`, `Procurement`, `Inventory`, `Sales`, `Service`, `Finance`, `Reporting`
- Admin can manage users/roles at **Admin → Users**.

## Master data

Set up the basics first:

- **Brands** (optional)
- **Warehouses** (required for stock operations)
- **Items** (SKU, tracking type, optional barcode)
- **Suppliers** (for procurement and AP)
- **Customers** (for sales/service and AR)
- **Reorder Settings** (warehouse + item reorder points)

## Import (Excel)

Admin can import master data in bulk:

- **Admin → Import**
  - Download the template
  - Fill sheets (Brands, Warehouses, Suppliers, Customers, Items, ReorderSettings, EquipmentUnits)
  - Upload the `.xlsx` file (import is transactional: all-or-nothing)

## Procurement

- **RFQ**
  - Create RFQ → add lines → Send
  - Download PDF from the RFQ detail page
- **Purchase Order**
  - Create PO → add lines → Approve
  - Approving a PO queues notifications (email/SMS) to the supplier (if enabled)
  - Download PDF from the PO detail page
- **Goods Receipt (GRN)**
  - Create GRN from PO → add lines (batch/serials if tracked) → Post
  - Posting increases stock and creates an AP entry
  - Download PDF from the GRN detail page
- **Supplier Return**
  - Create return → add lines → Post
  - Posting reduces stock and creates a **Supplier Credit Note** (no negative AP)
  - Download PDF from the return detail page

## Inventory

- **On Hand**: query stock by warehouse/item (and batch where applicable)
- **Reorder Alerts**: uses reorder settings + on-hand
- **Stock Adjustment**: add lines → Post/ Void
- **Stock Transfer**: move stock between warehouses → Post/ Void

All inventory documents support PDF download from their detail pages.

## Sales

- **Quote**
  - Create quote → add lines → Send
  - Download PDF from the quote detail page
- **Sales Order**
  - Create order → add lines → Confirm
  - Download PDF from the order detail page
- **Dispatch**
  - Create dispatch → add lines (batch/serials if tracked) → Post
  - Posting reduces stock
  - Download PDF from the dispatch detail page
- **Invoice**
  - Create invoice → add lines → Post
  - Posting creates an AR entry and queues notifications (email/SMS) to the customer (if enabled)
  - Download PDF from the invoice detail page

## Service

- **Equipment Units**: register serialized equipment linked to an item + customer
- **Service Jobs**: open jobs for equipment units, track progress, complete/close
- **Work Orders**: work items for jobs
- **Material Requisitions**: issue spare parts to jobs (stock decreases on post)
- **Quality Checks**: record pass/fail QC for a job

Service documents support PDF download from their detail pages.

## Finance

- **AR / AP**: view outstanding entries
- **Payments**: record incoming/outgoing payments and allocate to AR/AP
- **Credit Notes**: issue credits and allocate to AR/AP (manual or auto-allocate)
- **Debit Notes**: issue additional charges (creates AR/AP entries)

Finance documents support PDF download from their detail pages.

## Notifications (email/SMS)

- Events currently queue notifications:
  - PO approval (to supplier)
  - Invoice posting (to customer)
- View and retry queued items at **Admin → Notifications**
- To enable sending, configure SMTP/Twilio and enable the dispatcher in backend config.

## Barcode / labels

- Items can have a `barcode` value.
- You can download a label PDF per item from **Items → Label PDF**.

## Audit logs

- Every transaction writes an audit record.
- View at **Audit → Audit Logs**.

