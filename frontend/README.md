# ISS Frontend (Next.js)

This is the web UI for the ISS ERP system.

## Purpose

- Authenticated ERP frontend (master data, procurement, inventory, sales, service, finance, reporting, admin)
- Proxies API calls to the backend (`/api/backend/[...path]`)
- Stores auth JWT in an HTTP-only cookie (`iss_token`)

## Local Run

From the `frontend/` directory:

```powershell
copy .env.example .env.local
npm install
npm run dev
```

Open `http://localhost:3000`.

Required `.env.local` value:

- `ISS_API_BASE_URL` (defaults to `http://localhost:5257` if omitted)

## Key Frontend Architecture Files

- `src/app/(auth)/login/page.tsx` -> login/register UI
- `src/app/(app)/layout.tsx` -> authenticated app shell + sidebar/header
- `src/proxy.ts` -> route protection (redirects to login if JWT missing/expired)
- `src/app/api/backend/[...path]/route.ts` -> backend API proxy with auth forwarding
- `src/lib/backend.server.ts` -> server-component fetch helper
- `src/lib/api-client.ts` -> client-component API helpers
- `src/components/Sidebar.tsx` -> app navigation
- `src/components/DocumentCollaborationPanel.tsx` -> reusable comments/attachments UI

## Development Patterns

- Prefer server components for page-level data loading (`backendFetchJson`)
- Use client components for forms/actions (`apiPost`, `apiPostNoContent`, `apiPostForm`)
- Route all browser-side backend calls through `/api/backend/...`
- Keep UI changes aligned with backend DTO shape changes in the same checkpoint

## Build Validation

```powershell
npm run build
```

This is the primary local check for TypeScript and route compilation issues.

## System-Level Docs

See the repo root docs for full architecture and maintenance guidance:

- `../docs/system-technical-maintainer-guide.md`
- `../docs/backend-architecture.md`
- `../docs/frontend-architecture.md`
- `../docs/agent-change-playbook.md`
- `../docs/deployment.md`
- `../docs/csv-closure-audit.md`
