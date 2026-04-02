# ISS ERP Frontend Architecture and UI Integration Guide

This guide is focused on the Next.js frontend structure, auth/proxy flow, UI composition patterns, and how the frontend integrates with backend APIs.

Primary references:
- `docs/system-technical-maintainer-guide.md` (hub)
- `frontend/README.md` (frontend quick start)

## Frontend Architecture

### App Router Structure

Frontend routes are organized under `frontend/src/app` with route groups:

- `(auth)` -> login/registration flow
- `(app)` -> authenticated ERP UI (modules and detail pages)

Main module sections under `frontend/src/app/(app)`:

- `master-data`
- `procurement`
- `inventory`
- `sales`
- `service`
- `finance`
- `reporting`
- `admin`
- `audit-logs`

### Auth Flow and Route Protection

Key files:

- `frontend/src/app/api/auth/login/route.ts`
- `frontend/src/app/api/auth/register/route.ts` (same pattern)
- `frontend/src/lib/env.ts`
- `frontend/src/lib/jwt.ts`
- `frontend/src/proxy.ts`

How it works:

- Frontend auth API routes call backend `/api/auth/*`
- JWT is stored in an HTTP-only cookie (`iss_token`)
- `proxy.ts` checks presence/expiry of JWT and redirects unauthenticated users to `/login`
- Session display in the app layout decodes JWT claims client-side/server-side using helper utilities

Notes:

- `proxy.ts` is the middleware/protection entry point in this project (not `middleware.ts`)
- Expired tokens are cleared and redirected to login

### Backend API Access Patterns (Frontend)

There are two main patterns. Use the correct one depending on component type.

Server components:

- use `backendFetchJson` from `frontend/src/lib/backend.server.ts`
- requests go directly to backend API base URL
- auth token is read from cookie and forwarded as Bearer token
- best for page-level data fetching on initial render

Client components:

- use `api-client.ts` helpers (`apiGet`, `apiPost`, `apiPostNoContent`, `apiPostForm`, etc.)
- requests go through `frontend/src/app/api/backend/[...path]/route.ts`
- proxy route forwards method/body, Bearer token from cookie, and only a safe allowlist of request headers
- preserves binary/content-disposition responses for downloads

Maintainer rule:

- Do not call backend API URLs directly from browser client components
- Use `api-client.ts` so auth/cookie/proxy behavior stays consistent

### UI Composition Pattern (Common and Recommended)

Typical page structure:

- page is a server component (`page.tsx`)
- page fetches DTOs via `backendFetchJson`
- page renders read-only tables/cards and includes small client components for actions/forms

Typical action component pattern:

- client component with `useState` for busy/error
- calls `apiPost`/`apiPostNoContent`
- redirects or refreshes after success

Representative examples:

- PR list/detail pages and action forms
- `DocumentCollaborationPanel` for comments/attachments
- inventory reorder alerts page + action button

### Document Collaboration in UI

Reusable UI component:

- `frontend/src/components/DocumentCollaborationPanel.tsx`

This component handles:

- loading comments/attachments
- adding/deleting comments
- uploading/deleting attachments
- rendering image previews and file links

When enabling collaboration on a new document screen:

1. Add the component to the detail page
2. Pass the correct `referenceType` and document `id`
3. Ensure the backend reference type is accepted by normalization rules
4. Verify auth roles for the target module

### Frontend Navigation

Sidebar lives in:

- `frontend/src/components/Sidebar.tsx`

When adding/removing modules or report pages:

- update sidebar links
- ensure page route exists
- verify role access (frontend may still render link even if backend denies)

### Cross-Document Navigation

Related document references should use the shared routing helpers instead of hardcoded cross-module URLs.

- Document-to-document links should resolve through `ReferenceForms` route templates so navigation stays aligned with the master-data route map.
- Server-rendered pages should use the shared transaction link helper rather than rebuilding route resolution ad hoc.
- Item references should deep-link into `Master Data -> Items` using the shared item-routing helper so users land on the dedicated item detail screen.

Maintainer rule:

- When adding a new transactional reference to the UI, prefer the shared link helpers first.
- When introducing a new document type, add or update its `ReferenceForm` route template so cross-navigation works everywhere consistently.

### Implemented Module Coverage (UI)

Master data pages include:

- brands, items, item categories/subcategories, warehouses, suppliers, customers, reorder settings
- UoMs, UoM conversions
- taxes, tax conversions
- currencies, currency rates
- payment types, reference forms

Transactional module pages include:

- procurement: RFQ, purchase requisition, PO, GRN, direct purchase, supplier invoice, supplier return
- sales: quote, order, dispatch, direct dispatch, invoice, customer return
- service: equipment units, service contracts, jobs, estimates, expense claims, work orders, material requisitions, quality checks, handovers
- inventory: on-hand, reorder alerts, stock adjustments, stock transfers
- finance: AR/AP, payments, petty cash, credit notes, debit notes
- reporting: dashboard, stock-ledger, aging, tax summary, service KPIs, costing

### Dashboard Standard

The authenticated home route (`/`) is now treated as an operational dashboard rather than a report landing page.

- all business roles can access the dashboard route
- dashboard data is permission-aware and only returns module sections the signed-in user can act on
- the page is organized as:
  - hero KPI cards
  - attention/exception alerts
  - role-specific work-queue sections
  - quick-access links
- if the dashboard API is temporarily unavailable, the page still renders fallback quick links instead of failing blank

Maintainer rule:

- keep `/` useful for day-to-day operators
- keep analytical report pages under `/reporting/*` restricted to `Admin` and `Reporting`
- when adding a new transactional module, extend the dashboard section/quick-action model instead of adding one-off homepage widgets

### Inventory UI Behavior

Current inventory interaction pattern:

- `On Hand` uses warehouse and batch breakdown rows instead of a single merged balance
- draft stock transaction pages reuse the shared stock visibility explorer and inline live stock widget
- stock adjustment lines are entered as `counted quantity`; the UI shows current system quantity and expected variance
- stock transfer lines are entered as `move quantity`; the source warehouse stock widget stays visible while editing
- stock ledger renders signed movement quantities plus batch and serial detail for history review

### Line Grid Action Standard (Draft Documents)

The detail pages for line-based documents use a common behavior:

- show `Add line` card only while the document is draft/editable
- show an `Actions` column in the line grid while draft
- each line row supports `Edit`, `Save/Cancel`, and `Delete`

This is now applied consistently across all line-document detail pages (not just PO/GRN).

### Master Data Grid Action Standard

Master-data list pages now follow the same maintainability pattern:

- list table includes an `Actions` column
- each row supports `Edit`, `Save/Cancel`, and `Delete`
- errors are shown inline per row
- destructive actions use explicit confirmation prompts

This standard is now implemented across brands, customers, suppliers, warehouses, UoMs, UoM conversions, taxes, tax conversions, currencies, currency rates, payment types, reference forms, item categories/subcategories, and reorder settings.
Items now use a separate list/search page plus dedicated create, view, and edit pages. The item list grid exposes `View`, `Edit`, `Delete`, and item label links.

### Costing Reporting UI

Costing UI route:

- `/reporting/costing`

This page supports:

- warehouse/item filters
- default vs weighted average cost display
- last receipt cost/date display
- on-hand and inventory valuation totals in base currency

### Service And Finance Workflow UI

Recent workflow-specific UI behavior:

- the home dashboard now follows an industry-standard ERP pattern with exception alerts, operational queues, and role-aware quick links instead of a static four-card summary
- `/api/reporting/dashboard` is available to all business roles because it backs the authenticated home page, while the detailed reporting endpoints remain restricted to `Admin` and `Reporting`
- procurement list pages now expose explicit `View` / `Edit` actions for purchase requisitions, RFQs, purchase orders, goods receipts, direct purchases, supplier invoices, and supplier returns
- procurement list-page `Edit` remains available while the document is still `Draft`
- sales list pages now expose explicit `View` / `Edit` actions for quotes, orders, dispatches, direct dispatches, invoices, and customer returns
- sales list-page `Edit` remains available while the document is still `Draft` (and follows existing invoice permissions)
- equipment-unit detail pages support warranty ownership updates and list linked service contracts
- service-contract list/detail pages manage `AMC`, `SLA`, and warranty-extension coverage for installed units
- service-job create/detail screens surface entitlement source, coverage, billing treatment, and manual refresh when coverage changes
- service list pages now expose explicit `View` / `Edit` actions for contracts, equipment units, jobs, estimates, and expense claims
- service estimates support `Part`, `Labor`, and `Expense` line kinds, customer-approval state visibility, resend behavior on edited drafts, and explicit `Create Change Order` actions after approval/rejection
- service expense claim detail pages let service/finance users submit, approve/reject, settle, and convert billable claim lines into the working estimate
- service work-order detail pages support labor-entry capture plus submit/approve/reject actions, and show effective billable labor after entitlement coverage
- service handover invoice conversion supports fallback item mapping for expense lines and lets users choose estimate labor vs approved timesheet labor
- service job detail pages render a costing summary with estimate, invoice, material, direct-purchase, labor, and expense-claim breakdowns, including entitlement-adjusted labor billing visibility
- finance chart-of-accounts pages now support account creation/edit/delete, parent account structure, and posting/group flags
- finance petty cash pages provide fund creation, editing, top-up, adjustment, and settlement-ledger visibility
- audit logs now render structured field diffs with filters for technical rows and system-maintained fields instead of only dumping raw JSON

