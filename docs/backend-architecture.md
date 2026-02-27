# ISS ERP Backend Architecture and Operations

This guide is focused on backend architecture, persistence, testing, CI, migrations, and runtime operations.

Primary references:
- `docs/system-technical-maintainer-guide.md` (hub)
- `docs/deployment.md` (deployment runbook)
- `docs/csv-closure-audit.md` (feature/requirements closure status)

## Backend Architecture

### Layering and Responsibilities

The codebase uses a practical layered architecture:

- `ISS.Domain`
  - entity models and invariants
  - status enums
  - core workflow rules (for example, posting/approval restrictions)
- `ISS.Application`
  - orchestration services that coordinate domain entities, persistence, inventory movements, notifications
  - interfaces and cross-cutting abstractions (`IClock`, `IIssDbContext`, etc.)
- `ISS.Infrastructure`
  - EF Core `IssDbContext`
  - ASP.NET Identity persistence
  - implementations for document PDFs and notifications
- `ISS.Api`
  - HTTP endpoints, DTO mapping, authorization, startup, middleware, health, hosted background dispatch

Guiding principle:

- Business behavior should live in domain entities and application services.
- Controllers should stay thin (validate request shape, authorize, delegate, return DTOs).

### Startup Pipeline (`backend/src/ISS.Api/Program.cs`)

Key startup behaviors:

- Registers application and infrastructure layers (`AddIssApplication`, `AddIssInfrastructure`)
- Configures JWT bearer auth
- Registers a DB-backed health check (`/health`)
- Seeds roles on startup (`Admin`, `Procurement`, `Inventory`, `Sales`, `Service`, `Finance`, `Reporting`)
- Applies DB initialization behavior based on `Database:InitializationMode`
- Enables Swagger in Development
- Uses custom exception middleware for ProblemDetails responses

Important hardening behavior:

- Non-Development startup validates `Jwt:Key` via `JwtConfigurationValidator`
- In Development, missing JWT key falls back to built-in dev key with a warning

### Configuration Keys (High Value)

Common keys used in runtime and deployment:

- `ConnectionStrings__Default`
- `Database__InitializationMode` (`EnsureCreated`, `Migrate`, `None`)
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Key`
- `Notifications__Enabled`
- `Notifications__EmailEnabled`
- `Notifications__SmsEnabled`
- `Notifications__Dispatcher__Enabled`
- SMTP/Twilio notification provider settings (see `docs/deployment.md`)

### Authentication and Authorization

Auth endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`

Behavior:

- First registered user becomes `Admin`
- JWT token returned by auth endpoints contains user/role claims

Role constants are defined in:

- `backend/src/ISS.Api/Security/Roles.cs`

Authorization model:

- Controllers or actions generally use `[Authorize(Roles = "...")]`
- Reporting endpoints require `Admin` or `Reporting`
- Document collaboration spans multiple roles (`Procurement`, `Inventory`, `Sales`, `Service`, `Finance`)

When changing roles:

- Update `Roles.cs`
- Update controller `[Authorize]` attributes
- Verify seed behavior in `Program.cs`
- Update UI assumptions if menu visibility/paths depend on access
- Add/adjust integration tests for `403` behavior if introducing stricter policies

### Exception Handling and API Error Shape

Global error mapping is implemented in:

- `backend/src/ISS.Api/Middleware/ExceptionHandlingMiddleware.cs`

Mappings:

- `NotFoundException` -> `404 Not Found`
- `DomainValidationException` -> `400 Validation Error`
- `DbUpdateConcurrencyException` -> `409 Concurrency Conflict`
- all others -> `500 Server Error`

In Development, `500` responses include detailed exception text. This is useful for local debugging but should not be relied on by frontend logic.

### Persistence, EF Core, Identity, and Audit Logs

Primary DbContext:

- `backend/src/ISS.Infrastructure/Persistence/IssDbContext.cs`

Interface abstraction:

- `backend/src/ISS.Application/Persistence/IIssDbContext.cs`

Notes:

- `IssDbContext` inherits from `IdentityDbContext<...>`, so Identity tables live in the same PostgreSQL database
- Domain tables and Identity tables are managed together in EF migrations

Audit behavior:

- `SaveChangesAsync` in `IssDbContext` applies auditing (`CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`) for `AuditableEntity`
- It also writes `AuditLog` rows by inspecting EF change tracker entries
- Identity user/role changes are excluded from audit log generation

Implication for maintainers:

- If you add a new auditable entity, ensure it inherits the shared auditable base where appropriate
- If you add unusual shadow properties or custom persistence behavior, verify audit log serialization still behaves correctly

### Database Initialization Modes and Migration Strategy

The system supports three schema init modes at startup:

- `EnsureCreated`
  - convenient for quick dev bootstrap on a new database
  - does not maintain EF migration history correctly for long-term upgrades
- `Migrate`
  - preferred for realistic dev/staging/prod parity
  - applies EF migrations
- `None`
  - no schema changes at startup

Important rule for future agents:

- Prefer `Migrate` for persistent local/dev databases.
- `EnsureCreated` can create schema drift and later break when new tables are added (for example, missing `DocumentComments`/`DocumentAttachments` in an old local DB).

If you hit errors like:

- `relation "DocumentComments" does not exist`

it usually means app code is newer than the local DB schema. Fix by running migrations on the current DB, or reset/recreate local DB and migrate cleanly.

### Application Services (Business Orchestration)

Registered in `backend/src/ISS.Application/DependencyInjection.cs`:

- `InventoryService`
- `InventoryOperationsService`
- `DocumentNumberService`
- `ProcurementService`
- `SalesService`
- `ServiceManagementService`
- `FinanceService`
- `NotificationService`

Patterns:

- Controllers typically call these services for mutations/posting/conversions
- Services perform entity loading, invariants, downstream side effects (inventory, AR/AP, notifications)
- Document numbers are generated centrally (`DocumentNumberService`) and persisted via sequences

Document number reliability detail:

- `DocumentNumberService` runs serializable sequence updates inside EF Core execution strategy (`CreateExecutionStrategy().ExecuteAsync(...)`).
- This is required when Npgsql retry strategy is enabled (`EnableRetryOnFailure`) so user-initiated transactions remain retriable.

When extending workflows:

- Add behavior first in domain entity if it is an invariant/state transition
- Use application service for orchestration across aggregates/bounded concerns
- Keep controller logic focused on IO and DTO mapping

### Controller Organization

Top-level controller areas under `backend/src/ISS.Api/Controllers`:

- `Admin`
- `Documents`
- `Finance`
- `Inventory`
- `Procurement`
- `Sales`
- `Service`
- root-level controllers for auth/reporting/health-adjacent endpoints and shared APIs

Conventions commonly used:

- Route prefixes by module (for example `/api/procurement/...`, `/api/sales/...`)
- DTO `record` types defined inside controllers
- `NoContent()` for successful command-style actions
- domain/service exceptions mapped by middleware to ProblemDetails

Master data API surface (current):

- Core: `/api/items`, `/api/brands`, `/api/warehouses`, `/api/suppliers`, `/api/customers`
- Classification/units: `/api/item-categories`, `/api/item-subcategories`, `/api/uoms`, `/api/uom-conversions`
- Finance-related masters: `/api/taxes`, `/api/tax-conversions`, `/api/currencies`, `/api/currency-rates`, `/api/payment-types`
- Operational metadata: `/api/reference-forms`, `/api/reorder-settings`

Master-data API action standard:

- Core master data controllers now expose `GET`, `POST`, `PUT`, and `DELETE` actions for maintainable row-level operations.
- Delete actions return `409 Conflict` for in-use records, with guidance to mark records inactive instead.
- Reorder settings follow upsert + delete semantics:
  - `POST /api/reorder-settings` (upsert by warehouse+item)
  - `DELETE /api/reorder-settings/{id}`

Line item API standard:

- For line-based draft documents, controllers now expose:
  - `POST /{id}/lines` (add)
  - `PUT /{id}/lines/{lineId}` (edit)
  - `DELETE /{id}/lines/{lineId}` (delete)
- This is implemented across procurement, sales, inventory, and service line-document controllers, not only PO/GRN.
- Application services and domain aggregates enforce draft-only line mutation rules.

### Document Collaboration and Attachments (High Change Surface)

Document comments and attachments are handled by:

- `backend/src/ISS.Api/Controllers/Documents/DocumentCollaborationController.cs`

Capabilities:

- comments CRUD by `referenceType` + `referenceId`
- attachments list/upload/download/delete by `referenceType` + `referenceId`
- filesystem-backed storage under API `App_Data/document-attachments/...`

Reference types:

- Normalized/validated by the domain (`DocumentComment.NormalizeReferenceType(...)`)
- Used across procurement/sales/service screens to attach notes/files to business documents

Attachment hardening is centralized in:

- `backend/src/ISS.Api/AttachmentUploadPolicy.cs`

Current policy includes:

- file size limit (`25 MB`)
- per-record file count quota (`25`)
- per-record total storage quota (`100 MB`)
- extension allowlist
- content-type allowlist
- filename sanitization
- notes length validation
- file signature/magic-byte validation for common formats

Important maintainer rule:

- If changing attachment rules, update `AttachmentUploadPolicy` first and keep both attachment entry points aligned:
  - document collaboration attachments
  - item attachments (in `ItemsController`)
- Add integration tests for both allowed and rejected cases

### Reporting Module (Current)

Reporting endpoints live in:

- `backend/src/ISS.Api/Controllers/ReportingController.cs`

Current endpoints include:

- `/api/reporting/dashboard`
- `/api/reporting/stock-ledger`
- `/api/reporting/aging`
- `/api/reporting/tax-summary`
- `/api/reporting/service-kpis`
- `/api/reporting/costing`

Notes:

- The reporting pack covers operational dashboards and item-level costing visibility.
- Advanced reporting (for example profitability slices, supplier performance, export-heavy analytics) is still pending.

Implementation caution:

- Be careful with PostgreSQL numeric aggregation precision for complex computed sums
- In at least one case (`tax-summary`), a safer approach is filtering in SQL and doing arithmetic in C# to avoid `numeric` overflow during translation/aggregation

### Notifications and Outbox Dispatch

Notification enqueueing is handled in:

- `backend/src/ISS.Application/Services/NotificationService.cs`

Background dispatch is handled in:

- `backend/src/ISS.Api/Services/NotificationDispatcherHostedService.cs`

Pattern:

- workflow services enqueue outbox items (email/SMS)
- hosted service polls pending items
- sender implementations are real (SMTP/Twilio) or no-op based on config
- retries use exponential backoff

When adding new notification-producing actions:

- enqueue through `NotificationService`
- persist inside the same unit of work
- add integration tests for outbox entry creation (not external delivery)


## Testing Strategy and Tooling

### Unit Tests

Location:

- `backend/tests/ISS.UnitTests`

Used for:

- domain invariants
- application helpers/validators (for example JWT configuration validator)

### Integration Tests

Location:

- `backend/tests/ISS.IntegrationTests`

Key files:

- `Fixtures/IssApiFactory.cs`
- `Fixtures/IssApiFixture.cs`
- `EndToEndTests.cs`

Behavior:

- Spins up in-process API host using `WebApplicationFactory`
- By default uses Testcontainers PostgreSQL
- Seeds/obtains admin token automatically
- Disables background notification dispatcher for deterministic tests

Important nuance:

- Integration fixture currently uses `EnsureCreated()` / `EnsureDeleted()` semantics, not EF migrations
- This is fast and stable for flow tests, but it can miss migration-chain issues
- CI compensates with a dedicated migrations + health smoke job

External PostgreSQL fallback (for environments where Docker/Testcontainers pipe access is blocked):

- `ISS_INTEGRATIONTESTS_CONNECTION_STRING`
- `ISS_INTEGRATIONTESTS_RESET_EXISTING_DB`

This fallback is implemented in `IssApiFixture` and documented in `README.md` and `docs/deployment.md`.

### Frontend Validation

Primary validation in CI and local checks:

- `npm run build`

This catches:

- TypeScript issues
- App Router compile/runtime build issues
- route compilation and render-time fetch/type errors

### CI Workflow

Workflow file:

- `.github/workflows/ci.yml`

Jobs:

- `backend-tests` -> restore/build/unit/integration tests
- `frontend-build` -> `npm ci` + production Next.js build
- `migrations-health-smoke` -> apply EF migrations to fresh PostgreSQL, start API, run `scripts/ops/smoke-api.ps1`

Why this matters:

- Integration tests validate business flows
- Migrations smoke validates schema upgrade path (which integration tests do not fully cover)
- Frontend build validates route/component compilation

## Local Development and Operations

### Local Infrastructure

From repo root:

```powershell
docker compose up -d
```

Local compose defaults (`docker-compose.yml`):

- Host: `localhost`
- Port: `5433`
- Database: `iss`
- User: `pgadmin`
- Password: `vesper`

### Start Backend and Frontend

Backend:

```powershell
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

Frontend:

```powershell
cd frontend
copy .env.example .env.local
npm install
npm run dev
```

URLs:

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5257`
- Health: `http://localhost:5257/health`

### Migrations (Preferred for Ongoing Local DBs)

Apply migrations manually:

```powershell
$env:ConnectionStrings__Default="Host=localhost;Port=5433;Database=iss;Username=pgadmin;Password=vesper"
dotnet ef database update `
  --project backend/src/ISS.Infrastructure/ISS.Infrastructure.csproj `
  --startup-project backend/src/ISS.Api/ISS.Api.csproj
```

Or let startup apply migrations:

```powershell
$env:Database__InitializationMode="Migrate"
dotnet run --project backend/src/ISS.Api/ISS.Api.csproj
```

### Local Schema Drift Recovery (Common)

Symptom:

- `500 Server Error` with PostgreSQL relation missing (for example `DocumentComments`)

Cause:

- Local DB was created with old schema (`EnsureCreated`) and code now expects newer tables

Fastest local-dev fix:

1. Stop API (optional but recommended)
2. Reset local DB in Docker Postgres
3. Recreate DB
4. Apply EF migrations
5. Restart API

Example reset commands (local dev only, data destructive):

```powershell
docker exec iss-postgres psql -U pgadmin -d postgres -c "DROP DATABASE IF EXISTS iss WITH (FORCE);"
docker exec iss-postgres psql -U pgadmin -d postgres -c "CREATE DATABASE iss OWNER pgadmin;"

$env:ConnectionStrings__Default="Host=localhost;Port=5433;Database=iss;Username=pgadmin;Password=vesper"
dotnet build backend/src/ISS.Api/ISS.Api.csproj -c Release --nologo
dotnet ef database update --configuration Release --no-build `
  --project backend/src/ISS.Infrastructure/ISS.Infrastructure.csproj `
  --startup-project backend/src/ISS.Api/ISS.Api.csproj
```

### API Smoke Script

Use:

- `scripts/ops/smoke-api.ps1`

Supports:

- health-only checks
- login via email/password or bearer token
- basic authenticated read checks (`/reporting/dashboard`, `/audit-logs`, `/items`, `/customers`)

Used by CI and suitable for post-deploy smoke verification.

