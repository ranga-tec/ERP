# Next Session Resume Notes

## Snapshot

- Repo: `D:\VScode Projects\ISS`
- Branch: `main`
- Current purpose: resume from the completed finance bootstrap/auth recovery + Railway deploy-hardening checkpoint
- Current local follow-up: assistant + GRN partial-receipt checkpoint details are documented in `docs/assistant-progress.md`
- GitHub:
  - `99e5685` `Add auth bootstrap recovery and live capability checks`
  - `c0d424f` `Add Docker-based Railway deploy for API`
- Railway production state at handoff:
  - `iss-api`: latest deployment `SUCCESS`
  - `iss-web`: latest deployment `SUCCESS`
  - live URLs:
    - `https://iss-api-production.up.railway.app`
    - `https://iss-web-production.up.railway.app`

## What Was Completed (Latest)

### Finance/bootstrap fixes

- Fresh databases now auto-seed required finance/reporting reference data:
  - currencies
  - payment types
  - tax codes
  - reference forms
- The fresh-DB payment creation failure caused by missing active currencies was fixed.
- Manual UAT was previously completed for:
  - GRN -> AP
  - dispatch -> stock reduction
  - invoice -> AR
  - payment allocation
  - stock-take adjustment
  - costing

### Auth recovery + login UX

- Added backend auth capability endpoint:
  - `GET /api/auth/capabilities`
- Added frontend login-page capability detection so the UI no longer relies only on a production env guess to decide whether registration/bootstrap should be shown.
- Added optional bootstrap admin seeding from `Auth__BootstrapAdmin*` environment variables for managed-environment recovery.
- Confirmed live Railway login through both:
  - direct API `POST /api/auth/login`
  - web proxy `POST /api/auth/login`

### Railway deployment hardening

- Root cause of failed Railway redeploys:
  - `railway up` kept uploading the monorepo root
  - Railpack then could not infer the intended app root reliably
- Stable deploy path now uses explicit archive roots from repo root:

```powershell
railway up .\backend\src --path-as-root -s iss-api -e production -c
railway up .\frontend --path-as-root -s iss-web -e production -c
```

- `backend/src/Dockerfile` and `backend/src/.dockerignore` were added so API deploys no longer depend on Railpack inference.
- Frontend already had a Dockerfile; using explicit `.\frontend --path-as-root` makes Railway consume it correctly.

## Current Operational Notes

- Railway production already had persisted users; local test credentials such as `admin@local / Passw0rd1` do not apply there.
- Production had `Auth__AllowFirstUserBootstrapRegistration=false`, so the original Railway login problem was not an API outage; it was a missing known-admin/recovery path.
- A recovery admin account was provisioned on Railway to restore access.
- Important:
  - do not store the recovery password in repo docs
  - the Railway API service still has `Auth__BootstrapAdmin*` variables populated
  - after the owner rotates to their preferred admin account/password, remove those vars from Railway

## Validation Summary

- Unit tests: passed `26/26`
- Integration tests: passed `36/36`
- Frontend lint: passed
- Frontend production build: passed
- Local Docker build:
  - API Dockerfile built successfully
- Live verification:
  - `GET /api/auth/capabilities` returns `hasUsers=true`, registration disabled
  - live admin login through web and API succeeded

## Files/Areas Most Relevant for the Next Agent

- Auth backend:
  - `backend/src/ISS.Api/Controllers/AuthController.cs`
  - `backend/src/ISS.Api/Security/BootstrapAdminSeeder.cs`
  - `backend/src/ISS.Application/Options/AuthOptions.cs`
  - `backend/src/ISS.Api/Program.cs`
- Railway/API deploy:
  - `backend/src/Dockerfile`
  - `backend/src/.dockerignore`
  - `docs/deployment.md`
- Login UI:
  - `frontend/src/app/(auth)/login/page.tsx`
  - `frontend/src/app/api/auth/capabilities/route.ts`
  - `frontend/src/app/api/auth/login/route.ts`

## Immediate Next Checks

1. Remove or rotate the temporary Railway recovery admin once the owner confirms permanent access.
2. If another Railway deploy fails, use `railway status --json` first and inspect the latest deployment root/builder before changing code.
3. If workspace cleanliness matters for the next checkpoint, remove the local untracked folder `.railway-api-publish/` left from publish verification.
