# ISS ERP Full Help Tutorial

This guide is written for a new user, tester, or trainer. It explains the ISS ERP system in simple English and follows the same practical style used when teaching accounting or stock systems such as Tally.

Use this guide to learn:

- what each screen is for
- what to enter
- what output to expect
- what to check after saving, posting, approving, or printing

Screenshots used by this guide are stored in:

- `docs/assets/tester-trainer/`
- `frontend/public/help/system/`
- `frontend/public/help/job-orders/`

## 1. First Login And Screen Layout

![Login page](assets/tester-trainer/login-page.png)

Open the system and sign in.

What to input:

| Field | What to enter |
| --- | --- |
| Email | Your user email |
| Password | Your password |

Output:

- The system opens the dashboard.
- The left side menu shows only the modules you can access.
- The top bar shows your signed-in user and notification bell.

What to check:

- You can see the expected modules in the sidebar.
- The notification bell is visible.
- If a menu is missing, ask an admin to check `Admin -> Users -> Access permissions`.

## 2. Main Menu Map

![Dashboard](assets/tester-trainer/dashboard.png)

| Menu | Used for |
| --- | --- |
| Overview | Dashboard and quick business summary |
| Master Data | Setup data such as items, customers, suppliers, currencies, warehouses, taxes |
| Procurement | Purchase requisitions, RFQs, purchase orders, receipts, supplier invoices, supplier returns |
| Sales | Quotes, orders, dispatches, direct dispatches, invoices, customer returns |
| Service | Equipment, service jobs, daily sheets, work orders, estimates, handovers |
| Inventory | On-hand stock, availability, reorder alerts, stock adjustments, stock transfers |
| Finance | AR, AP, payments, petty cash, IOUs, credit notes, debit notes |
| Reporting | Stock, aging, tax, service, sales, purchase, supplier, and costing reports |
| Admin | Users, notifications, imports, settings |
| Audit Logs | History of user actions and system changes |

Common rule:

- Use `Create` or `New` to make a document.
- Use `Edit` only while a document is still draft.
- Use `Post`, `Approve`, `Confirm`, or `Send` when the document is ready.
- After posting, check the related output: stock, AR, AP, report, notification, or PDF.

## 3. Master Data Setup

![Currencies](assets/tester-trainer/master-data-currencies.png)

Master data must be correct before transactions are entered.

Recommended setup order:

1. Companies, currencies, and exchange rates
2. Taxes and tax conversions
3. Payment types
4. Warehouses and bins/racks
5. UoMs and unit conversions
6. Item categories and brands
7. Items
8. Suppliers and customers
9. Reorder settings

### 3.1 Currencies

What to input:

| Field | Example |
| --- | --- |
| Code | `LKR`, `USD` |
| Name | `Sri Lankan Rupee` |
| Symbol | `Rs.` |
| Minor units | `2` |
| Base currency | Tick for the main company currency |

Output:

- Currency appears in the currency list.
- Payment, invoice, and reporting screens can use the currency.

What to check:

- Exactly one base currency should be active.
- Exchange rate exists when using a foreign currency.

### 3.2 Items

What to input:

| Field | Example |
| --- | --- |
| SKU | `HF-001` |
| Name | `Hydraulic Filter` |
| Type | Stock, Service, Equipment, or Expense |
| Unit of measure | `PCS` |
| Default unit cost | `2500.00` |
| Tracking type | None, Batch, Serial, or Batch + Serial |

Output:

- Item appears in item lists.
- Item can be selected in purchase, sales, inventory, and service screens.

What to check:

- Stock items must have correct tracking type before transactions begin.
- Equipment items should use serial tracking if each machine has a serial number.

### 3.3 Customers And Suppliers

What to input:

| Field | Customer example | Supplier example |
| --- | --- | --- |
| Code | `CUS001` | `SUP001` |
| Name | `ABC Customer` | `Main Supplier` |
| Email/phone/address | Optional but recommended | Optional but recommended |

Output:

- Customer can be used in sales and service.
- Supplier can be used in procurement and AP.

What to check:

- Codes should not be duplicated.
- Inactive customers or suppliers should not be used for new transactions.

## 4. Procurement Tutorial

![Purchase orders](assets/tester-trainer/procurement-purchase-orders.png)

Procurement controls the buying cycle.

Main flow:

1. Purchase Requisition
2. RFQ
3. Purchase Order
4. Goods Receipt
5. Supplier Invoice
6. Supplier Payment

### 4.1 Purchase Requisition

Use when a department requests items before purchasing.

What to input:

| Field | Example |
| --- | --- |
| Required date | Expected need date |
| Reason/notes | Why the item is required |
| Item | `Hydraulic Filter` |
| Quantity | `10` |

Output:

- Draft PR is created.
- Lines can be added or edited while draft.

What to check:

- Submit sends the PR for approval.
- Users with `Procurement.PurchaseRequisition.Approve` receive notification.
- Approver can approve or reject.
- Approved PR can be converted to a PO.

### 4.2 RFQ

Use when asking suppliers for prices.

What to input:

| Field | Example |
| --- | --- |
| Supplier | Selected supplier |
| Item | Requested item |
| Quantity | Requested quantity |

Output:

- RFQ document is created.
- `Send` marks it as sent.

What to check:

- RFQ number is generated.
- PDF can be downloaded if needed.

### 4.3 Purchase Order

Use when confirming a purchase from a supplier.

What to input:

| Field | Example |
| --- | --- |
| Supplier | `SUP001` |
| Item | `Hydraulic Filter` |
| Quantity | `10` |
| Unit cost | `2500.00` |
| Tax | Applicable tax |

Output:

- PO total is calculated.
- PO stays draft until approved.

What to check:

- Approve action is available only for users with PO approve permission.
- After approval, PO can be used for GRN.
- Creator receives notification when approved.

### 4.4 Goods Receipt Note

Use when purchased goods arrive.

What to input:

| Field | Example |
| --- | --- |
| PO | Approved PO |
| Warehouse | `MAIN` |
| Received quantity | `10` |
| Cost | Supplier cost |
| Batch/serial | Required if item is tracked |

Output:

- GRN is created.
- Posting increases stock.
- Posting can create AP liability.

What to check:

- Inventory on-hand increased.
- AP entry exists for the supplier.
- Stock ledger/report shows receipt movement.

### 4.5 Supplier Invoice

Use to record supplier billing.

What to input:

| Field | Example |
| --- | --- |
| Supplier | Supplier from PO/GRN |
| Source document | PO, GRN, or direct purchase |
| Invoice date | Supplier invoice date |
| Supplier invoice number | Supplier reference |

Output:

- Supplier invoice is created.
- Posting updates AP.

What to check:

- Supplier AP balance increases.
- Invoice PDF is available.

### 4.6 Supplier Return

Use when returning items to supplier.

What to input:

| Field | Example |
| --- | --- |
| Supplier | Supplier |
| Warehouse | Where stock is returned from |
| Item | Returned item |
| Quantity | Return quantity |
| Reason | Damage, wrong item, excess |

Output:

- Posted return reduces stock.
- Supplier credit note is created.

What to check:

- Stock reduced.
- Supplier credit note appears in finance.

## 5. Inventory Tutorial

Inventory screens show and control stock.

### 5.1 Inventory Availability

Use this to search current stock.

What to input:

| Filter | Example |
| --- | --- |
| Warehouse | `MAIN` |
| Item | `Hydraulic Filter` |
| Batch/serial | Optional |

Output:

- Table shows item, warehouse, bin, batch, serial, quantity, unit cost, and value.

What to check:

- Stock received by GRN appears here.
- Stock sold or issued by MRN is reduced.
- Unassigned bin means stock exists without a bin/rack.

### 5.2 Stock Adjustment

Use when physical count differs from system stock.

What to input:

| Field | Example |
| --- | --- |
| Warehouse | `MAIN` |
| Reason | `Monthly stock count` |
| Item | Item being counted |
| Counted quantity | Actual physical quantity |

Output:

- Draft adjustment is created.
- Posting records only the variance.

What to check:

- On-hand becomes the counted quantity.
- Stock ledger shows adjustment movement.
- Voided drafts do not affect stock.

### 5.3 Stock Transfer

Use when moving stock between warehouses.

What to input:

| Field | Example |
| --- | --- |
| From warehouse | `MAIN` |
| To warehouse | `SERVICE` |
| Item | Item to move |
| Move quantity | Quantity transferred |

Output:

- Draft transfer is created.
- Posting reduces source stock and increases destination stock.

What to check:

- Source warehouse stock decreased.
- Destination warehouse stock increased.
- Batch/serial details are preserved.

## 6. Sales Tutorial

Sales controls quote-to-cash.

Main flow:

1. Quote
2. Sales Order
3. Dispatch or Direct Dispatch
4. Sales Invoice
5. Payment
6. Customer Return if needed

### 6.1 Sales Quote

What to input:

| Field | Example |
| --- | --- |
| Customer | `CUS001` |
| Valid until | Expiry date |
| Item/service | Quoted line |
| Quantity | `4` |
| Unit price | `3500.00` |

Output:

- Quote is created in draft.
- Sending marks it as sent.

What to check:

- Quote total is correct.
- PDF can be downloaded.

### 6.2 Sales Order

What to input:

| Field | Example |
| --- | --- |
| Customer | `CUS001` |
| Item | Sold item |
| Quantity | Ordered quantity |
| Price | Sales price |

Output:

- Draft sales order is created.
- Confirming accepts the order.

What to check:

- Confirmed order can be used for dispatch.
- Users with confirm rights see the confirm action.

### 6.3 Dispatch

Use when delivering goods from stock.

What to input:

| Field | Example |
| --- | --- |
| Sales order | Confirmed order |
| Warehouse | Dispatch warehouse |
| Item | Item to dispatch |
| Quantity | Dispatch quantity |
| Serial/batch | Required if tracked |

Output:

- Posting reduces stock.
- Serialized equipment can create equipment units.

What to check:

- Stock decreases.
- Equipment unit is created for serialized equipment.
- Dispatch PDF is available.

### 6.4 Direct Dispatch

Use for direct delivery/AOD without the normal order flow.

What to input:

| Field | Example |
| --- | --- |
| Customer or service job | Destination/customer context |
| Warehouse | Source warehouse |
| Item | Delivered item |
| Quantity | Delivered quantity |

Output:

- Posted direct dispatch reduces stock.
- Equipment units are created for serialized equipment where applicable.

What to check:

- Stock movement is correct.
- Customer/job link is correct.

### 6.5 Sales Invoice

What to input:

| Field | Example |
| --- | --- |
| Customer | `CUS001` |
| Source | Dispatch, direct dispatch, or manual |
| Item/line | Sales line |
| Quantity and price | Invoice quantity and price |

Output:

- Posting creates AR.

What to check:

- Customer AR balance increases.
- Invoice PDF is available.
- Invoice appears in aging reports.

### 6.6 Customer Return

What to input:

| Field | Example |
| --- | --- |
| Customer | Returning customer |
| Invoice | Optional source invoice |
| Item | Returned item |
| Quantity | Return quantity |
| Reason | Damage, wrong item, warranty, etc. |

Output:

- Posting returns stock.
- Customer credit note is created.

What to check:

- Stock increases.
- Customer credit note appears in finance.

## 7. Finance Tutorial

![Accounts receivable](assets/tester-trainer/finance-ar.png)

![Accounts payable](assets/tester-trainer/finance-ap.png)

Finance controls money owed, money payable, payments, petty cash, and notes.

### 7.1 Accounts Receivable

Use `Finance -> Accounts Receivable`.

Output:

- Shows customer invoice balances.
- Shows outstanding amounts.

What to check:

- Posted sales invoices appear here.
- Payments reduce outstanding amounts.
- Customer credit notes reduce receivable balance when allocated.

### 7.2 Accounts Payable

Use `Finance -> Accounts Payable`.

Output:

- Shows supplier invoice/GRN balances.
- Shows outstanding amounts.

What to check:

- Posted supplier invoices or purchase receipts appear here.
- Supplier payments reduce outstanding amounts.
- Supplier credit notes reduce payable balance when allocated.

### 7.3 Payments

What to input:

| Field | Incoming payment | Outgoing payment |
| --- | --- | --- |
| Direction | Incoming | Outgoing |
| Counterparty type | Customer | Supplier |
| Counterparty | Customer | Supplier |
| Payment type | Cash, bank, transfer, etc. | Cash, bank, transfer, etc. |
| Amount | Paid amount | Paid amount |
| Currency/rate | As applicable | As applicable |

Output:

- Payment document is created.
- Allocation action can allocate it to AR or AP.

What to check:

- Payment appears in payment list.
- Allocated amount reduces AR/AP outstanding.
- Payment creator receives notification when another user allocates it.

### 7.4 Credit Notes

What to input:

| Field | Example |
| --- | --- |
| Counterparty type | Customer or Supplier |
| Counterparty | Selected customer/supplier |
| Amount | Credit amount |
| Notes | Reason |

Output:

- Credit note is created.
- Allocation can apply it to AR or AP.

What to check:

- Remaining amount reduces after allocation.
- Allocation appears in the credit note detail.
- Creator receives notification when allocated.

### 7.5 Debit Notes

What to input:

| Field | Example |
| --- | --- |
| Counterparty type | Customer or Supplier |
| Counterparty | Selected counterparty |
| Amount | Additional charge |
| Notes | Reason |

Output:

- Debit note is created.

What to check:

- PDF is available.
- Related counterparty balance/reporting is reviewed.

### 7.6 Petty Cash Funds

What to input:

| Field | Example |
| --- | --- |
| Fund code | `PC-MAIN` |
| Name | `Main Petty Cash` |
| Currency | `LKR` |
| Custodian | Person responsible |
| Opening balance | Starting float |

Output:

- Petty cash fund is created.
- Top-up and adjustment entries can be posted by users with permission.

What to check:

- Balance updates after top-up.
- Balance updates after adjustment.
- Fund creator receives notification when another user updates, tops up, or adjusts the fund.

### 7.7 Petty Cash IOUs

Use IOUs when a user requests an advance.

What to input:

| Field | Example |
| --- | --- |
| Service job | Linked job |
| Requested amount | Advance amount |
| Purpose | Why cash is needed |
| Expected settlement date | Planned settlement date |

Output:

- Draft/submitted IOU is created.
- Users with approve rights receive notification.

What to check:

- Approver can approve or reject.
- Finance can release cash.
- Finance can settle the IOU.
- Requester receives status notifications.

## 8. Service Tutorial

Service manages equipment, jobs, daily work, materials, labour, expenses, handover, and closeout.

### 8.1 Service Job List

![Service job list](../frontend/public/help/job-orders/01-jobs-list.png)

What to input when creating:

| Field | Example |
| --- | --- |
| Equipment unit | Customer machine |
| Customer | Auto-filled or selected |
| Job type | Service, Repair, PDI, Warranty, Inspection |
| Problem/complaint | Customer complaint |
| Responsible officer | Job owner |
| Expected date | Planned finish/visit date |

Output:

- Job order is created.
- Job number is generated.

What to check:

- Equipment and customer are correct.
- Warranty/contract entitlement is correct.
- Job appears in command center and job list.

### 8.2 Job Overview

![Job overview](../frontend/public/help/job-orders/02-job-overview.png)

Output:

- Shows job number, status, type, equipment, customer, dates, cockpit, and process timeline.

What to check:

- Use timeline links to go to plan, daily work, materials, expenses, billing, costs, and files.
- Header edit is available only while the job is editable.

### 8.3 Daily Field Sheets

![Daily sheets](../frontend/public/help/job-orders/04-daily-sheets.png)

What to input:

| Field | Example |
| --- | --- |
| Work date | Today |
| Planned work | What should be done |
| Completed work | What was completed |
| Pending/issues | What remains |
| Site condition | Optional |

Output:

- Daily sheet card appears.
- Staff, progress, material, IOU, and expense counts appear on the card.

What to check:

- Create one daily sheet per work day.
- Approve or reject submitted sheets before closeout.

### 8.4 Daily Labour

![Daily labour](../frontend/public/help/job-orders/05-daily-labor.png)

Use daily labour to record attendance and daily work.

What to input:

| Field | Example |
| --- | --- |
| Technician/person | Worker |
| Work date/daily sheet | Selected sheet |
| Hours | Time worked |
| Notes | Work summary |

Output:

- Daily staff entry appears under the daily sheet.

What to check:

- This records attendance/supervision.
- It does not by itself create final billable invoice labour.

### 8.5 Work Orders / Job Sheets

Use work orders for billable or costed labour time entries.

What to input:

| Field | Example |
| --- | --- |
| Service job | Related job |
| Technician | Technician |
| Hours | Billable/cost hours |
| Cost rate | Internal cost |
| Billing rate | Customer charge |
| Billable | Yes/No |

Output:

- Time entry moves through Draft -> Submitted -> Approved/Rejected -> Invoiced.

What to check:

- Approved labour appears in costing.
- Billable approved labour can be invoiced.

### 8.6 Materials / MRN

![Materials](../frontend/public/help/job-orders/06-materials.png)

What to input:

| Field | Example |
| --- | --- |
| Service job | Related job |
| Warehouse | Source warehouse |
| Item | Spare part/material |
| Quantity | Required quantity |

Output:

- Draft MRN is created.
- Posting MRN issues stock to the job.

What to check:

- Draft MRN does not reduce stock.
- Posted MRN reduces stock.
- Material cost appears in job costing.
- Return/damage disposition is recorded before closeout.

### 8.7 Expenses

![Expenses](../frontend/public/help/job-orders/07-expenses.png)

Expense types:

| Type | Use when |
| --- | --- |
| IOU | User needs advance cash |
| Petty cash expense | Company petty cash was used |
| Out-of-pocket claim | Employee paid personally and needs reimbursement |

What to check:

- Submitted IOUs notify all users with approve rights.
- Approved/rejected users receive status notifications.
- Finance can settle approved claims.
- Billable expense lines can be converted to estimates where allowed.

### 8.8 Billing And Closeout

![Billing](../frontend/public/help/job-orders/08-billing.png)

What to check:

- Closeout readiness shows all blockers.
- Draft/submitted daily sheets must be cleared.
- Pending IOUs and expense claims must be cleared.
- Draft MRNs and unresolved materials must be cleared.
- Final invoice decision must be completed.

### 8.9 Costs

![Costs](../frontend/public/help/job-orders/09-costs.png)

Output:

- Shows material cost, labour cost, expense cost, quoted revenue, invoice revenue, and margin view.

What to check:

- Costs are reviewed before final billing.
- Uninvoiced billable labour is checked before closing.

## 9. Reporting Tutorial

![Costing report](assets/tester-trainer/reporting-costing.png)

Reports are used to check the result of transactions.

| Report | What to check |
| --- | --- |
| Stock Ledger | Every stock receipt, issue, adjustment, transfer |
| Aging | Customer AR and supplier AP outstanding |
| Tax Summary | Taxable sales/purchase totals |
| Service KPIs | Job and service performance |
| Sales Analysis | Customer/item sales totals |
| Purchase Analysis | Supplier/item purchase totals |
| Supplier Performance | Supplier delivery/purchase view |
| Costing | Inventory value and cost |

What to input:

- Date range
- Warehouse, item, customer, supplier, or service filter where available

Output:

- Report table and optional PDF.

What to check:

- Report totals match posted documents.
- Draft documents should not affect posted financial/stock reports.

## 10. Admin, Users, Access, And Notifications

![Admin users](assets/tester-trainer/admin-users.png)

### 10.1 User Roles

Roles are broad access groups:

- Admin
- Procurement
- Inventory
- Sales
- Service
- Finance
- Reporting

### 10.2 User Permissions

Permissions are detailed rights such as:

- View
- Create
- Edit
- Submit
- Approve
- Reject
- Post
- Allocate
- Settle
- Delete/Void where available

What to input:

| Field | Example |
| --- | --- |
| User | Selected user |
| Roles | Sales, Finance, etc. |
| Access permissions | Tick or untick exact permissions |

Output:

- User effective permissions update.
- Sidebar and action buttons follow those permissions.
- Backend still blocks unauthorized actions even if a user manually opens a URL.

What to check:

- View-only users can open pages but cannot create/edit/post.
- Approvers see approval actions.
- Multiple users with the same approve permission receive the same approval notification.

### 10.3 Notifications

Notifications appear in the notification bell and notification pages.

Examples:

- IOU submitted -> all IOU approvers are notified.
- Purchase requisition submitted -> all PR approvers are notified.
- Expense claim submitted -> all claim approvers are notified.
- Document approved/post/allocated -> creator is notified.

What to check:

- Notification title is clear.
- Notification link opens the correct document.
- After reading, notification status changes.

## 11. Audit Logs

Use Audit Logs to check who did what.

What to check:

- Created document
- Edited document
- Posted document
- Approved/rejected action
- User/admin changes

Audit logs are evidence for testing and troubleshooting.

## 12. End-To-End Example: Stock Purchase To Customer Payment

Use this as a complete training exercise.

1. Create or confirm item, supplier, customer, warehouse, tax, and currency.
2. Create purchase order for item quantity `10`.
3. Approve purchase order.
4. Create GRN from PO.
5. Post GRN.
6. Check inventory availability increased by `10`.
7. Create sales order for quantity `4`.
8. Confirm sales order.
9. Create dispatch.
10. Post dispatch.
11. Check stock reduced by `4`.
12. Create sales invoice.
13. Post invoice.
14. Check AR increased.
15. Create incoming customer payment.
16. Allocate payment to AR.
17. Check AR outstanding reduced.
18. Open stock ledger, aging report, and costing report.

What to check at the end:

- Stock: received `10`, dispatched `4`, remaining `6`.
- AR: invoice amount created and payment reduced it.
- Reports: stock ledger, aging, and costing match the documents.
- Audit: user actions are visible.
- PDFs: PO, GRN, dispatch, invoice, payment can be downloaded where available.

## 13. End-To-End Example: Service Job

1. Create or confirm equipment unit.
2. Create job order.
3. Start the job.
4. Create daily sheet.
5. Add daily staff.
6. Add progress update.
7. Request IOU if needed.
8. Create MRN and post material issue.
9. Create work order labour entry.
10. Submit and approve labour.
11. Create estimate if customer approval is needed.
12. Complete job.
13. Create handover/service taken.
14. Review billing and closeout readiness.
15. Review costs.
16. Close job after blockers are cleared.

What to check:

- Daily work exists for every working day.
- Materials and expenses are settled.
- Labour is approved.
- Billing decision is completed.
- Costs are reviewed.
- Job closes only when readiness is clear.

## 14. Testing Checklist For Each Transaction

For every transaction screen, check:

| Check | Pass condition |
| --- | --- |
| Create | Document number is generated |
| Edit | Draft document can be edited |
| Permission | Unauthorized user cannot perform action |
| Submit/Approve/Post | Status changes correctly |
| Notification | Correct users receive notification |
| Inventory impact | Stock changes only after posting |
| Finance impact | AR/AP changes only after posting/allocation |
| PDF | PDF opens/downloads |
| Audit | Action appears in audit log |
| Report | Related report reflects posted result |

## 15. Common Mistakes

| Mistake | Correct action |
| --- | --- |
| Trying to edit posted document | Create return, credit note, debit note, adjustment, or correction flow |
| Expecting draft to affect stock | Post the document first |
| Expecting daily labour to invoice customer | Use work order/job sheet billable time |
| Missing approve button | Check user permission |
| Missing menu item | Check role and view permission |
| No stock available | Check warehouse, bin, batch, serial, and posted receipts |
| Cannot close service job | Open Billing -> Closeout Readiness and clear blockers |

## 16. Quick Trainer Script

Say this to a new user:

1. "First we set master data: items, customers, suppliers, warehouses, taxes, and currency."
2. "Then we create business documents in draft."
3. "Draft documents are safe. They do not affect stock or accounts until posted or approved."
4. "Posting is the important action. It updates stock, AR, AP, reports, and audit."
5. "Permissions control who can view, create, edit, approve, post, allocate, or settle."
6. "Notifications tell the next responsible users what needs action."
7. "Reports are where we check that the transaction result is correct."

