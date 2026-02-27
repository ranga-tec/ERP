# Gap Checklist - ERP System Proposal (C-Com Equipment)

Source document: `ERP System Proposal - C-Com Equipment.pdf` (October 8, 2025).

This checklist maps proposal scope to the implemented repository scope.

Note:
- Proposal stack mentions `.NET + Angular + SQL Server`.
- This project implementation is `.NET 8 + Next.js + PostgreSQL`.
- Functional scope is mapped below.

## Core modules (proposal)

- [x] Master data management
  - [x] items, customers, suppliers, brands
  - [x] UoM and UoM conversions
  - [x] taxes and tax conversions
  - [x] currencies and exchange rates
  - [x] payment types and reference forms
- [x] Procurement cycle
  - [x] RFQ
  - [x] purchase requisition
  - [x] purchase order
  - [x] GRN
  - [x] direct purchase
  - [x] supplier invoice
  - [x] supplier returns
- [x] Inventory operations
  - [x] stock adjustments
  - [x] stock transfers
  - [x] reorder planning
- [x] Sales and dispatch management
  - [x] quotations
  - [x] orders
  - [x] dispatches
  - [x] direct dispatches
  - [x] invoicing
  - [x] customer returns
- [x] Service job management
  - [x] jobs
  - [x] work orders
  - [x] quality checks
  - [x] handovers
  - [x] service estimates
- [x] Spare parts management
  - [x] material requisitions
  - [x] direct issue flows via dispatch/direct dispatch
- [x] Serial and batch tracking
- [x] Accounts integration
  - [x] AR/AP
  - [x] payments + allocations
  - [x] credit/debit notes
- [x] Reporting and analytics
  - [x] dashboard KPIs
  - [x] stock ledger
  - [x] AR/AP aging
  - [x] tax summary
  - [x] service KPIs
  - [x] costing report
- [x] Multi-warehouse support
- [ ] Mobile-responsive design
  - [x] responsive foundation exists
  - [ ] full responsive QA across every module/screen
- [x] Role-based security and audit trails

## Included items (proposal)

- [x] Complete source code ownership
- [x] Database design and setup
- [x] Installation/deployment documentation
- [x] User and technical documentation
- [x] Data import from Excel
- [x] Barcode and QR support
- [x] Email and SMS notification framework
- [x] PDF export for documents
- [x] Audit trail for transactions

## Workflow depth and usability closure notes

- [x] Draft line grids now support full row actions (`Edit`, `Save/Cancel`, `Delete`) across all line-based document modules.
- [x] Master-data maintenance grids now support row actions (`Edit`, `Save/Cancel`, `Delete`) with backend delete compatibility.
- [ ] Final end-user acceptance and regression walkthrough on all responsive breakpoints.
