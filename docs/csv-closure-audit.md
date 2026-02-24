# CSV Closure Audit (Baseline)

Status snapshot after commit `f90b4c1` (production-hardening pass: JWT startup guard + smoke script).

This document is a traceability baseline against `docs/inventory list (1).csv`.
It is intended to guide closure work and UAT, not to replace row-level manual verification.

## Method

- Evidence sources:
  - Backend API controllers in `backend/src/ISS.Api/Controllers`
  - Frontend routes/pages in `frontend/src/app/(app)`
  - Integration tests in `backend/tests/ISS.IntegrationTests/EndToEndTests.cs`
  - Unit tests in `backend/tests/ISS.UnitTests`
  - Deployment/ops docs and CI workflow
- Status definitions:
  - `Done`: core workflow is implemented end-to-end and aligned enough to the CSV intent
  - `Partial`: core exists but field-level coverage, workflow depth, automation, or UX polish is incomplete
  - `Missing`: no clear implementation found

## High-Level Summary

- Major modules from the CSV are implemented across backend + frontend.
- Current work is mostly production hardening and completeness polish.
- Remaining gaps are concentrated in:
  - field-level master-data richness
  - advanced reporting
  - some workflow variants (multi-stage/approval/tolerance flows)
  - ops integrations (accounting export, webhooks/templates polish)
  - responsive/UAT verification

## Traceability Matrix (CSV -> Implementation)

| CSV item | Status | Evidence | Remaining gap / note |
| --- | --- | --- | --- |
| 1.1 Item (Product/Part) Master | Partial | `ItemsController`, item pages, barcode lookup + label PDF, item attachments, price history endpoint | Many CSV fields are not modeled yet (model/variant, dimensions/specs, warranty months/expiry, selling price/MRP/default discount/tax, preferred supplier, min stock on item itself); duplicate merge tool missing; code auto-generation not confirmed |
| 1.2 Unit (UoM) | Partial | `UnitOfMeasuresController`, `frontend/src/app/(app)/master-data/uoms` | Conversion rules are not implemented; CSV explicitly expects UoM conversions |
| 1.3 Brand | Partial | `BrandsController`, brand page | CSV optional fields (`country`, `website`) and logo upload are not implemented |
| 1.4 Category / Sub-Category | Partial | `ItemCategoriesController`, `ItemSubcategoriesController`, item category UI pages | Reorder categories action not evidenced |
| 1.5 Supplier | Partial | `SuppliersController`, supplier page, procurement + finance usage | CSV advanced supplier profile fields missing (contact person, tax id, payment terms, currency, lead time, rating/notes); attachment/contract support not shown on supplier master |
| 1.6 Customer | Partial | `CustomersController`, customer page, sales/service/finance usage | CSV advanced customer profile fields missing (tax id, credit limit, price tier, warranty/AMC tag); ledger shortcut is indirect via finance pages |
| 2.1 Purchase Requisition | Partial | `PurchaseRequisitionsController`, PR pages, `convert-to-po` endpoint | CSV richer workflow (department/requester/approver/reject, convert to RFQ) is only partially represented |
| 2.2 RFQ | Partial | `RfqsController`, RFQ pages, send action | Multi-supplier RFQ comparison/award flow is not clearly implemented (CSV expects quote comparison + award) |
| 2.3 Purchase Order (PO) | Partial | `PurchaseOrdersController`, PO pages, approve action, PDF export support, comments/attachments panel on detail | CSV extras like budget checks and richer payment/tax mode fields are not clearly implemented; receipt tracking depth may be basic |
| 2.4 Goods Received Note (GRN) | Partial | `GoodsReceiptsController`, GRN pages, integration tests for stock/AP impact | Over-receipt tolerance and explicit damaged/rejected handling workflow depth need confirmation; CSV is richer than core GRN flow |
| 2.5 Direct Purchase | Done | `DirectPurchasesController`, direct purchase pages, integration tests incl. duplicate-post guard | CSV intent appears covered; optional "create supplier invoice" flow exists via supplier invoice linkage |
| 2.6 Supplier Invoice (AP Bill) | Partial | `SupplierInvoicesController`, supplier invoice pages, extensive failure-path integration tests | Core AP bill + posting implemented; CSV "3-way match" exists in guard behavior but likely not fully surfaced as a dedicated reconciliation UX/report |
| 2.7 Supplier Return (Purchase Return) | Partial | `SupplierReturnsController`, supplier return pages, integration test + AP adjustment behavior | CSV says supplier debit note; current implementation uses supplier credit note/AP adjustment semantics (functionally close but terminology differs) |
| 3.1 Stock Adjustment | Partial | `StockAdjustmentsController`, stock adjustment pages, integration tests | Core post/void + audit trail exist; approval workflow for large variances is not implemented |
| 3.2 Stock Transfer (Inter-warehouse) | Partial | `StockTransfersController`, transfer pages, integration tests | CSV expects issue/in-transit/receive multi-stage flow; current implementation appears simpler post/void transfer |
| 3.3 Reorder Planning | Partial | reorder settings + reorder alerts page/API + dashboard KPI + reorder-alerts -> purchase requisition draft automation (API + UI button) | Reorder suggestions now generate PR drafts for a selected warehouse, but direct PO generation, supplier suggestion/assignment, and richer planning heuristics remain |
| 4.1 Sales Quotation | Partial | `QuotesController`, quote pages, send action, PDF export | CSV convert-to-SO/invoice workflow is not clearly implemented |
| 4.2 Sales Order (SO) | Partial | `OrdersController`, sales order pages, confirm action | Stock reservation and explicit conversion actions are not clearly implemented |
| 4.3 Dispatch / Delivery Challan | Partial | `DispatchesController`, dispatch pages, integration tests, PDF export | CSV conversion to sales invoice and transport/e-waybill details may be partial |
| 4.4 Direct Dispatch | Partial | `DirectDispatchesController`, direct dispatch pages, integration tests incl. duplicate-post guard | Core flow exists; invoice-later UX/explicit conversion workflow needs verification/polish |
| 4.5 Sales Invoice | Done | `InvoicesController`, invoice pages, integration tests for AR impact, PDF export, notifications | Core CSV intent is implemented end-to-end |
| 4.6 Customer Return (Sales Return) | Partial | `CustomerReturnsController`, customer return pages, integration tests incl. duplicate-post guard | Core stock-in + AR credit path exists; quarantine option and service-job repair linkage are not clearly implemented |
| 5.1 Service Job / Ticket (Job Card) | Partial | `ServiceJobsController`, service job pages, equipment units, document collaboration attachments/comments | Core service job flow exists; CSV fields/workflow depth (walk-in path, SLA timers, richer device metadata, video attachments, status automation) is broader |
| 5.2 Estimate / Quotation (Service) | Done | `ServiceEstimatesController`, estimate pages, approve/send actions, integration tests (including notifications) | Core estimate flow and send/approve behavior are covered |
| 5.3 Spare Parts Requisition | Partial | `MaterialRequisitionsController`, MR pages, integration test (stock consumption) | Core issue/post flow exists; CSV reserve-vs-trigger-PO workflow depth is broader |
| 5.4 Service Work Order | Partial | `WorkOrdersController`, WO pages, integration test + collaboration | Core WO creation exists; CSV pause/resume, labor time capture depth, and richer task/test tracking appear partial |
| 5.5 Quality Check (QC) | Partial | `QualityChecksController`, QC pages, integration test + collaboration | Core QC record exists; configurable checklist and reopen automation depth need confirmation |
| 5.6 Handover / Delivery (Service) | Partial | `ServiceHandoversController`, handover pages, convert-to-sales-invoice endpoint, integration tests | Core handover + invoice conversion exists; customer signature capture and pickup-ready messaging UX may be partial |
| 6.1 Receipts (AR) | Partial | Unified `PaymentsController` + finance payments pages + AR/AP pages; integration test shows payment allocation marking AR paid | Implemented under unified payments model, not a dedicated receipts module; AR receipt-specific UX/reporting may need polish |
| 6.2 Payments (AP) | Partial | `PaymentsController`, AP page, payment allocation UI | Core module exists; explicit AP-focused regression coverage is lighter than AR path |
| 6.3 Credit/Debit Notes | Done | `CreditNotesController`, `DebitNotesController`, finance note pages, allocations, integration + domain coverage | Core CSV intent implemented (explicit documents + allocations) |
| 7) Reporting & Analytics | Partial | `ReportingController` dashboard + stock ledger + AR/AP aging + tax summary + service KPI endpoints; frontend reporting pages (`/reporting`, `/reporting/stock-ledger`, `/reporting/aging`, `/reporting/tax-summary`, `/reporting/service-kpis`); integration coverage in `EndToEndTests` | First-wave operational reports are now implemented, but valuation, supplier performance, profitability/margin, and deeper analytics/export variants remain |
| 8) Security, Audit & Integrations | Partial | JWT auth, role-based authorization, audit logs, notifications outbox + SMTP/Twilio, barcode/QR support, REST API | Role granularity differs from CSV examples; barcode scanner UX is limited; accounting export (Tally/QuickBooks/CSV) not implemented; webhook/template management not clearly implemented |
| 9) Document Relationships (Flow Summary) | Partial | End-to-end modules across procurement/sales/service + integration tests for key chains and conversions | Major flows exist, but not every optional branch/relationship in the CSV has explicit traceability tests or UI affordances |
| Notes: "All documents support status/PDF/attachments/comments/activity logs" | Partial | Broad PDF support, audit logs, document collaboration panel rolled out to many detail pages; attachment upload safety now includes allowlists/quotas/signature checks | "All documents" claim still needs strict page-by-page verification and UI consistency review |

## Evidence Highlights

- Broad backend module coverage exists under `backend/src/ISS.Api/Controllers`:
  - `Procurement`, `Sales`, `Service`, `Finance`, `Inventory`, `Admin`, `Documents`
- Broad frontend module coverage exists under `frontend/src/app/(app)`:
  - master data, procurement, inventory, sales, service, finance, admin, audit
- Integration tests (`EndToEndTests.cs`) cover core flows and many guard/failure paths, including:
  - procurement posting/validation paths
  - sales dispatch/invoice/returns
  - service estimate/handover conversions
  - document collaboration comments/attachments
  - `/health` endpoint
- Production-hardening additions already in place:
  - DB-backed health check
  - startup JWT signing-key validation for non-Development
  - CI migrations/health smoke job using `scripts/ops/smoke-api.ps1`

## Highest-Value Remaining CSV Closure Work

- Advanced reporting pack (at least a minimal first wave):
  - stock ledger
  - AR/AP aging
  - tax/VAT summary
  - service KPI summary
- Master-data enrichment (incremental, not all at once):
  - supplier/customer finance terms fields
  - brand metadata/logo
  - UoM conversion rules
- Workflow depth upgrades where CSV expects more than a single post action:
  - stock transfer receive-stage flow
  - RFQ compare/award
  - reorder -> PR/PO suggestions
- Attachment hardening (production risk area already identified):
  - MIME allow-listing / content sniff checks
  - quota limits
  - scanning hook / quarantine path
  - stronger test coverage
- Final closure verification:
  - row-by-row manual UAT pass against `docs/inventory list (1).csv`
  - responsive QA across all screens

## Notes on "Done" vs "CSV-exact"

This project intentionally implements the proposal/CSV scope using:

- `.NET 8 + ASP.NET Core + PostgreSQL` (instead of the proposal's example stack)
- `Next.js` frontend (instead of Angular)

Some CSV rows are functionally satisfied with different naming or consolidation, for example:

- AR receipts and AP payments are handled through a unified payments module
- supplier return accounting impact may be represented with supplier credit note/AP adjustment semantics
