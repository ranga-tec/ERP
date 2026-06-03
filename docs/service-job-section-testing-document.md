# Service Job Section Full Testing Document

Date prepared: 2026-06-03

This document is the full manual testing guide for the ISS ERP Service Job section. It covers service jobs, equipment units, command center, dispatch board, technician workbench, daily sheets, staff/labour, progress, job sheets/work orders, materials, material returns, damage/rejection handling, IOUs, petty cash, out-of-pocket claims, accounts, estimates, service taken, manual invoice creation, final invoices, costs, closeout, files, and audit checks.

Use this document when the Service module must be proven accurate before release or customer demonstration.

## 1. Testing Rules

- Test from the UI first. Use the database only to confirm values when a UI page does not expose the full detail.
- Use one test job from start to closeout unless the step explicitly says to create another job.
- Record the job number, daily sheet number, MRN number, IOU number, claim number, quotation number, handover number, and invoice number as soon as they are created.
- Every created document must remain visible after save, refresh, logout/login, and navigation away/back.
- No transaction should disappear from the user after requesting or submitting it.
- Draft records must not update stock, cash, AR, AP, or job closeout as final transactions.
- Posted, approved, released, settled, invoiced, or closed records must update the related status and totals.
- If a validation error occurs, the message must clearly tell the tester what is missing or wrong.

## 2. Roles To Test

| Role | Main responsibility | Required checks |
| --- | --- | --- |
| Admin | Full test execution and user access | Can access all required menus and recover/reopen where allowed |
| Service | Job creation, execution, daily sheets, materials, progress, service taken | Can operate service job workflow without finance-only actions |
| Inventory | MRN posting, stock, returns, damage/rejection visibility | Can verify stock effects and material issue/return behavior |
| Finance | IOU approval/release/settlement, petty cash, claims, invoices, AR, accounts | Can approve and settle finance documents |
| Sales | Customer-facing invoice and credit checks where applicable | Can view invoice/AR impact where allowed |
| Reporting | KPI, stock, costing, and service reports | Can confirm final values in reports |

Minimum role test:

| Test | Expected result |
| --- | --- |
| Service user opens `Service -> Jobs` | Allowed |
| Service user opens `Finance -> Petty Cash IOUs` | Allowed for job-linked IOU status/operation if configured |
| Inventory user opens MRN/detail | Allowed |
| Finance user approves/settles IOU and claims | Allowed |
| Unauthorized user opens admin-only page | Blocked or redirected |

## 3. Environment Health Check

Run these before deep testing.

| Check | Where | Expected result |
| --- | --- | --- |
| Frontend | `http://127.0.0.1:3000/login` | Login page loads without CSS or JS 500 errors |
| API | `/api/auth/capabilities` through frontend | Returns 200 when app is healthy |
| Login | Admin credentials | Login succeeds |
| Sidebar | Service menu | Service menus appear, including `Help` inside Service section |
| Help page | `Service -> Help` | Rendered manual loads with screenshots |
| Static assets | Browser console | No failed `_next/static` CSS/JS assets |
| Browser console | Any service page | No repeated application errors |

If CSS or JS assets return 500, rebuild and restart the frontend before continuing.

## 4. Test Data

Use stable test data so expected stock, cash, and account values can be verified.

### 4.1 Master Data

| Data type | Code/name | Required values |
| --- | --- | --- |
| Customer | `CUS-SVC-001` | Service test customer |
| Supplier | `SUP-SVC-001` | Spare parts supplier |
| Warehouse | `MAIN` | Main stock warehouse |
| Warehouse bin | `MAIN-A1` | Active bin under `MAIN` if bins are enabled |
| Item category | `SPARES` | Spare parts |
| Item category | `SUNDRIES` | Grease, lubricants, consumables, small shop supplies |
| Item | `SKU-CORE` | Stock item, category `SPARES`, average cost `5.00` |
| Item | `SKU-SERIAL` | Serialized stock item, category `SPARES`, average cost `25.00` |
| Item | `SUN-GREASE` | Stock or non-stock item, category `SUNDRIES`, unit price/cost as configured |
| Item | `LAB-SVC` | Labour/service item if invoice labour uses item selection |
| Tax | `ZERO` | 0% tax for simple calculation |
| Petty cash fund | `WORKSHOP` | Active, enough balance for advances and settlements |
| Payment type | `Cash` | Active |
| Payment type | `Bank Transfer` | Active |
| Technician | `TECH1` | Cost rate `10.00`, billing rate `25.00` |

### 4.2 Opening Stock Required

Create or confirm stock before testing service material issue.

| Item | Warehouse/bin | Qty | Serial | Unit cost |
| --- | --- | ---: | --- | ---: |
| `SKU-CORE` | `MAIN` / `MAIN-A1` | `10` | blank | `5.00` |
| `SKU-SERIAL` | `MAIN` / `MAIN-A1` | `2` | `SER-SVC-001`, `SER-SVC-002` | `25.00` |
| `SUN-GREASE` | `MAIN` / `MAIN-A1` | `5` | blank | as configured |

Expected starting stock:

| Check | Expected result |
| --- | --- |
| `Inventory -> On Hand`, `SKU-CORE` | MAIN has `10` |
| `Inventory -> Availability`, `SER-SVC-001` | Available |
| `Inventory -> Availability`, `SER-SVC-002` | Available |
| `Inventory -> On Hand`, `SUN-GREASE` | MAIN has at least `5` if stocked |

## 5. Test Run Control Sheet

Fill this during testing.

| Document | Number / ID | Status at end | Notes |
| --- | --- | --- | --- |
| Equipment unit |  | Active |  |
| Service job |  | Closed or Reopened |  |
| Daily sheet |  | Approved |  |
| MRN |  | Posted |  |
| Material return |  | Posted |  |
| Damage/rejection record |  | Posted |  |
| IOU |  | Settled / Rejected / Cancelled |  |
| Petty cash voucher |  | Settled |  |
| Out-of-pocket claim |  | Settled |  |
| Job sheet / work order |  | Done |  |
| Labour entry |  | Approved |  |
| Quotation |  | Approved or skipped |  |
| Service taken / handover |  | Completed |  |
| Final invoice |  | Posted |  |
| Customer payment |  | Posted if tested |  |

## 6. Service Menu And Navigation Smoke Test

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Open `Service -> Command Center` | Metrics/cards load without server error |
| 2 | Open `Service -> Dispatch Board` | Job lanes load |
| 3 | Open `Service -> Technician Workbench` | Technician daily work page loads |
| 4 | Open `Service -> Equipment Units` | Equipment list loads before create form |
| 5 | Open `Service -> Jobs` | Job list loads before create form |
| 6 | Open `Service -> Job Sheets / Work Orders` | Work order list loads |
| 7 | Open `Service -> Material Requisitions` | MRN list loads |
| 8 | Open `Service -> Petty Cash` or expense claims page | Service expense list loads |
| 9 | Open `Service -> Service Taken` | Handover list loads |
| 10 | Open `Service -> Help` | Full rendered help page opens, not raw markdown |

UI rule:

| Check | Expected result |
| --- | --- |
| First screen view | Primary list/status appears first |
| Create buttons | Open modal dialogs or clear on-demand forms |
| Edit buttons | Open modal dialogs where the feature has been converted to the modal pattern |
| Long forms | Do not push important existing records below the first view |

## 7. Equipment Unit Test

Create one equipment unit before opening the job.

| Field | Input |
| --- | --- |
| Mode | Existing item |
| Equipment item | Use a generator/machine item, or create `EQP-GEN` if required |
| Serial number | `GEN-SVC-001` |
| Customer | `CUS-SVC-001` |
| Site/location | `Customer workshop` |
| Warranty coverage | Labour and parts if available |

Expected:

| Check | Where | Expected result |
| --- | --- | --- |
| Save | Equipment unit page | Equipment saves successfully |
| List | Equipment unit list | `GEN-SVC-001` appears |
| Customer link | Equipment detail/list | Customer is `CUS-SVC-001` |
| Warranty/contract fields | Equipment detail | Coverage data is visible if entered |
| Edit | Equipment row/detail | Editable fields save and remain after refresh |

Negative tests:

| Test | Expected result |
| --- | --- |
| Duplicate serial number | Clear duplicate validation |
| Missing customer | Required validation if customer is mandatory |
| Inactive item/customer | Cannot use, or clear warning appears |

## 8. Create Job Order

Open `Service -> Jobs`.

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Click `+ New Job Order` | Create modal opens in front of the list |
| 2 | Select equipment unit `GEN-SVC-001` | Customer defaults or validates to `CUS-SVC-001` |
| 3 | Enter job type `Repair` | Accepted |
| 4 | Enter customer requirement `Generator does not start` | Accepted |
| 5 | Enter job description `Repair and test generator starting system` | Accepted |
| 6 | Enter intake note `Starter control suspected` | Accepted |
| 7 | Save | Modal closes or confirms success; job row appears |

Expected:

| Check | Where | Expected result |
| --- | --- | --- |
| Job list | `Service -> Jobs` | New job appears without needing browser reload |
| Job status | List/detail | `Open` or correct initial status |
| Job number | List/detail | Generated job number appears |
| View | Job number/View action | Opens job detail |
| Edit from list | Editable job row | Opens edit modal directly, not forced to detail page first |
| Header data | Job detail | Equipment, customer, type, requirement, and notes are correct |

Negative tests:

| Test | Expected result |
| --- | --- |
| Save without equipment/customer | Clear required-field validation |
| Invalid date order | Clear date validation |
| Edit after job starts | Header editing blocked or limited according to status |

## 9. Job Detail UI And Tab Navigation

Open the created job.

| Area | Expected result |
| --- | --- |
| Header | Compact header with job number, status, type, equipment, customer, and action buttons |
| Dates/details | Secondary dates hidden behind `Show dates & details` |
| Actions | Start/Complete/Close/Reopen/Refresh Entitlement shown compactly |
| Tabs | `Overview`, `Plan`, `Daily Work`, `Materials`, `Expenses`, `Billing`, `Costs`, `Files & Notes` |
| Process timeline | Stage cards show status/count metadata |
| Timeline click | Opens/focuses the relevant tab area |
| Tab URL | Uses tab query and `#tab-content` anchor |
| First view | Current tab content is visible without a long scroll through unrelated forms |

Test each tab:

| Tab | Expected result |
| --- | --- |
| Overview | Cockpit and process timeline visible |
| Plan | Operation table visible; add action available |
| Daily Work | Daily sheet list/empty state visible first |
| Materials | Issued MRNs/return/damage areas visible |
| Expenses | IOU, petty cash, out-of-pocket workflows separated |
| Billing | Closeout readiness, entitlement, quotation/invoice trail |
| Costs | Cost summary and sources |
| Files & Notes | Comments and attachments |

## 10. Start Job

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Click `Start` | Confirmation/action succeeds |
| 2 | Refresh page | Status remains `In Progress` |
| 3 | Try to edit protected header fields | Blocked or disabled according to workflow |

Expected:

| Check | Expected result |
| --- | --- |
| Job status | `In Progress` |
| Command center | Job appears in active jobs |
| Dispatch board | Job appears in active/assigned or correct lane |
| Technician workbench | Job appears when assigned or daily sheet exists |

## 11. Plan Operations / Sub-Parts

Open `Plan`.

Create operation:

| Field | Input |
| --- | --- |
| Step no. | `10` |
| Work step / subassembly | `Diagnose starting system` |
| Required by | Tomorrow |
| Planned part | `SKU-SERIAL` |
| Planned qty | `1` |
| Labour hours | `2` |
| Description | `Check starter control circuit` |
| Notes | `Warranty coverage expected` |

Expected:

| Check | Expected result |
| --- | --- |
| Operation row | Appears in operations table |
| Planned stock | Does not reduce inventory |
| Start operation | Status becomes `In Progress` |
| Complete operation later | Status becomes `Completed` after actual work is recorded |
| Cost impact | No cost until actual MRN/labour/expense is posted or approved |

Negative tests:

| Test | Expected result |
| --- | --- |
| Missing work step | Required validation |
| Negative planned qty/hours | Rejected |
| Closed job operation update | Blocked |

## 12. Daily Field Sheet

Open `Daily Work -> Daily Sheets`.

Create daily sheet:

| Field | Input |
| --- | --- |
| Date/time | Today |
| Prepared by | Signed-in user or service supervisor |
| Site/location | `Customer workshop` |
| Shift | `Day` |
| Site condition | `Equipment received at workshop` |
| Planned work | `Diagnose generator starting system` |
| Completed work | `Initial inspection completed` |
| Pending work | `Issue parts and complete repair` |
| Problems found | `Control board output weak` |

Expected:

| Check | Where | Expected result |
| --- | --- | --- |
| Daily card | Daily Work | New daily sheet appears |
| Status | Daily card | `Draft` or correct initial status |
| Counts | Daily card | Staff/progress/MRN/return/expense/IOU counts start at `0` |
| Quick actions | Daily card | Links for staff, progress, materials, IOU, expense |
| Refresh | Browser refresh | Daily sheet remains |
| Command center | Command Center | Missing daily sheet count updates correctly |

Negative tests:

| Test | Expected result |
| --- | --- |
| Daily Staff/Labour before selecting sheet | Clean empty-state message, not disabled confusing form |
| Progress before selecting sheet | Clean empty-state message |
| Duplicate same-day sheet | Allowed or blocked based on business rule; observed behavior must be documented |

## 13. Daily Staff / Labour

Daily Staff / Labour is the attendance/daily operational record. It does not by itself create invoice labour.

Open `Daily Work -> Staff / Labor`.

Add daily assignment:

| Field | Input |
| --- | --- |
| Daily sheet | Daily sheet created above |
| Technician | `TECH1` |
| Manual employee name | Blank when technician is selected |
| Role | `Technician` |
| Assigned task | `Diagnose starting system and replace required parts` |
| Normal hours | `2` |
| Overtime hours | `0` |
| Daily work description | `Diagnosis and replacement completed` |

Expected:

| Check | Expected result |
| --- | --- |
| Assignment row | Visible in staff/labour register |
| Daily sheet staff count | Increases |
| Approval | Assignment can be submitted/approved if workflow requires it |
| Job cost | Does not create final billable labour unless work-order labour also exists |
| Audit | Created/approved user and time are traceable where available |

Negative tests:

| Test | Expected result |
| --- | --- |
| No technician and no manual name | Validation |
| Negative hours | Validation |
| Closed job | Cannot add labour |

## 14. Daily Progress

Open `Daily Work -> Progress`.

Add progress update:

| Field | Input |
| --- | --- |
| Daily sheet | Daily sheet created above |
| Work completed | `Starting system diagnosed and replacement parts installed` |
| Work pending | `Final customer confirmation` |
| Problems found | `Weak control board output` |
| Additional parts required | `None` |
| Additional labour required | `None` |
| Customer instructions | `Call before delivery` |
| Technician notes | `Test run completed` |
| Supervisor notes | `Ready for invoice review` |

Expected:

| Check | Expected result |
| --- | --- |
| Progress register | New progress row appears |
| Daily sheet count | Progress count increases |
| Overview cockpit | Latest progress updates from `None` to recent progress summary |
| Command center | Missing progress metric clears for this job/day |
| Refresh | Progress remains visible |

## 15. Job Sheet / Work Order Labour

Job Sheets / Work Orders Labour is the billable/costed labour source. It is different from Daily Staff / Labour.

Open `Service -> Job Sheets / Work Orders`.

Create job sheet/work order linked to the same job.

| Field | Input |
| --- | --- |
| Service job | Current job |
| Technician/owner | `TECH1` or responsible staff |
| Description | `Generator repair labour` |

Open the work order detail. Start it, then add labour:

| Field | Input |
| --- | --- |
| Technician | `TECH1` |
| Hours worked | `2` |
| Cost rate | `10.00` |
| Billing rate | `25.00` |
| Billable | Yes |

Submit and approve the labour entry. Mark the work order done.

Expected:

| Check | Where | Expected result |
| --- | --- | --- |
| Work order status | Work order detail | `Open -> In Progress -> Done` |
| Labour entry status | Labour table | `Draft -> Submitted -> Approved` |
| Labour cost | Work order/costs | `2 x 10 = 20.00` |
| Billable labour value | Work order/billing | `2 x 25 = 50.00` |
| Job Costs tab | Job detail | Approved labour cost appears |
| Billing/closeout | Job detail | Uninvoiced billable labour is flagged until invoiced or resolved |
| Daily staff count | Daily sheet | Does not increase because this is work-order labour, not attendance |

Negative tests:

| Test | Expected result |
| --- | --- |
| Approve without submit | Blocked or unavailable |
| Negative hours/rates | Validation |
| Closed work order add labour | Blocked |
| Closed job add work order | Blocked |

## 16. Materials / MRN Issue

Open `Materials` from the job or `Service -> Material Requisitions`.

Create MRN from the job Materials tab when possible.

| Field | Input |
| --- | --- |
| Job | Current job |
| Daily sheet | Daily sheet created above |
| Warehouse | `MAIN` |

Open MRN detail and add lines:

| Item | Qty | Serial | Expected |
| --- | ---: | --- | --- |
| `SKU-CORE` | `2` | blank | Saves |
| `SKU-SERIAL` | `1` | `SER-SVC-001` | Saves with serial picker |

Try an invalid over-request before posting:

| Item | Qty | Expected |
| --- | ---: | --- |
| `SKU-CORE` | `999` | Insufficient stock validation |

Post the MRN.

Expected:

| Check | Where | Expected result |
| --- | --- | --- |
| MRN status | MRN detail | `Posted` |
| Job Materials tab | Issued MRNs | MRN appears grouped by MRN number |
| Expanded MRN row | Issued MRNs | Item lines, quantities, daily sheet link visible |
| Daily sheet count | Daily sheet card | MRN count increases |
| `SKU-CORE` stock | Inventory On Hand | `10 - 2 = 8` before returns |
| `SER-SVC-001` | Inventory Availability | No longer available |
| `SER-SVC-002` | Inventory Availability | Still available |
| Material cost | Job Costs | `2 x 5 + 1 x 25 = 35.00` |
| Draft MRN closeout | Closeout readiness | Draft MRNs block closeout, posted MRN does not block as draft |

Negative tests:

| Test | Expected result |
| --- | --- |
| Post MRN with no lines | Blocked |
| Over-request quantity | Blocked |
| Serial item without serial | Blocked |
| Duplicate serial on same or another line | Blocked |
| Issue from inactive warehouse/bin | Blocked or clear validation |

## 17. Material Returns, Damage, And Rejection

Open job `Materials` tab.

### 17.1 Return Unused Material To Stock

Create return draft:

| Field | Input |
| --- | --- |
| Daily sheet | Daily sheet created above |
| Issued MRN line | `SKU-CORE` line |
| Disposition | `Not needed - return to stock` or equivalent |
| Quantity | `1` |
| Charge to | `Company` |
| Condition | `Good` |
| Reason | `Extra part not used` |

Expected before posting:

| Check | Expected result |
| --- | --- |
| Return row | Status `Draft` |
| Inventory | `SKU-CORE` still `8`; no stock increase while draft |
| Edit/void | Draft can be edited or voided |

Post return draft.

Expected after posting:

| Check | Expected result |
| --- | --- |
| Return row | Status `Posted` |
| Inventory | `SKU-CORE` becomes `9` |
| Daily sheet count | Return count increases |
| Closeout readiness | Returned qty counts toward material disposition |

### 17.2 Damage / Non-Returnable Material

Create damage draft:

| Field | Input |
| --- | --- |
| Daily sheet | Daily sheet created above |
| Issued MRN line | `SKU-SERIAL` line |
| Disposition | `Damaged - do not return to usable stock` or equivalent |
| Quantity | `1` |
| Serial | `SER-SVC-001` |
| Charge to | `Warranty` or `Company` |
| Reason | `Failed during final test` |

Post damage draft.

Expected:

| Check | Expected result |
| --- | --- |
| Damage row | Status `Posted` |
| Usable stock | `SER-SVC-001` does not return to available stock |
| Cost trail | Damage remains visible in job material disposition |
| Closeout readiness | Disposition clears for that issued serial/qty |

### 17.3 Rejected / Supplier Return Path

If the system supports a rejected/supplier-return disposition from service material:

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Mark issued material as rejected/supplier-return disposition | Draft saves |
| 2 | Post disposition | Stock returns to a returnable/quarantine location according to configuration |
| 3 | Create supplier return from procurement if required | Supplier return posts and adjusts supplier/AP credit path |
| 4 | Check job | Job still shows rejected material trail |

Negative tests:

| Test | Expected result |
| --- | --- |
| Return more than issued qty | Blocked |
| Return already disposed qty again | Blocked |
| Post draft without reason/disposition | Validation |
| Damaged serial returned to usable stock | Must not happen |

## 18. IOU / Employee Advance

Open job `Expenses -> IOU Advances`.

Create IOU:

| Field | Input |
| --- | --- |
| Daily sheet | Daily sheet created above |
| Requested by | Signed-in system user; should not be free-typed |
| Amount | `20.00` |
| Expected settlement | A future date |
| Purpose | `Travel and parking advance for generator repair` |

Expected immediately:

| Check | Expected result |
| --- | --- |
| Success confirmation | Shows IOU number and says request was sent/waiting for finance |
| Job IOU register | IOU remains visible to requester |
| Status | Submitted/Pending Approval or configured initial status |
| Daily sheet count | IOU count increases |
| Command center | Finance queue includes pending IOU |
| Date storage | No DateTimeOffset timezone error; API returns success |

Finance flow:

| Step | Where | Expected result |
| --- | --- | --- |
| Approve | `Finance -> Petty Cash IOUs` | Status moves to Approved |
| Release | Finance IOU row/detail | Released from selected fund/payment method |
| Settle | Finance IOU row/detail | Status becomes Settled |
| Job refresh | Job Expenses tab | Same IOU visible with latest status |
| Closeout readiness | Billing/Overview | IOU blocker clears only after settled/rejected/cancelled |

Accounting/cash checks:

| Check | Expected result |
| --- | --- |
| Petty cash fund | Balance decreases only when cash is actually released/settled according to implementation |
| Petty cash ledger | Advance/release/settlement entry visible |
| Job costs | IOU alone should not be final job cost unless converted/settled through a claim/expense process |
| Audit | Approver/releaser/settler and timestamps are traceable where available |

Negative tests:

| Test | Expected result |
| --- | --- |
| Zero or negative amount | Blocked |
| Missing purpose | Validation |
| Non-system requester typed manually | Not allowed |
| Settle before approval/release | Blocked |
| Close job with pending IOU | Blocked |

## 19. Petty Cash Expense Voucher

Open job `Expenses -> Petty Cash Expenses`.

Create petty cash voucher:

| Field | Input |
| --- | --- |
| Daily sheet | Daily sheet created above |
| Funding | `Petty Cash` |
| Petty cash fund | `WORKSHOP` if shown |
| Claimed by / receiver | `TECH1` or selected system user/employee |
| Expense date | Today |
| Merchant/vendor | `Test Vendor` |
| Bill number | `BILL-SVC-001` |
| Payment handover method | `Cash handover` |
| Notes | `Cash paid for small workshop consumable` |

Add line if the voucher opens a detail page:

| Description | Qty | Unit cost | Billable |
| --- | ---: | ---: | --- |
| `Parking fee` | `1` | `5.00` | Yes |

Expected:

| Check | Expected result |
| --- | --- |
| Voucher creation | Success and generated number shown |
| Register visibility | Voucher remains visible in job expense register |
| Bill number | Stored and visible |
| Handover method | Cash handover/bank deposit/other value visible |
| Status flow | Draft/submitted/approved/settled according to workflow |
| Daily sheet count | Expense count increases |
| Job cost | Settled/approved expense cost appears in Costs tab |
| Petty cash fund | Balance decreases when settled/paid from fund |
| Petty cash ledger | Voucher/claim settlement entry visible |
| Closeout readiness | Expense blocker clears after settled/rejected/cancelled |

Repeat handover method variants:

| Method | Expected result |
| --- | --- |
| Cash handover | Saved and visible |
| Bank deposit | Saved and visible; bank reference if required |
| Other | Saved with notes/reference |

Negative tests:

| Test | Expected result |
| --- | --- |
| Missing bill number if required by business rule | Validation |
| Missing fund for petty-cash-funded settlement | Validation |
| Settle more than fund balance | Blocked |
| Closed job add voucher | Blocked |

## 20. Out-Of-Pocket Claim

Open job `Expenses -> Out-of-Pocket Claims`.

Create claim:

| Field | Input |
| --- | --- |
| Daily sheet | Daily sheet created above |
| Funding source | `Out of Pocket` |
| Claimed by | `TECH1` or system user/employee |
| Expense date | Today |
| Merchant/vendor | `Test Vendor` |
| Receipt ref | `REC-SVC-001` |
| Notes | `Employee paid personally` |

Add line:

| Description | Qty | Unit cost | Billable |
| --- | ---: | ---: | --- |
| `Parking fee` | `1` | `5.00` | Yes |

Submit, approve, and settle the claim.

Expected:

| Check | Expected result |
| --- | --- |
| Claim status | `Settled` |
| Job expense register | Claim remains visible with status, total, line count |
| Job cost | `5.00` appears in Costs tab |
| Billable-unconverted count | Shows if billable line has not been converted to quotation/invoice |
| Finance settlement | Payment type/reference visible |
| Petty cash fund | Not required unless settled from petty cash |
| Closeout readiness | Expense blocker clears when settled/rejected/cancelled |

Negative tests:

| Test | Expected result |
| --- | --- |
| Submit claim with no lines | Blocked or clear validation |
| Approve without submit | Blocked |
| Settle rejected claim | Blocked |
| Expense line negative qty/cost | Blocked |

## 21. Accounts And Finance Accuracy Checks

This section confirms that service transactions affect accounts and cash correctly.

### 21.1 Petty Cash Fund

Open `Finance -> Petty Cash`.

| Check | Expected result |
| --- | --- |
| Fund `WORKSHOP` exists | Active |
| Opening balance | Visible |
| IOU release/settlement | Ledger entry visible according to workflow |
| Petty cash voucher settlement | Ledger entry visible |
| Balance | Opening balance plus top-ups/adjustments minus paid/settled cash items |
| Overdraw | System blocks payment above available balance, or records clear negative-balance policy if allowed |

### 21.2 Petty Cash IOUs

Open `Finance -> Petty Cash IOUs`.

| Check | Expected result |
| --- | --- |
| Job-linked IOU | Visible with job number and requester |
| Status timeline | Submitted, approved, released, settled/rejected/cancelled visible |
| Requester | System user/person, not arbitrary free text |
| Settlement | Requires valid amount/method/reference where configured |
| Job link | Opens related job or identifies job number |

### 21.3 AR / Customer Account

After final invoice:

| Check | Expected result |
| --- | --- |
| Sales invoice | Posted invoice visible |
| AR/customer account | Customer balance increases by invoice amount |
| Payment receipt | Posting payment reduces customer balance |
| Invoice status | Paid/partially paid according to payment amount |
| Customer statement/AR aging | Includes invoice until paid |

### 21.4 AP / Supplier Account For Rejected Material

If rejected service material leads to supplier return:

| Check | Expected result |
| --- | --- |
| Supplier return | Posted |
| Supplier/AP credit | Supplier credit/AP adjustment created according to system terminology |
| Inventory | Supplier return removes returned stock from warehouse/quarantine |
| Job | Rejected material remains traceable to the job |

### 21.5 Chart Of Accounts / Ledger Sanity

Where ledger pages expose the detail, verify:

| Transaction | Expected account direction |
| --- | --- |
| Posted service invoice | AR debit / revenue credit according to accounting setup |
| Customer payment | Cash/bank debit / AR credit |
| Petty cash top-up | Petty cash debit / source cash-bank credit |
| Petty cash voucher settlement | Expense or clearing debit / petty cash credit |
| IOU advance | Employee advance/IOU clearing and petty cash movement according to configured workflow |
| Supplier return | AP/credit note and inventory/return accounting according to configured workflow |

If the UI does not expose journal entries, verify through AR/AP, petty cash ledger, invoice status, and reporting totals.

## 22. Quotation / Estimate

Open `Service -> Quotations`.

Create quotation linked to current job.

| Kind | Item | Description | Qty | Unit price | Tax |
| --- | --- | --- | ---: | ---: | --- |
| Part | `SKU-CORE` | `Filter replacement` | `1` | `7.00` | `ZERO` |
| Labour | `LAB-SVC` | `Diagnosis labour` | `2` | `25.00` | `ZERO` |
| Expense | blank or expense item | `Parking fee` | `1` | `5.00` | `ZERO` |

Expected:

| Check | Expected result |
| --- | --- |
| Quotation total | `7 + 50 + 5 = 62.00` before discount/entitlement |
| Entitlement | Covered warranty/contract lines show correct zero/discount behavior if applicable |
| Status flow | Draft -> Sent -> Customer Approved |
| Change order | Approved quote supports change order/revision |
| Job Billing tab | Quote appears in quotation trail |

Negative tests:

| Test | Expected result |
| --- | --- |
| Approve unsent quote if not allowed | Blocked |
| Edit approved quote directly | Blocked; change order required |
| Invalid qty/rate | Validation |

## 23. Service Taken / Handover

Open `Service -> Service Taken`.

Create handover for current job.

| Field | Input |
| --- | --- |
| Service job | Current job |
| Items returned | `Generator returned after repair` |
| Customer acknowledgement | `Customer accepted` |
| Notes | `Test handover completed` |
| Post-service warranty months | Enter if applicable |

Expected:

| Check | Expected result |
| --- | --- |
| Handover | Created and linked to job |
| Status flow | Draft -> Completed |
| Job Billing tab | Service taken/handover visible if shown |
| Files/notes | Handover notes remain traceable |

Negative tests:

| Test | Expected result |
| --- | --- |
| Complete without required acknowledgement | Validation |
| Create handover for closed/cancelled job | Blocked or clear validation |

## 24. Final Invoice Without Mandatory Estimate

This is a critical service test. Users must be able to create an invoice without an approved estimate when the business process allows direct service/repair billing.

From completed service taken/handover, create final invoice manually.

Invoice header:

| Field | Input |
| --- | --- |
| Customer | `CUS-SVC-001` |
| Service job | Current job |
| Invoice note | `Direct service invoice without estimate` |

Invoice lines:

| Line type | Item/category | Description | Qty | Unit price | Discount | Tax |
| --- | --- | --- | ---: | ---: | ---: | --- |
| Labour | `LAB-SVC` | `Diagnosis and repair labour` | `2` | `25.00` | `0` | `ZERO` |
| Item | `SKU-CORE` | `Part used in repair` | `1` | `7.00` | `0` | `ZERO` |
| Sundries | `SUN-GREASE` / `SUNDRIES` | `Grease and lubricants` | `1` | `3.00` | `0` | `ZERO` |

Expected:

| Check | Expected result |
| --- | --- |
| Estimate requirement | Invoice creation succeeds without approved estimate |
| Labour line | Appears in invoice with work done/description |
| Item line | Appears correctly |
| Sundries line | Item belongs to `SUNDRIES` category |
| Discount field | Available and applied correctly |
| Invoice total | `50 + 7 + 3 = 60.00` before tax |
| Job Billing tab | Final invoice appears |
| Job Costs tab | Posted revenue appears |
| AR | Customer balance increases by invoice total when invoice is posted |
| Closeout readiness | Final invoice decision clears |

Discount variant:

| Input | Expected result |
| --- | --- |
| Labour discount `10%` on `50.00` | Labour net becomes `45.00` |
| Total with same item/sundries | `45 + 7 + 3 = 55.00` before tax |

Negative tests:

| Test | Expected result |
| --- | --- |
| Invoice with no lines | Blocked |
| Negative qty/price | Blocked |
| Sundries item not in `SUNDRIES` category | Tester records whether system allows it; business should prefer category validation |
| Invoicing closed/cancelled job | Blocked unless reopen/authorized path exists |

## 25. Estimate-Based Invoice Path

If quotation was approved in section 22, test estimate-based conversion too.

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Open completed service taken | Conversion mode allows approved estimate |
| 2 | Select approved quote | Quote lines load |
| 3 | Convert to invoice | Invoice is created |
| 4 | Open invoice | Lines match approved quotation or allowed conversion rules |
| 5 | Open job Billing tab | Quote and invoice both visible |

Expected:

| Check | Expected result |
| --- | --- |
| Estimate conversion | Still works after direct invoice capability exists |
| Duplicate billing | System warns/blocks if same approved labour/parts would be billed twice |
| Revenue | Costs tab shows posted invoice revenue |

## 26. Customer Payment And AR

Open `Finance -> Payments` or payment receipt workflow.

Create customer payment for final invoice.

| Field | Input |
| --- | --- |
| Customer | `CUS-SVC-001` |
| Invoice | Final invoice |
| Payment method | `Cash` or `Bank Transfer` |
| Amount | Full invoice total |
| Reference | `PAY-SVC-001` |

Expected:

| Check | Expected result |
| --- | --- |
| Payment status | Posted |
| Invoice status | Paid |
| AR/customer balance | Reduced by payment amount |
| AR aging | Invoice removed from open aging or shown as paid |
| Cash/bank report | Payment appears according to payment method |

Partial payment test:

| Input | Expected result |
| --- | --- |
| Pay half of invoice | Invoice becomes partial/open with remaining balance |

## 27. Customer Return / Credit After Service Invoice

Use this if a service invoice item needs customer return or credit adjustment.

Open `Sales -> Customer Returns`.

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Create return for `CUS-SVC-001` linked to the final invoice if supported | Return draft created |
| 2 | Select item line from invoice | Only invoice items are selectable if invoice-linked |
| 3 | Return qty `1` for `SKU-CORE` if applicable | Saves |
| 4 | Post return | Stock increases and customer credit note/AR credit is created |

Expected:

| Check | Expected result |
| --- | --- |
| Customer return status | Posted |
| Stock | Returned stocked item increases usable/quarantine stock according to return settings |
| AR credit | Customer credit note or AR adjustment created |
| Invoice/customer account | Customer balance reduced by credit |
| Service job trail | If service-job linkage exists, related credit is traceable; otherwise record as sales return linked to invoice |

Negative tests:

| Test | Expected result |
| --- | --- |
| Return more than invoiced qty | Blocked |
| Return draft affects stock | Must not happen |
| Return unposted invoice | Blocked |

## 28. Supplier Return For Rejected Service Material

Use this when a service-issued part is rejected and must go back to supplier.

Open `Procurement -> Supplier Returns`.

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Create supplier return for `SUP-SVC-001` | Draft created |
| 2 | Add rejected item/qty | Saves if stock exists in returnable location |
| 3 | Post supplier return | Stock decreases from returnable location |
| 4 | Check AP | Supplier credit/AP adjustment is created according to system terminology |

Expected:

| Check | Expected result |
| --- | --- |
| Supplier return status | Posted |
| Inventory | Returned qty leaves warehouse/quarantine |
| AP/supplier account | Supplier credit note or AP adjustment visible |
| Service job material trail | Original service material rejection remains visible |

## 29. Closeout Readiness

Open job `Billing -> Closeout Readiness`.

Before clearing, each blocker must link to the relevant area.

| Blocker | How to clear | Expected link behavior |
| --- | --- | --- |
| Daily field sheets | Submit/approve or cancel invalid daily sheets | Opens Daily Work daily sheet list |
| Expense claims | Settle/reject/cancel claims | Opens Expenses or claim list/detail |
| Petty cash IOUs | Settle/reject/cancel IOUs | Opens Expenses or finance IOU workflow |
| Draft material requisitions | Post/cancel draft MRNs | Opens Materials/MRN |
| Technician assignments | Approve/cancel pending assignments | Opens Daily Work Staff/Labour |
| Labour entries | Submit/approve/cancel entries | Opens work order/labour area |
| Job sheets/work orders | Complete/cancel work orders | Opens related work order |
| Material disposition | Dispose return/damage/rejected quantities | Opens Materials disposition tabs |
| Final invoice decision | Create invoice or mark not billable | Opens Billing invoice area |

Expected after all clearing:

| Check | Expected result |
| --- | --- |
| Closeout readiness | All required checks clear |
| Job cockpit | No pending closeout blocker |
| Command center | Job leaves blocker queue |
| Billing queue | Job appears as ready/cleared or closed path |

Negative closeout tests:

| Test | Expected result |
| --- | --- |
| Close with draft daily sheet | Blocked |
| Close with pending IOU | Blocked |
| Close with unsettled claim | Blocked |
| Close with draft MRN | Blocked |
| Close with undisposed material qty | Blocked |
| Close with uninvoiced billable labour | Blocked unless not-billable/resolved |
| Close without invoice decision | Blocked |

## 30. Complete, Close, Reopen

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Click `Complete` | Confirmation requires expected text if configured |
| 2 | Confirm completion | Job status becomes `Completed` or equivalent |
| 3 | Click `Close` after readiness clear | Job status becomes `Closed` |
| 4 | Try to add material/expense/labour after close | Blocked |
| 5 | Reopen as authorized user | Status becomes reopened/active |
| 6 | Add a new blocker after reopen if allowed | Closeout readiness recalculates |
| 7 | Clear blocker and close again | Close succeeds |

Expected:

| Check | Expected result |
| --- | --- |
| Closed job | Read-only for operational additions |
| Reopen permission | Restricted to authorized roles |
| Audit trail | Complete/close/reopen actions are traceable |
| PDF | Closed job PDF still downloads |

## 31. Files, Notes, And PDF

Open job `Files & Notes`.

| Step | Input | Expected result |
| --- | --- | --- |
| Add comment | `Customer confirmed generator starts after repair.` | Comment appears |
| Upload file | Any small PDF/image | Attachment row appears |
| Add note | `Before/after repair evidence` | Notes visible |
| Download | Click attachment link | File opens/downloads |
| PDF | Click `Download PDF` on job | PDF opens with correct job number |

Negative tests:

| Test | Expected result |
| --- | --- |
| Upload oversized file | Clear validation |
| Upload blocked file type | Clear validation |
| Comment blank | Blocked or ignored with clear behavior |

## 32. Costing And Profitability

Open job `Costs`.

Expected values from the base scenario:

| Source | Expected calculation |
| --- | ---: |
| Material issue cost | `SKU-CORE 2 x 5 + SKU-SERIAL 1 x 25 = 35.00` |
| Returned material | `SKU-CORE 1` returned to stock; issue trail remains visible |
| Labour cost | `2 x 10 = 20.00` |
| Billable labour revenue | `2 x 25 = 50.00` before discounts/entitlement |
| Expense claim cost | `5.00` if out-of-pocket claim was settled |
| Invoice revenue direct path | `60.00` before discount/tax in base direct invoice |

Check:

| Check | Expected result |
| --- | --- |
| Material cost | Matches posted MRN source |
| Labour cost | Matches approved work-order labour |
| Expense cost | Matches settled/approved claims |
| Invoice revenue | Matches posted final invoice |
| Uninvoiced billable labour | Clears after invoice/resolution |
| Margin | Revenue minus actual cost, according to system costing rules |
| Source drilldown | Cost source tables show MRN, labour, expense, invoice references |

## 33. Reporting Checks

After closeout, check reporting.

| Report/page | Expected result |
| --- | --- |
| `Reporting -> Service KPIs` | Job activity included |
| `Reporting -> Costing` | Service cost/inventory cost reflected |
| `Inventory -> On Hand` | `SKU-CORE` final qty reflects issue and return |
| `Inventory -> Availability` | `SER-SVC-001` unavailable if damaged/consumed; `SER-SVC-002` available |
| `Finance -> AR` | Invoice/payment reflected |
| `Finance -> Petty Cash` | IOU/voucher/claim cash movements reflected |
| `Finance -> Petty Cash IOUs` | Final IOU status settled/rejected/cancelled |

Expected stock from base scenario:

| Item | Starting | Issue | Return | Final expected |
| --- | ---: | ---: | ---: | ---: |
| `SKU-CORE` | `10` | `-2` | `+1` | `9` |
| `SKU-SERIAL SER-SVC-001` | `1` | `-1` | `0 usable` | Not available |
| `SKU-SERIAL SER-SVC-002` | `1` | `0` | `0` | Available |

## 34. Audit And Persistence

| Check | Expected result |
| --- | --- |
| Logout/login | Job and all linked records still visible |
| Browser refresh | Current tab records remain |
| List search | Job, MRN, IOU, claim, invoice can be found |
| Created user/time | Visible in detail/expanded row/audit where implemented |
| Edited user/time | Visible where implemented |
| Status timeline | Key finance/job status transitions visible |
| Audit logs | Important actions recorded if audit log covers that area |

## 35. Regression Checklist For Modal Dialog Pattern

Use this after UI changes.

| Area | Create opens modal | Edit opens modal | Register/list remains primary | Save refreshes list | Cancel does not save |
| --- | --- | --- | --- | --- | --- |
| Job order list | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Job detail edit header | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Plan operation | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Daily sheet | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Daily staff/labour | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Daily progress | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| IOU advance | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Petty cash voucher | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Out-of-pocket claim | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| MRN creation | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Material return | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |
| Damage/rejection | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail |

## 36. Final Pass Criteria

The Service Job section passes only when all conditions below are true.

| Area | Pass condition |
| --- | --- |
| UI/navigation | Service pages load, Help is in Service menu, no asset/runtime errors |
| Equipment | Equipment can be created, linked to customer, and used in job |
| Job order | Job can be created, started, executed, completed, closed, reopened where authorized |
| Daily sheets | Daily sheets track staff/progress/MRN/returns/expenses/IOUs |
| Labour | Daily attendance and billable work-order labour are separate and correct |
| Materials | MRN stock issue, serial validation, return, damage, rejection handling are accurate |
| Cash/expenses | IOU, petty cash voucher, out-of-pocket claim remain visible and follow approval/settlement |
| Accounts | Petty cash, AR, AP/supplier return, invoice, payment effects are accurate |
| Billing | Manual invoice without approved estimate works; estimate-based invoice still works |
| Sundries | Grease/lubricants use `SUNDRIES` item category and can be invoiced |
| Costs | Costs tab matches MRN, labour, expense, and invoice source documents |
| Closeout | Job cannot close with unresolved blockers and can close when blockers are clear |
| Reports | Inventory, finance, service KPI, and costing reports match expected values |
| Audit | Records persist and remain traceable after refresh/login/logout |

## 37. Tester Sign-Off

| Item | Value |
| --- | --- |
| Tester name |  |
| Test date |  |
| Environment | Local / Railway / Other |
| Frontend version/commit |  |
| API version/commit |  |
| Database used |  |
| Main service job number |  |
| Final result | Pass / Fail |
| Open defects |  |
| Notes |  |
