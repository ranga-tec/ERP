# Azure App Service deployment runbook

This is the fast, repeatable deployment path for the C-COM ERP monorepo. It is written so a new maintainer or coding agent can inspect the existing Azure resources, redeploy both applications, verify the database, and diagnose the failures already encountered on Azure Free F1.

## Current deployment

| Component | Azure resource | Runtime | Public endpoint |
|---|---|---|---|
| Web frontend | App Service `ISSMS` | Linux, Node 24 | `https://issms-exefdhb4hke4f7f2.southindia-01.azurewebsites.net` |
| API | App Service `issms-api-ccom` | Linux, .NET 8 | `https://issms-api-ccom.azurewebsites.net` |
| Database | PostgreSQL Flexible Server `issms-pg-ccom`, database `issms` | PostgreSQL 16 | Private credentials; never commit the connection string |
| Hosting plan | App Service plan `plan-ISSMS` | Linux Free F1 | Shared by the web and API apps |

The resources are in resource group `ISSMS`, South India. The intended source branch is `c-com-erp` in `https://github.com/ranga-tec/ERP`.

Important cost note: the App Service F1 plan is free, but PostgreSQL Flexible Server `Standard_B1ms` is a billable resource unless Azure credits cover it. Check Cost Management before leaving a test database running.

## 1. Preflight and ownership check

Run from the repository root in PowerShell:

```powershell
az login
az account show --query "{subscription:name,state:state}" -o table
git fetch origin
git switch c-com-erp
git status --short --branch

$rg = "ISSMS"
$webApp = "ISSMS"
$apiApp = "issms-api-ccom"
$pgServer = "issms-pg-ccom"

az webapp show -g $rg -n $webApp --query "{state:state,host:defaultHostName}" -o table
az webapp show -g $rg -n $apiApp --query "{state:state,host:defaultHostName}" -o table
az postgres flexible-server show -g $rg -n $pgServer --query "{state:state,version:version,sku:sku.name}" -o table
```

Stop if `git status` shows unrelated uncommitted changes. Deploy a known pushed commit, not an accidental working directory snapshot.

Do not assume Azure Deployment Center is deploying the right code. The portal-created workflow previously targeted `main` and ran Node commands at the repository root, but this is a monorepo and production work is on `c-com-erp`. The manual ZIP flow below is the known-good recovery path.

## 2. Configure and deploy the API

Create secrets locally for this shell session. Do not paste real values into this document, source control, chat logs, or screenshots.

```powershell
$dbAdmin = Read-Host "PostgreSQL admin user"
$dbPassword = Read-Host "PostgreSQL admin password"
$jwtKey = Read-Host "JWT key (at least 32 random characters)"
$adminEmail = Read-Host "Bootstrap admin email"
$adminPassword = Read-Host "Bootstrap admin password"
$connectionString = "Host=$pgServer.postgres.database.azure.com;Port=5432;Database=issms;Username=$dbAdmin;Password=$dbPassword;SSL Mode=Require;Trust Server Certificate=true"
```

Set the production configuration:

```powershell
az webapp config set -g $rg -n $apiApp --linux-fx-version "DOTNETCORE|8.0" --startup-file "dotnet ISS.Api.dll"

az webapp config appsettings set -g $rg -n $apiApp --settings `
  "ASPNETCORE_ENVIRONMENT=Production" `
  "ConnectionStrings__Default=$connectionString" `
  "Database__InitializationMode=Migrate" `
  "Database__EnableRetryOnFailure=true" `
  "Jwt__Issuer=neuedge" `
  "Jwt__Audience=neuedge" `
  "Jwt__Key=$jwtKey" `
  "Security__EnforceHttps=true" `
  "ReverseProxy__Enabled=true" `
  "Auth__AllowFirstUserBootstrapRegistration=true" `
  "Auth__AllowSelfRegistration=false" `
  "Auth__BootstrapAdminEmail=$adminEmail" `
  "Auth__BootstrapAdminPassword=$adminPassword"
```

Publish only the Linux x64 runtime assets. A generic publish can include every SkiaSharp native runtime and produce a package hundreds of megabytes larger.

```powershell
$repoRoot = (Get-Location).Path
$deployRoot = Join-Path $env:TEMP ("iss-azure-" + [guid]::NewGuid())
$apiPublish = Join-Path $deployRoot "api"
$apiZip = Join-Path $deployRoot "api.zip"
New-Item -ItemType Directory -Path $apiPublish -Force | Out-Null

dotnet publish .\backend\src\ISS.Api\ISS.Api.csproj `
  -c Release `
  -r linux-x64 `
  --self-contained false `
  -p:DebugSymbols=false `
  -p:DebugType=None `
  -o $apiPublish

Compress-Archive -Path "$apiPublish\*" -DestinationPath $apiZip -CompressionLevel Optimal
az webapp deploy -g $rg -n $apiApp --src-path $apiZip --type zip --timeout 1800000
```

The API applies EF Core migrations at startup because `Database__InitializationMode=Migrate`. Confirm both process and database health:

```powershell
(Invoke-WebRequest "https://issms-api-ccom.azurewebsites.net/health" -UseBasicParsing -TimeoutSec 120).StatusCode
```

Expected: HTTP `200` and body `Healthy`. Do not seed production with either script under `scripts/`; those scripts deliberately create test transactions and inventory value.

## 3. Configure and deploy the frontend

The Next.js server proxies browser API requests to the separate API App Service.

```powershell
az webapp config set -g $rg -n $webApp --linux-fx-version "NODE|24-lts" --startup-file "node server.js"

az webapp config appsettings set -g $rg -n $webApp --settings `
  "NODE_ENV=production" `
  "NEUEDGE_API_BASE_URL=https://issms-api-ccom.azurewebsites.net" `
  "NEUEDGE_SECURE_COOKIES=true" `
  "NEXT_PUBLIC_NEUEDGE_ALLOW_SELF_REGISTRATION=false" `
  "SCM_DO_BUILD_DURING_DEPLOYMENT=false" `
  "ENABLE_ORYX_BUILD=false" `
  "NEXT_TELEMETRY_DISABLED=1"

$env:NEUEDGE_API_BASE_URL = "https://issms-api-ccom.azurewebsites.net"
$env:NEUEDGE_SECURE_COOKIES = "true"
$env:NEXT_PUBLIC_NEUEDGE_ALLOW_SELF_REGISTRATION = "false"

Push-Location .\frontend
npm ci
npm run build
Pop-Location

$standalone = Join-Path $repoRoot "frontend\.next\standalone"
Copy-Item .\frontend\public (Join-Path $standalone "public") -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $standalone ".next") -Force | Out-Null
Copy-Item .\frontend\.next\static (Join-Path $standalone ".next\static") -Recurse -Force

$webZip = Join-Path $deployRoot "web-standalone.zip"
Compress-Archive -Path "$standalone\*" -DestinationPath $webZip -CompressionLevel Optimal
az webapp deploy -g $rg -n $webApp --src-path $webZip --type zip --timeout 1800000
```

The prebuilt standalone package avoids an `npm install` and full Next.js compilation on the memory-constrained F1 instance. Build-time public settings must be present before `npm run build`; server-only `NEUEDGE_API_BASE_URL` remains configured in App Service as well.

Verify the public flow:

```powershell
$login = Invoke-WebRequest "https://issms-exefdhb4hke4f7f2.southindia-01.azurewebsites.net/login" -UseBasicParsing -TimeoutSec 120
$login.StatusCode
$login.Content -match "C-COM ERP"

(Invoke-WebRequest "https://issms-exefdhb4hke4f7f2.southindia-01.azurewebsites.net/api/auth/capabilities" -UseBasicParsing -TimeoutSec 120).StatusCode
```

Expected: HTTP `200`, title match `True`, and capabilities HTTP `200`. Log in through the browser with the separately stored admin credentials; never add them to this runbook.

## 4. Free F1 limits and failure recovery

The two App Services share one F1 plan. F1 has tight CPU and management-operation quotas, cold starts, no Always On, and is suitable for testing rather than reliable ERP production use.

Use this order when a deployment appears stuck:

1. Check app and plan state before restarting anything:

   ```powershell
   az webapp show -g $rg -n $webApp --query state -o tsv
   az webapp show -g $rg -n $apiApp --query state -o tsv
   az appservice plan show -g $rg -n plan-ISSMS --query "{sku:sku.name,status:status}" -o table
   ```

2. Check the latest Kudu deployment instead of submitting another ZIP immediately:

   ```powershell
   az webapp log deployment list -g $rg -n $webApp -o table
   az webapp log deployment list -g $rg -n $apiApp -o table
   ```

3. Treat Kudu status `4` with `complete=true` and `active=true` as a successful deployment. A CLI `504` can mean the client timed out while Azure continued building.

4. If the app says `QuotaExceeded` or mentions `WP stop requests`, wait for the quota window to reset. Repeated restarts and redeployments consume more operations and extend the outage.

5. Stream logs only after the quota check:

   ```powershell
   az webapp log config -g $rg -n $apiApp --application-logging filesystem --level information
   az webapp log tail -g $rg -n $apiApp
   ```

## 5. Local run and safe sample data

### Database

The repository Docker Compose file starts PostgreSQL only and publishes it on host port `5433`:

```powershell
docker compose up -d db
docker compose ps
```

If Docker Desktop is unavailable but PostgreSQL 16 is installed locally, use a separate database and an explicit connection string. Never point the sample-data scripts at Azure.

### API

For the normal Docker database on port `5433`, start both applications with one command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-local-demo.ps1
```

The launcher waits for API and frontend health, prints both process IDs and URLs, and writes ignored local logs at the repository root. A brand-new database can spend several minutes loading reference data before the first health response. To use another local PostgreSQL database, pass `-DatabasePort`, `-DatabaseName`, and the other database parameters explicitly. If a current Debug build already exists, add `-NoBuild` to skip compilation. Use `-ApiOnly` or `-FrontendOnly` to resume one process without disturbing the other. If a current `.next` build exists and dev compilation is too slow, add `-ProductionFrontend`.

Manual startup equivalent:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5257"
$env:ConnectionStrings__Default = "Host=127.0.0.1;Port=5433;Database=neuedge;Username=pgadmin;Password=vesper;Include Error Detail=true"
$env:Database__InitializationMode = "Migrate"
$env:Security__EnforceHttps = "false"
$env:Auth__AllowFirstUserBootstrapRegistration = "true"
$env:Auth__AllowSelfRegistration = "true"
dotnet run --project .\backend\src\ISS.Api\ISS.Api.csproj
```

API: `http://127.0.0.1:5257`; Swagger: `http://127.0.0.1:5257/swagger`; health: `http://127.0.0.1:5257/health`.

### Frontend

Open a second PowerShell window:

```powershell
Set-Location .\frontend
$env:NEUEDGE_API_BASE_URL = "http://127.0.0.1:5257"
$env:NEUEDGE_SECURE_COOKIES = "false"
$env:NEXT_PUBLIC_NEUEDGE_ALLOW_SELF_REGISTRATION = "true"
npm install
npm run dev
```

Web: `http://localhost:3000/login`.

### Sample data options

For a new disposable local database, bootstrap a local admin first:

```powershell
$body = @{ email = "admin@local"; password = "Passw0rd1"; displayName = "Local Admin" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:5257/api/auth/register" -ContentType "application/json" -Body $body
```

Then add the verified, idempotent service dataset with varied customers, equipment, units, tracked spare parts, technicians, users, entitlement paths, inventory, and service jobs:

```powershell
python .\scripts\seed-service-test-data.py `
  --base "http://127.0.0.1:5257" `
  --email "admin@local" `
  --password "Passw0rd1"
```

The service seed also creates `service.manager@local` and three technician logins. Their local-only temporary password is `Demo@1234`; change it if the environment is shared.

The service seed refuses non-local hostnames. The broader `scripts/seed-uat-test-data.ps1` also has a localhost guard, but it clears existing test transactions and should be used only while maintaining its full end-to-end test scenario on a disposable database.

Local smoke checks:

```powershell
(Invoke-WebRequest "http://127.0.0.1:5257/health" -UseBasicParsing).StatusCode
(Invoke-WebRequest "http://localhost:3000/login" -UseBasicParsing).StatusCode
```

Expected: `200` from both endpoints.

## Official command references

- [Deploy ZIP packages to Azure App Service](https://learn.microsoft.com/azure/app-service/deploy-zip)
- [`az webapp deploy` command reference](https://learn.microsoft.com/cli/azure/webapp#az-webapp-deploy)
- [Configure Node.js on Azure App Service](https://learn.microsoft.com/azure/app-service/configure-language-nodejs)
- [`az postgres flexible-server` command reference](https://learn.microsoft.com/cli/azure/postgres/flexible-server)
