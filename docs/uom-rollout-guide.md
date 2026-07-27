# Unit of Measure rollout — implementation guide

Status as of commit `e3616f1` on branch `c-com-erp`.

This guide has two parts. **Part A** finishes the display pass that is already 80% done and is
mechanical. **Part B** is the real feature — entering a quantity in one unit and storing it in
another — which is a schema change and should be treated as a separate piece of work.

Read Part 0 first. It explains the model, and getting it wrong is how you end up with stock
balances that silently mean different things on different screens.

---

## Part 0 — How units work in this system today

### The model

`Item.UnitOfMeasure` is a **string holding a UoM code** (`backend/src/ISS.Domain/MasterData/Item.cs:63`,
max length 32). It is not a foreign key. It is validated on item create/update against active codes
in the `UnitOfMeasure` master — see `ValidateUnitOfMeasureAsync` in
`backend/src/ISS.Api/Controllers/ItemsController.cs`. That validation was added in `e3616f1`; before
it, the field was free text and had already drifted (two items carry `HRS`, which is not in the
master).

There are two master tables, both plain CRUD:

| Entity | File | Used by |
| --- | --- | --- |
| `UnitOfMeasure` | `backend/src/ISS.Domain/MasterData/UnitOfMeasure.cs` | Item form dropdown, item validation |
| `UnitConversion` | `backend/src/ISS.Domain/MasterData/UnitConversion.cs` | **Nothing but the purchase requisition add-line form** |

`UnitConversion` holds `FromUnitOfMeasureId`, `ToUnitOfMeasureId`, `Factor`. API at
`api/unit-conversions`, UoM master at `api/uoms`.

### The critical fact about calculations

**There is no unit-aware arithmetic anywhere in the system.** A quantity is a bare `decimal`. Every
line, every movement, every stock balance is implicitly "N of the item's base unit". Nothing
converts, nothing validates that two quantities being compared share a unit.

This is internally consistent — 250 received becomes 250 on hand becomes 250 issued — so nothing is
currently *wrong*. But it means:

- If an operator types 250 intending millilitres on an item whose base unit is Litres, the system
  records 250 litres and every downstream number is wrong. Nothing catches it.
- Buying a 20 L drum of an item stocked in ML cannot be expressed. You must do the arithmetic in
  your head and type 20000.

The only existing attempt at conversion is
`frontend/src/app/(app)/procurement/purchase-requisitions/PurchaseRequisitionLineAddForm.tsx`. It
lets you pick a request UoM, resolves a factor via `resolveConversionFactor`, multiplies, and posts
the **base** quantity. The entered unit and factor are then **appended to the line's `notes` string**
as prose. That is a workaround, not a design — the data is not queryable, not printable as a
separate column, and is lost if someone edits the note. Do not copy this pattern; Part B replaces it.

### What was already done in `e3616f1`

Display only, no schema change:

1. `ItemOptionDto` (`backend/src/ISS.Api/Controllers/ItemOptionsController.cs`) now returns
   `UnitOfMeasure`. This is the endpoint most line forms read.
2. The shared data grid gained a `unit?: (row) => string | null` hook on `ColumnBase`
   (`frontend/src/components/data-grid/types.ts`), rendered by `withUnit()` in
   `frontend/src/components/data-grid/EditableDataTable.tsx`. It appends a muted unit **only when
   the cell is not being edited**, so the editor stays a plain number input.
3. Ten line grids pass an `itemUomById: Map<string, string>` prop and set
   `unit: (line) => itemUomById.get(line.itemId) ?? null` on their quantity columns. The map is
   built on the page as `new Map(items.map((i) => [i.id, i.unitOfMeasure]))`.
4. Twelve add-line forms derive `selectedItemUom` and show it beside the Qty label.
5. PDFs: `FormatQty(decimal, Item?)` overload in
   `backend/src/ISS.Infrastructure/Documents/DocumentPdfService.cs`, applied at every
   item-quantity cell in `InventoryPdf`, `ProcurementPdf`, `SalesPdf`, `ServicePdf`.

**Reference implementation to copy:** `SalesOrderLinesEditor.tsx` + `sales/orders/[id]/page.tsx`
for a grid, `SalesOrderLineAddForm.tsx` for a form.

### Data note for whoever runs this

UoM master codes are currently `Liters`, `Mililiters` (sic — misspelled), `PCS`. The **code is what
prints next to every quantity**, so documents read `250 Liters`. That is legible but non-standard;
`L` / `ML` is the convention. Renaming a code requires updating `Item.UnitOfMeasure` on every item
that references it **in the same transaction**, because the link is by string, not by id. Do not
rename one without the other.

---

## Part A — Finish the display pass

Mechanical, no schema change, no migration. Each item below is independent.

### A1. Goods receipt plan form

`frontend/src/app/(app)/procurement/goods-receipts/GoodsReceiptReceiptPlanForm.tsx`

The only remaining `EditableDataTable` grid without units. It has a `quantity` column at roughly
line 433. It does not currently receive an item map — check whether the row type carries `itemId`
and whether the parent page already loads items; if not, thread `itemUomById` from
`procurement/goods-receipts/[id]/page.tsx` the same way the other nine pages do.

Note this grid shows ordered / already received / receiving now — put the unit on **all** quantity
columns, as was done for purchase orders (`orderedQuantity` and `receivedQuantity` both carry it).

### A2. Three service add-line forms

| File | Note |
| --- | --- |
| `service/estimates/ServiceEstimateLineAddForm.tsx` | Item is **optional** on estimate lines (labour lines have no item). Show the unit only when an item is selected. |
| `service/expense-claims/ServiceExpenseClaimLineAddForm.tsx` | Same — expense lines are often item-less. Label is `Quantity`, not `Qty`. |
| `service/jobs/ServiceJobMaterialDispositionAddForm.tsx` | Label is `Quantity`. Item is required. |

Pattern, from `SalesOrderLineAddForm.tsx`:

```tsx
type ItemRef = { id: string; sku: string; name: string; unitOfMeasure: string };

const selectedItemUom = items.find((i) => i.id === itemId)?.unitOfMeasure ?? "";

<label className="mb-1 block text-sm font-medium">
  Qty{selectedItemUom ? <span className="ml-1 text-xs font-normal text-zinc-500">({selectedItemUom})</span> : null}
</label>
```

Remember to add `unitOfMeasure: string` to the parent page's local `ItemDto` type, or `tsc` fails
with "Property 'unitOfMeasure' is missing".

### A3. Read-only quantity tables

These render quantities in plain `<table>` markup rather than through the data grid, so the `unit`
hook does not reach them. Each needs the item's unit resolved and rendered after the number.

- `procurement/direct-purchases/[id]/page.tsx`
- `procurement/goods-receipts/GoodsReceiptDraftLinesTable.tsx`
- `service/expense-claims/[id]/page.tsx`
- `service/jobs/[id]/page.tsx` — several tables: issued materials, dispositions, costing lines
- `service/material-requisitions/[id]/page.tsx`
- `service/handovers/ServiceJobBillingBuilder.tsx` — the parts section (`remainingQuantity`,
  the qty input) and the labour section (hours; label those `hrs`, not the item unit)

### A4. Inventory screens

Stock balances are the place a missing unit does the most damage, and none of these carry one yet.

- `inventory/onhand/page.tsx` and `OnHandQuery.tsx`
- `inventory/availability/InventoryAvailabilityBrowser.tsx`
- `inventory/reorder-alerts/page.tsx` — reorder point and on-hand columns
- `inventory/stock-adjustments/StockAdjustmentLineAddForm.tsx` and `StockAdjustmentLineRow.tsx`
- `inventory/stock-transfers/StockTransferLineAddForm.tsx` and `StockTransferLineRow.tsx`

Check whether the backend rows these read already carry the item's unit. If not, add
`UnitOfMeasure` to the relevant DTO rather than making the client fetch and join the item list —
`ItemOptionsController` is the precedent for adding the field at the source.

### A5. Shared pickers

- `frontend/src/components/AvailableBatchPicker.tsx` — quantities per batch
- `frontend/src/components/AvailableSerialPicker.tsx` — serials are inherently 1 each; probably
  nothing to do, confirm and move on
- `frontend/src/components/StockAvailabilityExplorer.tsx` and `StockAvailabilityModal.tsx`

### A6. Reporting

- `reporting/stock-ledger/page.tsx`
- `reporting/sales-analysis/page.tsx`

For reports that **aggregate across items**, do not print a unit on the total — summing 3 litres and
2 pieces into "5" is already meaningless and a unit label would make it look authoritative. Put the
unit on per-item rows only, and leave mixed totals bare.

### A7. Verification for Part A

```bash
cd frontend && npx tsc --noEmit && npx eslint "src/app/(app)" src/components
```

Both must exit 0. `eslint` exits 0 on warnings too, so read the output — adding `itemUomById` to a
`useMemo` body requires adding it to the dependency array or you get an `exhaustive-deps` warning.

The frontend is served by `next start` from a **production build**. Changes are invisible until
`npm run build` is re-run and the process restarted. This is not a bug; it has already caused one
false "the change didn't work" report.

---

## Part B — Entering quantities in a different unit

This is the actual "standard way" and it is a schema change. Do not start it as part of Part A.

### What it must achieve

Buy a 20 L drum of an item stocked in ML. The purchase order prints "1 Drum" or "20 L" — whatever
was ordered — while inventory, costing and stock balances move in the item's base unit.

### The rule that must not be broken

**Stock, costing and all inventory movements stay in the item's base unit. Always.** The alternative
unit is a presentation and data-entry concern only. If a conversion factor ever reaches
`InventoryMovement.Quantity`, stock balances become unauditable.

### Schema

Every transaction line that a user types a quantity into gets three columns:

| Column | Type | Meaning |
| --- | --- | --- |
| `EntryUnitOfMeasure` | `string?` (32) | The code the user typed in. Null means "base unit", so existing rows need no backfill. |
| `EntryQuantity` | `decimal?` | What the user typed. |
| `UnitConversionFactor` | `decimal?` | Multiply `EntryQuantity` by this to get the existing `Quantity`. |

The existing `Quantity` column keeps its meaning exactly — base unit — so nothing downstream
changes. `Quantity == EntryQuantity * UnitConversionFactor` is an invariant the domain should
enforce on construction.

Line entities to change (`backend/src/ISS.Domain/`):

```
Inventory/StockAdjustment.cs        Procurement/SupplierReturn.cs
Inventory/StockTransfer.cs          Sales/CustomerReturn.cs
Procurement/DirectPurchase.cs       Sales/DirectDispatch.cs
Procurement/GoodsReceipt.cs         Sales/DispatchNote.cs
Procurement/PurchaseOrder.cs        Sales/SalesInvoice.cs
Procurement/PurchaseRequisition.cs  Sales/SalesOrder.cs
Procurement/RequestForQuote.cs      Sales/SalesQuote.cs
                                    Service/MaterialRequisition.cs
                                    Service/ServiceEstimate.cs
                                    Service/ServiceJobMaterialDisposition.cs
```

Do **not** add them to `Inventory/InventoryMovement.cs` — movements are the base-unit ledger and
must stay unit-free. `Service/ServiceExpenseClaim.cs` is a judgement call; its quantities are
usually counts of receipts rather than of stock, so probably leave it.

That is one migration covering all of them. Configure precision `(18,4)` on the decimals and max
length 32 on the code, matching `Item.UnitOfMeasure`, in
`backend/src/ISS.Infrastructure/Persistence/IssDbContext.cs`.

### Conversion resolution belongs on the server

`resolveConversionFactor` currently lives in the purchase requisition form, in TypeScript. Move it
to a single application service, e.g. `UnitConversionService` in
`backend/src/ISS.Application/Services/`, with:

```csharp
Task<decimal?> ResolveFactorAsync(string fromCode, string toCode, CancellationToken ct);
```

Semantics, copied from the existing frontend implementation so behaviour does not change:

1. Same code → factor 1.
2. Active conversion `from → to` → its factor.
3. Active conversion `to → from` → `1 / factor`.
4. Otherwise null, and the caller returns a 400 naming both units.

Two-hop resolution (L → ML via a third unit) is **not** supported today. Leave it unsupported unless
asked; silent multi-hop conversion is hard to audit.

The client may still resolve a factor to show a live preview, but the server must resolve it again
and use its own answer. Never trust a factor sent by the client — that is a price-manipulation-shaped
hole for anything where quantity drives cost.

### API

Every add-line and update-line request that takes a quantity gains two optional fields:

```csharp
string? EntryUnitOfMeasure,
decimal? EntryQuantity
```

When both are supplied, the server resolves the factor, computes the base quantity, and stores all
three plus the derived `Quantity`. When they are absent, behaviour is exactly as today — this keeps
every existing caller and integration working.

Response DTOs should return all three so grids can show what was ordered *and* what it means in base
units.

### UI

In line grids, replace the single Qty column with a Qty + Unit pair:

- **Unit** — a select of active UoM codes, defaulting to the item's base unit.
- **Qty** — the number, in the chosen unit.
- Show the base-unit equivalent as muted helper text under the input whenever the chosen unit is not
  the base unit: `= 20,000 ML`. Never hide it; the operator must be able to sanity-check the factor.
- If no conversion rule exists between the two units, block submit with a message naming both codes
  and pointing at `Master Data > Unit Conversions`.

The `unit` hook added in Part A is then driven by `line.entryUnitOfMeasure ?? itemUomById.get(...)`
rather than the item's base unit alone.

### PDFs

`FormatQty(decimal, Item?)` currently prints the base unit. Add an overload that prefers the line's
entry unit and quantity when present, so the customer or supplier sees what was actually
transacted. Where both matter — goods receipt, especially — print the entry quantity in the Qty
column and the base equivalent in a secondary line, the way the invoice line description is
rendered in `SalesPdf.cs`.

### Migration of the existing workaround

Purchase requisition lines created by the current form have their conversion recorded in the `notes`
string, in the form `Requested 20 L converted to 20000 ML (factor 1000).` Once Part B lands, that
form should write the real columns instead. Do **not** attempt to parse historical notes into the
new columns — leave old rows with null entry units, which correctly means "recorded in base units",
and strip the auto-appended sentence from the form's note-building code so new notes stay clean.

### Verification for Part B

- Unit tests on `UnitConversionService`: same-unit, direct, inverse, missing rule, inactive rule.
- Unit test per changed aggregate that `Quantity == EntryQuantity * Factor` after construction.
- An end-to-end check that ordering 20 L of an item based in ML puts **20000** into
  `InventoryMovement.Quantity` on receipt, and that on-hand reads 20000 ML.
- A regression check that a line posted **without** entry-unit fields behaves exactly as before.

---

## Working notes for this repo

- Backend: `cd backend && dotnet build ISS.sln`. **Stop the API before building** or the build fails
  with `MSB3027` file locks. Find it with `netstat -ano | grep :5257`.
- Run the API: `cd backend/src/ISS.Api && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5257 dotnet run --no-build --no-launch-profile`
- Local DB is native Postgres on `localhost:5432/iss`. Do **not** point it at the compose instance
  on 5433 — that is a different database and the credentials differ.
- Migrations: `dotnet ef migrations add <Name> --project src/ISS.Infrastructure --startup-project src/ISS.Api --no-build`, then `database update` the same way.
- Tests: `dotnet test tests/ISS.UnitTests`. Integration tests need Docker.
- Frontend checks must both be run and both must exit 0: `npx tsc --noEmit`, `npx eslint "src/app/(app)" src/components`. Do not chain them with `;` and assume success — check the exit code.
