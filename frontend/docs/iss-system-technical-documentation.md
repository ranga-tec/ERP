# ISS ERP System Technical Documentation (Frontend + Backend)

Generated from code inspection on 2026-02-22.

## 0. Implementation Update (2026-02-27)

The following scope was implemented after the previous documentation snapshot:

- Master-data expansion:
  - payment types
  - taxes
  - tax conversions
  - currencies
  - currency rates
  - reference forms
  - UoM conversion management surfaced in UI and API
- Reporting expansion:
  - costing report endpoint and UI page (`/api/reporting/costing`, `/reporting/costing`)
- Draft line-grid action completeness:
  - all line-based document detail screens now support row-level `Edit`, `Save/Cancel`, and `Delete`
  - backend line APIs were standardized with `POST/PUT/DELETE` line endpoints across procurement, sales, inventory, and service documents
- Master-data maintenance completeness:
  - all maintained master-data grids now expose row-level `Edit`, `Save/Cancel`, and `Delete`
  - backend master-data controllers now expose delete endpoints with conflict-safe responses for in-use records
  - item maintenance panel now supports direct item deletion with confirmation
- Reliability fix:
  - `DocumentNumberService` sequence transaction now runs inside EF execution strategy for compatibility with Npgsql retry strategy

These updates supersede earlier notes that line editing/deleting existed only for PO/GRN.

## 0. Implementation Update (2026-02-22, Post-Documentation)

The following features were implemented after the initial documentation pass:

- Phase 1 (Master Data / Item enhancements)
  - `UnitOfMeasure` master (backend API + frontend page)
  - `ItemCategory` and `ItemSubcategory` masters (backend API + frontend page)
  - `Item` classification support (`CategoryId`, `SubcategoryId`) with validation
  - Item list search/filter panel (search, type, tracking, active, brand, category, UoM)
  - Item edit UI for existing records (including cost updates and active flag)
  - Item default cost history endpoint (derived from audit log deltas on `Items.DefaultUnitCost`)
- Item attachments/images
  - Initial metadata-only attachment support was added first
  - Then upgraded to real file upload/storage on backend filesystem (`multipart/form-data`)
  - File content is served via backend streaming endpoint; UI uses proxy URLs (`/api/backend/...`)
- Phase 2 (started)
  - New `Purchase Requisition` module (list/create/detail/add lines/submit/approve/reject/cancel)
  - PR -> PO conversion flow (approved requisition can be converted to draft PO with selected supplier)

### New / Updated Backend Endpoints (high-level)

- `GET/POST/PUT /api/items` (extended item DTO/contracts with category/subcategory)
- `GET /api/items/{id}/price-history`
- `GET/POST/DELETE /api/items/{id}/attachments...` (metadata)
- `POST /api/items/{id}/attachments/upload` (binary upload)
- `GET /api/items/{id}/attachments/{attachmentId}/content` (stream file)
- `GET/POST/PUT /api/uoms`
- `GET/POST/PUT /api/item-categories`
- `GET/POST/PUT /api/item-subcategories`
- `GET/POST /api/procurement/purchase-requisitions`
- `GET /api/procurement/purchase-requisitions/{id}`
- `POST /api/procurement/purchase-requisitions/{id}/lines`
- `POST /api/procurement/purchase-requisitions/{id}/submit|approve|reject|cancel`
- `POST /api/procurement/purchase-requisitions/{id}/convert-to-po`

### Local DB Reset / Recreate Notes

- Development DB is PostgreSQL (`docker-compose.yml`, service `db`, exposed at `localhost:5433`)
- The backend uses `EnsureCreated()` on startup, so schema changes are not applied to an existing DB automatically
- To load the new tables/columns in local development, the DB volume was reset and recreated
- During recreation, backend startup initially failed due an overridden environment connection string with incorrect credentials (`user "pward"`)
- Startup succeeded after forcing `ConnectionStrings__Default=Host=localhost;Port=5433;Database=iss;Username=pgadmin;Password=vesper;...`

### Data Storage Note (Item Attachments)

- Uploaded item attachments are stored on backend server filesystem under:
  - `App_Data/item-attachments/<itemId>/<attachmentId>.<ext>`
- Metadata persists in `ItemAttachments` table, including:
  - `Url` (API content endpoint)
  - `StoragePath` (server-relative path)
  - content metadata + notes + audit fields

### Current Phase 2 Scope Status

- Implemented:
  - Purchase Requisition (core workflow)
  - PR -> PO conversion bridge
- Not yet implemented:
  - Direct Purchase
  - Supplier Invoice / AP Bill
  - Advanced PR/PO matching workflows and reporting around PR lifecycle

## 1. Scope and Purpose

This document describes the current implementation of the ISS ERP system in this repository, with emphasis on:

- Architecture (frontend and backend)
- Runtime request flow and authentication
- Module-by-module implementation patterns
- Domain workflow behavior (create/add lines/post/approve/allocate)
- Extension/change impact guidance
- Known risks and technical debt relevant to future changes

Primary inspected areas:

- Frontend: `frontend/src/**` (Next.js 16 App Router app)
- Backend: `backend/src/**` (ASP.NET Core 8 layered solution)
- Tests: `backend/tests/**`

## 2. Repository / Solution Structure

Top-level (relevant to this document):

- `frontend/` - Next.js UI app and Next API proxy/auth endpoints
- `backend/` - ASP.NET Core API + Application + Domain + Infrastructure + tests
- `docs/` - project-level docs (requirements, deployment, etc.)
- `docker-compose.yml` - local orchestration support

Backend solution projects (`backend/src`):

- `ISS.Api` - HTTP API, auth, controllers, middleware, hosted services
- `ISS.Application` - application services and abstractions
- `ISS.Domain` - entities, enums, invariants, domain validation
- `ISS.Infrastructure` - EF Core persistence, Identity, PDF rendering, notifications

## 3. System Architecture Summary

### 3.1 High-level runtime topology

1. Browser loads the Next.js frontend (`frontend`).
2. Next route guard (`src/proxy.ts`) checks for `iss_token` and redirects to `/login` when absent.
3. UI pages fetch data via:
   - server components -> `backendFetchJson()` -> backend directly
   - client components -> `api-client.ts` -> `/api/backend/*` -> backend
4. Next catch-all proxy route `src/app/api/backend/[...path]/route.ts` forwards to the ASP.NET backend (`ISS_API_BASE_URL`).
5. ASP.NET Core controllers call application services and/or EF Core via `IIssDbContext`.
6. EF Core (`IssDbContext`) persists to PostgreSQL and auto-records audit logs on save.

### 3.2 Backend layering model

- Controllers are thin:
  - bind request DTOs
  - shape response DTOs
  - delegate to application services
- Application services orchestrate:
  - document numbers
  - posting workflows
  - inventory movements
  - AR/AP and allocations
  - notifications
- Domain entities enforce invariants and state transitions.

## 4. Frontend Technical Architecture

## 4.1 Frontend stack and config

Files:

- `frontend/package.json`
- `frontend/next.config.ts`
- `frontend/tsconfig.json`
- `frontend/postcss.config.mjs`
- `frontend/eslint.config.mjs`

Key points:

- Next.js `16.1.6`, React `19.2.3`, TypeScript `5`
- Tailwind CSS v4 (`@tailwindcss/postcss`)
- App Router architecture (`src/app`)
- TS path alias `@/* -> src/*`
- `next.config.ts` is effectively default
- `frontend/README.md` is still the default Next template and does not document the ERP implementation

## 4.2 Frontend app structure and rendering model

Key files:

- `frontend/src/app/layout.tsx` - root layout, fonts, global CSS
- `frontend/src/app/(app)/layout.tsx` - authenticated shell (sidebar + header + logout)
- `frontend/src/app/(auth)/login/page.tsx` - login/register page
- `frontend/src/components/Sidebar.tsx` - static module navigation
- `frontend/src/components/ui.tsx` - reusable UI primitives

Rendering pattern:

- List/detail pages are mostly server components.
- Forms and action buttons are client components (`\"use client\"`).
- Server pages fetch with `backendFetchJson`.
- Client mutations call `apiPost/apiPostNoContent/apiPut`, then `router.refresh()`.

Observed frontend scale (`src/app` scan):

- 113 `.ts/.tsx` files
- 52 `page.tsx` routes
- 5 Next API route files
- 55 client components (approx. by `\"use client\"` header)

## 4.3 Authentication and session flow (frontend)

Relevant files:

- `frontend/src/proxy.ts`
- `frontend/src/app/api/auth/login/route.ts`
- `frontend/src/app/api/auth/register/route.ts`
- `frontend/src/app/api/auth/logout/route.ts`
- `frontend/src/app/api/auth/session/route.ts`
- `frontend/src/lib/jwt.ts`
- `frontend/src/lib/env.ts`

Flow:

1. Protected route is requested.
2. `src/proxy.ts` checks for cookie `iss_token`.
3. Missing cookie -> redirect to `/login?next=<pathname>`.
4. Login/register UI posts to frontend auth routes.
5. Frontend auth routes call backend `/api/auth/login` or `/api/auth/register`.
6. Frontend stores JWT in HTTP-only cookie `iss_token` (8h).
7. App shell decodes JWT payload to display email/roles in header.

Important details:

- `sessionFromToken()` decodes JWT payload without signature verification (display-only use).
- `src/proxy.ts` checks cookie presence, not validity/expiry.

## 4.4 Frontend data access implementation

### 4.4.1 Server helper: `backendFetchJson`

File: `frontend/src/lib/backend.server.ts`

Behavior:

- Reads `iss_token` from Next cookies
- Builds backend URL using `ISS_API_BASE_URL` (default `http://localhost:5257`)
- Adds bearer token if present
- Uses `cache: \"no-store\"`
- Throws on non-2xx with backend text body

### 4.4.2 Client helper: `api-client`

File: `frontend/src/lib/api-client.ts`

Functions:

- `apiGet`
- `apiPost`
- `apiPostNoContent`
- `apiPut`

Behavior:

- Targets `/api/backend/*`
- Assumes JSON for `apiGet/apiPost/apiPut`
- Throws raw response text on error

### 4.4.3 Next backend proxy route

File: `frontend/src/app/api/backend/[...path]/route.ts`

Responsibilities:

- Forward `GET/POST/PUT/PATCH/DELETE` to backend `/api/...`
- Preserve query params
- Remove `host`/`cookie` headers
- Inject bearer token from cookie
- Support binary responses (PDF/template downloads)
- Preserve `content-type` and `content-disposition`

## 4.5 UI component and styling approach

Files:

- `frontend/src/components/ui.tsx`
- `frontend/src/app/globals.css`

Current approach:

- Small local UI primitives (`Card`, `Button`, `SecondaryButton`, `SecondaryLink`, `Input`, `Textarea`, `Select`, `Table`)
- Tailwind utility classes used directly in pages/components
- No shared form-state or table library
- No shared DTO/types package; types are defined per page/component

Notable detail:

- `globals.css` defines font CSS variables but `body` still uses `Arial, Helvetica, sans-serif`.

## 4.6 Route groups and navigation

Navigation source: `frontend/src/components/Sidebar.tsx`

Route groups:

- `src/app/(auth)` - authentication
- `src/app/(app)` - authenticated application routes

Sidebar sections:

- Overview
- Master Data
- Procurement
- Sales
- Service
- Inventory
- Finance
- Audit
- Admin

## 4.7 Recurring frontend implementation pattern

This pattern is repeated across Procurement, Sales, Inventory, Service, and Finance:

1. Server `page.tsx`
   - fetch main list/detail + reference lookups
   - build `Map` objects (`id -> code/name`)
   - render cards and tables
   - render client form/action component(s)

2. Client create form
   - local `useState` per field
   - local numeric/date parsing and validation
   - `apiPost(...)`
   - success path:
     - `router.push()` to new detail page for document workflows, or
     - `router.refresh()` for master-data list refresh

3. Client action / line-add form
   - local `busy` + `error`
   - `apiPostNoContent(...)`
   - `router.refresh()`

4. Detail pages
   - summary header
   - actions card (often includes PDF link)
   - `Add line` section shown only in Draft status
   - lines table

This consistency is a major strength for extending the system.

## 4.8 Frontend module-by-module implementation inventory

### 4.8.1 Master Data

Routes:

- `/master-data/brands`
- `/master-data/items`
- `/master-data/customers`
- `/master-data/suppliers`
- `/master-data/warehouses`
- `/master-data/reorder-settings`

Representative files:

- `frontend/src/app/(app)/master-data/brands/page.tsx`
- `frontend/src/app/(app)/master-data/items/page.tsx`
- `frontend/src/app/(app)/master-data/customers/page.tsx`
- `frontend/src/app/(app)/master-data/suppliers/page.tsx`
- `frontend/src/app/(app)/master-data/warehouses/page.tsx`
- `frontend/src/app/(app)/master-data/reorder-settings/page.tsx`

Notable behavior:

- `Items` maps enum values to labels locally (`itemTypeLabel`, `trackingLabel`)
- `Items` exposes label PDF via `/api/backend/items/{id}/label/pdf`
- `ReorderSettingUpsertForm` uses `POST /reorder-settings` as upsert semantics
- Master-data create forms generally submit then `router.refresh()`

### 4.8.2 Procurement

Routes:

- `/procurement/rfqs` and `/procurement/rfqs/{id}`
- `/procurement/purchase-orders` and `/procurement/purchase-orders/{id}`
- `/procurement/goods-receipts` and `/procurement/goods-receipts/{id}`
- `/procurement/supplier-returns` and `/procurement/supplier-returns/{id}`

Representative files:

- `frontend/src/app/(app)/procurement/rfqs/[id]/page.tsx`
- `frontend/src/app/(app)/procurement/purchase-orders/[id]/page.tsx`
- `frontend/src/app/(app)/procurement/goods-receipts/[id]/page.tsx`
- `frontend/src/app/(app)/procurement/supplier-returns/[id]/page.tsx`

Workflow pattern:

- RFQ: Draft -> Sent
- PO: Draft -> Approved -> Partially Received/Closed (backend-driven)
- GRN: Draft -> Posted
- Supplier Return: Draft -> Posted

Tracked inventory line handling (GRN / Supplier Return):

- `batchNumber` + `serials` inputs
- local `parseList()` for serials (comma/newline split)
- client-side numeric validation, backend enforces tracking rules

### 4.8.3 Sales

Routes:

- `/sales/quotes` and detail
- `/sales/orders` and detail
- `/sales/dispatches` and detail
- `/sales/invoices` and detail

Representative files:

- `frontend/src/app/(app)/sales/quotes/[id]/page.tsx`
- `frontend/src/app/(app)/sales/orders/[id]/page.tsx`
- `frontend/src/app/(app)/sales/dispatches/[id]/page.tsx`
- `frontend/src/app/(app)/sales/invoices/[id]/page.tsx`

Workflow pattern:

- Quote: Draft -> Sent
- Order: Draft -> Confirmed -> Fulfilled/Closed
- Dispatch: Draft -> Posted (inventory issue)
- Invoice: Draft -> Posted -> Paid (via finance allocation side effects)

Line form variants:

- Quote / Order lines: `itemId`, `quantity`, `unitPrice`
- Dispatch lines: `itemId`, `quantity`, `batchNumber`, `serials`
- Invoice lines: adds `discountPercent` and `taxPercent`

### 4.8.4 Service

Routes:

- `/service/equipment-units` and detail
- `/service/jobs` and detail
- `/service/work-orders` and detail
- `/service/material-requisitions` and detail
- `/service/quality-checks` and detail

Representative files:

- `frontend/src/app/(app)/service/jobs/[id]/page.tsx`
- `frontend/src/app/(app)/service/material-requisitions/[id]/page.tsx`
- `frontend/src/app/(app)/service/quality-checks/[id]/page.tsx`

Key behavior:

- Equipment Units are linked to equipment Items and Customers
- Service Jobs expose `start`, `complete`, `close` actions
- Work Orders are currently create + view (no frontend status mutation UI)
- Material Requisitions are document workflows with tracked line inputs and post action
- Quality Checks are simple QA result records (`passed` + notes)

### 4.8.5 Inventory

Routes:

- `/inventory/onhand`
- `/inventory/reorder-alerts`
- `/inventory/stock-adjustments` and detail
- `/inventory/stock-transfers` and detail

Distinct patterns:

- `OnHandQuery` is a client-side query widget (not a list page) using `apiGet`
- `Reorder Alerts` uses server-side `searchParams` filtering with `<form method=\"GET\">`

Stock movement workflows:

- Stock Adjustment:
  - Draft + lines + `post`/`void`
  - line field `quantityDelta` supports positive and negative
- Stock Transfer:
  - Draft + lines + `post`/`void`
  - backend domain enforces `fromWarehouseId != toWarehouseId`

### 4.8.6 Finance

Routes:

- `/finance/ar`
- `/finance/ap`
- `/finance/payments` and detail
- `/finance/credit-notes` and detail
- `/finance/debit-notes` and detail

Key behavior:

- AR/AP pages are read-model views with `outstandingOnly` filter
- Payment detail computes allocated/remaining client-side and conditionally loads AR or AP entries by direction
- Payment allocation filters entries by counterparty where possible
- Credit Note detail supports manual and auto allocation (`auto-allocate`)
- Debit Notes are create/list/detail only (no allocation UI)

Reference-type link routing is hardcoded in frontend payment and AR/AP pages, so backend `ReferenceType` strings are a cross-layer contract.

### 4.8.7 Admin and Audit

Routes:

- `/admin/import`
- `/admin/notifications`
- `/admin/users`
- `/audit-logs`

Key behavior:

- Excel import uploads `.xlsx` via `FormData` and supports template download
- Notification outbox UI supports retry per row
- Admin Users UI supports:
  - create user
  - edit roles
  - reset password
  - enable/disable accounts
- Audit logs render `changesJson` inside expandable `<details>`

## 4.9 Frontend route inventory (high-level grouped)

- Dashboard: `/`
- Auth: `/login`
- Master Data: brands, items, customers, suppliers, warehouses, reorder-settings
- Procurement: rfqs, purchase-orders, goods-receipts, supplier-returns (+ detail pages)
- Sales: quotes, orders, dispatches, invoices (+ detail pages)
- Service: equipment-units, jobs, work-orders, material-requisitions, quality-checks (+ detail pages)
- Inventory: onhand, reorder-alerts, stock-adjustments, stock-transfers (+ detail pages)
- Finance: ar, ap, payments, credit-notes, debit-notes (+ detail pages)
- Admin: import, notifications, users
- Audit: audit-logs

## 4.10 Frontend change hotspots

When changing a document workflow (PO/GRN/Dispatch/Invoice/etc.), the usual frontend touchpoints are:

1. List page DTO + columns
2. Create form payload and validation
3. Detail page DTO and summary fields
4. Line add form payload and validation
5. Action button endpoint path / gating logic
6. Status label maps (`Record<number, string>`)
7. Sidebar navigation (if route is new)

Because status labels and DTOs are duplicated across pages, backend contract changes require a wide UI sweep.

## 5. Backend Technical Architecture

## 5.1 Backend stack

From project files (`backend/src/*.csproj`):

- .NET 8 (`net8.0`)
- ASP.NET Core Web API
- EF Core 8 + Npgsql
- ASP.NET Core Identity (EF-backed)
- JWT bearer auth
- Swashbuckle / Swagger
- ClosedXML (Excel import/template)
- QuestPDF + QRCoder + ZXing + SkiaSharp (PDFs, QR/barcode)
- MailKit (SMTP)
- Twilio integration via `HttpClient`

## 5.2 Backend composition root and startup behavior

File: `backend/src/ISS.Api/Program.cs`

Key startup behavior:

- Registers application and infrastructure layers via DI extension methods
- Configures JWT bearer validation from `Jwt` config section
- Registers `JwtTokenService` and `NotificationDispatcherHostedService`
- Enables controllers + Swagger
- Calls `db.Database.EnsureCreatedAsync()`
- Seeds Identity roles from `ISS.Api.Security.Roles.All`
- Adds global exception middleware

Important operational note:

- The API currently uses `EnsureCreated`, not EF migrations.

## 5.3 Backend configuration model

Relevant files:

- `backend/src/ISS.Api/appsettings.json`
- `backend/src/ISS.Api/appsettings.Development.json`
- `backend/src/ISS.Application/Options/NotificationOptions.cs`
- `backend/src/ISS.Application/Options/NotificationDispatcherOptions.cs`

Key config sections:

- `ConnectionStrings:Default`
- `Jwt:{Issuer,Audience,Key}`
- `Notifications`
  - `Enabled`
  - `EmailEnabled`
  - `SmsEnabled`
  - `Dispatcher:{Enabled,PollSeconds,BatchSize,MaxAttempts}`
  - SMTP config
  - Twilio config

## 5.4 Authentication, authorization, and identity

Relevant files:

- `backend/src/ISS.Api/Controllers/AuthController.cs`
- `backend/src/ISS.Api/Services/JwtTokenService.cs`
- `backend/src/ISS.Api/Services/CurrentUser.cs`
- `backend/src/ISS.Api/Security/Roles.cs`

Implementation notes:

- ASP.NET Core Identity handles users, passwords, and roles.
- JWT contains:
  - `sub`
  - `ClaimTypes.NameIdentifier`
  - `ClaimTypes.Name`
  - optional `ClaimTypes.Email`
  - `ClaimTypes.Role` (one claim per role)
- Token lifetime is 8 hours.
- First registered user is auto-assigned `Admin`.

Role set used across controllers:

- `Admin`
- `Procurement`
- `Inventory`
- `Sales`
- `Service`
- `Finance`
- `Reporting`

## 5.5 Global exception handling

File: `backend/src/ISS.Api/Middleware/ExceptionHandlingMiddleware.cs`

Handled mappings:

- `NotFoundException` -> HTTP 404
- `DomainValidationException` -> HTTP 400
- `DbUpdateConcurrencyException` -> HTTP 409
- unhandled `Exception` -> HTTP 500

Response shape:

- JSON `ProblemDetails`
- dev mode includes more detail

## 5.6 Persistence and EF Core model

Primary files:

- `backend/src/ISS.Infrastructure/Persistence/IssDbContext.cs`
- `backend/src/ISS.Application/Persistence/IIssDbContext.cs`

Key characteristics:

- `IssDbContext` extends `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`
- Identity and domain tables share the same DB
- Rich `OnModelCreating` configuration covers:
  - unique indexes (codes, numbers, barcodes, serials)
  - decimal precision (qty/cost/amount)
  - string lengths
  - cascade delete for lines and serial child rows

### 5.6.1 Audit logging on save

`IssDbContext.SaveChangesAsync()` performs:

1. `ApplyAuditing()` on `AuditableEntity`
2. `AddAuditLogs()` by scanning the EF change tracker

`AddAuditLogs()`:

- captures added/modified/deleted entities
- skips `AuditLog` itself and Identity user/role entities
- serializes changed property values to JSON
- inserts `AuditLog` rows automatically

This is the backend source for the frontend `/audit-logs` page.

## 5.7 Domain model implementation style

Key files:

- `backend/src/ISS.Domain/Common/Guard.cs`
- `backend/src/ISS.Domain/Common/DomainValidationException.cs`
- `backend/src/ISS.Domain/Common/AuditableEntity.cs`
- entity files under `backend/src/ISS.Domain/*`

Conventions:

- Constructors enforce initial invariants
- `Update(...)` methods re-validate and normalize
- State transitions are explicit methods (`Post`, `Approve`, `Confirm`, `Void`, `MarkSent`, etc.)
- Invalid state transitions throw `DomainValidationException`

Examples inspected:

- `Item` enforces required fields and non-negative `DefaultUnitCost`
- `StockTransfer` enforces different source/destination warehouses and draft-only edits
- `SalesInvoice` computes totals from lines and controls Draft/Posted/Paid/Voided transitions
- `ServiceJob` enforces Open/InProgress/Completed/Closed lifecycle
- `CreditNote` tracks `RemainingAmount` and prevents over-allocation

## 5.8 Application services (business workflow orchestration)

DI registration: `backend/src/ISS.Application/DependencyInjection.cs`

Registered services:

- `InventoryService`
- `InventoryOperationsService`
- `DocumentNumberService`
- `ProcurementService`
- `SalesService`
- `ServiceManagementService`
- `FinanceService`
- `NotificationService`

### 5.8.1 DocumentNumberService

File: `backend/src/ISS.Application/Services/DocumentNumberService.cs`

Behavior:

- Generates sequential numbers per document type using `DocumentSequence`
- Uses a `Serializable` DB transaction to avoid duplicate numbers

### 5.8.2 InventoryService (movement engine)

File: `backend/src/ISS.Application/Services/InventoryService.cs`

Responsibilities:

- `GetOnHandAsync`
- record inventory movements for receipts, issues, adjustments, transfers, consumption
- validate tracking rules for serial/batch items
- enforce stock availability for outbound movements

This service is a core dependency for Procurement, Sales, and Service posting flows.

### 5.8.3 InventoryOperationsService

File: `backend/src/ISS.Application/Services/InventoryOperationsService.cs`

Responsibilities:

- create/post/void stock adjustments
- create/post/void stock transfers
- add lines/serials
- invoke `InventoryService` during posting to persist movement rows

### 5.8.4 ProcurementService

File: `backend/src/ISS.Application/Services/ProcurementService.cs`

Responsibilities:

- RFQs: create, add line, mark sent
- Purchase Orders: create, add line, approve (+ optional supplier notifications)
- Goods Receipts: create, add line, post
  - posting records inventory receipts
  - updates PO received quantities
  - creates AP entries
- Supplier Returns: create, add line, post
  - posting issues inventory
  - creates supplier credit note
  - auto-allocates that credit note to outstanding AP

### 5.8.5 SalesService

File: `backend/src/ISS.Application/Services/SalesService.cs`

Responsibilities:

- Quotes: create, add line, mark sent
- Sales Orders: create, add line, confirm
- Dispatch Notes: create, add line, post
  - posting issues inventory
  - marks order fulfilled
- Sales Invoices: create, add line, post
  - posting creates AR entry
  - may enqueue customer notifications

### 5.8.6 ServiceManagementService

File: `backend/src/ISS.Application/Services/ServiceManagementService.cs`

Responsibilities:

- equipment unit creation
- service job create/start/complete/close
- work order creation
- material requisition create/add-line/post
  - posting records inventory consumption
- quality check creation

### 5.8.7 FinanceService

File: `backend/src/ISS.Application/Services/FinanceService.cs`

Responsibilities:

- payment creation
- payment allocation to AR/AP
- credit note creation and allocation (manual + auto)
- debit note creation (auto-creates AR/AP charge entry)
- marks sales invoices paid when AR invoice entries are fully settled

Cross-domain significance:

- Finance allocations can update Sales invoice state (`MarkPaid`)
- Supplier returns (Procurement) and debit notes (Finance) affect AP/AR balances

## 5.9 API controller design and routing

Approximate controller count: 33 (`backend/src/ISS.Api/Controllers`)

Controller design pattern:

- `[ApiController]` + `[Route(\"api/...\")]`
- `[Authorize(Roles = \"...\")]` at controller level
- request/response DTOs defined as nested `record` types
- list endpoints often support `skip/take`
- detail endpoints often return denormalized DTOs
- `/pdf` endpoints delegate to `IDocumentPdfService`
- mutations delegate to application services and return `NoContent()` or DTOs

Examples inspected:

- `ItemsController` (CRUD + barcode lookup + label PDF)
- `PurchaseOrdersController` (list/detail/create/add-line/approve/PDF)
- `PaymentsController` (list/detail/create/allocate/PDF)
- `CreditNotesController` (list filters + detail + manual/auto allocation + PDF)
- `ServiceJobsController` (start/complete/close actions)
- `StockAdjustmentsController` (create/add-line/post/void/PDF)
- `ArApController` (read models with `outstandingOnly`)
- `ImportController` (Excel template + transactional import)
- `NotificationsController` (outbox list/get/retry)
- `AuditLogsController` (recent audit list)

## 5.10 PDF generation subsystem

Files:

- `backend/src/ISS.Application/Abstractions/IDocumentPdfService.cs`
- `backend/src/ISS.Infrastructure/Documents/DocumentPdfService.cs`
- partial document renderers in `backend/src/ISS.Infrastructure/Documents/*`

Capabilities:

- Generates PDFs for business documents and item labels
- Uses QuestPDF for layout
- Adds QR and barcode payloads using QRCoder + ZXing + SkiaSharp

Frontend integration:

- UI links to `/api/backend/.../pdf`
- Next proxy forwards binary response and preserves `content-disposition`

## 5.11 Notification subsystem (outbox + dispatcher)

Files:

- `backend/src/ISS.Application/Services/NotificationService.cs`
- `backend/src/ISS.Api/Services/NotificationDispatcherHostedService.cs`
- `backend/src/ISS.Infrastructure/Notifications/*`

Architecture:

1. Business services enqueue notification outbox records transactionally.
2. Background dispatcher polls pending items.
3. Dispatcher marks `Processing`, attempts delivery, then marks `Sent` or `Failed`.
4. Retry uses exponential backoff (30s, 60s, 120s, ... capped at 1h).
5. Admin UI can force retry via notifications controller.

Adapters:

- Email: `SmtpEmailSender` or `NullEmailSender`
- SMS: `TwilioSmsSender` or `NullSmsSender`

## 5.12 Excel import subsystem

Primary file:

- `backend/src/ISS.Api/Controllers/Admin/ImportController.cs`

What it does:

- `GET /api/admin/import/template` builds an Excel template with multiple worksheets
- `POST /api/admin/import/excel` imports master data from `.xlsx`
- import is wrapped in a DB transaction (all-or-nothing)
- row-level validation errors are accumulated and returned to the client

Imported worksheets currently include:

- Brands
- Warehouses
- Suppliers
- Customers
- Items
- ReorderSettings
- EquipmentUnits

Change impact:

- Master-data schema changes may require template, parser, validation, and integration test updates.

## 5.13 Testing architecture and coverage shape

Projects:

- `backend/tests/ISS.UnitTests`
- `backend/tests/ISS.IntegrationTests`

Observed test stack:

- xUnit
- ASP.NET Core `WebApplicationFactory`
- Testcontainers + PostgreSQL for integration tests
- ClosedXML for import test scenarios

Coverage style:

- Unit tests focus on domain invariants and state transitions
- Integration tests cover end-to-end flows including:
  - master-data create
  - procurement -> inventory -> finance side effects
  - supplier returns / credit note behavior
  - PDF endpoints
  - Excel import

Quick stats from scan:

- Approx. `Fact` count: 33
- Approx. controller count: 33

## 6. Cross-System Contract and Workflow Mapping

## 6.1 Status enums and frontend label maps

Frontend pages typically define local `statusLabel: Record<number, string>` maps that assume backend enum numeric values.

Implications:

- Changing enum numeric values in backend is high-risk (UI can silently mislabel statuses).
- Safer pattern: preserve numeric values, add new ones carefully, update all affected frontend label maps.

## 6.2 Reference type strings (important cross-module contract)

Reference type strings are used across backend and frontend for finance linking and UI navigation, e.g.:

- `INV` (sales invoice)
- `DN` (dispatch note)
- `GRN` (goods receipt)
- `SR` (supplier return)
- `MR` (material requisition)
- `ADJ` (stock adjustment)
- `TRF` (stock transfer)

Frontend AR/AP and Payment pages contain hardcoded routing logic based on these strings. If they change, links break.

## 6.3 Document/PDF endpoint convention

The system consistently uses:

- `/api/<module>/<document>/{id}/pdf`

Frontend depends on this pattern heavily (`SecondaryLink` -> `/api/backend/.../pdf`).

## 7. How to Safely Implement Changes (Practical Guidance)

## 7.1 Adding a new field to an existing document

Typical backend changes:

1. Domain entity constructor / `Update(...)` / validation
2. EF mapping in `IssDbContext` (precision/length/index if needed)
3. Controller request + response DTOs
4. Application service create/add-line/post flow
5. PDF rendering (if shown in exported documents)
6. Excel import/template (if importable)
7. Unit/integration tests

Typical frontend changes:

1. Create form state/inputs/payload
2. Detail page DTO + display fields
3. List page DTO/columns
4. Validation and reset logic
5. Any action gating affected by the new field

## 7.2 Adding a new action/state transition

Backend:

1. Domain enum + transition method
2. Application service orchestration
3. Controller action endpoint
4. Tests (domain + integration)

Frontend:

1. Status label maps
2. Action component and endpoint path
3. Detail-page `canX`/status gating
4. List/detail copy if process description changes

## 7.3 Adding a new module/workflow

Recommended pattern (fits current architecture):

- Backend:
  - domain entity + line entity + status enum
  - EF mapping in `IssDbContext`
  - application service methods: create, add line, post/approve
  - controller with nested DTOs and `/pdf` endpoint (if document-like)
  - unit tests for invariants + integration tests for side effects
- Frontend:
  - list page (server)
  - create form (client)
  - detail page (server)
  - line form (client) if line-based
  - actions component (client) if transitions exist
  - sidebar entry

## 7.4 Changing authentication/session behavior

Frontend touchpoints:

- `frontend/src/proxy.ts`
- `frontend/src/app/api/auth/*`
- `frontend/src/lib/jwt.ts`
- `frontend/src/lib/backend.server.ts`

Backend touchpoints:

- `backend/src/ISS.Api/Program.cs`
- `backend/src/ISS.Api/Controllers/AuthController.cs`
- `backend/src/ISS.Api/Services/JwtTokenService.cs`
- Identity config in `backend/src/ISS.Infrastructure/DependencyInjection.cs`

Be careful with claim names because the frontend session decoder expects standard JWT + `ClaimTypes.*` claims.

## 8. Current Technical Risks / Debt (Change-Relevant)

## 8.1 Frontend duplication of DTOs and status labels

Symptoms:

- DTO types are redefined per page/component
- `statusLabel` maps are duplicated in many list/detail pages
- repeated validation and `parseList()` helpers across line forms

Impact:

- Backend contract changes require many small frontend edits
- Easy to miss one page and create inconsistent behavior

## 8.2 Frontend route guard checks only cookie presence

File: `frontend/src/proxy.ts`

Impact:

- expired/invalid tokens can still access the shell until API calls fail
- errors appear later instead of immediate re-auth redirect

## 8.3 Frontend JWT decode is non-verifying (display-only)

File: `frontend/src/lib/jwt.ts`

This is acceptable for display-only session metadata, but should not be used for authoritative security decisions.

## 8.4 Backend startup uses `EnsureCreated` instead of migrations

File: `backend/src/ISS.Api/Program.cs`

Impact:

- schema evolution is less controlled
- production upgrade workflows become riskier as the schema changes

## 8.5 Potential N+1 behavior in dashboard / reorder alerts

Files:

- `backend/src/ISS.Api/Controllers/ReportingController.cs`
- `backend/src/ISS.Api/Controllers/InventoryController.cs`

Observed pattern:

- iterate reorder settings and call `InventoryService.GetOnHandAsync(...)` per row

Impact:

- can become slow as reorder settings grow

## 8.6 Frontend docs lag implementation

- `frontend/README.md` is still the default Next template
- important architecture/runtime details were previously only discoverable in code

## 8.7 Default passwords in admin UI forms

Files:

- `frontend/src/app/(app)/admin/users/UserCreateForm.tsx`
- `frontend/src/app/(app)/admin/users/UserRowActions.tsx`

Notes:

- convenient for dev/demo workflows
- risky if reused in shared or production-like environments

## 9. Suggested Change Workflow

For non-trivial changes, use this sequence:

1. Start with backend domain and application-service behavior.
2. Update controller DTOs/endpoints.
3. Update frontend DTOs/forms/pages.
4. Update PDF rendering if document output changes.
5. Update Excel import/template if master-data schema changes.
6. Add or update unit and integration tests.
7. Validate end-to-end in UI and API.

## 10. Quick Reference: Important Files by Concern

### Frontend core plumbing

- `frontend/src/proxy.ts`
- `frontend/src/lib/backend.server.ts`
- `frontend/src/lib/api-client.ts`
- `frontend/src/lib/jwt.ts`
- `frontend/src/app/api/backend/[...path]/route.ts`
- `frontend/src/app/api/auth/login/route.ts`
- `frontend/src/app/api/auth/register/route.ts`
- `frontend/src/app/api/auth/logout/route.ts`
- `frontend/src/app/(app)/layout.tsx`
- `frontend/src/components/Sidebar.tsx`

### Backend composition and infrastructure

- `backend/src/ISS.Api/Program.cs`
- `backend/src/ISS.Api/Middleware/ExceptionHandlingMiddleware.cs`
- `backend/src/ISS.Application/DependencyInjection.cs`
- `backend/src/ISS.Infrastructure/DependencyInjection.cs`
- `backend/src/ISS.Infrastructure/Persistence/IssDbContext.cs`

### Backend business orchestration

- `backend/src/ISS.Application/Services/DocumentNumberService.cs`
- `backend/src/ISS.Application/Services/InventoryService.cs`
- `backend/src/ISS.Application/Services/InventoryOperationsService.cs`
- `backend/src/ISS.Application/Services/ProcurementService.cs`
- `backend/src/ISS.Application/Services/SalesService.cs`
- `backend/src/ISS.Application/Services/ServiceManagementService.cs`
- `backend/src/ISS.Application/Services/FinanceService.cs`
- `backend/src/ISS.Application/Services/NotificationService.cs`

### Backend auth / admin operations

- `backend/src/ISS.Api/Controllers/AuthController.cs`
- `backend/src/ISS.Api/Controllers/Admin/UsersController.cs`
- `backend/src/ISS.Api/Controllers/Admin/NotificationsController.cs`
- `backend/src/ISS.Api/Controllers/Admin/ImportController.cs`
- `backend/src/ISS.Api/Services/JwtTokenService.cs`
- `backend/src/ISS.Api/Services/NotificationDispatcherHostedService.cs`

### PDF and notification infrastructure

- `backend/src/ISS.Application/Abstractions/IDocumentPdfService.cs`
- `backend/src/ISS.Infrastructure/Documents/DocumentPdfService.cs`
- `backend/src/ISS.Application/Abstractions/INotificationSenders.cs`
- `backend/src/ISS.Infrastructure/Notifications/SmtpEmailSender.cs`
- `backend/src/ISS.Infrastructure/Notifications/TwilioSmsSender.cs`

### Tests

- `backend/tests/ISS.UnitTests/Domain/*.cs`
- `backend/tests/ISS.IntegrationTests/EndToEndTests.cs`
- `backend/tests/ISS.IntegrationTests/Fixtures/*`

## 11. What This Means for Your Upcoming Changes

This system is already structured well for iterative feature work:

- Frontend patterns are consistent across modules.
- Backend business rules are centralized in application services and domain entities.
- Cross-domain effects (inventory, finance, notifications) are explicit in service methods.
- There is real integration-test coverage for critical workflows.

The main practical risk for new changes is cross-layer contract drift (backend DTOs/enums/actions changing without updating duplicated frontend types, labels, and forms).

If you share the exact change you want to make next, the fastest path is to map it across:

1. backend domain/service/controller
2. frontend pages/forms/actions
3. tests (unit + integration)
4. PDF/import/config if impacted
