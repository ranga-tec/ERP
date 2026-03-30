# ISS ERP System

## Stack
- Backend: ASP.NET Core (.NET 8) + PostgreSQL
- Frontend: Next.js (App Router) + TypeScript + Tailwind

## Docs
- Gap checklist (proposal mapping): `docs/gap-checklist.md`
- Tester + trainer handbook: `docs/iss-tester-trainer-handbook.md`
- Role-based test checklists: `docs/role-based-test-checklists.md`
- User manual (quick): `docs/user-manual.md`
- Deployment / installation: `docs/deployment.md`
- Manual UAT / test script: `docs/manual-uat-guide.md`
- System + technical maintainer guide: `docs/system-technical-maintainer-guide.md`
- Assistant progress + GRN handover: `docs/assistant-progress.md`
- Backend architecture + operations: `docs/backend-architecture.md`
- Frontend architecture + UI integration: `docs/frontend-architecture.md`
- Agent change playbook + troubleshooting: `docs/agent-change-playbook.md`
- CSV closure tracking baseline: `docs/csv-closure-audit.md`
- Session handover notes: `docs/next-session-notes.md`

## Current Scope Highlights

- Master data includes:
  - items, brands, categories/subcategories, warehouses, suppliers, customers
  - UoMs and UoM conversions
  - taxes and tax conversions
  - currencies and FX rates
  - payment types and reference forms
- Procurement includes:
  - RFQ, purchase requisition, purchase order, goods receipt, direct purchase, supplier invoice, supplier return
- Sales includes:
  - quote, order, dispatch, direct dispatch, invoice, customer return
- Service includes:
  - equipment units, service contracts, jobs, work orders, estimates, expense claims, material requisitions, quality checks, handovers
- Finance includes:
  - AR/AP, payments + allocations, petty cash funds/ledger, credit notes, debit notes, finance approval/settlement of service expense claims
- Reporting includes:
  - dashboard, stock ledger, aging, tax summary, service KPIs, costing
- Draft document detail pages with line grids now support row actions:
  - `Edit`, `Save/Cancel`, and `Delete` (PO/GRN and all other line-based documents)
- Master-data maintenance grids now support row actions:
  - `Edit`, `Save/Cancel`, and `Delete` across brands/customers/suppliers/warehouses/UoMs/conversions/taxes/currencies/payment types/reference forms/categories/subcategories/reorder settings
  - items use separate list, create, view, and edit screens; the list grid exposes `View`, `Edit`, `Delete`, and label links
- Goods receipt from PO now supports PO-linked receipt planning:
  - creating a GRN from a PO loads every open PO line into the `Receive From PO` grid
  - users can receive only the lines/quantities delivered now and leave the balance for later GRNs
  - tracked serial/batch validation happens before posting, and the GRN screen includes search for both receipt-plan and draft-line tables
- Service workflow now supports:
  - equipment-unit warranty coverage with editable warranty end date and coverage scope
  - service contracts (`AMC`, `SLA`, `Warranty Extension`) linked to specific customer equipment units
  - `Service` and `Repair` job types on service jobs
  - open service jobs can be edited before work starts; saving re-evaluates entitlement against the selected unit/customer
  - automatic entitlement snapshot on service jobs from active equipment warranty or service contract, plus manual entitlement refresh on the job
  - work-order labor entries with draft, submitted, approved, rejected, and invoiced states
  - draft service estimate headers and lines remain editable until approval or rejection
  - estimate revisions so approved/rejected estimates are preserved and additional findings can be resent as a new draft revision
  - direct purchases linked to service jobs for outside emergency buys
  - service expense claims for petty-cash and out-of-pocket spending, with finance approval and settlement tracking
  - billable expense-claim lines can be converted into the working estimate or an automatic estimate revision
  - handover-to-invoice conversion now supports expense estimate lines with fallback invoice item mapping and approved timesheet-based labor billing
  - service job detail now includes actual job-cost rollup across material issues, direct purchases, approved labor, expense claims, estimates, and invoices
- Finance workflow now supports:
  - petty cash funds with opening balance, top-up, adjustment, and expense-settlement ledger entries
- The authenticated sidebar now opens expanded by default and includes top-of-menu search when expanded

## Local infrastructure (PostgreSQL)
Use a local PostgreSQL server on the machine running the app.

- PostgreSQL: `localhost:5432`
- Main database: `iss`
- Integration-test database: `iss_integration_local`
- User: `pgadmin`
- Password: `vesper`
- Docker Compose is optional and is no longer the default local database path for this repo

## Backend (API)

```bash
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

- API base URL (launch profile): `http://localhost:5257`
- Auth: the **first registered user becomes Admin**
- Fresh databases auto-seed default currencies, payment types, tax codes, and reference forms needed for core finance/reporting flows
- Startup DB initialization is controlled by `Database__InitializationMode`:
  - `EnsureCreated`
  - `Migrate` (default in Development and recommended for controlled environments)
  - `None` (default in non-Development)
- Health endpoint: `GET /health`

For production deployment steps (migrations, env vars, health checks), see `docs/deployment.md`.

## Frontend (Web)

```bash
cd frontend
copy .env.example .env.local
npm install
npm run dev
```

Open `http://localhost:3000`.

## Tests

```bash
dotnet test backend/tests/ISS.UnitTests/ISS.UnitTests.csproj -c Release
dotnet test backend/tests/ISS.IntegrationTests/ISS.IntegrationTests.csproj -c Release

cd frontend
npm run lint
npm run build
```

If Docker/Testcontainers is unavailable in your shell, integration tests can use an existing PostgreSQL instance:

```powershell
$env:ISS_INTEGRATIONTESTS_CONNECTION_STRING="Host=localhost;Port=5432;Database=iss_integration_local;Username=pgadmin;Password=vesper"
$env:ISS_INTEGRATIONTESTS_RESET_EXISTING_DB="1"
dotnet test backend/tests/ISS.IntegrationTests/ISS.IntegrationTests.csproj -c Release --nologo
```
