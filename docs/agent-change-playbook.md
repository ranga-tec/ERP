# ISS ERP Agent Change Playbook and Troubleshooting Guide

This guide is focused on making safe changes to the system: feature additions/modifications/removals, closure planning, troubleshooting, and documentation hygiene.

Use this alongside:
- `docs/system-technical-maintainer-guide.md` (hub)
- `docs/backend-architecture.md`
- `docs/frontend-architecture.md`
- `docs/csv-closure-audit.md`

## Feature Change Playbook (Future Agent)

This section is the fastest path to safe modifications.

### Add a New Backend Workflow/Module Feature

Recommended order:

1. Identify domain/entity changes
   - add/extend domain types in `ISS.Domain`
   - preserve invariants and status transitions
2. Add persistence support
   - update `IssDbContext` mappings
   - update `IIssDbContext` if new DbSet is needed
   - create EF migration
3. Add application service orchestration
   - prefer service methods for multi-entity flows and side effects
4. Add API endpoints/controllers
   - request/response DTO records
   - authorization attributes
   - thin controller methods
5. Add frontend pages/components
   - server page + client action forms pattern
   - sidebar link if user-facing
6. Add tests
   - integration tests for flow success and key failure paths
   - unit tests if adding reusable validators/helpers/domain rules
7. Update docs
   - `docs/csv-closure-audit.md`
   - `README.md` / `docs/deployment.md` if runbook/config changes

### Modify an Existing Feature Safely

Before editing:

- Search for:
  - controller endpoint
  - corresponding service method
  - domain entity state transitions
  - frontend list/detail pages
  - integration tests for that flow

Suggested search commands:

```powershell
rg -n "FeatureName|endpoint-path" backend/src backend/tests frontend/src
rg -n "DocumentCollaborationPanel|referenceType" frontend/src
```

Change impact checklist:

- backend DTO shape changed?
  - update frontend types and integration tests
- domain status rules changed?
  - add failure-path tests
- file uploads/docs touched?
  - update `AttachmentUploadPolicy` and tests
- new DB fields?
  - migration + mapping + serialization + UI edit form + detail display
- reporting math changed?
  - verify PostgreSQL translation/overflow behavior

### Remove or Rename a Feature/Module

Do not remove only one layer.

Minimum removal checklist:

- backend controller endpoints
- application service methods
- domain references / enums / status transitions
- frontend routes/pages/components
- sidebar links
- integration/unit tests
- documentation references
- CSV closure audit notes

For renames:

- preserve backward compatibility if external clients may exist
- otherwise update frontend proxy callers and tests in the same checkpoint

### Add a New Report

Current reporting pattern is centralized in `ReportingController`.

Recommended approach:

1. Add endpoint and DTO(s) to `ReportingController`
2. Keep SQL filtering in EF, but be careful with provider translation and numeric precision
3. Add integration test coverage in `EndToEndTests.cs`
4. Add frontend page under `frontend/src/app/(app)/reporting/...`
5. Add sidebar link
6. Update `docs/csv-closure-audit.md`

### Add Comments/Attachments to a New Document Screen

1. Choose/confirm a `referenceType` convention
2. Ensure backend normalization accepts it
3. Add `DocumentCollaborationPanel` to the frontend detail page
4. Pass `referenceType` and document id
5. Validate auth roles for the page and document APIs
6. Test comment add/delete and attachment upload/download/delete

### Add New Configuration or Startup Hardening

If adding env vars or startup guards:

- document defaults and constraints in `README.md` and `docs/deployment.md`
- add unit tests for validators where possible (as done for JWT key validation)
- ensure CI settings still satisfy the startup requirements

## Known Gaps and Closure Planning

Use `docs/csv-closure-audit.md` as the working closure tracker.

As of the current state, remaining work is mostly:

- workflow depth upgrades (for example RFQ compare/award, multi-stage stock transfer receive)
- advanced reporting second wave (valuation, supplier performance, profitability)
- richer master-data fields and UoM conversion support
- final responsive/UAT pass and row-level CSV verification

This means future agents should prioritize:

1. high-value partial rows in `docs/csv-closure-audit.md`
2. integration test coverage for new failure/guard states
3. documentation updates for any behavior/config change

## Common Pitfalls and Troubleshooting

### 1. `relation "DocumentComments" does not exist` (or similar missing table)

Cause:

- local DB schema is behind code

Fix:

- run EF migrations, or reset local DB and re-migrate (local dev only)

### 2. `dotnet ef database update` collides with existing tables

Cause:

- DB was created via `EnsureCreated` without matching migration history

Fix options:

- local dev: reset DB and migrate cleanly
- preserved data environment: baseline/repair migration history carefully before applying newer migrations

### 3. Integration tests fail because Docker/Testcontainers is unavailable

Symptom:

- Testcontainers cannot connect to Docker daemon / named pipe

Fix:

- use external PostgreSQL fallback env vars in `IssApiFixture`
- run integration tests against an existing local PostgreSQL test database

### 4. App fails on startup in non-Development due to JWT key validation

Cause:

- missing/short/default `Jwt:Key`

Fix:

- set `Jwt__Key` to a strong non-default value (minimum 32 chars)

### 5. Frontend paths containing `(app)` fail in PowerShell commands

Cause:

- unquoted parentheses are parsed by PowerShell

Fix:

- quote paths when using shell commands:

```powershell
Get-Content -LiteralPath '..\frontend\src\app\(app)\inventory\reorder-alerts\page.tsx'
```

## Documentation Maintenance Rules (for Future Agents)

When you change behavior, also update documentation in the same checkpoint.

At minimum:

- `docs/csv-closure-audit.md` for requirement/closure status changes
- `docs/deployment.md` for config/env/deploy/runbook changes
- `README.md` for local run/test command changes
- `frontend/README.md` if frontend developer workflow or architecture assumptions change

Keep this guide current when any of these change:

- startup/auth/config model
- migration strategy
- test fixture behavior
- frontend proxy/auth flow
- attachment/document storage behavior
- CI jobs or smoke test scope

## Quick Onboarding Checklist (Future Agent)

1. Read `README.md`
2. Read `docs/system-technical-maintainer-guide.md` (this file)
3. Read `docs/csv-closure-audit.md` to see what is still partial/missing
4. Start local DB (`docker compose up -d`)
5. Run API + frontend
6. Run backend integration tests (or external PostgreSQL fallback if Docker pipe is blocked)
7. Make one focused checkpoint:
   - code
   - tests
   - docs
8. Push and summarize exactly what changed, what was validated, and what remains

