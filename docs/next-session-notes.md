# Next Session Resume Notes (Pre-Reboot)

## Snapshot

- Repo: `D:\VScode Projects\ISS`
- Branch: `main`
- Latest pushed commit: `064f16a`
- Purpose of this note: resume quickly after Windows reboot / terminal chat context loss.

## What Was Recently Completed (Pushed)

### Core hardening and closure checkpoints
- `2970053` - attachment upload hardening + CSV closure audit baseline
- `de63c7f` - integration tests can use external PostgreSQL fallback (bypass Testcontainers/Docker pipe issues)
- `ab0ed44` - docs alignment for local Postgres compose settings (`5433`, `pgadmin`, `vesper`) + test fallback docs
- `87eb41a` - first-wave reporting endpoints + UI + integration coverage
  - `/api/reporting/stock-ledger`
  - `/api/reporting/aging`
  - `/api/reporting/tax-summary`
  - `/api/reporting/service-kpis`
- `22ca3e7` - reorder alerts -> purchase requisition draft automation (API + UI + integration test)
- `064f16a` - maintainer docs split into backend/frontend/playbook guides + README updates

### Earlier production-hardening milestones (already pushed before the above)
- DB-backed `/health` check
- supplier-invoice/customer-return failure-path integration tests
- JWT startup guard for non-development (`Jwt:Key` validation)
- `scripts/ops/smoke-api.ps1` and CI smoke adoption

## Current Product State (Honest Summary)

- Major ERP/service modules are implemented and usable.
- Current work is mainly production-hardening + CSV closure (completeness / edge cases / workflow depth).
- Not 100% CSV-complete yet.

Working estimate discussed:
- Core product implementation: ~85-90%
- CSV/action-level closure + production signoff: ~70-80%

## Most Important Docs (Read First Next Session)

### Hub and architecture docs
- `docs/system-technical-maintainer-guide.md` (hub/index)
- `docs/backend-architecture.md`
- `docs/frontend-architecture.md`
- `docs/agent-change-playbook.md`

### Scope / closure / runbooks
- `docs/csv-closure-audit.md` (current closure baseline and remaining gaps)
- `docs/deployment.md`
- `README.md`
- `frontend/README.md`

## Local Environment State (Important)

### Database schema issue was fixed locally
A local runtime error occurred on PR page (`DocumentComments` table missing) because the DB schema was older than current code.

I reset and re-migrated the local Docker Postgres database (`iss`) successfully.

Actions already performed:
- Dropped and recreated DB `iss` in container `iss-postgres`
- Applied EF migrations successfully:
  - `20260223065027_InitialCreate`
  - `20260223072919_AddServiceHandoverInvoiceLink`
  - `20260223091521_AddDocumentCommentsAndAttachments`
- Verified tables exist:
  - `DocumentComments`
  - `DocumentAttachments`
  - `__EFMigrationsHistory`

### Consequence
- Local DB data was wiped (as approved).
- After reboot/startup, you must:
  1. start backend API
  2. open frontend
  3. register a user again (first user becomes Admin)

## Current Local Git Status (Before Reboot)

Only local/non-product files are uncommitted:
- modified: `.claude/settings.local.json`
- untracked: `docs/inventory list (1).csv`

No pending code changes in repo worktree.

## Run / Test Commands (Known Good)

### Start local infra (repo compose)
```powershell
cd D:\VScode Projects\ISS
docker compose up -d
```

Repo compose Postgres defaults:
- Host: `localhost`
- Port: `5433`
- DB: `iss`
- User: `pgadmin`
- Password: `vesper`

### Start backend
```powershell
cd D:\VScode Projects\ISS
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

### Start frontend
```powershell
cd D:\VScode Projects\ISS\frontend
copy .env.example .env.local
npm install
npm run dev
```

### URLs
- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5257`
- Health: `http://localhost:5257/health`

### Backend tests (normal)
```powershell
cd D:\VScode Projects\ISS
dotnet test backend/tests/ISS.UnitTests/ISS.UnitTests.csproj -c Release --nologo
dotnet test backend/tests/ISS.IntegrationTests/ISS.IntegrationTests.csproj -c Release --nologo
```

### Integration tests fallback (if Docker/Testcontainers pipe is blocked in shell)
```powershell
cd D:\VScode Projects\ISS
$env:ISS_INTEGRATIONTESTS_CONNECTION_STRING="Host=localhost;Port=5433;Database=iss_integration_local;Username=pgadmin;Password=vesper"
$env:ISS_INTEGRATIONTESTS_RESET_EXISTING_DB="1"
dotnet test backend/tests/ISS.IntegrationTests/ISS.IntegrationTests.csproj -c Release --nologo
```

## Recent Validations (Before This Note)

- Frontend build passed (`npm run build`) after reporting pages and reorder-planning UI changes
- Backend integration tests passed using external Postgres fallback:
  - `33/33` after reporting first-wave
  - `34/34` after reorder-alerts -> PR automation
- Docs split checkpoint (`064f16a`) was docs-only (no tests run)

## What Was Added in Reporting / Planning (Useful to Remember)

### Reporting first wave
Backend endpoints:
- stock ledger
- AR/AP aging
- tax summary
- service KPIs

Frontend pages:
- `/reporting`
- `/reporting/stock-ledger`
- `/reporting/aging`
- `/reporting/tax-summary`
- `/reporting/service-kpis`

### Reorder planning improvement
- New backend automation endpoint:
  - `POST /api/inventory/reorder-alerts/create-purchase-requisition`
- New UI action button on reorder alerts page to create PR draft for selected warehouse

## Remaining High-Value Work (Recommended Next)

Still not 100% CSV-complete. Best next slices:

1. RFQ compare/award workflow (high closure value)
2. Stock transfer multi-stage receive flow (issue/in-transit/receive)
3. Reporting second wave (valuation, supplier performance, profitability)
4. Master-data enrichment (UoM conversions, supplier/customer finance fields, brand metadata/logo)
5. Final responsive/UAT pass + row-by-row CSV verification

## If Docker Desktop Is Still Broken After Reboot

- Start Windows and launch Docker Desktop first
- Wait until Docker engine is actually running
- Verify with:
```powershell
docker version
docker ps
```
- If needed, use the admin PowerShell recovery commands already discussed (service + WSL reset)

## Suggested Resume Prompt (for a new agent/session)

Use something like:

> Continue from commit `064f16a` in `D:\VScode Projects\ISS`. Read `docs/system-technical-maintainer-guide.md`, `docs/backend-architecture.md`, `docs/frontend-architecture.md`, `docs/agent-change-playbook.md`, and `docs/csv-closure-audit.md`. Latest work includes reporting first-wave endpoints/UI and reorder-alerts -> PR automation. Local DB `iss` was reset and migrated successfully; need to run app and re-register admin user. Then continue CSV closure work from the highest-value remaining workflow-depth gaps.
