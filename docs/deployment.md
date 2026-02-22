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

- On startup, the API runs `EnsureCreated()` to create tables if the database is empty.
- Roles are seeded on startup.
- The first registered user becomes `Admin`.

### Required environment variables

- `ConnectionStrings__Default`
- `Jwt__Key` (set a strong value in production)

Optional:

- `Jwt__Issuer`, `Jwt__Audience`
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

