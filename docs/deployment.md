# Deployment / Installation

This repo contains a complete ISS ERP system:

- Backend: ASP.NET Core (.NET 8) + PostgreSQL
- Frontend: Next.js (App Router) + TypeScript + Tailwind

## Local infrastructure (PostgreSQL + pgAdmin)

From the repo root:

```bash
docker compose up -d
```

- PostgreSQL: `localhost:5432` (db `iss`, user `pward`, password `vesper`)
- pgAdmin: `http://localhost:5050` (email `vesper@local`, password `vesper`)

## Backend (API)

The API requires a connection string. Example (PowerShell):

```powershell
$env:ConnectionStrings__Default="Host=localhost;Database=iss;Username=pward;Password=vesper"
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

Notes:

- Startup DB initialization is controlled by `Database__InitializationMode`:
  - `EnsureCreated` (default in Development)
  - `Migrate` (recommended for new environments / controlled deployments)
  - `None` (default in non-Development)
- Roles are seeded on startup.
- The first registered user becomes `Admin`.
- Health endpoint: `GET /health` (now includes a DB connectivity check, not just process liveness)

### EF migrations (production-ready schema deployment)

Baseline migration is now included under `backend/src/ISS.Infrastructure/Persistence/Migrations`.

Generate future migrations:

```powershell
dotnet ef migrations add <Name> `
  --project backend/src/ISS.Infrastructure/ISS.Infrastructure.csproj `
  --startup-project backend/src/ISS.Api/ISS.Api.csproj `
  --output-dir Persistence/Migrations
```

Apply migrations:

```powershell
$env:ConnectionStrings__Default="Host=localhost;Database=iss;Username=pward;Password=vesper"
dotnet ef database update `
  --project backend/src/ISS.Infrastructure/ISS.Infrastructure.csproj `
  --startup-project backend/src/ISS.Api/ISS.Api.csproj
```

Or let the API apply them on startup:

```powershell
$env:Database__InitializationMode="Migrate"
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

If you already created a database using `EnsureCreated`, recreate it (recommended for non-production) before switching to migrations, or align it manually before inserting migration history.

### Required environment variables

- `ConnectionStrings__Default`
- `Jwt__Key` (required in non-Development, minimum 32 chars, and must not use the built-in dev default)

Optional:

- `Jwt__Issuer`, `Jwt__Audience`
- `Database__InitializationMode` (`EnsureCreated` | `Migrate` | `None`)
- `Notifications__Enabled`, `Notifications__EmailEnabled`, `Notifications__SmsEnabled`
- `Notifications__Dispatcher__Enabled` (enables background outbox dispatcher)
- `Notifications__Email__Smtp__Host` / `Port` / `User` / `Password` / `FromEmail` / `FromName`
- `Notifications__Sms__Twilio__AccountSid` / `AuthToken` / `From`

## Frontend (Web)

```bash
cd frontend
copy .env.example .env.local
npm install
npm run dev
```

Environment variables:

- `ISS_API_BASE_URL` (defaults to `http://localhost:5257`)

## Production notes (high level)

- Put the API behind HTTPS (reverse proxy like Nginx/IIS) and set a strong `Jwt__Key`.
- Run the notification dispatcher only when SMTP/Twilio are configured and `Notifications__Dispatcher__Enabled=true`.
- Persist backend file storage (`App_Data/`) across deployments. This now includes:
  - `App_Data/item-attachments`
  - `App_Data/document-attachments`
- Include both the PostgreSQL database and `App_Data/` in backups (same retention policy window).

### Deployment smoke script

This repo now includes `scripts/ops/smoke-api.ps1` for post-deploy API smoke checks.

Health-only smoke (useful in CI):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\ops\smoke-api.ps1 `
  -ApiBaseUrl "https://erp.example.com" `
  -SkipAuth
```

Authenticated smoke (admin user):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\ops\smoke-api.ps1 `
  -ApiBaseUrl "https://erp.example.com" `
  -Email "admin@example.com" `
  -Password "<admin-password>"
```

### Backup / Restore examples (PowerShell)

Database backup:

```powershell
pg_dump --format=custom --file .\backup\iss-$(Get-Date -Format yyyyMMdd-HHmmss).dump `
  --host localhost --port 5432 --username pward --dbname iss
```

Database restore (to a recreated/empty target DB):

```powershell
pg_restore --clean --if-exists --no-owner --no-privileges `
  --host localhost --port 5432 --username pward --dbname iss `
  .\backup\iss-YYYYMMDD-HHMMSS.dump
```

File storage backup:

```powershell
Compress-Archive -Path .\backend\src\ISS.Api\App_Data\* `
  -DestinationPath .\backup\iss-app-data-$(Get-Date -Format yyyyMMdd-HHmmss).zip
```

## Rollout / Rollback Checklist

### Rollout (recommended)

1. Backup database and `App_Data/`.
2. Deploy new application build.
3. Apply EF migrations (`dotnet ef database update`) or start API with `Database__InitializationMode=Migrate`.
4. Verify `GET /health` returns HTTP `200` (`Healthy`).
5. Run smoke checks:
   - `scripts/ops/smoke-api.ps1` (health-only or authenticated)
   - Login (UI)
   - Open dashboard (UI)
   - Create/read one document (e.g. service estimate)
   - Generate one PDF

### Rollback (application only)

1. Stop new app version.
2. Re-deploy previous app build.
3. If schema changes were applied and rollback is not backward compatible, restore DB backup (and matching `App_Data/`) instead of only rolling back binaries.
