# Next Session Resume Notes

## Snapshot

- Repo: `D:\VScode Projects\ISS`
- Branch: `main`
- Current purpose: resume from the deployed service-job workflow tabs, clickable closeout readiness, daily-sheet-driven staff/progress flow, ERP-density, searchable-dropdown, finance account-mapping, chart-of-accounts workspace, reusable line-grid rollout, and direct-edit workflow checkpoints
- Current tracked code state: latest pushed/deployed service-job workflow baseline is `2e29358`
- Current local artifacts:
  - untracked `.playwright-cli/`
  - untracked `images/`

## Current GitHub Checkpoints

Most relevant recent commits on `main`:

- `11752e7` `Show stock ledger document numbers`
- `9788070` `Add service job operations planning`
- `3b41460` `Normalize Railway start script line endings`
- `909f5b1` `Fix service job operations actual labor query`
- `acc3298` `Simplify service job detail workflow`
- `8af1961` `Organize service job detail into workflow tabs`
- `93432b9` `Revise service job testing checklist for tabs`
- `debe9df` `Refine service job daily work flow`
- `2e29358` `Link service job closeout checks to workflows`
- `20225c0` `Tighten ERP UI density and shared grid styling`
- `a1d7ed2` `Make app dropdowns searchable`
- `fd7426a` `Add category default account mappings`
- `97a359d` `Resolve accounts on transaction lines`
- `dc50cab` `Roll out shared line editors to wave 2 documents`
- `5f69cda` `Add chart of accounts workspace modes`
- `cc0f80b` `Roll out tracked document line editors`
- `3d97d3d` `Fix finance list actions and warehouse selectors`
- `72b504f` `Fix direct edit entry and service draft workflows`

## Current Production Deployment State

- current active production target for the latest service-job work is Railway
- latest deployed commit: `2e29358`
- live Railway URL: `https://erp-production-e16a.up.railway.app`
- deploy command:
  - `npx @railway/cli@latest up --service ERP --environment production --detach`
- deploy from a clean detached worktree to avoid uploading unrelated local changes
- verified after latest deployment:
  - `/login` returned `200`
  - job detail returned `200`
  - daily-work sub-tabs returned `200`
  - daily sheet labor/progress deep links returned `200`

Historical VPS deployment notes remain below for reference:

- prior production approach used a single Ubuntu VPS running Docker Compose
- provider: Contabo
- public server IP: `178.238.230.31`
- raw-IP URL: `http://178.238.230.31`
- tracked deploy assets:
  - `deploy/docker-compose.vps.yml`
  - `deploy/.env.example`
  - `deploy/backup.sh`
- server application root: `/opt/iss`
- live runtime env file: `/opt/iss/deploy/.env` and it is not committed to git
- persistent runtime data:
  - PostgreSQL Docker volume `iss_postgres_data`
  - backend file-storage Docker volume `iss_api_app_data`
- scheduled backup:
  - `0 2 * * * /opt/iss/deploy/backup.sh >> /opt/iss-backups/backup.log 2>&1`
- operator access rule:
  - use SSH key access through the non-root deploy user
  - do not re-enable password or root SSH login
- HTTP/TLS note:
  - raw-IP HTTP deployments require `ISS_SECURE_COOKIES=false` and `SECURITY_ENFORCE_HTTPS=false`
  - after attaching real HTTPS, set both values to `true`

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

### Direct-edit entry behavior now live

The latest release now includes this behavior:

- list-page `Edit` opens shared-grid documents directly in edit mode through `?mode=edit`
- the detail pages hide the separate add-line form while in direct-edit mode and show a short `Switch to Add Line` notice instead
- this avoids the current UX confusion where the blank add-line form looks like a broken saved row with an empty item selector

Documents covered by this direct-edit entry change:

- quotes
- sales orders
- invoices
- dispatches
- direct dispatches
- customer returns
- purchase orders
- direct purchases
- purchase requisitions
- RFQs
- supplier returns
- stock adjustments
- service estimates

Shared file:

- `frontend/src/components/DocumentDirectEditNotice.tsx`

### Service handover and material requisition editing now live

The latest release also includes:

- a real draft update workflow for service handovers
- explicit list-page `View` / `Edit` actions for material requisitions
- direct edit entry for draft material-requisition lines

Relevant files:

- `backend/src/ISS.Domain/Service/ServiceHandover.cs`
- `backend/src/ISS.Application/Services/ServiceManagementService.cs`
- `backend/src/ISS.Api/Controllers/Service/ServiceHandoversController.cs`
- `frontend/src/app/(app)/service/handovers/page.tsx`
- `frontend/src/app/(app)/service/handovers/[id]/page.tsx`
- `frontend/src/app/(app)/service/handovers/ServiceHandoverEditForm.tsx`
- `frontend/src/app/(app)/service/material-requisitions/page.tsx`
- `frontend/src/app/(app)/service/material-requisitions/[id]/page.tsx`
- `frontend/src/app/(app)/service/material-requisitions/MaterialRequisitionLineRow.tsx`

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
   - stock transfers
   - service expense claims
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
  - `frontend/src/app/(app)/inventory/stock-transfers/StockTransferLineRow.tsx`
  - `frontend/src/app/(app)/service/expense-claims/ServiceExpenseClaimLineRow.tsx`
  - `frontend/src/app/(app)/service/material-requisitions/MaterialRequisitionLineRow.tsx`
  - `frontend/src/app/(app)/service/work-orders/WorkOrderTimeEntryRow.tsx`
  - `frontend/src/app/(app)/service/quality-checks/[id]/page.tsx`

## Operational Notes

- if another push hangs locally, use the explicit owner-qualified remote form:

```powershell
$env:GCM_INTERACTIVE='Never'
git push "https://ranga-tec@github.com/ranga-tec/ERP.git" main:main
```

- if another VPS deploy is needed, sync the changed `backend/`, `frontend/`, and `deploy/` directories to `/opt/iss`, then rebuild from the server:

```bash
docker compose --env-file /opt/iss/deploy/.env -f /opt/iss/deploy/docker-compose.vps.yml up -d --build
```
