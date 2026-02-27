# ISS ERP System: Technical Maintainer Guide

## Purpose

This document is the navigation hub for future agents and developers working on the ISS ERP system.

It provides the high-level system context and points to the detailed guides for backend, frontend, and change workflows.

Use this hub first, then jump into the specialized docs linked below.

## Current Project Status (Practical Summary)

- Major ERP/service modules are implemented across backend + frontend.
- Production-hardening work is active (auth, reverse-proxy safety, retries, migration discipline, test coverage).
- Line-level editing/deleting is now implemented across all draft document detail grids.
- Master-data list maintenance now includes row-level edit/delete actions and backend delete APIs.
- Master data now includes UoM conversions, payment types, taxes/tax conversions, currencies/rates, and reference forms.
- Reporting now includes costing in addition to dashboard, stock-ledger, aging, tax summary, and service KPIs.
- The system is close to full CSV closure but still requires final UAT/responsive verification and workflow-depth validation.
- Use `docs/csv-closure-audit.md` as the working status baseline for closure planning.

## Stack and Runtime Topology

- Backend: ASP.NET Core (.NET 8), EF Core, PostgreSQL, ASP.NET Identity, JWT auth
- Frontend: Next.js App Router, TypeScript, Tailwind CSS
- Database: PostgreSQL (`docker-compose.yml` local default maps host `5433 -> container 5432`)
- Docs/file attachments: local filesystem storage under API `App_Data`
- Notifications: outbox-based email/SMS dispatch (SMTP/Twilio adapters, null senders when not configured)

Typical local runtime:

- Frontend (`localhost:3000`) -> Next.js proxy route -> Backend API (`localhost:5257`) -> PostgreSQL (`localhost:5433`)

## Implemented Functional Coverage Snapshot

- Master data:
  - items, brands, categories/subcategories, warehouses, suppliers, customers, reorder settings
  - UoMs, UoM conversions
  - taxes, tax conversions
  - currencies, currency rates
  - payment types, reference forms
  - row-level edit/delete actions on all master-data maintenance grids (items via edit panel)
- Procurement:
  - RFQ, purchase requisition, purchase order, goods receipt, direct purchase, supplier invoice, supplier return
- Sales:
  - quote, order, dispatch, direct dispatch, invoice, customer return
- Service:
  - equipment units, jobs, work orders, estimates, material requisitions, quality checks, handovers
- Finance:
  - AR/AP, payments + allocations, credit notes, debit notes
- Reporting:
  - dashboard, stock ledger, aging, tax summary, service KPIs, costing

## Repository Map

Repo root:

- `backend/`
- `frontend/`
- `docs/`
- `scripts/`
- `.github/workflows/ci.yml`
- `docker-compose.yml`
- `ISS.slnx`

Backend source projects (`backend/src`):

- `ISS.Api` -> HTTP API, auth, controllers, middleware, health checks, hosted services
- `ISS.Application` -> application services, interfaces, options, validators
- `ISS.Domain` -> domain entities, enums, invariants, business rules
- `ISS.Infrastructure` -> EF Core persistence, Identity integration, PDF/docs, notifications adapters

Backend tests (`backend/tests`):

- `ISS.UnitTests` -> domain/application unit tests
- `ISS.IntegrationTests` -> end-to-end API tests via in-process test host + PostgreSQL

Frontend (`frontend/src`):

- `app/` -> App Router routes (`(auth)` and `(app)` route groups)
- `components/` -> shared UI/components (including collaboration panel)
- `lib/` -> backend proxy fetch helpers, auth/session helpers, environment helpers
- `proxy.ts` -> route protection middleware/proxy logic (Next.js middleware entry)

Ops scripts:

- `scripts/ops/smoke-api.ps1` -> health/auth/basic API smoke checks (CI and post-deploy use)

## Detailed Guides

- `docs/backend-architecture.md`
  - backend layering, startup, auth/roles, persistence, audit, notifications
  - testing strategy, CI jobs, migrations, local DB operations, smoke script
- `docs/frontend-architecture.md`
  - App Router structure, auth cookie/proxy flow, API access patterns, UI composition
  - document collaboration UI integration and navigation conventions
- `docs/agent-change-playbook.md`
  - how to add/modify/remove features safely
  - reporting/attachment/change playbooks, closure planning, troubleshooting, onboarding

## Core Supporting Docs

- `README.md` -> quick local setup/run/test commands
- `frontend/README.md` -> frontend-specific quick start and integration entry points
- `docs/deployment.md` -> deployment/migrations/env vars/smoke checks/backup-restore runbook
- `docs/csv-closure-audit.md` -> requirements traceability and remaining gaps
- `docs/user-manual.md` -> functional walkthroughs

## Suggested Usage (Future Agent)

1. Read `README.md` for local setup and run commands.
2. Read this hub doc for system scope and documentation map.
3. Read `docs/csv-closure-audit.md` to understand what is still partial/missing.
4. Read the relevant detailed guide(s) for the area you will change.
5. Implement one focused checkpoint with code + tests + docs updates together.

