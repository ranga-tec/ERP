# Gap Checklist — ERP System Proposal (C‑Com Equipment)

Source document: `ERP System Proposal - C-Com Equipment.pdf` (dated October 8, 2025).

This checklist maps the proposal’s stated scope (“Core Modules” + “What’s Included”) to what exists in this repo, and tracks any gaps to close.

Note: the proposal suggests `.NET Core + Angular + MS SQL Server`. This repo implements `.NET 8 + Next.js + PostgreSQL` per the project request; the functional scope is mapped below.

## Core modules (proposal)

- [x] Master Data Management (Items, Customers, Suppliers, Brands)
- [x] Procurement Cycle (RFQ, PO, GRN, Supplier Returns)
- [x] Inventory Operations (Stock Adjustments, Transfers, Reorder Planning)
- [x] Sales & Dispatch Management (Quotations, Orders, Invoicing)
- [x] Service Job Management (Job Cards, Work Orders, QC)
- [x] Spare Parts Management (Direct sales, Material Requisitions)
- [x] Serial & Batch Tracking (traceability for equipment/parts)
- [x] Accounts Integration (AR/AP, Payments, Credit/Debit Notes)
  - [x] AR/AP entries
  - [x] Payments + allocations
  - [x] Credit notes as explicit documents + allocations
  - [x] Debit notes as explicit documents (create AR/AP charges)
- [ ] Advanced Reporting & Analytics (real-time dashboards)
  - [x] Basic dashboard KPIs
  - [ ] Additional “advanced” reports (optional; proposal is not specific)
- [x] Multi-warehouse Support (branch operations)
- [ ] Mobile-responsive Design (access anywhere)
  - [x] Responsive UI foundation (Tailwind)
  - [ ] Responsive QA pass on all modules
- [x] Role-based Security (user permissions & audit trails)
  - [x] API authorization by role + JWT auth
  - [x] Audit trail table for transactional entities
  - [x] Admin UI to manage users/roles (and/or admin endpoints)

## “What’s Included” (proposal)

- [x] Complete source code ownership
- [x] Database design & setup (EF Core model + `docker-compose.yml` for Postgres + pgAdmin)
- [x] Installation on your server (deployment/run instructions)
- [x] User manuals & documentation (module/workflow docs)
- [x] Data migration from Excel (import tooling + templates)
- [x] Barcode/QR code integration (labels + scanning UX)
- [x] Email & SMS notifications (outbox + providers + event triggers)
- [x] PDF export for all documents
- [x] Audit trail for all transactions
