# Next Session Resume Notes

## Snapshot

- Repo: `D:\VScode Projects\ISS`
- Branch: `main`
- Current purpose: resume from the deployed ERP-density, searchable-dropdown, finance account-mapping, chart-of-accounts workspace, and reusable line-grid rollout checkpoints
- Current tracked code state: no pending tracked code WIP
- Current local artifacts:
  - untracked `.playwright-cli/`
  - untracked `images/`

## Current GitHub Checkpoints

Most relevant recent commits on `main`:

- `20225c0` `Tighten ERP UI density and shared grid styling`
- `a1d7ed2` `Make app dropdowns searchable`
- `fd7426a` `Add category default account mappings`
- `97a359d` `Resolve accounts on transaction lines`
- `dc50cab` `Roll out shared line editors to wave 2 documents`
- `5f69cda` `Add chart of accounts workspace modes`
- `cc0f80b` `Roll out tracked document line editors`

## Railway Production State

- `iss-api`: latest deployment `b96ede1d-f774-4115-8acd-cd58c53355b6`
- `iss-web`: latest deployment `a50572cc-d1fc-40bb-a027-560d102d23af`
- Live URLs:
  - `https://iss-api-production.up.railway.app`
  - `https://iss-web-production.up.railway.app`

## Current Frontend State

### Shared UI behavior now live

- compact ERP-style density is live across the app
- the shared `Select` now routes through `frontend/src/components/SearchableSelect.tsx`
- editable grid select cells also use searchable selection
- lookup/grid rendering now falls back to the stored value when an option list is incomplete
- service job equipment-unit selection is searchable and preserves the saved value on reopen

### Reusable line-grid rollout now live

Shared module:

- `frontend/src/components/data-grid/`

Live screens on the reusable grid:

- GRN receipt plan
- purchase-order lines
- invoice lines
- quotes
- sales orders
- RFQs
- purchase requisitions
- dispatches
- direct dispatches
- customer returns
- supplier returns

Wave checkpoints:

- framework extraction: `8ebd929`
- quotes / sales orders / RFQs / purchase requisitions: `dc50cab`
- dispatches / direct dispatches / customer returns / supplier returns: `cc0f80b`

### Chart of accounts UI now live

Finance accounts page now supports:

- `Classic` workspace mode
- `Priority Grid` workspace mode
- inline row create/edit/delete in the dense grid mode

Relevant files:

- `frontend/src/app/(app)/finance/accounts/page.tsx`
- `frontend/src/app/(app)/finance/accounts/LedgerAccountsWorkspace.tsx`
- `frontend/src/app/(app)/finance/accounts/LedgerAccountsClassicView.tsx`
- `frontend/src/app/(app)/finance/accounts/LedgerAccountsGrid.tsx`

Commit:

- `5f69cda`

## Current Finance Mapping State

Implemented:

- item-level default revenue / expense account assignment
- category-level default revenue / expense account assignment
- transaction-line account resolution snapshots on:
  - sales invoices
  - direct purchases
  - service expense claims

Relevant commits:

- `fd7426a` category defaults
- `97a359d` transaction-line account resolution

Important scope limit:

- this is account determination / mapping support
- it is not yet a full GL journal posting engine
- it does not yet mean all operational documents create balanced ledger journals automatically

## Recommended Next Engineering Steps

1. Continue the reusable line-grid rollout to the remaining one-off line screens:
   - direct purchases
   - stock transfers
   - stock adjustments
   - service estimates
   - service expense claims
   - material requisitions
2. Decide whether finance should stop at account determination or continue into real journal posting automation.
3. After the remaining grid rollout is stable, remove or archive unused `*LineRow.tsx` components that are no longer wired into detail pages.

## Files Most Relevant For The Next Agent

- Searchable shared controls:
  - `frontend/src/components/SearchableSelect.tsx`
  - `frontend/src/components/ui.tsx`
  - `frontend/src/components/data-grid/EditableDataTable.tsx`
  - `frontend/src/components/data-grid/LookupCell.tsx`
- Finance account mapping:
  - `backend/src/ISS.Api/Controllers/ItemsController.cs`
  - `backend/src/ISS.Api/Controllers/MasterData/ItemCategoriesController.cs`
  - `backend/src/ISS.Application/Services/DocumentAccountMappingService.cs`
  - `backend/src/ISS.Application/Services/SalesService.cs`
  - `backend/src/ISS.Application/Services/ProcurementService.cs`
  - `backend/src/ISS.Application/Services/ServiceManagementService.cs`
- Finance accounts workspace:
  - `frontend/src/app/(app)/finance/accounts/page.tsx`
  - `frontend/src/app/(app)/finance/accounts/LedgerAccountsWorkspace.tsx`
  - `frontend/src/app/(app)/finance/accounts/LedgerAccountsGrid.tsx`
- Remaining non-grid line-row screens:
  - `frontend/src/app/(app)/procurement/direct-purchases/DirectPurchaseLineRow.tsx`
  - `frontend/src/app/(app)/inventory/stock-transfers/StockTransferLineRow.tsx`
  - `frontend/src/app/(app)/inventory/stock-adjustments/StockAdjustmentLineRow.tsx`
  - `frontend/src/app/(app)/service/estimates/ServiceEstimateLineRow.tsx`
  - `frontend/src/app/(app)/service/expense-claims/ServiceExpenseClaimLineRow.tsx`
  - `frontend/src/app/(app)/service/material-requisitions/MaterialRequisitionLineRow.tsx`

## Operational Notes

- if another push hangs locally, use the explicit owner-qualified remote form:

```powershell
$env:GCM_INTERACTIVE='Never'
git push "https://ranga-tec@github.com/ranga-tec/ERP.git" main:main
```

- if another Railway deploy is needed, use a detached worktree from the exact commit being released and deploy only the intended root:

```powershell
git worktree add --detach D:\VScode Projects\ISS-deploy-<commit> <commit>
railway up D:\VScode Projects\ISS-deploy-<commit>\frontend --path-as-root --service iss-web --environment production --detach
```
