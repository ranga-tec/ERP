# Deployment / Installation

This repo contains a complete ISS ERP system:

- Backend: ASP.NET Core (.NET 8) + PostgreSQL
- Frontend: Next.js (App Router) + TypeScript + Tailwind

## Local infrastructure (PostgreSQL)

From the repo root:

```bash
docker compose up -d
```

- PostgreSQL: `localhost:5433` (db `iss`, user `pgadmin`, password `vesper`)
- Note: the current repo `docker-compose.yml` starts PostgreSQL only (no pgAdmin service)

## Backend (API)

The API requires a connection string. Example (PowerShell):

```powershell
$env:ConnectionStrings__Default="Host=localhost;Port=5433;Database=iss;Username=pgadmin;Password=vesper"
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
$env:ConnectionStrings__Default="Host=localhost;Port=5433;Database=iss;Username=pgadmin;Password=vesper"
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
- `Database__EnableRetryOnFailure` (`true` | `false`; default `true`)
- `Database__MaxRetryCount` (default `5`)
- `Database__MaxRetryDelaySeconds` (default `10`)
- `Auth__AllowSelfRegistration` (`true` | `false`; default is `true` in Development and `false` in non-Development)
- `Auth__AllowFirstUserBootstrapRegistration` (`true` | `false`; default `true`)
- `ReverseProxy__Enabled` (`true` | `false`; default `false`)
- `ReverseProxy__ForwardLimit` (default `1`)
- `ReverseProxy__KnownProxies__0`, `ReverseProxy__KnownProxies__1`, ...
- `ReverseProxy__KnownNetworks__0`, `ReverseProxy__KnownNetworks__1`, ... (CIDR format, e.g. `10.0.0.0/8`)
- `Notifications__Enabled`, `Notifications__EmailEnabled`, `Notifications__SmsEnabled`
- `Notifications__Dispatcher__Enabled` (enables background outbox dispatcher)
- `Notifications__Email__Smtp__Host` / `Port` / `User` / `Password` / `FromEmail` / `FromName`
- `Notifications__Sms__Twilio__AccountSid` / `AuthToken` / `From`

### Integration test database fallback (without Testcontainers)

If Docker/Testcontainers cannot access the Docker daemon from your shell/session, the integration tests can run against an existing PostgreSQL instance:

```powershell
$env:ISS_INTEGRATIONTESTS_CONNECTION_STRING="Host=localhost;Port=5433;Database=iss_integration_local;Username=pgadmin;Password=vesper"
$env:ISS_INTEGRATIONTESTS_RESET_EXISTING_DB="1"
$env:ISS_INTEGRATIONTESTS_HTTP_TIMEOUT_SECONDS="60"
$env:ISS_INTEGRATIONTESTS_DB_READY_TIMEOUT_SECONDS="60"
dotnet test .\backend\tests\ISS.IntegrationTests\ISS.IntegrationTests.csproj -c Release --nologo --no-build --logger "console;verbosity=minimal"
```

Notes:

- `ISS_INTEGRATIONTESTS_RESET_EXISTING_DB=1` will delete and recreate the target database before each run.
- Use a dedicated test database name (not your main `iss` database).
- Build once before `--no-build` runs:
  - `dotnet build .\backend\tests\ISS.IntegrationTests\ISS.IntegrationTests.csproj -c Release --nologo`

## Frontend (Web)

```bash
cd frontend
copy .env.example .env.local
npm install
npm run dev
```

Environment variables:

- `ISS_API_BASE_URL` (defaults to `http://localhost:5257`)
- `ISS_BACKEND_PROXY_TIMEOUT_MS` (optional; backend proxy upstream timeout in ms, default `30000`)
- `NEXT_PUBLIC_ISS_ALLOW_SELF_REGISTRATION` (optional; login UI register link is enabled by default in dev and disabled by default in production)

## Production notes (high level)

- Put the API behind HTTPS (reverse proxy like Nginx/IIS) and set a strong `Jwt__Key`.
- If running behind a reverse proxy/load balancer, set:
  - `ReverseProxy__Enabled=true`
  - trusted proxy IPs/networks via `ReverseProxy__KnownProxies__*` and/or `ReverseProxy__KnownNetworks__*`
  - `ReverseProxy__ForwardLimit` matching your hop count
- For production, keep self-registration disabled unless intentionally required:
  - Backend default in non-Development already disables self-registration after initial bootstrap
  - Set `Auth__AllowFirstUserBootstrapRegistration=false` after provisioning the first admin user
  - Only set `Auth__AllowSelfRegistration=true` if you intentionally support open signup
  - Set `NEXT_PUBLIC_ISS_ALLOW_SELF_REGISTRATION=true` only when you want the login UI to expose the register option
- Run the notification dispatcher only when SMTP/Twilio are configured and `Notifications__Dispatcher__Enabled=true`.
- Persist backend file storage (`App_Data/`) across deployments. This now includes:
  - `App_Data/item-attachments`
  - `App_Data/document-attachments`
- Include both the PostgreSQL database and `App_Data/` in backups (same retention policy window).

### Reverse Proxy Examples

Nginx -> Kestrel on same host:

```nginx
location / {
  proxy_pass         http://127.0.0.1:5257;
  proxy_set_header   Host $host;
  proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
  proxy_set_header   X-Forwarded-Proto $scheme;
}
```

API environment for this topology:

```powershell
$env:ReverseProxy__Enabled="true"
$env:ReverseProxy__ForwardLimit="1"
$env:ReverseProxy__KnownProxies__0="127.0.0.1"
```

Load balancer/private network in front of API:

```powershell
$env:ReverseProxy__Enabled="true"
$env:ReverseProxy__ForwardLimit="2"
$env:ReverseProxy__KnownNetworks__0="10.0.0.0/8"
$env:ReverseProxy__KnownNetworks__1="172.16.0.0/12"
$env:ReverseProxy__KnownNetworks__2="192.168.0.0/16"
```

Only trust proxy addresses that are actually under your control.

### Attachment upload safety limits

The API enforces upload safety checks for item attachments and document collaboration attachments:

- Per-file size limit: `25 MB`
- Per-record attachment count limit: `25 files`
- Per-record total attachment storage limit: `100 MB`
- Allowed extensions: `.pdf`, `.txt`, `.csv`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, `.doc`, `.docx`, `.xls`, `.xlsx`
- Allowed content types are restricted (common PDF/image/Office/text types)
- File content is signature-checked (magic-byte sniffing) for common formats to reject renamed/disguised files

Operational note:

- If users need additional file types, extend the server allowlist and add tests before enabling them in production.

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
  --host localhost --port 5433 --username pgadmin --dbname iss
```

Database restore (to a recreated/empty target DB):

```powershell
pg_restore --clean --if-exists --no-owner --no-privileges `
  --host localhost --port 5433 --username pgadmin --dbname iss `
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
