# Next Session Resume Notes

## Snapshot

- Repo: `D:\VScode Projects\ISS`
- Branch: `main`
- Current purpose: resume from the service-job module UX revamp (modal forms, viewport-fit overview, AppFormModal pattern) and proceed to reporting, PDF, finance refinements, and deployment
- Current tracked code state: latest local commits include the full Job Order UX revamp session (2026-06-04)
- Current local artifacts (untracked, not committed):
  - `frontend/iss_pw*.cjs` — Playwright test scripts from UX review session (safe to delete or .gitignore)
  - `SS/` — UI screenshots from Playwright verification
  - `data/`
  - `Modification reqs/`
  - `docs/testing-input-output-checklist.pdf`
  - `image.png`

## What Was Completed This Session (2026-06-04)

### Job Order Module — Full UI/UX Revamp

- `AppFormModal` component added (`frontend/src/components/AppFormModal.tsx`): client-side modal dialog for all create/edit forms
- All job order create/edit forms converted to modals (see user manual section 7–18)
- Job list: list-first layout, `+ New Job Order` modal button top-right, Edit from list row → modal
- Job detail header: compact 2-line header, status/type badges, `Show dates & details ▾` toggle, inline PDF and workflow actions
- Tab navigation: all links append `#tab-content` → browser scrolls directly to tab content on click
- Overview tab: Job Cockpit + Process Timeline fit in one 1440×900 viewport; Billing Entitlement moved to Billing tab; no extra sections below the timeline except collapsed Edit Job and Job Intake
- Daily Sheets: `+ Create First Daily Sheet` modal CTA when empty; `+ Add Another Day` in header when sheets exist
- Staff/Labor and Progress: clean empty-state with `Go to Daily Sheets` button when no sheet selected (no disabled form shown)
- Plan tab: `+ Add Operation` button-styled row; form expands inline
- Materials tab: `+ New MRN` button-styled row
- Billing tab: Warranty/Billing Entitlement section added here
- User manual rewritten: `docs/job-orders-user-manual.md`
- Agent handover updated: `docs/agent-handover-service-job-daily-operations.md`

### Local Server State

- Production `next start` running on port 3000 (ISS ERP, full build with all changes)
- Dev `next dev` running on port 3003 (ISS ERP, Turbopack, live changes)
- Backend running on port 5257

## Current GitHub Checkpoints

Most relevant recent commits on `main`:

- `1b2788f` `Add service job section testing document`
- `4a30445` `Move help link into service menu`
- `f62b95c` `Add rendered help manual page`
- `d1cfe98` `Expand service job section manual`
- `9d0dfe2` `Add job orders user manual`
- `a490ba3` `Open job list edit in modal`
- `b1dced2` `Promote service job form modal`
- `0e1e0db` `Fix IOU date UTC handling`
- `20266a6` `Refine service job expense workflows`
- `c83df06` `Fix job order modal close behavior`
- `ae5994f` `Add job order modal entry forms`
- `9d01963` `Document and push job order UX revamp`
- `415a60e` `Remove service job next actions panel`
- `6cd43e6` `Compact service job overview dashboard`
- `2c07a87` `Improve service job workflow navigation`

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
