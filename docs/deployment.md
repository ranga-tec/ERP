# Deployment / Installation

This repo contains a complete ISS ERP system:

- Backend: ASP.NET Core (.NET 8) + PostgreSQL
- Frontend: Next.js (App Router) + TypeScript + Tailwind

The current production approach is a single Ubuntu VPS running Docker Compose. The tracked deployment assets for that flow live under:

- `deploy/docker-compose.vps.yml`
- `deploy/.env.example`
- `deploy/backup.sh`

## Local infrastructure (PostgreSQL)

From the repo root:

```bash
docker compose up -d
```

- PostgreSQL: `localhost:5432` (db `iss`, user `pgadmin`, password `vesper`)
- Note: the repo-root `docker-compose.yml` starts PostgreSQL only

## Backend (API)

The API requires a connection string. Example (PowerShell):

```powershell
$env:ConnectionStrings__Default="Host=localhost;Port=5432;Database=iss;Username=pgadmin;Password=vesper"
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

Notes:

- Startup DB initialization is controlled by `Database__InitializationMode`:
  - `EnsureCreated`
  - `Migrate` (default in Development and recommended for new environments / controlled deployments)
  - `None` (default in non-Development)
- Roles are seeded on startup.
- Fresh databases also seed default currencies, payment types, tax codes, and reference forms required by core finance/reporting screens.
- Fresh databases seed default companies (`ISS`, `C-COM`) and C-COM demo master data used by the hosted demo.
- The first registered user becomes `Admin`.
- Health endpoint: `GET /health` (includes DB connectivity, not just process liveness)

### Multi-company deployment notes

Production environments should keep:

```text
Database__InitializationMode=Migrate
```

That lets startup apply the company/user migrations and seed required demo data. The current Railway production service is configured this way.

After deployment, verify:

```sql
select count(*) from "Companies" where "Code" in ('ISS','C-COM');
select count(*) from "AspNetUsers" where "CompanyId" is null;
select count(*) from "Items" i join "Companies" c on c."Id" = i."CompanyId" where c."Code" = 'C-COM';
select count(*) from "Suppliers" s join "Companies" c on c."Id" = s."CompanyId" where c."Code" = 'C-COM';
```

Expected C-COM demo counts:

- companies: `2` (`ISS`, `C-COM`)
- C-COM disabled category: `1`
- C-COM items: `170`
- C-COM suppliers: `29`

### EF migrations (production-ready schema deployment)

Baseline migration is included under `backend/src/ISS.Infrastructure/Persistence/Migrations`.

Generate future migrations:

```powershell
dotnet ef migrations add <Name> `
  --project backend/src/ISS.Infrastructure/ISS.Infrastructure.csproj `
  --startup-project backend/src/ISS.Api/ISS.Api.csproj `
  --output-dir Persistence/Migrations
```

Apply migrations:

```powershell
$env:ConnectionStrings__Default="Host=localhost;Port=5432;Database=iss;Username=pgadmin;Password=vesper"
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

### Backend environment variables

Required:

- `ConnectionStrings__Default` or Railway/Postgres-style `DATABASE_URL`
- `Jwt__Key` (required in non-Development, minimum 32 chars, and must not use the built-in dev default)

Optional:

- `Jwt__Issuer`, `Jwt__Audience`
- `Database__InitializationMode` (`EnsureCreated` | `Migrate` | `None`)
- `Database__EnableRetryOnFailure` (`true` | `false`; default `true`)
- `Database__MaxRetryCount` (default `5`)
- `Database__MaxRetryDelaySeconds` (default `10`)
- `Security__EnforceHttps` (`true` | `false`; defaults to `true` outside Development)
- `Auth__AllowSelfRegistration` (`true` | `false`; default is `true` in Development and `false` in non-Development)
- `Auth__AllowFirstUserBootstrapRegistration` (`true` | `false`; default `true`)
- `Auth__BootstrapAdminEmail` / `Auth__BootstrapAdminPassword` / `Auth__BootstrapAdminDisplayName`
- `ReverseProxy__Enabled` (`true` | `false`; default `false`)
- `ReverseProxy__ForwardLimit` (default `1`)
- `ReverseProxy__KnownProxies__0`, `ReverseProxy__KnownProxies__1`, ...
- `ReverseProxy__KnownNetworks__0`, `ReverseProxy__KnownNetworks__1`, ... (CIDR format, for example `10.0.0.0/8`)
- `Notifications__Enabled`, `Notifications__EmailEnabled`, `Notifications__SmsEnabled`
- `Notifications__Dispatcher__Enabled`
- `Notifications__Email__Smtp__Host` / `Port` / `User` / `Password` / `FromEmail` / `FromName`
- `Notifications__Sms__Twilio__AccountSid` / `AuthToken` / `From`

## Frontend (Web)

```bash
cd frontend
copy .env.example .env.local
npm install
npm run dev
```

Open `http://localhost:3000`.

Frontend environment variables:

- `ISS_API_BASE_URL` (defaults to `http://localhost:5257`)
- `ISS_SECURE_COOKIES` (`true` | `false`; defaults to `true` in production, set to `false` only when deliberately serving plain HTTP)
- `ISS_BACKEND_PROXY_TIMEOUT_MS` (optional; backend proxy upstream timeout in ms, default `30000`)
- `NEXT_PUBLIC_ISS_ALLOW_SELF_REGISTRATION` (optional; register link is enabled by default in dev and disabled by default in production)

## Railway deployment notes

The repo-root `Dockerfile` runs the API and frontend in one Railway web service:

- frontend listens on Railway's `$PORT`
- API listens internally on `API_PORT` (default `8080`)
- `ISS_API_BASE_URL` defaults to `http://127.0.0.1:8080`
- `Database__InitializationMode` defaults to `Migrate` in the Railway image

Railway's PostgreSQL service exposes `DATABASE_URL` by default. The API accepts that URL directly, so a Railway service can either reference the database URL:

```text
DATABASE_URL=${{Postgres.DATABASE_URL}}
```

or provide the native .NET setting:

```text
ConnectionStrings__Default=Host=...;Port=5432;Database=...;Username=...;Password=...
```

Keep `Jwt__Key` set to a real production secret. After deploying, check `/health`; it includes database connectivity and will fail if the Railway service cannot reach Postgres or migrations did not apply.

### Deploy to Railway from the CLI

Use the latest Railway CLI package instead of an old globally installed `railway` binary:

```powershell
npx @railway/cli@latest link --project <project-id> --environment production --service ERP
npx @railway/cli@latest up --service ERP --environment production --detach
```

When deploying from a local machine with uncommitted work, deploy from a clean git worktree or a clean checkout of the pushed commit. That prevents unrelated local files from being uploaded:

```powershell
git worktree add --detach ..\ISS-deploy-<commit> <commit>
cd ..\ISS-deploy-<commit>
npx @railway/cli@latest link --project <project-id> --environment production --service ERP
npx @railway/cli@latest up --service ERP --environment production --detach
```

After the detached upload starts, verify Railway status and the live app:

```powershell
npx @railway/cli@latest status
Invoke-WebRequest https://<railway-app-url>/login -UseBasicParsing
Invoke-WebRequest https://<railway-app-url>/health -UseBasicParsing
```

If the container fails immediately with a shell startup error, check that `deploy/railway/start.sh` is normalized to Unix line endings in the Docker image before execution.

## Integration test database fallback (without Testcontainers)

If Docker/Testcontainers cannot access the Docker daemon from your shell/session, the integration tests can run against an existing PostgreSQL instance:

```powershell
$env:ISS_INTEGRATIONTESTS_CONNECTION_STRING="Host=localhost;Port=5432;Database=iss_integration_local;Username=pgadmin;Password=vesper"
$env:ISS_INTEGRATIONTESTS_RESET_EXISTING_DB="1"
$env:ISS_INTEGRATIONTESTS_HTTP_TIMEOUT_SECONDS="60"
$env:ISS_INTEGRATIONTESTS_DB_READY_TIMEOUT_SECONDS="60"
dotnet test .\backend\tests\ISS.IntegrationTests\ISS.IntegrationTests.csproj -c Release --nologo --no-build --logger "console;verbosity=minimal"
```

Notes:

- `ISS_INTEGRATIONTESTS_RESET_EXISTING_DB=1` will delete and recreate the target database before each run.
- Use a dedicated test database name, not the main `iss` database.
- Build once before `--no-build` runs:
  - `dotnet build .\backend\tests\ISS.IntegrationTests\ISS.IntegrationTests.csproj -c Release --nologo`

## Single-VPS production deployment (current approach)

This is the tracked deployment baseline for this repo.

### Current live server snapshot

- provider: Contabo VPS
- public IPv4: `178.238.230.31`
- current access pattern: raw-IP HTTP until a real domain/TLS terminator is attached
- deployment ownership target: use a non-root operator account for routine access and deploys

### Topology

The current production path runs the entire system on one Ubuntu VPS:

- `db`: PostgreSQL 16
- `api`: ASP.NET Core API
- `web`: Next.js app
- persistent Docker volumes:
  - `iss_postgres_data`
  - `iss_api_app_data`

The frontend is the only public container. It listens on port `80` and proxies:

- `/api/backend/*` to the API container
- `/api/auth/*` to the backend auth endpoints through the Next.js server routes

The API is internal-only on the Docker network and is not published on a host port.

### Repo assets used by the VPS flow

- `backend/Dockerfile`
- `backend/.dockerignore`
- `frontend/Dockerfile`
- `deploy/docker-compose.vps.yml`
- `deploy/.env.example`
- `deploy/backup.sh`

### VPS prerequisites

Recommended baseline:

- Ubuntu 24.04 LTS
- Docker Engine with Compose plugin
- SSH key-based access for a non-root operator account
- `ufw` enabled with at least:
  - `OpenSSH`
  - `80/tcp`
  - `443/tcp` (reserve now even if TLS is added later)

Recommended filesystem layout on the server:

- app root: `/opt/iss`
- backups: `/opt/iss-backups`
- runtime env file: `/opt/iss/deploy/.env`

### Host hardening checklist

Before deploying the app:

1. Create a non-root operator account, for example `deploy`.
2. Add your SSH public key to that account.
3. Verify `ssh deploy@<server>` works.
4. Disable SSH password authentication and root SSH login.
5. Enable the firewall.
6. Install Docker and add the operator account to the `docker` group.

Do not operate the deployment through password-based root SSH.

### Prepare the server

Typical package bootstrap on a fresh Ubuntu VPS:

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl git ufw
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
sudo mkdir -p /opt/iss /opt/iss-backups
sudo chown -R $USER:$USER /opt/iss /opt/iss-backups
```

### Prepare runtime secrets

Copy the template:

```bash
cp deploy/.env.example deploy/.env
chmod 600 deploy/.env
```

Then set real values in `deploy/.env`.

Important variables in the tracked VPS template:

- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_KEY`
- `BOOTSTRAP_ADMIN_EMAIL`
- `BOOTSTRAP_ADMIN_PASSWORD`
- `BOOTSTRAP_ADMIN_DISPLAY_NAME`
- `SECURITY_ENFORCE_HTTPS`
- `ISS_SECURE_COOKIES`
- `ISS_BACKEND_PROXY_TIMEOUT_MS`
- `NEXT_PUBLIC_ISS_ALLOW_SELF_REGISTRATION`

Rules:

- keep `deploy/.env` out of git
- use a long random `JWT_KEY`
- use a long random `POSTGRES_PASSWORD`
- treat `BOOTSTRAP_ADMIN_*` as bootstrap/recovery settings and rotate or remove them after the real admin account is established

### Plain HTTP mode vs HTTPS mode

The tracked VPS compose file is intentionally usable on a raw IP address before a domain is attached.

#### Plain HTTP mode

Use these settings when serving the app directly on `http://<server-ip>`:

- `SECURITY_ENFORCE_HTTPS=false`
- `ISS_SECURE_COOKIES=false`

Current live raw-IP endpoint:

- `http://178.238.230.31`

Why both matter:

- if `Security__EnforceHttps` stays enabled without TLS, the API can redirect or reject traffic in ways that break the container-to-container and browser flow
- if `ISS_SECURE_COOKIES=true` on plain HTTP, the browser will reject the auth cookie and login will appear to succeed while the session does not persist

#### HTTPS mode

After the site is reachable through real HTTPS:

- set `SECURITY_ENFORCE_HTTPS=true`
- set `ISS_SECURE_COOKIES=true`

At that point, terminate TLS in front of the frontend container with a host-level or containerized reverse proxy such as Caddy or Nginx, and forward the scheme correctly.

### Copy the app to the server

The server only needs these tracked directories:

- `backend/`
- `frontend/`
- `deploy/`

You can upload them with your preferred tool:

- `scp`
- `sftp`
- `rsync`
- a tarball extracted into `/opt/iss`

The current workflow does not require a git checkout on the server.

### Deploy or update the stack

From `/opt/iss` on the VPS:

```bash
docker compose --env-file /opt/iss/deploy/.env -f /opt/iss/deploy/docker-compose.vps.yml up -d --build
```

Check status:

```bash
docker compose --env-file /opt/iss/deploy/.env -f /opt/iss/deploy/docker-compose.vps.yml ps
```

Expected state:

- `db` healthy
- `api` up
- `web` up and bound on `0.0.0.0:80`

### Verify the deployment

Health and smoke checks:

```powershell
Invoke-WebRequest -UseBasicParsing -Uri "http://<server-ip>" | Select-Object -ExpandProperty StatusCode
Invoke-WebRequest -UseBasicParsing -Uri "http://<server-ip>/login" | Select-Object -ExpandProperty StatusCode
```

Current live checks:

```powershell
Invoke-WebRequest -UseBasicParsing -Uri "http://178.238.230.31" | Select-Object -ExpandProperty StatusCode
Invoke-WebRequest -UseBasicParsing -Uri "http://178.238.230.31/login" | Select-Object -ExpandProperty StatusCode
```

API smoke script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\ops\smoke-api.ps1 `
  -ApiBaseUrl "http://<server-ip>" `
  -Email "<admin-email>" `
  -Password "<admin-password>"
```

Browser validation should include:

1. login
2. dashboard load
3. create/read one document
4. generate one PDF

### Reverse proxy notes

Once TLS is added, the API should trust only your actual proxy hop(s).

Example API settings behind a single same-host reverse proxy:

```bash
ReverseProxy__Enabled=true
ReverseProxy__ForwardLimit=1
ReverseProxy__KnownProxies__0=127.0.0.1
```

Only trust proxy addresses or networks that are actually under your control.

### Attachment upload safety limits

The API enforces upload safety checks for item attachments and document collaboration attachments:

- Per-file size limit: `25 MB`
- Per-record attachment count limit: `25 files`
- Per-record total attachment storage limit: `100 MB`
- Allowed extensions: `.pdf`, `.txt`, `.csv`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, `.doc`, `.docx`, `.xls`, `.xlsx`
- Allowed content types are restricted to common PDF, image, Office, and text types
- File content is signature-checked (magic-byte sniffing) for common formats to reject renamed or disguised files

If users need additional file types, extend the server allowlist and add tests before enabling them in production.

## Backup / restore

### Automated backup script

The tracked VPS backup script is:

- `deploy/backup.sh`

It currently:

- runs `pg_dump` against the live `db` container
- archives the `iss_api_app_data` Docker volume
- writes files into `/opt/iss-backups`
- deletes backups older than 7 days

Recommended cron entry:

```bash
0 2 * * * /opt/iss/deploy/backup.sh >> /opt/iss-backups/backup.log 2>&1
```

### Manual backup examples

Database:

```powershell
pg_dump --format=custom --file .\backup\iss-$(Get-Date -Format yyyyMMdd-HHmmss).dump `
  --host localhost --port 5432 --username pgadmin --dbname iss
```

Database restore (to a recreated or empty target DB):

```powershell
pg_restore --clean --if-exists --no-owner --no-privileges `
  --host localhost --port 5432 --username pgadmin --dbname iss `
  .\backup\iss-YYYYMMDD-HHMMSS.dump
```

File storage:

```powershell
Compress-Archive -Path .\backend\src\ISS.Api\App_Data\* `
  -DestinationPath .\backup\iss-app-data-$(Get-Date -Format yyyyMMdd-HHmmss).zip
```

### What must be backed up

Always back up both:

- PostgreSQL data
- `App_Data/` content, including:
  - `App_Data/item-attachments`
  - `App_Data/document-attachments`

## Rollout / rollback checklist

### Rollout

1. Back up database and `App_Data`.
2. Sync the new `backend/`, `frontend/`, and `deploy/` files to `/opt/iss`.
3. Review `/opt/iss/deploy/.env` for any new or changed variables.
4. Run:

```bash
docker compose --env-file /opt/iss/deploy/.env -f /opt/iss/deploy/docker-compose.vps.yml up -d --build
```

5. Verify the stack with `docker compose ... ps`.
6. Verify `GET /health` through the frontend proxy path or direct API network access.
7. Run smoke checks:
   - `scripts/ops/smoke-api.ps1`
   - login
   - dashboard
   - create/read one document
   - generate one PDF

### Rollback

1. Stop the new stack:

```bash
docker compose --env-file /opt/iss/deploy/.env -f /opt/iss/deploy/docker-compose.vps.yml down
```

2. Restore the previous app files under `/opt/iss`.
3. Start the previous stack again with `docker compose ... up -d --build`.
4. If schema changes are not backward compatible, restore the matching DB and `App_Data` backups instead of only rolling back code.
