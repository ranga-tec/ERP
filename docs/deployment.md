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
- Health endpoint: `GET /health`

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
- `Jwt__Key` (set a strong value in production)

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
