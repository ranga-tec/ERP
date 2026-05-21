# Revised Agent Handover - 2026-05-21

Use this document to resume the ISS ERP work in a fresh chat without rediscovering the recent context.

## Environment

- Repo: `D:\VScode Projects\ISS`
- Branch: `main`
- Backend API: `http://127.0.0.1:5257`
- Frontend: `http://127.0.0.1:3000`
- Local admin login: `admin@local` / `Passw0rd1`
- Last confirmed backend health: `200 Healthy`
- GitHub remote: `https://github.com/ranga-tec/ERP.git`

## Latest Pushed Commits

- `29af56d` - `Update service returns and finance workflows`
- `5875be2` - `Add typed confirmation for job completion`

## Current Unpushed Work

There are currently local changes for the service job materials UI and service costing DTO:

- `backend/src/ISS.Application/Services/ServiceCostingService.cs`
- `frontend/src/app/(app)/service/jobs/[id]/page.tsx`
- `frontend/src/app/(app)/service/jobs/ServiceJobMaterialDispositionAddForm.tsx`
- `docs/testing-input-output-checklist.md`
- this handover document

Untracked local artifacts still intentionally not pushed:

- `Modification reqs/`
- `data/`
- `docs/testing-input-output-checklist.pdf`

Before pushing, run:

```powershell
git status --short
dotnet build backend/src/ISS.Application/ISS.Application.csproj --nologo
cd frontend
npx eslint "src/app/(app)/service/jobs/[id]/page.tsx" "src/app/(app)/service/jobs/ServiceJobMaterialDispositionAddForm.tsx"
```

## Major Changes Already Implemented

### Authentication And Test Data

- Admin user exists locally: `admin@local` / `Passw0rd1`.
- Test master data was created for items, supplier, customer, warehouses, and bins.
- Seed script added: `scripts/seed-uat-test-data.ps1`.

### Sales Invoices From Source Documents

Sales invoice creation now supports:

- AOD / dispatch source selection
- direct dispatch source selection
- manual invoice creation

Backend endpoints added:

- `POST /api/sales/invoices/from-dispatch`
- `POST /api/sales/invoices/from-direct-dispatch`

Key files:

- `backend/src/ISS.Application/Services/SalesService.cs`
- `backend/src/ISS.Api/Controllers/Sales/InvoicesController.cs`
- `frontend/src/app/(app)/sales/invoices/page.tsx`
- `frontend/src/app/(app)/sales/invoices/InvoiceCreateForm.tsx`

### Customer Returns

Implemented:

- Draft invoices are excluded from customer-return invoice selection.
- If a customer return references an invoice, selectable return items are restricted to that invoice's items.
- If no invoice is selected, all items remain selectable.
- Standalone stock visibility selector was removed from customer return detail to avoid duplicate item-entry UI.

Key files:

- `backend/src/ISS.Application/Services/SalesService.cs`
- `frontend/src/app/(app)/sales/customer-returns/page.tsx`
- `frontend/src/app/(app)/sales/customer-returns/[id]/page.tsx`
- `frontend/src/app/(app)/sales/customer-returns/CustomerReturnLineAddForm.tsx`

### Finance Credit Note Standardization

User asked to align with SAP/standard ERP terminology.

Implemented:

- A/R Credit Notes route: `/finance/ar-credit-notes`
- A/P Credit Notes route: `/finance/ap-credit-notes`
- Generic `/finance/credit-notes` redirects to A/R credit notes.
- Credit note detail labels itself A/R or A/P based on counterparty type.
- AR page shows customer credit notes.
- AP page shows supplier credit notes.

Backend still uses a shared `CreditNotes` table/model.

Key files:

- `frontend/src/app/(app)/finance/ar-credit-notes/page.tsx`
- `frontend/src/app/(app)/finance/ap-credit-notes/page.tsx`
- `frontend/src/app/(app)/finance/credit-notes/CreditNoteListTable.tsx`
- `frontend/src/app/(app)/finance/credit-notes/CreditNoteCreateForm.tsx`
- `frontend/src/app/(app)/finance/credit-notes/[id]/page.tsx`
- `frontend/src/components/Sidebar.tsx`

### Equipment Warranty UX

Warranty coverage is no longer visually locked until a warranty date is entered.

Rules:

- Warranty coverage and warranty end date must be provided together.
- Entering warranty date while coverage is `No Warranty` defaults coverage to `Labor and Parts`.

Key files:

- `frontend/src/app/(app)/service/equipment-units/EquipmentUnitCreateForm.tsx`
- `frontend/src/app/(app)/service/equipment-units/EquipmentUnitEditForm.tsx`

### Service Job Material Returns

Current pushed behavior:

- Material disposition saves as draft first.
- Draft does not update inventory.
- Draft can be edited or voided.
- `Post` updates inventory only for returnable outcomes.
- Damaged material does not return to usable stock.
- Existing active records from the old immediate-post behavior were migrated as posted.

Migration:

- `20260521053118_AddServiceJobMaterialDispositionPosting`

Key files:

- `backend/src/ISS.Domain/Service/ServiceJobMaterialDisposition.cs`
- `backend/src/ISS.Application/Services/ServiceManagementService.cs`
- `backend/src/ISS.Api/Controllers/Service/ServiceJobsController.cs`
- `frontend/src/app/(app)/service/jobs/ServiceJobMaterialDispositionActions.tsx`
- `frontend/src/app/(app)/service/jobs/ServiceJobMaterialDispositionAddForm.tsx`

### Service Job Materials UI - Current Local Work

The latest local work splits `Job -> Materials` into three sub-views:

- `Issued MRNs`
- `Return Materials`
- `Damage Material`

Behavior:

- `Issued MRNs` shows posted MRNs grouped by MRN number.
- MRN group shows daily sheet number when linked.
- Expanding a group shows issued item lines.
- `Return Materials` has a return-only disposition form/list.
- `Damage Material` has a damage-only disposition form/list.

Backend DTO update:

- `ServiceCostingService.MaterialCostLine` now includes:
  - `OccurredAt`
  - `ServiceJobDailySheetId`
  - `ServiceJobDailySheetNumber`
  - `WarehouseId`
  - `WarehouseCode`

Validation already run for this local work:

- `dotnet build backend/src/ISS.Application/ISS.Application.csproj --nologo` passed.
- Focused ESLint for job materials files passed.
- Backend restarted and health returned `200`.

Important: This local service materials UI work has not been pushed yet as of this handover.

### Service Job Completion Safety

Clicking `Complete` on a job now opens a dialog.

The user must type:

```text
COMPLETE
```

Then the `Complete Job` button is enabled.

Commit pushed:

- `5875be2`

Key file:

- `frontend/src/app/(app)/service/jobs/ServiceJobActions.tsx`

### Sidebar Hydration Fix

Fixed hydration mismatch caused by localStorage-dependent sidebar state during first render.

Key files:

- `frontend/src/components/Sidebar.tsx`
- `frontend/src/components/AppShell.tsx`

## Performance Smoke Test Results

Local dev smoke test ran on 2026-05-21.

Limitations:

- Dev API and Next dev server.
- PowerShell timing overhead.
- Not a production capacity test.

Backend highlights:

- API health: `200 Healthy`
- Slowest endpoint average: login around `796ms`
- Service job costing average: around `383ms`
- `20/20` concurrent service costing requests succeeded

Frontend dev page highlights:

- `/master-data/items`: around `2546ms` avg
- `/service/jobs/{id}?tab=materials&materialView=issues`: around `2185ms` avg
- `/service/jobs`: around `2040ms` avg
- `/sales/invoices`: around `1913ms` avg

Recommended next performance step:

- Re-run against `next build` and `next start`.
- Consider a combined service-job detail API if job detail latency matters in production.
- Watch `service/jobs/{id}/costing`, `items/options`, and reporting endpoints as data grows.

## Current Product Direction Decisions

Service material best practice chosen:

- MRN issue consumes stock.
- If issued material is used, users do not need to record it again.
- Material return/damage workflow is for exceptions only:
  - not needed
  - wrongly issued
  - supplier rejected
  - damaged/unusable

Finance credit note best practice chosen:

- Customer returns create A/R credit notes.
- Supplier returns create A/P credit notes.
- Shared backend table is acceptable, but UI must separate A/R and A/P terminology.

## Suggested Next Work

1. Review and push the current local service materials UI/doc changes.
2. Consider a dedicated material issue register API instead of relying on costing DTO if the UI grows.
3. Add Playwright coverage for:
   - job materials sub-tabs
   - MRN expansion
   - return draft/post
   - damage draft/post
4. Run production-mode performance smoke test:

```powershell
cd frontend
npm run build
npm run start
```

5. Decide whether to replace local `<details>` expansion with the shared data grid later. The current shared editable data grid does not provide a ready master-detail expansion API.

## Useful Commands

Start backend:

```powershell
$repo='D:\VScode Projects\ISS'
$backendArgs=@('-NoProfile','-ExecutionPolicy','Bypass','-Command',"Set-Location '$repo'; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --no-build --project backend/src/ISS.Api/ISS.Api.csproj --urls http://127.0.0.1:5257")
Start-Process -FilePath powershell.exe -ArgumentList $backendArgs -WorkingDirectory $repo -WindowStyle Hidden -RedirectStandardOutput (Join-Path $repo '.local-api-uat.out.log') -RedirectStandardError (Join-Path $repo '.local-api-uat.err.log')
```

Health check:

```powershell
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:5257/health
```

Focused frontend lint:

```powershell
cd frontend
npx eslint "src/app/(app)/service/jobs/[id]/page.tsx" "src/app/(app)/service/jobs/ServiceJobMaterialDispositionAddForm.tsx"
```

Push:

```powershell
git add <files>
git commit -m "..."
git push origin main
```
