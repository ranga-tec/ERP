# ISS ERP Frontend Architecture and UI Integration Guide

This guide is focused on the Next.js frontend structure, auth/proxy flow, UI composition patterns, and how the frontend integrates with backend APIs.

Primary references:
- `docs/system-technical-maintainer-guide.md` (hub)
- `frontend/README.md` (frontend quick start)
- `docs/csv-closure-audit.md` (module closure status)

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
- proxy route forwards method/body and Bearer token from cookie
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
- service: equipment units, jobs, work orders, estimates, material requisitions, quality checks, handovers
- inventory: on-hand, reorder alerts, stock adjustments, stock transfers
- finance: AR/AP, payments, credit notes, debit notes
- reporting: dashboard, stock-ledger, aging, tax summary, service KPIs, costing

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
Items support `Save` and `Delete` through the dedicated edit panel.

### Costing Reporting UI

Costing UI route:

- `/reporting/costing`

This page supports:

- warehouse/item filters
- default vs weighted average cost display
- last receipt cost/date display
- on-hand and inventory valuation totals in base currency

