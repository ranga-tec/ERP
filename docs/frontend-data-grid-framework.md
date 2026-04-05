# Frontend Data Grid Framework

This document describes the reusable editable grid module used for transaction-line editing in the ISS frontend.

Primary module path:

- `frontend/src/components/data-grid`

## Purpose

The data-grid module provides a reusable editing surface for dense ERP-style tables without coupling the UI layer to any single business document.

The module owns:

- table rendering and table semantics
- typed column definitions
- cell editor rendering
- searchable lookup dropdown behavior
- `Tab` cell flow through editable controls
- `Enter` row submit support
- optional footer rows for totals and summaries

The document screen still owns:

- DTO shape and mapping
- validation rules
- permission checks
- save/delete APIs
- document-specific toolbars and warnings

This keeps the grid portable enough to reuse in other ISS areas or copy into another React/Tailwind application.

## Current entrypoints

- `frontend/src/components/data-grid/index.ts`
- `frontend/src/components/data-grid/EditableDataTable.tsx`
- `frontend/src/components/data-grid/LookupCell.tsx`
- `frontend/src/components/data-grid/formatters.ts`
- `frontend/src/components/data-grid/types.ts`

Compatibility shim:

- `frontend/src/components/EditableDataTable.tsx`

The shim currently re-exports the new module so older imports do not break during rollout.

## Supported column kinds

- `display`
- `text`
- `number`
- `money`
- `percent`
- `date`
- `datetime`
- `textarea`
- `select`
- `lookup`

`lookup` is the searchable dropdown cell type intended for cases such as:

- UoM selection
- item selection
- warehouse selection
- tax-code selection
- reference document pickers

## Keyboard behavior

Current standard behavior:

- `Tab` moves between the editable inputs in the natural row order
- `Enter` saves the active row for row-edit screens
- `Textarea` keeps multiline entry by default; use `Ctrl+Enter` / `Cmd+Enter` to submit if the screen wires row submit
- bulk-edit screens such as the GRN receipt plan keep explicit document-level save

## Current live usage

The framework is currently applied to:

- `Procurement -> Goods Receipts` receipt-plan grid
- `Procurement -> Purchase Orders` line editor
- `Sales -> Invoices` line editor
- `Sales -> Quotes` line editor
- `Sales -> Orders` line editor
- `Procurement -> RFQs` line editor
- `Procurement -> Purchase Requisitions` line editor
- `Sales -> Dispatches` line editor
- `Sales -> Direct Dispatches` line editor
- `Sales -> Customer Returns` line editor
- `Procurement -> Supplier Returns` line editor

Notable behaviors in the current rollout:

- GRN draft mode now uses a single primary `Receive From PO` working grid instead of a second duplicate draft-line table
- PO and invoice line tables keep explicit row-level `Save` / `Cancel` / `Delete`
- quotes, sales orders, RFQs, and purchase requisitions now follow the same explicit-save row editing pattern
- dispatches, direct dispatches, customer returns, and supplier returns now use the same pattern, including multiline serial capture and stock insight below the active row
- line totals and visible totals are rendered through shared grid formatting and footer support

## Recommended rollout order

Best next modules for the same pattern:

- direct purchases
- stock transfers
- stock adjustments
- service estimates
- service expense claims
- material requisitions

## Integration pattern

Use the grid as a shell and keep business logic outside it.

1. Map backend DTOs into edit-safe row state.
2. Define typed columns in the document screen.
3. Keep validation in the document screen or service.
4. Save explicitly per row or per document, depending on the transaction.
5. Use `lookup` only when the backend really supports changing that field.

Do not move document workflow rules into the reusable grid module.
