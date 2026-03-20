# Assistant Progress and GRN Handover

## Purpose

This document captures the current state of the AI assistant work and the GRN partial-receipt checkpoint so a later session can continue without rebuilding context from code.

This checkpoint is still local workspace state, not a fully documented or fully committed feature branch.

## Intended Product Goal

The target behavior is an in-app assistant that can drive ERP transactions through guided chat instead of acting as a disconnected Q&A bot.

Expected pattern:

- user opens the assistant from the main app shell
- user asks to start a transaction such as GRN, stock transfer, or purchase order
- assistant opens the actual transaction screen
- assistant gathers missing mandatory fields step by step
- assistant writes to the real draft document in the background as the user answers
- assistant supports revising already entered values, skipping lines, pausing, resuming, saving drafts, and posting when ready
- assistant should eventually cover all inventory transactions and also help open or preview reports

The immediate business priority before broad assistant rollout is the GRN partial-receipt flow:

- selecting a PO should load all PO lines into the GRN receipt grid
- user enters only the quantities actually received now
- skipped lines remain open
- partially received PO lines remain open for later GRNs
- posting the first GRN should leave the PO `PartiallyReceived`
- later GRNs should be able to receive the balance and eventually close the PO

## Current Workspace Snapshot

As of this checkpoint, the workspace contains two related bodies of work:

1. GRN partial-receipt support across backend, frontend, and integration tests
2. a separate AI assistant module with tool-like guided workflows

Important workspace note:

- much of the assistant module still appears as local untracked files in `git status`
- the GRN receipt-plan work is also still part of the local checkpoint
- this means the code exists and builds locally, but the checkpoint should not be treated as a clean committed milestone yet

Relevant local paths currently carrying assistant work:

- `backend/src/ISS.Api/Assistant/`
- `backend/src/ISS.Api/Controllers/AssistantController.cs`
- `backend/src/ISS.Api/Controllers/AssistantSettingsController.cs`
- `backend/src/ISS.Domain/Assistant/`
- `backend/src/ISS.Infrastructure/Persistence/Migrations/20260316164901_AddAssistantSettingsModule.cs`
- `frontend/src/components/assistant/`
- `frontend/src/app/(app)/settings/AssistantSettingsCard.tsx`

Relevant GRN partial-receipt paths:

- `backend/src/ISS.Api/Controllers/Procurement/GoodsReceiptsController.cs`
- `backend/src/ISS.Application/Services/ProcurementService.cs`
- `backend/src/ISS.Domain/Procurement/GoodsReceipt.cs`
- `backend/src/ISS.Domain/Procurement/PurchaseOrder.cs`
- `backend/src/ISS.Infrastructure/Persistence/Migrations/20260319104500_AddGoodsReceiptPurchaseOrderLineLink.cs`
- `frontend/src/app/(app)/procurement/goods-receipts/GoodsReceiptCreateForm.tsx`
- `frontend/src/app/(app)/procurement/goods-receipts/GoodsReceiptReceiptPlanForm.tsx`
- `frontend/src/app/(app)/procurement/goods-receipts/[id]/page.tsx`
- `backend/tests/ISS.IntegrationTests/EndToEndTests.cs`

## Assistant Architecture Implemented So Far

### Backend wiring

The API startup already registers the assistant module in `backend/src/ISS.Api/Program.cs`:

- `AssistantSessionStore` as a singleton
- `AssistantProviderGateway` via `HttpClient`
- `AssistantSettingsService`
- `AssistantCoordinator`

The assistant is exposed through:

- `POST /api/assistant/chat`
- `GET/PUT /api/assistant/settings/...` endpoints for access policy, user preference, provider profiles, connection tests, and model discovery

### Session model

Assistant conversations are stored in memory inside `AssistantSessionStore`.

Current implication:

- sessions survive across chat messages while the same API process is alive
- sessions do not survive API restarts
- sessions are not durable across multiple backend instances

This is acceptable for a local checkpoint, but it is not production-grade conversation persistence yet.

### Settings and provider model

The assistant settings module is persisted in the database through:

- `AssistantAccessPolicy`
- `AssistantProviderProfile`
- `AssistantUserPreference`

The current migration for this is:

- `20260316164901_AddAssistantSettingsModule`

Supported provider types in the settings UI/backend:

- OpenAI
- Anthropic
- Ollama
- OpenAI-compatible

Provider keys are stored encrypted through ASP.NET Core data protection in `AssistantSettingsService`.

### Frontend wiring

The assistant panel is mounted globally in the main app shell:

- `frontend/src/components/AppShell.tsx`

The settings page includes assistant configuration:

- `frontend/src/app/(app)/settings/SettingsPanel.tsx`
- `frontend/src/app/(app)/settings/AssistantSettingsCard.tsx`

This means the assistant is already positioned as a first-class in-app module, not a one-off dev tool.

## Assistant Scope Already Implemented

### 1. Purchase order drafting

The assistant can already guide a purchase-order draft flow:

- start a PO flow
- resolve supplier
- add lines
- revise the current line
- save/approve through the guided state machine

The purchase-order workflow is represented in `AssistantCoordinator.cs` and surfaced to the UI through `purchaseOrderDraft`.

### 2. GRN guided drafting

The assistant can already guide a GRN draft from a PO:

- ask for a receivable PO
- load the remaining PO lines into the guided workflow
- ask for a warehouse
- create the real GRN draft
- walk line by line asking received quantity
- handle `skip`
- capture batch or serial values when applicable
- allow line revisions such as `change line 2 qty 5`
- save as draft
- request confirmation before posting
- post the real GRN and refresh the current page

This is the most complete assistant transaction flow in the current checkpoint.

### 3. Stock transfer guided drafting

Backend support for a guided stock-transfer flow now exists in the split coordinator files:

- source warehouse
- destination warehouse
- item
- quantity
- batch/serial capture
- line revisions
- posting confirmation

This was previously blocked by a compile failure caused by duplicated stock-transfer code across `AssistantCoordinator.cs` and `AssistantCoordinator.StockTransfers.cs`.

That blocker has been fixed in the local workspace.

### 4. Report previews

The assistant can resolve and preview:

- dashboard
- stock ledger
- aging
- costing

The assistant builds report requests and the frontend renders a small preview card with summary values and limited rows.

## Assistant Gaps Still Open

The assistant module is real and partially functional, but it is not complete for the stated end goal yet.

Known gaps:

- only a subset of transactions is covered today; this is not yet "all inventory functions"
- the frontend assistant panel still does not render `stockTransferDraft`, even though the backend response already includes it
- the assistant panel default copy is outdated and still describes a smaller scope than the backend now supports
- there is only targeted assistant integration coverage for the GRN flow; the newer stock-transfer assistant path does not yet have equivalent end-to-end coverage
- sessions are in-memory only
- the assistant is not yet documented anywhere else in repo docs in a way that future sessions can safely rely on

## GRN Partial-Receipt Work Already Implemented

### Backend behavior

The GRN model now supports PO-line linkage through `PurchaseOrderLineId` on GRN lines.

The backend also exposes a dedicated receipt-plan API:

- `GET /api/procurement/goods-receipts/{id}/receipt-plan`
- `PUT /api/procurement/goods-receipts/{id}/receipt-plan`

`ProcurementService.ReplaceGoodsReceiptReceiptPlanAsync(...)` now:

- replaces the current GRN draft lines from a PO-based receipt plan
- validates that a PO line appears only once in the request
- prevents quantities above the unreceived balance
- accounts for quantities reserved in other draft GRNs for the same PO

When a GRN is posted:

- inventory receipt movements are created
- PO line `ReceivedQuantity` values are updated
- the PO becomes `PartiallyReceived` if some lines remain open
- the PO becomes `Closed` once all lines are fully received

### Frontend behavior

The GRN detail page now loads the receipt plan and shows it as a grid:

- all PO lines are shown
- already posted quantity is displayed
- quantity reserved in other draft GRNs is displayed
- remaining available quantity is displayed
- user enters only what is received on this GRN
- blank or `0` keeps a line open for a later GRN

This is implemented in:

- `frontend/src/app/(app)/procurement/goods-receipts/[id]/page.tsx`
- `frontend/src/app/(app)/procurement/goods-receipts/GoodsReceiptReceiptPlanForm.tsx`

The GRN create screen currently lets the user choose a receivable PO and a warehouse, then opens the draft GRN detail screen.

### Tests already present

Integration coverage currently includes:

- `Procurement_GRN_Can_Post_Partial_Receipts_Against_The_Same_PO`
- `Assistant_GRN_Workflow_Can_Save_Draft_Then_Post_A_Partial_Receipt`

These tests validate the core partial-receipt path and the assistant-driven GRN path.

## Validation Completed In This Checkpoint

The following validations were completed successfully in the local environment:

- `dotnet build backend/src/ISS.Api/ISS.Api.csproj -c Release --nologo`
- `dotnet test backend/tests/ISS.UnitTests/ISS.UnitTests.csproj -c Release --nologo`
- `npm run build` inside `frontend/`

The following validation could not be completed in the current shell:

- integration tests requiring Docker/Testcontainers or a reachable PostgreSQL test database

Reason:

- Docker daemon was unavailable
- local PostgreSQL on `localhost:5433` was not reachable in this environment

## GRN Edge-Case Gaps To Address Before or During Live Validation

These are the main issues still visible from code review.

### 1. Tracking requirements are enforced too late

Severity: high

Files:

- `backend/src/ISS.Application/Services/ProcurementService.cs`
- `backend/src/ISS.Application/Services/InventoryService.cs`
- `frontend/src/app/(app)/procurement/goods-receipts/GoodsReceiptReceiptPlanForm.tsx`
- `backend/src/ISS.Api/Assistant/AssistantCoordinator.cs`

Issue:

- `ReplaceGoodsReceiptReceiptPlanAsync(...)` allows saving a receipt plan with quantity on batch-tracked or serial-tracked items even when batch/serial data is missing
- the hard validation only happens later in `InventoryService.RecordReceiptAsync(...)` during posting

Practical effect:

- users can build a draft GRN that looks valid
- posting may fail at the end because batch or serial requirements were not enforced at plan-entry time
- the assistant currently even tells users they can `skip` batch/serial capture for now, which is misleading for tracked items

Recommended next fix:

- validate tracking requirements when saving the GRN receipt plan, not only when posting
- align assistant prompts and frontend form messaging with the actual rule

### 2. Assistant GRN plan loading does not account for other draft reservations

Severity: medium

Files:

- `backend/src/ISS.Api/Assistant/AssistantCoordinator.cs`
- `backend/src/ISS.Application/Services/ProcurementService.cs`

Issue:

- `LoadGoodsReceiptPlanLinesAsync(...)` loads `remainingQuantity = ordered - received`
- it does not subtract quantities already reserved in other draft GRNs
- the actual backend save path does subtract those reservations

Practical effect:

- the assistant can present a larger available quantity than the receipt-plan service will finally allow
- the user may only see the conflict when the assistant tries to save the plan

Recommended next fix:

- make assistant line loading use the same reservation logic as the GRN receipt-plan API

### 3. The GRN create flow can still open unnecessary extra draft GRNs

Severity: medium

Files:

- `backend/src/ISS.Application/Services/ProcurementService.cs`
- `frontend/src/app/(app)/procurement/goods-receipts/GoodsReceiptCreateForm.tsx`

Issue:

- GRN creation is allowed for any PO in `Approved` or `PartiallyReceived` status as long as posted receipts do not fully close the PO
- it does not consider quantities already reserved in other draft GRNs

Practical effect:

- users can create another draft GRN even when all remaining quantity is already fully reserved in existing draft GRNs
- the new GRN may open with no truly available quantity to receive

Recommended next fix:

- optionally block new GRN creation when no unreserved quantity remains
- or at minimum warn the user before opening a draft with zero available lines

### 4. Assistant frontend is behind the backend on stock transfer progress

Severity: medium

Files:

- `backend/src/ISS.Api/Assistant/AssistantModels.cs`
- `frontend/src/components/assistant/AssistantPanel.tsx`

Issue:

- backend responses now include `stockTransferDraft`
- the frontend panel still only handles PO, GRN, and report preview cards

Practical effect:

- the stock-transfer assistant backend exists, but the user does not get the same visual draft feedback in the assistant panel

Recommended next fix:

- add stock-transfer draft rendering to the assistant panel and update the default summary text

### 5. Small UI polish issue on the GRN create form

Severity: low

File:

- `frontend/src/app/(app)/procurement/goods-receipts/GoodsReceiptCreateForm.tsx`

Issue:

- warehouse labels render with mojibake text `â€”` instead of a normal separator

Practical effect:

- cosmetic only, but it should be cleaned up during the next GRN polish pass

## Suggested Next Work Order

1. Run live GRN validation once Docker or PostgreSQL is available.
2. Fix the early tracking validation gap so batch/serial mistakes fail during plan entry, not only at post time.
3. Align the assistant GRN loader with draft-reservation logic.
4. Finish the stock-transfer assistant frontend card and add equivalent integration coverage.
5. Continue assistant rollout transaction by transaction only after the GRN flow is stable end to end.

## Live Validation Checklist

When the environment is ready, validate this exact sequence:

1. Create an approved PO with multiple lines.
2. Create the first GRN from that PO.
3. Confirm all PO lines load into the receipt grid.
4. Receive one line fully, one line partially, and leave one line blank if available.
5. Save the receipt plan.
6. Post the GRN.
7. Confirm PO status becomes `PartiallyReceived`.
8. Create a second GRN for the same PO.
9. Confirm only the remaining balance is available.
10. Post the second GRN and confirm the PO closes.
11. Repeat at least one scenario with a batch-tracked item.
12. Repeat at least one scenario with a serial-tracked item.
13. Run the same workflow once through the assistant.

## Resume Notes For The Next Session

If the next session starts cold, read these in order:

1. `README.md`
2. `docs/system-technical-maintainer-guide.md`
3. this file: `docs/assistant-progress.md`
4. `backend/src/ISS.Api/Assistant/AssistantCoordinator.cs`
5. `backend/src/ISS.Application/Services/ProcurementService.cs`
6. `frontend/src/app/(app)/procurement/goods-receipts/GoodsReceiptReceiptPlanForm.tsx`
7. `backend/tests/ISS.IntegrationTests/EndToEndTests.cs`

If the next task is assistant-focused, do not assume the UI and backend are at the same completion level. Check both before making changes.
