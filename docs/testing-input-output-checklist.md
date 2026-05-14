# ISS Testing Input / Output Checklist

This document gives testers exact values to enter, expected outputs, and where to verify each result.

Use it for manual UAT, demo database testing, and regression checks after deployments. Run the scenario as an `Admin` user first. Role-by-role testing can be done later using `docs/role-based-test-checklists.md`.

## 1. Reset Test Data

Use this only on a test database.

Go to `Admin -> Testing Cleanup`.

Type:

| Field | Input |
| --- | --- |
| Confirmation | `CLEAR` |

Recommended reset before this full test:

1. Click `Clear PO`
2. Type `CLEAR` again
3. Click `Clear Jobs / Service`
4. Type `CLEAR` again
5. Click `Zero Stock`

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Cleanup response | Same page | Success message after each button |
| Inventory reset | `Inventory -> Inventory Availability`, click `Load inventory` | No inventory rows |
| PO/GRN reset | `Procurement -> Purchase Orders`, `Procurement -> Goods Receipts` | No old test PO/GRN documents, unless the database has unrelated retained data |

Do not use cleanup buttons on real production data.

## 2. Master Data Inputs

Create these values before transaction testing.

### Warehouses And Bins

Go to `Master Data -> Warehouses`.

| Record | Field | Input |
| --- | --- | --- |
| Warehouse 1 | Code | `MAIN` |
| Warehouse 1 | Name | `Main Warehouse` |
| Warehouse 2 | Code | `SEC` |
| Warehouse 2 | Name | `Secondary Warehouse` |
| MAIN bin 1 | Bin Code | `A1-R1-S1` |
| MAIN bin 1 | Name | `Aisle 1 Rack 1 Shelf 1` |
| MAIN bin 1 | Zone | `A1` |
| MAIN bin 1 | Rack | `R1` |
| MAIN bin 1 | Shelf | `S1` |
| SEC bin 1 | Bin Code | `B1-R1-S1` |
| SEC bin 1 | Name | `Secondary Rack 1 Shelf 1` |
| SEC bin 1 | Zone | `B1` |
| SEC bin 1 | Rack | `R1` |
| SEC bin 1 | Shelf | `S1` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Warehouse list | `Master Data -> Warehouses` | `MAIN` and `SEC` appear |
| Bin list | `Bins / Racks` section | `A1-R1-S1` under `MAIN`, `B1-R1-S1` under `SEC` |

### Supplier And Customer

| Page | Field | Input |
| --- | --- | --- |
| `Master Data -> Suppliers` | Code | `SUP1` |
| `Master Data -> Suppliers` | Name | `Test Supplier` |
| `Master Data -> Customers` | Code | `CUS1` |
| `Master Data -> Customers` | Name | `Test Customer` |
| `Master Data -> Customers` | Phone | `0770000000` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Supplier exists | Supplier list | `SUP1 - Test Supplier` |
| Customer exists | Customer list | `CUS1 - Test Customer` |

### Items

Go to `Master Data -> Items`.

| Item | Field | Input |
| --- | --- | --- |
| Normal stock item | SKU | `SKU-CORE` |
| Normal stock item | Name | `Hydraulic Filter` |
| Normal stock item | Type | `Spare Part` |
| Normal stock item | Tracking | `None` |
| Normal stock item | UoM | `PCS` |
| Normal stock item | Default Unit Cost | `5` |
| Batch item | SKU | `SKU-BATCH` |
| Batch item | Name | `Engine Oil Lot Item` |
| Batch item | Type | `Spare Part` |
| Batch item | Tracking | `Batch` |
| Batch item | UoM | `PCS` |
| Batch item | Default Unit Cost | `8` |
| Serial item | SKU | `SKU-SERIAL` |
| Serial item | Name | `Control Board Serialized` |
| Serial item | Type | `Spare Part` |
| Serial item | Tracking | `Serial` |
| Serial item | UoM | `PCS` |
| Serial item | Default Unit Cost | `25` |
| Equipment item | SKU | `EQP-GEN` |
| Equipment item | Name | `Generator Model A` |
| Equipment item | Type | `Equipment` |
| Equipment item | Tracking | `Serial` |
| Equipment item | UoM | `PCS` |
| Equipment item | Default Unit Cost | `100` |
| Labor item | SKU | `LAB-SVC` |
| Labor item | Name | `Service Labor` |
| Labor item | Type | `Service` |
| Labor item | Tracking | `None` |
| Labor item | UoM | `HOUR` |
| Labor item | Default Unit Cost | `0` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Item search | `Master Data -> Items` | All SKUs can be searched and opened |
| Tracking setup | Item detail/edit page | `SKU-BATCH` is batch tracked; `SKU-SERIAL` and `EQP-GEN` are serial tracked |

## 3. Procurement: PO And Partial GRNs

This phase proves partial receiving. One PO line for `20` units will be received in two GRNs: first `8`, then `12`.

### 3.1 Create And Approve PO

Go to `Procurement -> Purchase Orders`.

| Field | Input |
| --- | --- |
| Supplier | `SUP1` |

Add PO lines:

| Line | Item | Qty | Unit Price |
| --- | --- | ---: | ---: |
| 1 | `SKU-CORE` | `20` | `5` |
| 2 | `SKU-BATCH` | `10` | `8` |
| 3 | `SKU-SERIAL` | `2` | `25` |

Click `Approve`.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| PO status | PO detail | `Approved` |
| PO total | PO detail | `20 x 5 + 10 x 8 + 2 x 25 = 230` |
| Open receipt quantity | GRN create/detail from PO | PO lines are available to receive |

### 3.2 GRN 1 - Partial Receipt

Go to `Procurement -> Goods Receipts`.

Create GRN:

| Field | Input |
| --- | --- |
| Purchase Order | The approved PO |
| Warehouse | `MAIN` |

In `Receive From PO`, enter:

| PO Line | Receive Qty | Unit Cost | Batch | Serials |
| --- | ---: | ---: | --- | --- |
| `SKU-CORE` | `8` | `5` | blank | blank |
| `SKU-BATCH` | `4` | `8` | `LOT-A` | blank |
| `SKU-SERIAL` | `1` | `25` | blank | `SER-001` |

Click `Save receipt plan`, then `Post`.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| GRN 1 status | GRN detail | `Posted` |
| GRN 1 stock movement | `Inventory -> Inventory Availability`, filter item `SKU-CORE` | `SKU-CORE` on hand `8` in `MAIN`, bin `Unassigned` |
| Batch stock | `Inventory -> Inventory Availability`, search `LOT-A` | `SKU-BATCH`, batch `LOT-A`, on hand `4` |
| Serial stock | `Inventory -> Inventory Availability`, search `SER-001` | `SKU-SERIAL`, serial `SER-001`, on hand `1` |
| AP entry | `Finance -> AP` | GRN/AP outstanding includes `8 x 5 + 4 x 8 + 1 x 25 = 97` |
| Costing | `Reporting -> Costing`, item `SKU-CORE` | On hand `8`, weighted avg cost `5`, value `40` |

### 3.3 GRN 2 - Remaining Receipt

Create another GRN from the same approved PO.

| Field | Input |
| --- | --- |
| Purchase Order | Same approved PO |
| Warehouse | `MAIN` |

In `Receive From PO`, enter only the remaining quantities:

| PO Line | Receive Qty | Unit Cost | Batch | Serials |
| --- | ---: | ---: | --- | --- |
| `SKU-CORE` | `12` | `5` | blank | blank |
| `SKU-BATCH` | `6` | `8` | `LOT-A` | blank |
| `SKU-SERIAL` | `1` | `25` | blank | `SER-002` |

Click `Save receipt plan`, then `Post`.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| GRN 2 status | GRN detail | `Posted` |
| Total `SKU-CORE` stock | `Inventory -> On Hand`, item `SKU-CORE`, warehouse `MAIN` | `20` |
| Total batch stock | `Inventory -> Inventory Availability`, search `LOT-A` | `SKU-BATCH`, batch `LOT-A`, on hand `10` |
| Serial stock | `Inventory -> Inventory Availability`, search `SER-001` and `SER-002` | both serials available with on hand `1` each |
| AP total from two GRNs | `Finance -> AP` | GRN entries total `230` before payment/invoice allocation |
| PO remaining receipt | Create another GRN from same PO | no remaining quantity should be available for the fully received lines |

### 3.4 Negative GRN Validation

Try creating a third GRN from the same PO and receiving:

| Item | Qty |
| --- | ---: |
| `SKU-CORE` | `1` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Over receipt prevention | GRN receipt plan save/post | Error that quantity exceeds remaining PO quantity, or no open PO line available |

## 4. Inventory Visibility Checks

### 4.1 Full Inventory Browser

Go to `Inventory -> Inventory Availability`.

Click `Load inventory`.

Expected rows:

| Item | Warehouse | Bin/Rack | Batch/Lot | Serial | On Hand | Unit Cost | Value |
| --- | --- | --- | --- | --- | ---: | ---: | ---: |
| `SKU-CORE` | `MAIN` | `Unassigned` | `-` | `-` | `20` | `5` | `100` |
| `SKU-BATCH` | `MAIN` | `Unassigned` | `LOT-A` | `-` | `10` | `8` | `80` |
| `SKU-SERIAL` | `MAIN` | `Unassigned` | `-` | `SER-001` | `1` | `25` | `25` |
| `SKU-SERIAL` | `MAIN` | `Unassigned` | `-` | `SER-002` | `1` | `25` | `25` |

Expected totals:

| Total | Expected |
| --- | ---: |
| On hand quantity | `32` |
| Inventory value | `230` |

Search tests:

| Search | Expected |
| --- | --- |
| `SKU-CORE` | Only core item row |
| `LOT-A` | Batch item row |
| `SER-001` | Serial row for `SER-001` |
| `MAIN` | All rows in main warehouse |

### 4.2 On Hand Query

Go to `Inventory -> On Hand`.

| Filter | Input | Expected |
| --- | --- | --- |
| Warehouse | `MAIN` | Results are limited to `MAIN` |
| Item | `SKU-CORE` | On hand `20` |
| View | `Warehouse wise` | `MAIN = 20` |
| View | `Warehouse + batch` | `MAIN / No batch = 20` |

Batch item:

| Filter | Input | Expected |
| --- | --- | --- |
| Warehouse | `MAIN` | Results are limited to `MAIN` |
| Item | `SKU-BATCH` | On hand `10` |
| Batch | `LOT-A` | On hand `10` |

## 5. Stock Transfer

Go to `Inventory -> Stock Transfers`.

Create transfer:

| Field | Input |
| --- | --- |
| From Warehouse | `MAIN` |
| To Warehouse | `SEC` |

Add line:

| Item | Qty | Batch | Serials |
| --- | ---: | --- | --- |
| `SKU-CORE` | `5` | blank | blank |

Post transfer.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Transfer status | Transfer detail | `Posted` |
| MAIN stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `15` |
| SEC stock | `Inventory -> On Hand`, `SEC` + `SKU-CORE` | `5` |
| Total company stock | `Inventory -> Inventory Availability`, search `SKU-CORE` | `20` total remains unchanged |

## 6. Sales Direct Dispatch And Invoice

### 6.1 Direct Dispatch

Go to `Sales -> Direct Dispatches`.

Create:

| Field | Input |
| --- | --- |
| Customer | `CUS1` |
| Warehouse | `MAIN` |
| Reason | `Test direct dispatch` |

Add line:

| Item | Qty | Batch | Serials |
| --- | ---: | --- | --- |
| `SKU-CORE` | `6` | blank | blank |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Direct dispatch status | Detail page | `Posted` |
| MAIN stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `9` |
| SEC stock | `Inventory -> On Hand`, `SEC` + `SKU-CORE` | `5` |
| Total stock | `Inventory -> Inventory Availability`, search `SKU-CORE` | `14` |

### 6.2 Sales Invoice

Go to `Sales -> Invoices`.

Create invoice:

| Field | Input |
| --- | --- |
| Customer | `CUS1` |

Add line:

| Item | Qty | Unit Price | Discount % | Tax % |
| --- | ---: | ---: | ---: | ---: |
| `SKU-CORE` | `6` | `7` | `0` | `0` |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Invoice total | Invoice detail | `42` |
| AR entry | `Finance -> AR` | Customer `CUS1`, amount `42`, outstanding `42` |
| Inventory | `Inventory -> On Hand` | unchanged from dispatch; invoice should not issue stock again |

## 7. Customer Return

Go to `Sales -> Customer Returns`.

Create:

| Field | Input |
| --- | --- |
| Customer | `CUS1` |
| Warehouse | `MAIN` |
| Sales Invoice | Invoice from section 6.2 |
| Reason | `Test return` |

Add line:

| Item | Qty | Unit Price |
| --- | ---: | ---: |
| `SKU-CORE` | `1` | `7` |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Return status | Customer return detail | `Posted` |
| MAIN stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `10` |
| Customer credit note | `Finance -> Credit Notes` | Credit note for `CUS1`, amount `7` |

## 8. Supplier Return

Go to `Procurement -> Supplier Returns`.

Create:

| Field | Input |
| --- | --- |
| Supplier | `SUP1` |
| Warehouse | `MAIN` |
| Reason | `Test supplier return` |

Add line:

| Item | Qty | Unit Cost |
| --- | ---: | ---: |
| `SKU-CORE` | `2` | `5` |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Supplier return status | Detail page | `Posted` |
| MAIN stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `8` |
| Supplier credit note | `Finance -> Credit Notes` | Credit note for `SUP1`, amount `10` |

## 9. Stock Adjustment

Go to `Inventory -> Stock Adjustments`.

Create:

| Field | Input |
| --- | --- |
| Warehouse | `MAIN` |

Add counted line:

| Item | Counted Qty | Unit Cost |
| --- | ---: | ---: |
| `SKU-CORE` | `9` | `5` |

Post.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| System quantity before count | Stock adjustment stock widget | `8` |
| Expected variance | Stock adjustment line/widget | `+1` |
| Posted stock | `Inventory -> On Hand`, `MAIN` + `SKU-CORE` | `9` |
| Stock ledger | `Reporting -> Stock Ledger`, item `SKU-CORE` | Adjustment movement `+1` |

## 10. Material Requisition For Service Job

### 10.1 Create Equipment And Job

Go to `Service -> Equipment Units`.

Create equipment unit:

| Field | Input |
| --- | --- |
| Mode | `Existing item` |
| Equipment item | `EQP-GEN` |
| Serial number | `GEN-SN-001` |
| Customer | `CUS1` |
| Warranty coverage | `Labor and Parts` |

Go to `Service -> Jobs`.

Create job:

| Field | Input |
| --- | --- |
| Equipment unit | `GEN-SN-001` |
| Customer | `CUS1` |
| Kind | `Repair` |
| Problem description | `Generator does not start` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Equipment unit | `Service -> Equipment Units` | `GEN-SN-001` exists |
| Job status | Job detail | `Open` |

### 10.2 MRN Available Stock Validation

Go to `Service -> Material Requisitions`.

Create MRN:

| Field | Input |
| --- | --- |
| Job | The job from 10.1 |
| Warehouse | `MAIN` |

Add valid line:

| Item | Qty |
| --- | ---: |
| `SKU-CORE` | `2` |

Expected:

| Check | Where | Expected output |
| --- | --- |
| Line save | MRN detail | Line saves successfully because `MAIN` has `9` |

Try invalid line:

| Item | Qty |
| --- | ---: |
| `SKU-CORE` | `999` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Over-request validation | MRN add line | Error showing insufficient stock / available quantity |

### 10.3 MRN Serial Picker Validation

Add serial item line:

| Item | Qty | Serial |
| --- | ---: | --- |
| `SKU-SERIAL` | `1` | Select `SER-001` from available serial picker |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Serial picker | MRN detail | `SER-001` appears as available |
| Line save | MRN detail | Saves with selected serial |

Post the MRN.

Expected after posting:

| Check | Where | Expected output |
| --- | --- | --- |
| MRN status | MRN detail | `Posted` |
| MAIN `SKU-CORE` | `Inventory -> On Hand` | `7` because `9 - 2 = 7` |
| Serial `SER-001` | `Inventory -> Inventory Availability`, search `SER-001` | no available row, or on hand removed |
| Serial `SER-002` | `Inventory -> Inventory Availability`, search `SER-002` | still available with on hand `1` |
| Job costing | Service job detail costing section | Material cost includes `2 x 5 + 1 x 25 = 35` |

## 11. Service Estimate, Work Order, Expense, Handover

### 11.1 Technician And Work Order

Go to `Service -> Technicians`.

Create:

| Field | Input |
| --- | --- |
| Code | `TECH1` |
| Name | `Workshop Technician` |
| Default Cost Rate | `10` |
| Default Billing Rate | `25` |

Go to `Service -> Work Orders`.

Create work order for the job.

Add time entry:

| Field | Input |
| --- | --- |
| Technician | `TECH1` |
| Hours worked | `2` |
| Cost rate | `10` |
| Billing rate | `25` |
| Billable | `Yes` |

Submit and approve the time entry.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Labor cost | Work order/job costing | `2 x 10 = 20` |
| Billable labor before entitlement | Work order | `2 x 25 = 50` |
| If warranty/contract covers labor | Estimate/invoice conversion | effective customer billing can be `0` for covered labor |

### 11.2 Estimate

Go to `Service -> Estimates`.

Create estimate for the job.

Add lines:

| Kind | Item | Description | Qty | Unit Price | Tax % |
| --- | --- | --- | ---: | ---: | ---: |
| Part | `SKU-CORE` | `Filter replacement` | `2` | `7` | `0` |
| Labor | `LAB-SVC` | `Diagnosis labor` | `2` | `25` | `0` |
| Expense | blank or expense item | `Travel cost` | `1` | `15` | `0` |

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Description display | Estimate detail | Descriptions show in expandable detail rows, not squeezed into the main grid |
| Estimate total before entitlement | Estimate detail | `14 + 50 + 15 = 79` |
| Covered lines | Estimate detail | Covered parts/labor may show unit price `0` depending on job entitlement |

Send estimate, mark customer approved.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Approval state | Estimate detail | `Customer Approval = Approved` |
| Change order | Approved estimate detail | `Create Change Order` creates a new draft revision |

### 11.3 Expense Claim

Go to `Service -> Petty Cash` or `Service -> Expense Claims`.

Create claim:

| Field | Input |
| --- | --- |
| Job | The same service job |
| Funding Source | `Out of Pocket` |
| Merchant | `Test Vendor` |

Add line:

| Description | Qty | Unit Cost | Billable |
| --- | ---: | ---: | --- |
| `Parking fee` | `1` | `5` | `Yes` |

Submit, approve, and settle.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Claim status | Claim detail | `Settled` |
| Job costing | Job detail | Expense claim cost includes `5` |
| Convert to estimate | Claim detail | Billable line can be converted into draft estimate/change order |

### 11.4 Handover And Invoice

Go to `Service -> Handovers`.

Create handover for the job.

| Field | Input |
| --- | --- |
| Items returned | `Generator returned after repair` |
| Customer acknowledgement | `Customer accepted` |
| Notes | `Test handover` |

Complete handover and convert to invoice.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Handover status | Handover detail | `Completed` |
| Sales invoice | Handover detail / `Sales -> Invoices` | Invoice created and linked |
| Job costing | Job detail | Invoice value appears in costing summary |

## 12. Finance Payment Checks

### Customer Payment

Go to `Finance -> Payments`.

Create incoming payment:

| Field | Input |
| --- | --- |
| Direction | `Incoming` |
| Counterparty Type | `Customer` |
| Counterparty | `CUS1` |
| Currency | Base currency |
| Amount | `42` |

Allocate to the sales invoice from section 6.2.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Payment detail | Payment detail | Allocated `42`, remaining `0` |
| AR | `Finance -> AR`, outstanding only | Invoice no longer outstanding |
| Invoice status | Invoice detail | `Paid` if fully allocated |

### Supplier Payment

Create outgoing payment:

| Field | Input |
| --- | --- |
| Direction | `Outgoing` |
| Counterparty Type | `Supplier` |
| Counterparty | `SUP1` |
| Currency | Base currency |
| Amount | `230` |

Allocate to GRN/AP entries.

Expected:

| Check | Where | Expected output |
| --- | --- | --- |
| Payment detail | Payment detail | Allocated up to `230`, remaining `0` if all AP entries selected |
| AP | `Finance -> AP`, outstanding only | GRN/AP entries no longer outstanding |

## 13. Reporting Checks

### Stock Ledger

Go to `Reporting -> Stock Ledger`.

Filter item `SKU-CORE`.

Expected movement trail:

| Movement | Expected Qty |
| --- | ---: |
| GRN 1 receipt | `+8` |
| GRN 2 receipt | `+12` |
| Transfer out MAIN | `-5` |
| Transfer in SEC | `+5` |
| Direct dispatch | `-6` |
| Customer return | `+1` |
| Supplier return | `-2` |
| Stock adjustment | `+1` |
| MRN consumption | `-2` |

Expected final balances:

| Warehouse | Expected `SKU-CORE` |
| --- | ---: |
| MAIN | `7` |
| SEC | `5` |
| Total | `12` |

### Costing

Go to `Reporting -> Costing`.

Filter item `SKU-CORE`.

Expected:

| Check | Expected |
| --- | ---: |
| Total on hand | `12` |
| Weighted avg cost | `5` |
| Inventory value | `60` |

### Service KPIs

Go to `Reporting -> Service KPIs`.

Expected:

| Check | Expected |
| --- | --- |
| Jobs | At least one service job appears in KPI totals |
| Material requisitions | Posted MRN count increases |
| Labor/expense/costing | Service job costing data is reflected if the report includes those values |

## 14. PDF And Document Checks

Open PDF/download links for at least:

| Area | Document |
| --- | --- |
| Procurement | PO |
| Procurement | GRN 1 |
| Procurement | GRN 2 |
| Sales | Invoice |
| Inventory | Stock adjustment |
| Service | Estimate |
| Service | MRN |
| Service | Handover |

Expected:

| Check | Expected |
| --- | --- |
| PDF opens/downloads | Browser receives PDF |
| Document number | Matches screen |
| Totals | Match screen totals |
| Lines | Match entered items, quantities, batch/serial where applicable |

## 15. Final Reconciliation

After all sections above:

| Area | Where | Expected |
| --- | --- | --- |
| `SKU-CORE` stock | `Inventory -> Inventory Availability` | MAIN `7`, SEC `5`, total `12` |
| `SKU-BATCH` stock | Availability search `LOT-A` | MAIN `10`, batch `LOT-A` |
| `SKU-SERIAL` stock | Availability search `SER-001` | consumed/unavailable |
| `SKU-SERIAL` stock | Availability search `SER-002` | available `1` |
| Inventory value for `SKU-CORE` | `Reporting -> Costing` | `12 x 5 = 60` |
| Customer AR | `Finance -> AR` | sales invoice cleared if payment allocated |
| Supplier AP | `Finance -> AP` | GRN/AP entries cleared if payment allocated |
| Service job costing | Job detail | material `35`, labor cost `20`, expense `5`, plus invoice/estimate values from service flow |

## 16. Common Failures And Where To Look

| Problem | Check |
| --- | --- |
| GRN cannot post | Confirm PO is approved and receipt quantity does not exceed remaining quantity |
| Batch item cannot save | Confirm batch/lot is entered for `SKU-BATCH` |
| Serial item cannot save | Confirm serial count equals quantity and serials are unique |
| MRN says insufficient stock | Check `Inventory -> Inventory Availability` for the source warehouse |
| MRN serial not listed | Serial may already be consumed or not received into that warehouse |
| Stock value does not match | Check `Reporting -> Stock Ledger` movement quantities and unit costs |
| AP/AR does not clear | Confirm payment allocation is saved against the correct outstanding entry |
| Service estimate billing differs from raw total | Check warranty/contract entitlement on the job; covered labor/parts can bill at zero |

