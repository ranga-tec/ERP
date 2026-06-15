# ISS ERP User Manual

This manual explains how to use the ISS ERP system in simple English. It is written for daily users, supervisors, finance users, store users, managers, trainers, and testers.

The system follows one main rule:

**Draft documents are preparation. Approval, posting, allocation, settlement, completion, or closeout is what updates stock, finance, reports, notifications, and audit logs.**

Use this manual when you need to know:

- what each module is for
- what to enter
- what output to expect
- what to check after saving, approving, posting, or closing
- how the Service module works, including daily sheets and job sheets

## 1. Login, Menu, Roles, And Access

Open the app and sign in with your email and password.

After login:

- the dashboard opens
- the sidebar shows the modules you are allowed to use
- action buttons appear only when your user has the required permission
- the notification bell shows workflow messages assigned to you

Common roles:

| Role | Main use |
| --- | --- |
| Admin | User management, access permissions, settings, import, full control |
| Procurement | Purchase requisitions, RFQs, POs, GRNs, supplier documents |
| Inventory | Stock availability, stock adjustments, transfers, material issue checks |
| Sales | Quotes, sales orders, dispatches, invoices, returns |
| Service | Equipment, service jobs, daily work, estimates, handovers |
| Finance | AR, AP, payments, IOUs, petty cash, settlements, allocations |
| Reporting | Business, stock, finance, sales, purchase, and service reports |

What to check after login:

- Expected modules are visible.
- Restricted modules are hidden.
- Notification bell is visible.
- Your name/user appears correctly.
- If a needed menu is missing, ask Admin to check `Admin -> Users -> Access permissions`.

## 2. Main Menu Map

| Menu | Used for |
| --- | --- |
| Overview | Dashboard, summaries, and queues |
| Master Data | Items, customers, suppliers, warehouses, currencies, taxes, payment types |
| Procurement | Purchase requests, RFQs, POs, goods receipts, supplier invoices, supplier returns |
| Inventory | On-hand stock, availability, reorder alerts, stock adjustments, stock transfers |
| Sales | Quotes, orders, dispatches, direct dispatches, invoices, customer returns |
| Service | Equipment, contracts, jobs, daily sheets, work orders, MRNs, estimates, handovers |
| Finance | AR, AP, payments, credit/debit notes, petty cash, IOUs, allocations |
| Reporting | Stock, aging, tax, service, sales, purchase, supplier, and costing reports |
| Admin | Users, permissions, imports, notification rules, settings |
| Audit Logs | Evidence of user and system actions |

## 3. Master Data

Master data should be prepared before live transactions are entered.

Recommended setup order:

1. Company and base currency
2. Currencies and exchange rates
3. Tax codes and tax conversions
4. Payment types
5. Warehouses and bins/racks
6. Units of measure and conversions
7. Item categories and brands
8. Items
9. Suppliers and customers
10. Reorder settings
11. Technicians and service master data

### 3.1 Currencies

What to input:

| Field | Example |
| --- | --- |
| Code | `LKR`, `USD` |
| Name | `Sri Lankan Rupee`, `US Dollar` |
| Symbol | `Rs.`, `$` |
| Minor units | `2` |
| Base currency | Tick only the main company currency |

Output:

- Currency appears in finance, sales, purchase, and reports.

What to check:

- Only one active base currency exists.
- Foreign currency transactions have an exchange rate.

### 3.2 Warehouses And Bins

Use warehouses for main stock locations and bins/racks for internal storage positions.

What to input:

| Field | Example |
| --- | --- |
| Warehouse code | `MAIN` |
| Warehouse name | `Main Stores` |
| Bin code | `MAIN-A1` |
| Zone/rack/shelf | Optional physical location details |

Output:

- Stock can be received, issued, counted, and transferred by location.

What to check:

- Stock without bin history may show as `Unassigned`.
- Store users must select the correct warehouse/bin before posting stock movements.

### 3.3 Items

Items are used in procurement, inventory, sales, and service.

What to input:

| Field | Example |
| --- | --- |
| SKU | `FLT-001` |
| Name | `Hydraulic Filter` |
| Type | Stock, Service, Equipment, Expense |
| UoM | `PCS`, `LTR`, `HOUR` |
| Tracking | None, Batch, Serial, Batch + Serial |
| Cost/price/tax | According to company policy |

Output:

- Item can be selected in documents.

What to check:

- Tracking type is correct before transactions start.
- Equipment items should normally use serial tracking.
- Service/labour items should not be treated as stock unless required.

### 3.4 Customers And Suppliers

Customers are used in sales and service. Suppliers are used in procurement and AP.

What to input:

| Field | Example |
| --- | --- |
| Code | `CUS001`, `SUP001` |
| Name | Customer or supplier name |
| Contact | Phone, email, address |
| Active status | Active for normal use |

Output:

- Customer/supplier becomes selectable in transactions.

What to check:

- Do not duplicate codes.
- If a party has old transactions, make it inactive instead of deleting it.

## 4. Procurement

Procurement controls the buying cycle.

Common flow:

1. Purchase requisition
2. RFQ
3. Purchase order
4. Goods receipt
5. Supplier invoice
6. Supplier payment

### 4.1 Purchase Requisition

Use when a department requests items before buying.

What to input:

| Field | Example |
| --- | --- |
| Required date | Date item is needed |
| Reason | Why the item is required |
| Item | Requested item |
| Quantity | Required quantity |

Output:

- Draft PR is created.
- Lines can be edited while draft.
- Submit sends the PR for approval.

What to check:

- Approvers receive a notification.
- Only users with approve rights can approve or reject.
- Approved PR can be converted to a purchase order.

### 4.2 RFQ

Use RFQ when requesting supplier prices.

What to input:

| Field | Example |
| --- | --- |
| Supplier | Supplier to quote |
| Item | Requested item |
| Quantity | Requested quantity |
| Notes | Delivery or quotation instructions |

Output:

- RFQ document is created and can be marked as sent.

What to check:

- RFQ number is generated.
- PDF can be downloaded if required.

### 4.3 Purchase Order

Use PO when confirming a purchase.

What to input:

| Field | Example |
| --- | --- |
| Supplier | Selected supplier |
| Item | Item to buy |
| Quantity | Purchase quantity |
| Unit cost | Supplier cost |
| Tax | Applicable tax |

Output:

- Draft PO is created.
- Approval confirms the purchase.

What to check:

- Approver receives a notification after submission.
- Creator receives status notification after approval or rejection.
- Approved PO can be used in goods receipt.

### 4.4 Goods Receipt Note

Use GRN when goods arrive.

What to input:

| Field | Example |
| --- | --- |
| PO | Approved PO |
| Warehouse/bin | Receiving location |
| Received quantity | Actual received quantity |
| Batch/serial | Required for tracked items |

Output:

- Posting increases stock.
- AP liability may be created depending on setup.

What to check:

- Inventory availability increased.
- Stock ledger shows the receipt.
- Supplier AP or supplier invoice process is correct.

### 4.5 Supplier Invoice And Supplier Return

Supplier invoice records supplier billing. Supplier return sends goods back to the supplier.

What to check:

- Posted supplier invoice updates AP.
- Posted supplier return reduces stock and creates supplier credit note.
- AP aging and supplier balances match posted documents.

## 5. Inventory

Inventory is used to check and control stock.

### 5.1 Inventory Availability

Use this for a searchable stock list.

Filters:

- warehouse
- bin/rack
- item
- batch
- serial

Output:

- item, warehouse, bin, batch, serial, on-hand quantity, unit cost, and value

What to check:

- GRN increases stock.
- Dispatch and posted service MRN reduce stock.
- Transfers move stock between warehouses without changing total company stock.

### 5.2 On Hand

Use this when you need the exact balance for one item or location.

What to check:

- Item balance by warehouse
- Item balance by batch
- Item balance by warehouse + batch

### 5.3 Stock Adjustment

Use when physical count differs from system quantity.

What to input:

| Field | Example |
| --- | --- |
| Warehouse/bin | Counted location |
| Item | Counted item |
| System quantity | System shows automatically |
| Counted quantity | Physical counted quantity |
| Reason | Count correction reason |

Output:

- Posting updates only the variance.

What to check:

- Draft adjustment does not affect stock.
- Posted adjustment appears in stock ledger.

### 5.4 Stock Transfer

Use when moving stock between warehouses or bins.

What to check:

- Source decreases.
- Destination increases.
- Total company stock remains the same.

## 6. Sales

Sales controls quote to cash.

Common flow:

1. Quote
2. Sales order
3. Dispatch or direct dispatch
4. Sales invoice
5. Customer payment
6. Customer return if required

### 6.1 Quote

Use quote when giving a price before the customer confirms.

Output:

- Draft quote can be edited.
- Sending marks it as sent.

What to check:

- Price, discount, tax, and total are correct.
- PDF is correct.

### 6.2 Sales Order

Use sales order when the customer confirms.

Output:

- Confirmed order can be dispatched.

What to check:

- Customer, item, quantity, and price are correct.
- Stock availability is checked before dispatch.

### 6.3 Dispatch And Direct Dispatch

Dispatch issues stock to the customer. Direct dispatch can be used for AOD/direct delivery.

Output:

- Posting reduces stock.
- Serialized equipment items can create customer equipment units automatically.

What to check:

- Correct serial numbers were issued.
- Stock reduced from the correct warehouse.
- Equipment ownership is correct if serialized equipment was dispatched.

### 6.4 Sales Invoice And Payment

Posting a sales invoice creates AR. Payment allocation reduces AR.

What to check:

- Invoice appears in AR.
- Aging report includes the invoice until paid.
- Payment allocation reduces outstanding balance.

### 6.5 Customer Return

Use when customer returns goods.

Output:

- Posting increases stock and creates customer credit note.

What to check:

- Stock returned to the correct warehouse.
- Credit note is available for allocation.

## 7. Service

The Service module handles customer equipment, repair/service jobs, daily field records, billable job sheets, materials, expenses, estimates, handovers, billing, costs, and closeout.

The most important service rule:

**Open one job order for the customer equipment problem. Then use the job tabs to record each day of work, labour, materials, expenses, customer approval, handover, billing, and closeout.**

### 7.1 Service Menu Areas

| Screen | Purpose |
| --- | --- |
| Command Center | Supervisor view of active jobs, overdue jobs, missing daily sheets, finance blockers, billing readiness, and closeout blockers |
| Dispatch Board | Operational lane view for unassigned, active, waiting, and completed jobs |
| Technician Workbench | Technician daily view for assignments, open daily sheets, and quick actions |
| Equipment Units | Customer-owned machines/equipment that can receive service jobs |
| Service Contracts | AMC, SLA, warranty extension, and coverage information |
| Job Orders | Main service job record |
| Daily Work inside job | Daily sheets, staff/labour attendance, progress |
| Job Sheets / Work Orders | Billable/costed labour and time entries |
| Material Requisitions | MRNs used to issue parts to the job |
| Expense Claims / IOUs | Advances, petty cash, and reimbursement claims |
| Estimates | Customer quotations and change orders |
| Service Taken / Handovers | Customer handover and final service confirmation |
| Quality Checks | Inspection or QC records |
| Costs | Job profitability and actual cost review |

### 7.2 Equipment Units

Use `Service -> Equipment Units` before opening a job.

An equipment unit represents one customer-owned machine or unit. It usually has:

- linked item/model
- serial number
- customer owner
- site/location
- warranty end date
- warranty coverage
- service interval or next service date

What to input:

| Field | Example |
| --- | --- |
| Mode | Existing item or outside equipment |
| Item/model | Generator, compressor, hydraulic machine |
| Serial number | `GEN-001` |
| Customer | Equipment owner |
| Warranty details | Coverage and end date |

Output:

- Equipment becomes selectable when creating a service job.

What to check:

- Customer is correct.
- Serial number is unique.
- Warranty/contract coverage is correct before opening the job.

### 7.3 Service Contracts

Use contracts for AMC, SLA, or warranty extension coverage.

What to input:

| Field | Example |
| --- | --- |
| Customer equipment | Selected equipment unit |
| Contract type | AMC, SLA, Warranty Extension |
| Coverage | Inspection, Labor, Parts, Labor and Parts |
| Start/end dates | Contract period |

Output:

- Job entitlement can be calculated from active contract coverage.

What to check:

- Contract dates cover the job date.
- Coverage type is correct.
- If contract is added after job creation, use `Refresh Entitlement` on the job.

### 7.4 Job Orders

A job order is the main service document. It represents one customer service, repair, PDI, warranty, or inspection job.

Create a job from `Service -> Jobs` using `+ New Job Order`.

What to input:

| Field | Example |
| --- | --- |
| Equipment unit | Customer machine |
| Customer | Defaults from equipment when available |
| Job type | Service, Repair, PDI, Warranty, Inspection |
| Site/location | Customer site or workshop |
| Responsible officer | Service coordinator/supervisor |
| Complaint/problem | What the customer reported |
| Expected date | Planned completion or visit date |

Output:

- Job number is generated.
- Job appears in service queues.
- Entitlement is checked using contract first, then warranty.

What to check:

- Equipment and customer are correct.
- Job status is correct.
- Entitlement source and coverage are correct.
- Creator or responsible user receives the expected notification when workflow status changes.

### 7.5 Job Detail Page

Open the job to use the detail page.

Main areas:

| Area | Purpose |
| --- | --- |
| Header | Job number, status, type, equipment, customer, site, responsible officer, action buttons |
| Overview | Cockpit summary and process timeline |
| Plan | Planned operations or repair stages |
| Daily Work | Daily sheets, daily staff/labour, daily progress |
| Materials | MRNs, material returns, damage/rejection |
| Expenses | IOUs, petty cash, reimbursement claims |
| Billing | Closeout readiness, entitlement, estimates, invoices |
| Costs | Actual cost, quoted revenue, invoice revenue, profitability |
| Files & Notes | Comments and attachments |

What to check:

- Use `Show dates & details` for less-used date fields.
- Use the process timeline to jump to a job section.
- Once execution starts, do not edit operational work from the header. Use the correct tab.

### 7.6 Daily Sheets

A daily sheet is the daily field record for one job on one working day or site visit.

Use it to answer:

- What was planned today?
- What was completed today?
- What is pending?
- What problems or site conditions were found?
- Who worked on this job today?
- Were materials, expenses, IOUs, or progress updates recorded for this day?

Create one daily sheet for each working day.

What to input:

| Field | Example |
| --- | --- |
| Work date | Today or site visit date |
| Prepared by | Service supervisor/technician |
| Planned work | What was expected for the day |
| Completed work | What was completed |
| Pending/issues | Remaining work or blockers |
| Site condition | Customer site/workshop condition |
| Notes | Any special instructions |

Output:

- Daily sheet card appears in the job.
- Related daily staff, progress, MRN, return, expense, and IOU counts are shown.
- Daily sheet can be submitted for approval.

What to check:

- There should normally be one sheet for each working day.
- Staff and progress require a daily sheet first.
- Draft/submitted daily sheets block job closeout.
- Approved daily sheets are locked for normal correction.

Daily sheet statuses:

| Status | Meaning |
| --- | --- |
| Draft | Created but not submitted |
| Submitted | Sent for supervisor approval |
| Approved | Accepted and locked |
| Rejected | Returned for correction |

### 7.7 Daily Staff / Labour

Daily staff/labour is attendance and daily work allocation linked to a daily sheet.

Use it to record:

- who attended the job that day
- normal/overtime hours for supervision
- what each person did
- technician or helper notes

This is not the same as billable job-sheet labour.

Output:

- Daily sheet staff count increases.
- Supervisors can see who worked on the job that day.

What to check:

- A daily sheet must exist first.
- The correct date and daily sheet are selected.
- This entry alone should not create final invoice labour.

### 7.8 Daily Progress

Daily progress records the service situation for the day.

Use it to record:

- completed work
- pending work
- problems found
- parts required
- customer instructions
- site issues
- supervisor or technician remarks

Output:

- Latest progress appears in the job cockpit and service queues.
- Daily sheet progress count increases.

What to check:

- Progress is linked to the correct daily sheet.
- Important blockers are clear enough for supervisors to act.

### 7.9 Daily Sheets Vs Job Sheets / Work Orders

This is a key difference.

| Daily Sheet / Daily Staff | Job Sheet / Work Order |
| --- | --- |
| Daily operational record | Billable/costed labour record |
| One per working day or site visit | One or more per job/task depending on labour billing |
| Shows what happened today | Shows work/time used for costing and invoicing |
| Records attendance, progress, issues, materials, IOUs, expenses for the day | Records time entries, labour cost, billing rate, approval, invoice status |
| Helps supervisor answer: "What happened today?" | Helps finance/sales answer: "What labour should be charged or costed?" |
| Does not by itself create final billable labour | Approved billable entries can feed invoices |

Simple example:

1. Technician visits customer today.
2. Create a daily sheet for today's visit.
3. Add the technician under daily staff/labour so attendance is recorded.
4. Add progress explaining the fault and work done.
5. If 3 hours should be costed or billed, create a job sheet/work order time entry for those 3 hours.

### 7.10 Job Sheets / Work Orders

Use `Service -> Job Sheets / Work Orders` for labour costing and customer billing support.

What to input:

| Field | Example |
| --- | --- |
| Service job | Selected job |
| Technician | Technician master record |
| Work date | Labour date |
| Hours | Normal/billable hours |
| Cost rate | Technician cost |
| Billing rate | Customer billing rate |
| Notes | Work performed |

Output:

- Labour entry can move through draft, submitted, approved/rejected, invoiced.
- Approved labour feeds job costing.
- Approved billable labour can feed service billing.

What to check:

- Technician rate is correct.
- Billable/non-billable decision is correct.
- Warranty or contract coverage may reduce effective billing.
- Approved labour appears in Costs and Billing sections.

### 7.11 Plan

Use the Plan tab for planned operations or repair stages.

Planning does not reduce stock and does not create labour cost.

Use it for:

- repair step planning
- subassembly planning
- expected parts
- expected labour hours
- due dates

What to check:

- Actual stock is issued only through MRN.
- Actual billable labour is entered through work orders/job sheets.

### 7.12 Materials And MRNs

Use MRNs to issue spare parts or materials from inventory to a job.

What to input:

| Field | Example |
| --- | --- |
| Job | Service job |
| Warehouse/bin | Stock source |
| Item | Spare part |
| Quantity | Issue quantity |
| Batch/serial | Required for tracked items |

Output:

- Draft MRN is created first.
- Posting MRN reduces stock and updates job material cost.

What to check:

- Draft MRN does not affect stock.
- Posted MRN reduces stock from the correct warehouse/bin.
- Serialized items use correct serial numbers.
- Issued materials are later marked used, returned, damaged, or rejected where required.

### 7.13 Material Returns, Damage, And Rejection

Use material disposition when parts issued to the job were not fully consumed.

Examples:

- unused part returned to stores
- wrong item issued
- customer rejected a part
- part damaged during job
- supplier/manufacturer issue found

What to check:

- Returned material goes back to stock only when posted through the correct return flow.
- Damaged/rejected material is visible for closeout and reporting.
- Job closeout should not allow unresolved material disposition.

### 7.14 IOUs, Petty Cash, And Expense Claims

Service expenses are split into three flows.

| Flow | Use when |
| --- | --- |
| IOU / Employee Advance | Employee needs money before spending or before final receipts are ready |
| Petty Cash Voucher | Company petty cash was used for the job |
| Out-Of-Pocket Claim | Employee paid personally and needs reimbursement |

What to check:

- IOU requester and finance approvers receive notifications.
- Approved IOU can be released.
- Settled IOU clears the advance.
- Petty cash and reimbursement claims follow finance approval/settlement.
- Pending finance documents block closeout where required.

### 7.15 Estimates / Quotations

Use estimates when customer approval is required before repair or extra work.

Estimate lines can include:

- parts
- labour
- billable expenses

What to check:

- Draft estimate can be edited.
- Sending sets customer approval to pending.
- Customer approval/rejection is recorded.
- If scope changes after approval, create a change order instead of overwriting the approved estimate.
- Warranty or contract coverage can make covered lines zero charge.

### 7.16 Service Taken / Handover

Use handover when service is completed and returned or confirmed with the customer.

What to input:

| Field | Example |
| --- | --- |
| Handover date | Completion date |
| Customer acknowledgement | Customer confirmation |
| Returned items | Any returned parts/items |
| Post-service warranty | Warranty months if applicable |
| Notes | Final remarks |

Output:

- Service taken record is linked to the job.
- Billing/final invoice path can continue where required.

What to check:

- Customer acknowledgement is captured.
- Returned items are recorded.
- Handover status is correct before closing.

### 7.17 Billing And Closeout

Open the job Billing tab before closing.

Closeout readiness checks blockers such as:

- draft or submitted daily sheets
- pending IOUs
- pending expense claims
- draft MRNs
- open labour entries
- unresolved material returns/damage/rejection
- missing final invoice decision

What to check:

- Each blocker is cleared.
- Entitlement is correct.
- Estimate/invoice status is correct.
- Costs are reviewed.
- Job can be closed only after required steps are completed.

### 7.18 Costs

The Costs tab shows:

- material cost
- direct purchase cost
- approved labour cost
- approved expense claims
- quoted revenue
- posted invoice revenue
- profitability view

What to check:

- Posted MRNs are included.
- Approved labour is included.
- Settled/approved expense claims are included according to workflow.
- Invoice revenue matches final billing.

### 7.19 Recommended End-To-End Service Flow

1. Create or confirm the customer equipment unit.
2. Create or confirm service contract/warranty details.
3. Create a job order.
4. Review entitlement or click `Refresh Entitlement`.
5. Start the job.
6. Plan operations if needed.
7. Create a daily sheet for the first working day.
8. Add daily staff/labour attendance.
9. Add daily progress.
10. Request IOU or enter expense if needed.
11. Create and post MRN if parts are used.
12. Record material return/damage/rejection if required.
13. Create job sheet/work order labour for billable/costed hours.
14. Create estimate if customer approval is required.
15. Complete the job when technical work is finished.
16. Create service taken/handover.
17. Review Billing and Costs.
18. Clear closeout blockers.
19. Close the job.

## 8. Finance

Finance controls AR, AP, payments, credit/debit notes, petty cash, IOUs, allocations, release, and settlement.

### 8.1 Accounts Receivable

AR shows customer outstanding amounts.

What creates AR:

- posted sales invoices
- posted service invoices where applicable
- debit notes

What reduces AR:

- payment allocation
- customer credit note allocation

What to check:

- Customer aging is correct.
- Allocations reduce outstanding balance.
- Invoice PDF and AR total match.

### 8.2 Accounts Payable

AP shows supplier outstanding amounts.

What creates AP:

- posted supplier invoices
- applicable purchase documents

What reduces AP:

- supplier payment allocation
- supplier credit note allocation

### 8.3 Payments And Allocations

What to input:

| Field | Example |
| --- | --- |
| Direction | Incoming or outgoing |
| Counterparty | Customer or supplier |
| Payment type | Cash, bank transfer, cheque |
| Currency/rate | Currency and exchange rate |
| Amount | Payment amount |

Output:

- Payment is created.
- Allocation applies it to AR/AP documents.

What to check:

- Outstanding balance reduces only after allocation.
- Audit log shows who created and allocated.

### 8.4 IOUs And Petty Cash

IOU flow:

1. Request IOU.
2. Approver reviews.
3. Finance approves or rejects.
4. Cash is released.
5. Employee submits receipts/settlement.
6. Finance settles or clears.

What to check:

- Approvers receive notifications.
- Requester can track status.
- Settlement updates balances.
- Open IOUs appear as blockers where relevant.

## 9. Admin, Access, And Notifications

### 9.1 User Access Management

Admin can create users and assign permissions.

Permissions may include:

- View
- Create
- Edit
- Delete
- Submit
- Approve
- Reject
- Post
- Allocate
- Release
- Settle
- Close

What to check:

- A view-only user can open allowed pages but cannot create/edit/post.
- A creator can create drafts but cannot approve if approve permission is not granted.
- An approver can see approval requests for their permitted documents.
- Backend blocks unauthorized actions even if the user opens a URL manually.

### 9.2 Notifications

Notifications are used when a document needs attention.

Examples:

| Action | Who should be notified |
| --- | --- |
| PR submitted | Users with PR approve rights |
| PO approved/rejected | Creator or responsible user |
| IOU requested | IOU approvers/finance approvers |
| IOU approved/released/settled | Requester and related finance users |
| Service job status changes | Responsible service users |
| Payment allocated | Related creator/responsible users where applicable |

If multiple users have the same approval right, all of them should see the request notification. When one authorized user completes the action, the document status should change for everyone.

What to check:

- Notification title is understandable.
- Notification link opens the correct document.
- Notification can be marked read.
- Unauthorized users do not receive actionable approval links.

## 10. Reporting

Reports confirm the effect of posted transactions.

| Report | Use |
| --- | --- |
| Stock Ledger | Every stock receipt, issue, adjustment, and transfer |
| Inventory Availability | Current stock by warehouse/bin/batch/serial |
| Aging | Customer AR and supplier AP outstanding |
| Tax Summary | Tax totals from posted documents |
| Service KPIs | Job volume, status, and service performance |
| Sales Analysis | Customer/item sales totals |
| Purchase Analysis | Supplier/item purchase totals |
| Supplier Performance | Supplier purchasing activity |
| Costing | Inventory value and cost |

What to check:

- Draft documents do not appear as posted results.
- Posted transactions match report values.
- Date filters are correct.
- Export/PDF output matches screen totals.

## 11. Audit Logs

Audit logs show who did what and when.

Use audit logs to check:

- create
- edit
- delete
- submit
- approve/reject
- post
- allocate
- settle
- close

Audit logs are important when investigating wrong postings, permission issues, or user questions.

## 12. End-To-End Training Examples

### 12.1 Purchase To Sales Payment

1. Create or confirm supplier, customer, item, warehouse, tax, and currency.
2. Create purchase requisition.
3. Submit and approve purchase requisition.
4. Create purchase order.
5. Approve purchase order.
6. Create goods receipt.
7. Post goods receipt.
8. Check inventory increased.
9. Create sales order.
10. Confirm sales order.
11. Create dispatch.
12. Post dispatch.
13. Check inventory reduced.
14. Create sales invoice.
15. Post sales invoice.
16. Check AR increased.
17. Create incoming payment.
18. Allocate payment to invoice.
19. Check AR outstanding reduced.
20. Check stock ledger, aging, reports, notifications, PDFs, and audit logs.

### 12.2 Full Service Job

1. Create or confirm equipment unit.
2. Create or confirm contract/warranty.
3. Create job order.
4. Start job.
5. Create daily sheet.
6. Add daily staff/labour attendance.
7. Add daily progress.
8. Create IOU or expense if needed.
9. Create and post MRN if parts are used.
10. Create work order/job sheet labour.
11. Approve billable labour.
12. Create estimate if customer approval is needed.
13. Complete job.
14. Create service taken/handover.
15. Review billing and costs.
16. Clear blockers.
17. Close job.
18. Check reports and audit logs.

## 13. Common User Questions

| Question | Answer |
| --- | --- |
| Why is a menu missing? | Your user may not have access. Ask Admin to check permissions. |
| Can I edit a posted document? | Usually no. Use the correct correction transaction such as return, credit note, adjustment, or reversal flow. |
| Does a draft affect stock or finance? | No. Stock/finance changes after posting, approval, allocation, settlement, or closeout depending on the document. |
| Should I create a daily sheet or job sheet? | Create a daily sheet for each working day. Use job sheet/work order for costed or billable labour. |
| Does daily staff create an invoice? | No. Daily staff records attendance. Approved billable work-order time entries can feed invoicing. |
| Why can't I close a job? | Open Billing -> Closeout Readiness and clear the blockers. |
| Who receives approval notifications? | Users who have the approval permission for that document type. |
| Where do I check stock after posting? | Inventory Availability, On Hand, and Stock Ledger. |
| Where do I check customer outstanding? | Finance -> AR and Aging reports. |
| Where do I check job profit? | Service job Costs tab. |

## 14. Trainer And Tester Checklist

For every module, check:

- create works
- edit works only while allowed
- validation messages are clear
- submit/approve/post/status actions work
- notifications go to the correct users
- unauthorized users are blocked
- PDFs open where expected
- reports match posted documents
- audit logs show the action
- refresh/logout/login does not lose saved data
