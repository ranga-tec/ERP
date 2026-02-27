# Next Session Resume Notes

## Snapshot

- Repo: `D:\VScode Projects\ISS`
- Branch: `main`
- Status at authoring: broad backend/frontend/docs changes staged for commit and push
- Purpose: fast restart context after terminal/session interruption

## What Was Completed In This Session

### Functional coverage added
- Master data modules: `unit conversions`, `taxes`, `tax conversions`, `currencies`, `currency rates`, `payment types`, `reference forms`
- New costing report endpoint/UI added under reporting
- Line-level editing and deletion support implemented across draft document grids in procurement, sales, inventory, and service

### Reliability fix
- Direct Purchase creation failure (`NpgsqlRetryingExecutionStrategy` with user transaction) addressed by wrapping document-number generation logic with EF execution strategy in `DocumentNumberService`
- Related redundant manual transaction usage was removed from import path to stay retry-strategy-safe

### UI and API behavior alignment
- Added row action buttons for line grids where add-only behavior existed before
- Standardized draft line endpoints to support create/update/delete consistently

## Documentation Updated

- `README.md`
- `frontend/README.md`
- `docs/system-technical-maintainer-guide.md`
- `docs/backend-architecture.md`
- `docs/frontend-architecture.md`
- `docs/user-manual.md`
- `docs/gap-checklist.md`
- `docs/requirements.md`
- `docs/master-data-costing-best-practice-notes.md`
- `frontend/docs/iss-system-technical-documentation.md`

## Validation Summary (This Workstream)

- Backend build: passed
- Unit tests: passed
- Integration tests: passed
- Frontend production build: passed

## Runbook

### Start infra
```powershell
cd D:\VScode Projects\ISS
docker compose up -d
```

### Backend
```powershell
cd D:\VScode Projects\ISS
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

### Frontend
```powershell
cd D:\VScode Projects\ISS\frontend
copy .env.example .env.local
npm install
npm run dev
```

## Priority Next Checks

1. Full UAT pass on all line-edit/delete flows (focus: validation and totals refresh)
2. Role/permission verification for new master-data screens
3. Reporting accuracy checks for costing output against expected calculation samples
4. Final responsive pass and CSV closure review
