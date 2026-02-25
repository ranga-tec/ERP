# ISS ERP System

## Stack
- Backend: ASP.NET Core (.NET 8) + PostgreSQL
- Frontend: Next.js (App Router) + TypeScript + Tailwind

## Docs
- Gap checklist (proposal mapping): `docs/gap-checklist.md`
- User manual (quick): `docs/user-manual.md`
- Deployment / installation: `docs/deployment.md`
- System + technical maintainer guide: `docs/system-technical-maintainer-guide.md`
- Backend architecture + operations: `docs/backend-architecture.md`
- Frontend architecture + UI integration: `docs/frontend-architecture.md`
- Agent change playbook + troubleshooting: `docs/agent-change-playbook.md`
- CSV closure tracking baseline: `docs/csv-closure-audit.md`

## Local infrastructure (PostgreSQL)
From the repo root:

```bash
docker compose up -d
```

- PostgreSQL: `localhost:5433` (db `iss`, user `pgadmin`, password `vesper`)
- Note: the current `docker-compose.yml` starts PostgreSQL only (no pgAdmin service)

## Backend (API)

```bash
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

- API base URL (launch profile): `http://localhost:5257`
- Auth: the **first registered user becomes Admin**
- Startup DB initialization is controlled by `Database__InitializationMode`:
  - `EnsureCreated` (default in Development)
  - `Migrate` (recommended for controlled environments)
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
$env:ISS_INTEGRATIONTESTS_CONNECTION_STRING="Host=localhost;Port=5433;Database=iss_integration_local;Username=pgadmin;Password=vesper"
$env:ISS_INTEGRATIONTESTS_RESET_EXISTING_DB="1"
dotnet test backend/tests/ISS.IntegrationTests/ISS.IntegrationTests.csproj -c Release --nologo
```
