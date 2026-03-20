using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ISS.Api.Security;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Common;
using ISS.Domain.Inventory;
using ISS.Domain.MasterData;
using ISS.Domain.Procurement;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Assistant;

public sealed partial class AssistantCoordinator(
    AssistantSessionStore sessionStore,
    AssistantProviderGateway providerGateway,
    IIssDbContext dbContext,
    ProcurementService procurementService,
    InventoryOperationsService inventoryOperationsService,
    ILogger<AssistantCoordinator> logger)
{
    private const int MaxLookupOptions = 5;

    internal async Task<AssistantChatResponse> HandleAsync(
        AssistantActor actor,
        AssistantChatRequest request,
        CancellationToken cancellationToken)
    {
        var session = sessionStore.GetOrCreate(actor.UserId, request.SessionId);
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            var userMessage = (request.Message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return await BuildResponseAsync(
                    session,
                    AddAssistantReply(session, "Tell me what you want to do. Phase 1 supports purchase-order drafting, GRN drafting, stock transfer drafting, and report lookups."),
                    reportRequest: null,
                    navigateTo: null,
                    refreshCurrentPage: false,
                    cancellationToken: cancellationToken);
            }

            session.Transcript.Add(new AssistantTranscriptMessage("user", userMessage, DateTimeOffset.UtcNow));

            AssistantOutcome outcome;
            try
            {
                outcome = await ProcessAsync(session, actor, userMessage, request.Provider, cancellationToken);
            }
            catch (Exception ex) when (ex is DomainValidationException or NotFoundException)
            {
                logger.LogInformation(ex, "Assistant business operation failed.");
                outcome = new AssistantOutcome(ex.Message);
            }

            var reply = AddAssistantReply(session, outcome.Reply);
            return await BuildResponseAsync(
                session,
                reply,
                outcome.ReportRequest,
                outcome.NavigateTo,
                outcome.RefreshCurrentPage,
                cancellationToken);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private async Task<AssistantOutcome> ProcessAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;
        var goodsReceiptWorkflow = session.GoodsReceipt;
        var stockTransferWorkflow = session.StockTransfer;
        if (workflow.HasDraft)
        {
            await SyncPurchaseOrderWorkflowAsync(workflow, cancellationToken);
        }

        if (goodsReceiptWorkflow.HasDraft)
        {
            await SyncGoodsReceiptWorkflowAsync(goodsReceiptWorkflow, cancellationToken);
        }

        if (stockTransferWorkflow.HasDraft)
        {
            await SyncStockTransferWorkflowAsync(stockTransferWorkflow, cancellationToken);
        }

        if (goodsReceiptWorkflow.AwaitingPostConfirmation)
        {
            return await HandleGoodsReceiptPostConfirmationAsync(session, actor, message, cancellationToken);
        }

        if (stockTransferWorkflow.AwaitingPostConfirmation)
        {
            return await HandleStockTransferPostConfirmationAsync(session, actor, message, cancellationToken);
        }

        if (goodsReceiptWorkflow.IsActive)
        {
            return await HandleGoodsReceiptWorkflowAsync(session, actor, message, provider, cancellationToken);
        }

        if (stockTransferWorkflow.IsActive)
        {
            return await HandleStockTransferWorkflowAsync(session, actor, message, provider, cancellationToken);
        }

        if (goodsReceiptWorkflow.HasDraft)
        {
            var pausedGoodsReceiptOutcome = await TryHandlePausedGoodsReceiptAsync(session, actor, message, provider, cancellationToken);
            if (pausedGoodsReceiptOutcome is not null)
            {
                return pausedGoodsReceiptOutcome;
            }
        }

        if (stockTransferWorkflow.HasDraft)
        {
            var pausedStockTransferOutcome = await TryHandlePausedStockTransferAsync(session, actor, message, provider, cancellationToken);
            if (pausedStockTransferOutcome is not null)
            {
                return pausedStockTransferOutcome;
            }
        }

        if (workflow.AwaitingApprovalConfirmation)
        {
            return await HandleApprovalConfirmationAsync(session, message, cancellationToken);
        }

        if (workflow.HasDraft && LooksLikeNewPurchaseOrderCommand(message))
        {
            if (!HasAnyRole(actor, Roles.Admin, Roles.Procurement))
            {
                return new AssistantOutcome("You do not have Procurement access, so I cannot create or edit purchase orders for you.");
            }

            workflow.ResetConversation();
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingRequisitionChoice;
            return new AssistantOutcome(
                "Starting a separate purchase order. Do you want to create it from an approved purchase requisition? Reply with the requisition number, say `yes`, or say `no requisition` to start from scratch.");
        }

        if (workflow.HasDraft)
        {
            var approvalOutcome = TryStartPurchaseOrderApproval(workflow, message);
            if (approvalOutcome is not null)
            {
                return approvalOutcome;
            }

            if (!workflow.CanEditDraft && workflow.Stage != AssistantPurchaseOrderStage.Idle)
            {
                workflow.Stage = AssistantPurchaseOrderStage.Paused;
                workflow.ResetCurrentLine();
            }
        }

        if (workflow.IsActive)
        {
            return await HandlePurchaseOrderWorkflowAsync(session, actor, message, provider, cancellationToken);
        }

        if (workflow.HasDraft)
        {
            var pausedOutcome = await TryHandlePausedPurchaseOrderAsync(session, actor, message, provider, cancellationToken);
            if (pausedOutcome is not null)
            {
                return pausedOutcome;
            }
        }

        var intent = await ResolveIdleIntentAsync(message, provider, cancellationToken);
        if (intent == "start_purchase_order")
        {
            if (!HasAnyRole(actor, Roles.Admin, Roles.Procurement))
            {
                return new AssistantOutcome("You do not have Procurement access, so I cannot create or edit purchase orders for you.");
            }

            goodsReceiptWorkflow.ResetConversation();
            stockTransferWorkflow.ResetConversation();
            workflow.ResetConversation();
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingRequisitionChoice;
            return new AssistantOutcome(
                "Do you want to create this purchase order from an approved purchase requisition? Reply with the requisition number, say `yes`, or say `no requisition` to start from scratch.");
        }

        if (intent == "start_goods_receipt")
        {
            if (!HasAnyRole(actor, Roles.Admin, Roles.Procurement, Roles.Inventory))
            {
                return new AssistantOutcome("You do not have inventory or procurement access, so I cannot create or edit goods receipts for you.");
            }

            workflow.ResetConversation();
            stockTransferWorkflow.ResetConversation();
            goodsReceiptWorkflow.ResetConversation();
            goodsReceiptWorkflow.Stage = AssistantGoodsReceiptStage.AwaitingPurchaseOrder;
            return new AssistantOutcome("Send me the purchase order number for the GRN.");
        }

        if (intent == "start_stock_transfer")
        {
            if (!HasAnyRole(actor, Roles.Admin, Roles.Inventory))
            {
                return new AssistantOutcome("You do not have Inventory access, so I cannot create or edit stock transfers for you.");
            }

            workflow.ResetConversation();
            goodsReceiptWorkflow.ResetConversation();
            stockTransferWorkflow.ResetConversation();
            stockTransferWorkflow.Stage = AssistantStockTransferStage.AwaitingFromWarehouse;
            return new AssistantOutcome("Tell me the source warehouse for the stock transfer.");
        }

        var reportRequest = await ResolveReportRequestAsync(intent, actor, message, provider, cancellationToken);
        if (reportRequest is not null)
        {
            return new AssistantOutcome(
                BuildReportReply(reportRequest),
                ReportRequest: reportRequest);
        }

        if (workflow.HasDraft && LooksLikeResumeCommand(message))
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingItem;
            return new AssistantOutcome(
                $"Resuming PO {workflow.PurchaseOrderNumber}. Tell me the next item, or say `finish` to stop the guided flow.",
                NavigateTo: PurchaseOrderPath(workflow.PurchaseOrderId!.Value));
        }

        return new AssistantOutcome(
            "I can currently help you create a purchase order draft, create a GRN from a PO one step at a time, create a stock transfer draft, and show dashboard, stock ledger, aging, or costing report previews.");
    }

    private async Task<AssistantOutcome?> TryHandlePausedPurchaseOrderAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;
        if (workflow.Stage != AssistantPurchaseOrderStage.Paused && workflow.Stage != AssistantPurchaseOrderStage.Idle)
        {
            return null;
        }

        if (!HasAnyRole(actor, Roles.Admin, Roles.Procurement))
        {
            return null;
        }

        if (!workflow.CanEditDraft &&
            (LooksLikeResumeCommand(message) ||
             TryParseLastLineQuantityUpdate(message, out _) ||
             TryParseLastLinePriceUpdate(message, out _) ||
             LooksLikeRemoveLastLineCommand(message) ||
             LooksLikeApprovePurchaseOrderCommand(message)))
        {
            return BuildReadOnlyPurchaseOrderOutcome(workflow);
        }

        if (TryParseLastLineQuantityUpdate(message, out var quantity))
        {
            return await UpdateLastLineQuantityAsync(session, quantity, cancellationToken);
        }

        if (TryParseLastLinePriceUpdate(message, out var unitPrice))
        {
            return await UpdateLastLinePriceAsync(session, unitPrice, cancellationToken);
        }

        if (LooksLikeRemoveLastLineCommand(message))
        {
            return await RemoveLastLineAsync(session, cancellationToken);
        }

        var idleIntent = await ResolveIdleIntentAsync(message, provider, cancellationToken);
        if (idleIntent == "start_purchase_order" || LooksLikeResumeCommand(message))
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingItem;
            return new AssistantOutcome(
                $"PO {workflow.PurchaseOrderNumber} is still open. Tell me the next item to add, or say `new po` if you want a separate draft.",
                NavigateTo: PurchaseOrderPath(workflow.PurchaseOrderId!.Value));
        }

        return null;
    }

    private async Task<AssistantOutcome> HandlePurchaseOrderWorkflowAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;

        if (!HasAnyRole(actor, Roles.Admin, Roles.Procurement))
        {
            workflow.ResetConversation();
            return new AssistantOutcome("You no longer have Procurement access in this session, so I stopped the guided purchase-order flow.");
        }

        if (!workflow.CanEditDraft)
        {
            workflow.Stage = AssistantPurchaseOrderStage.Paused;
            workflow.ResetCurrentLine();
            return BuildReadOnlyPurchaseOrderOutcome(workflow);
        }

        if (LooksLikeCancelCommand(message))
        {
            workflow.Stage = workflow.HasDraft ? AssistantPurchaseOrderStage.Paused : AssistantPurchaseOrderStage.Idle;
            workflow.ResetCurrentLine();
            return workflow.HasDraft
                ? new AssistantOutcome($"I stopped prompting. Draft PO {workflow.PurchaseOrderNumber} stays open. Say `resume PO` when you want to continue.")
                : new AssistantOutcome("I cancelled the current purchase-order flow.");
        }

        if (TryParseLastLineQuantityUpdate(message, out var lastLineQuantity))
        {
            return await UpdateLastLineQuantityAsync(session, lastLineQuantity, cancellationToken);
        }

        if (TryParseLastLinePriceUpdate(message, out var lastLinePrice))
        {
            return await UpdateLastLinePriceAsync(session, lastLinePrice, cancellationToken);
        }

        if (LooksLikeRemoveLastLineCommand(message))
        {
            return await RemoveLastLineAsync(session, cancellationToken);
        }

        return workflow.Stage switch
        {
            AssistantPurchaseOrderStage.AwaitingRequisitionChoice => await HandleRequisitionChoiceAsync(workflow, message, provider, cancellationToken),
            AssistantPurchaseOrderStage.AwaitingRequisitionNumber => await HandleRequisitionNumberAsync(workflow, message, provider, cancellationToken),
            AssistantPurchaseOrderStage.AwaitingSupplier => await HandleSupplierAsync(workflow, message, provider, cancellationToken),
            AssistantPurchaseOrderStage.AwaitingItem => await HandleItemAsync(workflow, message, provider, cancellationToken),
            AssistantPurchaseOrderStage.AwaitingQuantity => await HandleQuantityAsync(workflow, message, provider, cancellationToken),
            AssistantPurchaseOrderStage.AwaitingUnitPrice => await HandleUnitPriceAsync(workflow, message, provider, cancellationToken),
            AssistantPurchaseOrderStage.AwaitingLineConfirmation => await HandleLineConfirmationAsync(session, message, provider, cancellationToken),
            AssistantPurchaseOrderStage.Paused => await HandlePausedWorkflowAsync(workflow, message, provider, cancellationToken),
            _ => new AssistantOutcome("Tell me what you want to do next."),
        };
    }

    private async Task SyncPurchaseOrderWorkflowAsync(
        AssistantPurchaseOrderWorkflow workflow,
        CancellationToken cancellationToken)
    {
        if (workflow.PurchaseOrderId is null)
        {
            return;
        }

        var purchaseOrder = await dbContext.PurchaseOrders.AsNoTracking()
            .Where(x => x.Id == workflow.PurchaseOrderId.Value)
            .Select(x => new { x.Number, x.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (purchaseOrder is null)
        {
            workflow.ResetConversation();
            return;
        }

        workflow.PurchaseOrderNumber = purchaseOrder.Number;
        workflow.CurrentStatus = purchaseOrder.Status;
    }

    private AssistantOutcome? TryStartPurchaseOrderApproval(
        AssistantPurchaseOrderWorkflow workflow,
        string message)
    {
        if (!LooksLikeApprovePurchaseOrderCommand(message) || workflow.PurchaseOrderId is null)
        {
            return null;
        }

        if (!workflow.CanEditDraft)
        {
            return BuildReadOnlyPurchaseOrderOutcome(workflow);
        }

        if (workflow.CurrentLine.ItemId.HasValue ||
            workflow.CurrentLine.Quantity.HasValue ||
            workflow.CurrentLine.UnitPrice.HasValue)
        {
            return new AssistantOutcome(
                $"There is still a pending line for PO {workflow.PurchaseOrderNumber}. Confirm it or discard it before approving the PO.",
                NavigateTo: PurchaseOrderPath(workflow.PurchaseOrderId.Value));
        }

        workflow.AwaitingApprovalConfirmation = true;
        workflow.Stage = AssistantPurchaseOrderStage.Paused;
        return new AssistantOutcome(
            $"Approve PO {workflow.PurchaseOrderNumber}? This will lock line editing. Reply `confirm` to approve it or `cancel` to keep it as a draft.",
            NavigateTo: PurchaseOrderPath(workflow.PurchaseOrderId.Value));
    }

    private async Task<AssistantOutcome> HandleApprovalConfirmationAsync(
        AssistantSession session,
        string message,
        CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;
        var normalized = Normalize(message);

        if (LooksLikeConfirmCommand(normalized))
        {
            await procurementService.ApprovePurchaseOrderAsync(workflow.PurchaseOrderId!.Value, cancellationToken);
            workflow.AwaitingApprovalConfirmation = false;
            workflow.CurrentStatus = PurchaseOrderStatus.Approved;
            workflow.Stage = AssistantPurchaseOrderStage.Paused;
            workflow.ResetCurrentLine();
            return new AssistantOutcome(
                $"PO {workflow.PurchaseOrderNumber} is now approved. I opened the document again in read-only mode.",
                NavigateTo: PurchaseOrderPath(workflow.PurchaseOrderId.Value));
        }

        if (LooksLikeRejectCommand(normalized) || LooksLikeCancelCommand(message))
        {
            workflow.AwaitingApprovalConfirmation = false;
            return new AssistantOutcome($"Keeping PO {workflow.PurchaseOrderNumber} as a draft.");
        }

        return new AssistantOutcome(
            $"Approve PO {workflow.PurchaseOrderNumber}? Reply `confirm` to approve it or `cancel` to keep it as a draft.");
    }

    private async Task<AssistantOutcome> HandleRequisitionChoiceAsync(
        AssistantPurchaseOrderWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(message);
        if (LooksLikeNoRequisition(normalized))
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingSupplier;
            return new AssistantOutcome("Starting a fresh PO. Tell me the supplier code or supplier name.");
        }

        var llm = await InterpretAsync("po-requisition-choice", message, provider, cancellationToken);
        var referenceNumber = llm?.ReferenceNumber;
        var intent = llm?.Intent;
        if (!string.IsNullOrWhiteSpace(referenceNumber) || LooksLikeReferenceNumber(message))
        {
            return await ResolvePurchaseRequisitionAsync(workflow, referenceNumber ?? message, cancellationToken);
        }

        if (LooksLikeYesCommand(normalized) || intent == "use_requisition")
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingRequisitionNumber;
            return new AssistantOutcome("Send me the approved purchase requisition number.");
        }

        return new AssistantOutcome("Reply with an approved requisition number, say `yes`, or say `no requisition` to start a PO from scratch.");
    }

    private async Task<AssistantOutcome> HandleRequisitionNumberAsync(
        AssistantPurchaseOrderWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var llm = await InterpretAsync("po-requisition-number", message, provider, cancellationToken);
        var referenceNumber = llm?.ReferenceNumber ?? message;
        return await ResolvePurchaseRequisitionAsync(workflow, referenceNumber, cancellationToken);
    }

    private async Task<AssistantOutcome> ResolvePurchaseRequisitionAsync(
        AssistantPurchaseOrderWorkflow workflow,
        string candidate,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(candidate);
        var requisition = await dbContext.PurchaseRequisitions.AsNoTracking()
            .Where(x => x.Number.ToLower().Contains(normalized))
            .OrderByDescending(x => x.RequestDate)
            .Select(x => new { x.Id, x.Number, x.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (requisition is null)
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingRequisitionNumber;
            return new AssistantOutcome("I could not find that purchase requisition. Send the requisition number again or say `no requisition`.");
        }

        if (requisition.Status != PurchaseRequisitionStatus.Approved)
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingRequisitionNumber;
            return new AssistantOutcome($"I found requisition {requisition.Number}, but it is {requisition.Status}. Only approved requisitions can be converted to a PO.");
        }

        workflow.PurchaseRequisitionId = requisition.Id;
        workflow.PurchaseRequisitionNumber = requisition.Number;
        workflow.CreatedFromRequisition = true;
        workflow.Stage = AssistantPurchaseOrderStage.AwaitingSupplier;
        return new AssistantOutcome($"I found requisition {requisition.Number}. Now tell me the supplier for the purchase order.");
    }

    private async Task<AssistantOutcome> HandleSupplierAsync(
        AssistantPurchaseOrderWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        if (workflow.CandidateSuppliers.Count > 0 &&
            TryResolveOptionSelection(message, workflow.CandidateSuppliers, out var selectedSupplier))
        {
            workflow.CandidateSuppliers.Clear();
            return await CommitSupplierAsync(workflow, selectedSupplier, cancellationToken);
        }

        var llm = await InterpretAsync("po-supplier", message, provider, cancellationToken);
        var supplierQuery = llm?.SupplierText ?? message;
        var matches = await FindSupplierMatchesAsync(supplierQuery, cancellationToken);
        if (matches.Count == 0)
        {
            return new AssistantOutcome("I could not match that supplier. Send the supplier code or a clearer part of the supplier name.");
        }

        if (matches.Count == 1)
        {
            return await CommitSupplierAsync(workflow, matches[0], cancellationToken);
        }

        workflow.CandidateSuppliers.Clear();
        workflow.CandidateSuppliers.AddRange(matches);
        return new AssistantOutcome(
            $"I found multiple suppliers. Reply with the option number or supplier code:{Environment.NewLine}{FormatOptions(matches)}");
    }

    private async Task<AssistantOutcome> CommitSupplierAsync(
        AssistantPurchaseOrderWorkflow workflow,
        AssistantLookupOption supplier,
        CancellationToken cancellationToken)
    {
        workflow.SupplierId = supplier.Id;
        workflow.SupplierCode = supplier.Code;
        workflow.SupplierName = supplier.Label;

        if (workflow.PurchaseOrderId is null)
        {
            var poId = workflow.PurchaseRequisitionId.HasValue
                ? await procurementService.CreatePurchaseOrderFromPurchaseRequisitionAsync(workflow.PurchaseRequisitionId.Value, supplier.Id, cancellationToken)
                : await procurementService.CreatePurchaseOrderAsync(supplier.Id, cancellationToken);

            workflow.PurchaseOrderId = poId;
            workflow.PurchaseOrderNumber = await dbContext.PurchaseOrders.AsNoTracking()
                .Where(x => x.Id == poId)
                .Select(x => x.Number)
                .FirstAsync(cancellationToken);
            workflow.CurrentStatus = PurchaseOrderStatus.Draft;
        }

        if (workflow.PurchaseRequisitionId.HasValue)
        {
            workflow.Stage = AssistantPurchaseOrderStage.Paused;
            return new AssistantOutcome(
                $"PO {workflow.PurchaseOrderNumber} was created from requisition {workflow.PurchaseRequisitionNumber} for supplier {supplier.DisplayText}. I opened the draft. Say `resume PO` if you want me to add more lines.",
                NavigateTo: PurchaseOrderPath(workflow.PurchaseOrderId!.Value));
        }

        workflow.Stage = AssistantPurchaseOrderStage.AwaitingItem;
        return new AssistantOutcome(
            $"Draft PO {workflow.PurchaseOrderNumber} is ready for supplier {supplier.DisplayText}. I opened the draft. Tell me the first item.",
            NavigateTo: PurchaseOrderPath(workflow.PurchaseOrderId!.Value));
    }

    private async Task<AssistantOutcome> HandleItemAsync(
        AssistantPurchaseOrderWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(message);
        if (LooksLikeFinishCommand(normalized))
        {
            workflow.Stage = AssistantPurchaseOrderStage.Paused;
            workflow.ResetCurrentLine();
            return new AssistantOutcome($"I stopped the guided flow. Draft PO {workflow.PurchaseOrderNumber} stays open. Say `resume PO` when you want more lines.");
        }

        if (workflow.CandidateItems.Count > 0 &&
            TryResolveOptionSelection(message, workflow.CandidateItems, out var selectedItem))
        {
            workflow.CandidateItems.Clear();
            SplitCodeAndName(selectedItem, workflow.CurrentLine);
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingQuantity;
            return new AssistantOutcome($"Selected {selectedItem.DisplayText}. What quantity do you want?");
        }

        var llm = await InterpretAsync("po-item", message, provider, cancellationToken);
        var itemQuery = llm?.ItemText ?? message;
        var matches = await FindItemMatchesAsync(itemQuery, cancellationToken);
        if (matches.Count == 0)
        {
            return new AssistantOutcome("I could not match that item. Send the item SKU, barcode, or a clearer part of the item name.");
        }

        if (matches.Count == 1)
        {
            SplitCodeAndName(matches[0], workflow.CurrentLine);
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingQuantity;
            return new AssistantOutcome($"Selected {matches[0].DisplayText}. What quantity do you want?");
        }

        workflow.CandidateItems.Clear();
        workflow.CandidateItems.AddRange(matches);
        return new AssistantOutcome(
            $"I found multiple items. Reply with the option number or item code:{Environment.NewLine}{FormatOptions(matches)}");
    }

    private async Task<AssistantOutcome> HandleQuantityAsync(
        AssistantPurchaseOrderWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var quantity = TryExtractDecimal(message);
        if (quantity is null)
        {
            var llm = await InterpretAsync("po-quantity", message, provider, cancellationToken);
            quantity = llm?.Quantity;
        }

        if (quantity is null || quantity <= 0)
        {
            return new AssistantOutcome("Quantity is mandatory and must be greater than 0. Send the quantity for this line.");
        }

        workflow.CurrentLine.Quantity = quantity.Value;
        workflow.Stage = AssistantPurchaseOrderStage.AwaitingUnitPrice;
        return new AssistantOutcome($"Quantity set to {quantity.Value}. What unit price should I use?");
    }

    private async Task<AssistantOutcome> HandleUnitPriceAsync(
        AssistantPurchaseOrderWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var unitPrice = TryExtractDecimal(message);
        if (unitPrice is null)
        {
            var llm = await InterpretAsync("po-unit-price", message, provider, cancellationToken);
            unitPrice = llm?.UnitPrice;
        }

        if (unitPrice is null || unitPrice < 0)
        {
            return new AssistantOutcome("Unit price is mandatory and must be 0 or greater. Send the unit price for this line.");
        }

        workflow.CurrentLine.UnitPrice = unitPrice.Value;
        workflow.Stage = AssistantPurchaseOrderStage.AwaitingLineConfirmation;
        var previewTotal = (workflow.CurrentLine.Quantity ?? 0) * unitPrice.Value;
        return new AssistantOutcome(
            $"Line ready:{Environment.NewLine}- Item: {workflow.CurrentLine.DisplayLabel}{Environment.NewLine}- Qty: {workflow.CurrentLine.Quantity}{Environment.NewLine}- Unit price: {unitPrice.Value}{Environment.NewLine}- Line total: {previewTotal}{Environment.NewLine}Reply `confirm` to add it, or tell me what to change.");
    }

    private async Task<AssistantOutcome> HandleLineConfirmationAsync(
        AssistantSession session,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;
        var normalized = Normalize(message);
        var llm = await InterpretAsync("po-line-confirmation", message, provider, cancellationToken);

        if (LooksLikeConfirmCommand(normalized) || llm?.Intent == "confirm")
        {
            var lineDraft = workflow.CurrentLine;
            await procurementService.AddPurchaseOrderLineAsync(
                workflow.PurchaseOrderId!.Value,
                lineDraft.ItemId!.Value,
                lineDraft.Quantity!.Value,
                lineDraft.UnitPrice!.Value,
                cancellationToken);

            var latestLine = await ResolveLatestAddedLineAsync(session, cancellationToken);
            if (latestLine is not null)
            {
                workflow.AddedLines.Add(latestLine);
            }

            var addedLabel = lineDraft.DisplayLabel;
            workflow.ResetCurrentLine();
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingItem;
            return new AssistantOutcome(
                $"Added {addedLabel} to PO {workflow.PurchaseOrderNumber}. Tell me the next item, say `finish`, or ask me to change/remove the last line.",
                RefreshCurrentPage: true);
        }

        if (LooksLikeRejectCommand(normalized))
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingItem;
            workflow.ResetCurrentLine();
            return new AssistantOutcome("I discarded that pending line. Tell me the item you want instead.");
        }

        if (TryParseCurrentLineQuantityUpdate(message, out var quantity))
        {
            workflow.CurrentLine.Quantity = quantity;
            var previewTotal = quantity * (workflow.CurrentLine.UnitPrice ?? 0);
            return new AssistantOutcome(
                $"Updated quantity to {quantity}. Current line total is {previewTotal}. Reply `confirm` to add it, or tell me another change.");
        }

        if (TryParseCurrentLinePriceUpdate(message, out var unitPrice))
        {
            workflow.CurrentLine.UnitPrice = unitPrice;
            var previewTotal = (workflow.CurrentLine.Quantity ?? 0) * unitPrice;
            return new AssistantOutcome(
                $"Updated unit price to {unitPrice}. Current line total is {previewTotal}. Reply `confirm` to add it, or tell me another change.");
        }

        if (llm?.ItemText is { Length: > 0 })
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingItem;
            workflow.ResetCurrentLine();
            return await HandleItemAsync(workflow, llm.ItemText, provider, cancellationToken);
        }

        return new AssistantOutcome("Reply `confirm` to add this line, or say something like `qty 5`, `price 12.50`, or `change item`.");
    }

    private async Task<AssistantOutcome> HandlePausedWorkflowAsync(
        AssistantPurchaseOrderWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        if (!workflow.CanEditDraft)
        {
            return BuildReadOnlyPurchaseOrderOutcome(workflow);
        }

        if (LooksLikeResumeCommand(message))
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingItem;
            return new AssistantOutcome($"Resuming PO {workflow.PurchaseOrderNumber}. Tell me the next item.");
        }

        var llm = await InterpretAsync("po-paused", message, provider, cancellationToken);
        if (llm?.Intent == "select_item")
        {
            workflow.Stage = AssistantPurchaseOrderStage.AwaitingItem;
            return await HandleItemAsync(workflow, llm.ItemText ?? message, provider, cancellationToken);
        }

        if (LooksLikeFinishCommand(Normalize(message)))
        {
            return new AssistantOutcome($"Draft PO {workflow.PurchaseOrderNumber} is ready. Open it any time and say `resume PO` if you want me to continue guiding line entry.");
        }

        return new AssistantOutcome($"Draft PO {workflow.PurchaseOrderNumber} is paused. Say `resume PO`, `add line`, or ask to change/remove the last line.");
    }

    private async Task<AssistantOutcome> UpdateLastLineQuantityAsync(
        AssistantSession session,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;
        var lastLine = workflow.AddedLines.LastOrDefault();
        if (lastLine is null)
        {
            return new AssistantOutcome("There is no assistant-added line to change yet.");
        }

        await procurementService.UpdatePurchaseOrderLineAsync(
            workflow.PurchaseOrderId!.Value,
            lastLine.LineId,
            quantity,
            lastLine.UnitPrice,
            cancellationToken);

        workflow.AddedLines[^1] = lastLine with { Quantity = quantity };
        workflow.Stage = workflow.Stage == AssistantPurchaseOrderStage.Paused
            ? AssistantPurchaseOrderStage.Paused
            : AssistantPurchaseOrderStage.AwaitingItem;

        return new AssistantOutcome(
            $"Updated the last line quantity to {quantity}.",
            RefreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> UpdateLastLinePriceAsync(
        AssistantSession session,
        decimal unitPrice,
        CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;
        var lastLine = workflow.AddedLines.LastOrDefault();
        if (lastLine is null)
        {
            return new AssistantOutcome("There is no assistant-added line to change yet.");
        }

        await procurementService.UpdatePurchaseOrderLineAsync(
            workflow.PurchaseOrderId!.Value,
            lastLine.LineId,
            lastLine.Quantity,
            unitPrice,
            cancellationToken);

        workflow.AddedLines[^1] = lastLine with { UnitPrice = unitPrice };
        workflow.Stage = workflow.Stage == AssistantPurchaseOrderStage.Paused
            ? AssistantPurchaseOrderStage.Paused
            : AssistantPurchaseOrderStage.AwaitingItem;

        return new AssistantOutcome(
            $"Updated the last line unit price to {unitPrice}.",
            RefreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> RemoveLastLineAsync(AssistantSession session, CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;
        var lastLine = workflow.AddedLines.LastOrDefault();
        if (lastLine is null)
        {
            return new AssistantOutcome("There is no assistant-added line to remove yet.");
        }

        await procurementService.RemovePurchaseOrderLineAsync(
            workflow.PurchaseOrderId!.Value,
            lastLine.LineId,
            cancellationToken);

        workflow.AddedLines.RemoveAt(workflow.AddedLines.Count - 1);
        workflow.Stage = workflow.Stage == AssistantPurchaseOrderStage.Paused
            ? AssistantPurchaseOrderStage.Paused
            : AssistantPurchaseOrderStage.AwaitingItem;

        return new AssistantOutcome(
            $"Removed the last line: {lastLine.ItemCode} - {lastLine.ItemName}.",
            RefreshCurrentPage: true);
    }

    private async Task<AssistantPurchaseOrderLineSnapshot?> ResolveLatestAddedLineAsync(
        AssistantSession session,
        CancellationToken cancellationToken)
    {
        var workflow = session.PurchaseOrder;
        var lineDraft = workflow.CurrentLine;
        var knownIds = workflow.AddedLines.Select(x => x.LineId).ToHashSet();

        var lines = await dbContext.PurchaseOrders.AsNoTracking()
            .Where(x => x.Id == workflow.PurchaseOrderId)
            .SelectMany(x => x.Lines)
            .Select(x => new { x.Id, x.ItemId, x.OrderedQuantity, x.UnitPrice })
            .ToListAsync(cancellationToken);

        var latest = lines
            .Where(x => !knownIds.Contains(x.Id)
                        && x.ItemId == lineDraft.ItemId
                        && x.OrderedQuantity == lineDraft.Quantity
                        && x.UnitPrice == lineDraft.UnitPrice)
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        return latest is null
            ? null
            : new AssistantPurchaseOrderLineSnapshot(
                latest.Id,
                latest.ItemId,
                lineDraft.ItemCode ?? string.Empty,
                lineDraft.ItemName ?? string.Empty,
                latest.OrderedQuantity,
                latest.UnitPrice);
    }

    private async Task<AssistantOutcome?> TryHandlePausedGoodsReceiptAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.GoodsReceipt;
        if (workflow.Stage != AssistantGoodsReceiptStage.Paused && workflow.Stage != AssistantGoodsReceiptStage.Idle)
        {
            return null;
        }

        if (!HasAnyRole(actor, Roles.Admin, Roles.Procurement, Roles.Inventory))
        {
            return null;
        }

        if (!workflow.CanEditDraft &&
            (LooksLikeResumeCommand(message) ||
             TryParseGoodsReceiptLineQuantityChange(message, out _, out _) ||
             TryParseGoodsReceiptLineBatchChange(message, out _, out _) ||
             TryParseGoodsReceiptLineSerialChange(message, out _, out _) ||
             LooksLikeShowLinesCommand(message) ||
             LooksLikePostGoodsReceiptCommand(message)))
        {
            return BuildReadOnlyGoodsReceiptOutcome(workflow);
        }

        var revisionOutcome = await TryHandleGoodsReceiptRevisionAsync(session, message, cancellationToken);
        if (revisionOutcome is not null)
        {
            return revisionOutcome;
        }

        if (LooksLikeShowLinesCommand(message))
        {
            return new AssistantOutcome(BuildGoodsReceiptPlanOverview(workflow), NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        if (workflow.HasDraft && LooksLikePostGoodsReceiptCommand(message))
        {
            if (workflow.Lines.All(x => !x.HasPlannedReceipt))
            {
                return new AssistantOutcome(
                    $"GRN {workflow.GoodsReceiptNumber} does not have any received lines yet. Enter at least one received quantity before posting it.",
                    NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
            }

            workflow.AwaitingPostConfirmation = true;
            return new AssistantOutcome(
                BuildGoodsReceiptPostConfirmationReply(workflow),
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        if (LooksLikeResumeCommand(message))
        {
            var nextIndex = workflow.Lines.FindIndex(x => !x.IsAnswered);
            if (nextIndex >= 0)
            {
                workflow.CurrentLineIndex = nextIndex;
                workflow.Stage = AssistantGoodsReceiptStage.AwaitingQuantity;
                return new AssistantOutcome(
                    BuildGoodsReceiptCurrentLinePrompt(workflow.CurrentLine!),
                    NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
            }

            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            return new AssistantOutcome(
                BuildGoodsReceiptVerificationReply(workflow),
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        if (LooksLikeFinishCommand(Normalize(message)))
        {
            return new AssistantOutcome(
                $"GRN {workflow.GoodsReceiptNumber} is still open. Review it behind the chat window, or say `post grn` when you are ready to post it.",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        return null;
    }

    private async Task<AssistantOutcome> HandleGoodsReceiptWorkflowAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.GoodsReceipt;

        if (!HasAnyRole(actor, Roles.Admin, Roles.Procurement, Roles.Inventory))
        {
            workflow.ResetConversation();
            return new AssistantOutcome("You no longer have inventory or procurement access in this session, so I stopped the guided GRN flow.");
        }

        if (!workflow.CanEditDraft)
        {
            workflow.Stage = AssistantGoodsReceiptStage.Paused;
            return BuildReadOnlyGoodsReceiptOutcome(workflow);
        }

        if (LooksLikeCancelCommand(message))
        {
            workflow.Stage = workflow.HasDraft ? AssistantGoodsReceiptStage.Paused : AssistantGoodsReceiptStage.Idle;
            return workflow.HasDraft
                ? new AssistantOutcome(
                    $"I stopped prompting. Draft GRN {workflow.GoodsReceiptNumber} stays open. Say `resume grn` when you want to continue.",
                    NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value))
                : new AssistantOutcome("I cancelled the current GRN flow.");
        }

        var revisionOutcome = await TryHandleGoodsReceiptRevisionAsync(session, message, cancellationToken);
        if (revisionOutcome is not null)
        {
            return revisionOutcome;
        }

        if (LooksLikeShowLinesCommand(message))
        {
            return new AssistantOutcome(
                BuildGoodsReceiptPlanOverview(workflow),
                NavigateTo: workflow.GoodsReceiptId is null ? null : GoodsReceiptPath(workflow.GoodsReceiptId.Value));
        }

        if (workflow.HasDraft && LooksLikePostGoodsReceiptCommand(message))
        {
            if (workflow.Lines.All(x => !x.HasPlannedReceipt))
            {
                return new AssistantOutcome(
                    $"GRN {workflow.GoodsReceiptNumber} does not have any received lines yet. Enter at least one received quantity before posting it.",
                    NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
            }

            workflow.AwaitingPostConfirmation = true;
            return new AssistantOutcome(
                BuildGoodsReceiptPostConfirmationReply(workflow),
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        return workflow.Stage switch
        {
            AssistantGoodsReceiptStage.AwaitingPurchaseOrder => await HandleGoodsReceiptPurchaseOrderAsync(workflow, message, provider, cancellationToken),
            AssistantGoodsReceiptStage.AwaitingWarehouse => await HandleGoodsReceiptWarehouseAsync(workflow, message, provider, cancellationToken),
            AssistantGoodsReceiptStage.AwaitingQuantity => await HandleGoodsReceiptQuantityAsync(workflow, message, provider, cancellationToken),
            AssistantGoodsReceiptStage.AwaitingBatch => await HandleGoodsReceiptBatchAsync(workflow, message, cancellationToken),
            AssistantGoodsReceiptStage.AwaitingSerials => await HandleGoodsReceiptSerialsAsync(workflow, message, cancellationToken),
            AssistantGoodsReceiptStage.AwaitingVerification => await HandleGoodsReceiptVerificationAsync(session, message, cancellationToken),
            AssistantGoodsReceiptStage.Paused => new AssistantOutcome(
                $"Draft GRN {workflow.GoodsReceiptNumber} is paused. Say `resume grn`, `show lines`, `change line 2 qty 5`, or `post grn`.",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value)),
            _ => new AssistantOutcome("Tell me the PO number for the GRN."),
        };
    }

    private async Task<AssistantOutcome> HandleGoodsReceiptPostConfirmationAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        CancellationToken cancellationToken)
    {
        var workflow = session.GoodsReceipt;
        if (!HasAnyRole(actor, Roles.Admin, Roles.Procurement, Roles.Inventory))
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = workflow.HasDraft ? AssistantGoodsReceiptStage.Paused : AssistantGoodsReceiptStage.Idle;
            return new AssistantOutcome("You no longer have inventory or procurement access in this session, so I stopped the GRN posting flow.");
        }

        if (workflow.GoodsReceiptId is null)
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantGoodsReceiptStage.Idle;
            return new AssistantOutcome("There is no GRN draft open to post.");
        }

        if (!workflow.CanEditDraft)
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantGoodsReceiptStage.Paused;
            return BuildReadOnlyGoodsReceiptOutcome(workflow);
        }

        var revisionOutcome = await TryHandleGoodsReceiptRevisionAsync(session, message, cancellationToken);
        if (revisionOutcome is not null)
        {
            workflow.AwaitingPostConfirmation = false;
            return revisionOutcome;
        }

        if (LooksLikeShowLinesCommand(message))
        {
            return new AssistantOutcome(
                $"{BuildGoodsReceiptPlanOverview(workflow)}{Environment.NewLine}{Environment.NewLine}{BuildGoodsReceiptPostConfirmationReply(workflow)}",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value));
        }

        if (LooksLikeResumeCommand(message))
        {
            workflow.AwaitingPostConfirmation = false;

            var nextIndex = workflow.Lines.FindIndex(x => !x.IsAnswered);
            if (nextIndex >= 0)
            {
                workflow.CurrentLineIndex = nextIndex;
                workflow.Stage = AssistantGoodsReceiptStage.AwaitingQuantity;
                return new AssistantOutcome(
                    BuildGoodsReceiptCurrentLinePrompt(workflow.CurrentLine!),
                    NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value));
            }

            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            return new AssistantOutcome(
                BuildGoodsReceiptVerificationReply(workflow),
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value));
        }

        var normalized = Normalize(message);
        if (LooksLikeConfirmCommand(normalized))
        {
            await procurementService.PostGoodsReceiptAsync(workflow.GoodsReceiptId.Value, cancellationToken);
            await SyncGoodsReceiptWorkflowAsync(workflow, cancellationToken);
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantGoodsReceiptStage.Paused;
            return new AssistantOutcome(
                $"GRN {workflow.GoodsReceiptNumber} has been posted. The document behind the chat is now read-only.",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value),
                RefreshCurrentPage: true);
        }

        if (LooksLikeRejectCommand(normalized) || LooksLikeCancelCommand(message))
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantGoodsReceiptStage.Paused;
            return new AssistantOutcome(
                $"Keeping GRN {workflow.GoodsReceiptNumber} as a draft. Say `post grn` when you want to post it.",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value));
        }

        return new AssistantOutcome(
            BuildGoodsReceiptPostConfirmationReply(workflow),
            NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value));
    }

    private async Task SyncGoodsReceiptWorkflowAsync(
        AssistantGoodsReceiptWorkflow workflow,
        CancellationToken cancellationToken)
    {
        if (workflow.GoodsReceiptId is null)
        {
            return;
        }

        var goodsReceipt = await dbContext.GoodsReceipts.AsNoTracking()
            .Where(x => x.Id == workflow.GoodsReceiptId.Value)
            .Select(x => new { x.Number, x.Status, x.PurchaseOrderId, x.WarehouseId })
            .FirstOrDefaultAsync(cancellationToken);

        if (goodsReceipt is null)
        {
            workflow.ResetConversation();
            return;
        }

        workflow.GoodsReceiptNumber = goodsReceipt.Number;
        workflow.CurrentStatus = goodsReceipt.Status;
        workflow.PurchaseOrderId ??= goodsReceipt.PurchaseOrderId;
        workflow.WarehouseId ??= goodsReceipt.WarehouseId;
    }

    private async Task<AssistantOutcome> HandleGoodsReceiptPurchaseOrderAsync(
        AssistantGoodsReceiptWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        if (workflow.CandidatePurchaseOrders.Count > 0 &&
            TryResolveOptionSelection(message, workflow.CandidatePurchaseOrders, out var selectedPurchaseOrder))
        {
            workflow.CandidatePurchaseOrders.Clear();
            return await CommitGoodsReceiptPurchaseOrderAsync(workflow, selectedPurchaseOrder, cancellationToken);
        }

        var llm = await InterpretAsync("grn-purchase-order", message, provider, cancellationToken);
        var query = llm?.ReferenceNumber ?? message;
        var matches = await FindReceivablePurchaseOrderMatchesAsync(query, cancellationToken);
        if (matches.Count == 0)
        {
            return new AssistantOutcome("I could not find a receivable purchase order. Send the PO number again.");
        }

        if (matches.Count == 1)
        {
            return await CommitGoodsReceiptPurchaseOrderAsync(workflow, matches[0], cancellationToken);
        }

        workflow.CandidatePurchaseOrders.Clear();
        workflow.CandidatePurchaseOrders.AddRange(matches);
        return new AssistantOutcome(
            $"I found multiple purchase orders. Reply with the option number or PO number:{Environment.NewLine}{FormatOptions(matches)}");
    }

    private async Task<AssistantOutcome> CommitGoodsReceiptPurchaseOrderAsync(
        AssistantGoodsReceiptWorkflow workflow,
        AssistantLookupOption purchaseOrder,
        CancellationToken cancellationToken)
    {
        var poSnapshot = await (
            from po in dbContext.PurchaseOrders.AsNoTracking()
            join supplier in dbContext.Suppliers.AsNoTracking() on po.SupplierId equals supplier.Id
            where po.Id == purchaseOrder.Id
            select new
            {
                po.Id,
                po.Number,
                SupplierCode = supplier.Code,
                SupplierName = supplier.Name
            }).FirstOrDefaultAsync(cancellationToken);

        if (poSnapshot is null)
        {
            return new AssistantOutcome("That purchase order is no longer available. Send the PO number again.");
        }

        var planLines = await LoadGoodsReceiptPlanLinesAsync(poSnapshot.Id, cancellationToken);
        if (planLines.Count == 0)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingPurchaseOrder;
            return new AssistantOutcome($"PO {poSnapshot.Number} has no remaining quantity to receive.");
        }

        workflow.PurchaseOrderId = poSnapshot.Id;
        workflow.PurchaseOrderNumber = poSnapshot.Number;
        workflow.Lines.Clear();
        workflow.Lines.AddRange(planLines);
        workflow.Stage = AssistantGoodsReceiptStage.AwaitingWarehouse;

        return new AssistantOutcome(
            $"I found PO {poSnapshot.Number} for {poSnapshot.SupplierCode} - {poSnapshot.SupplierName}.{Environment.NewLine}{BuildGoodsReceiptRemainingLinesSummary(workflow)}{Environment.NewLine}Tell me the warehouse for this GRN.");
    }

    private async Task<AssistantOutcome> HandleGoodsReceiptWarehouseAsync(
        AssistantGoodsReceiptWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        if (workflow.CandidateWarehouses.Count > 0 &&
            TryResolveOptionSelection(message, workflow.CandidateWarehouses, out var selectedWarehouse))
        {
            workflow.CandidateWarehouses.Clear();
            return await CommitGoodsReceiptWarehouseAsync(workflow, selectedWarehouse, cancellationToken);
        }

        var llm = await InterpretAsync("grn-warehouse", message, provider, cancellationToken);
        var warehouseQuery = llm?.WarehouseText ?? message;
        var matches = await FindWarehouseMatchesAsync(warehouseQuery, cancellationToken);
        if (matches.Count == 0)
        {
            return new AssistantOutcome("I could not match that warehouse. Send the warehouse code or a clearer warehouse name.");
        }

        if (matches.Count == 1)
        {
            return await CommitGoodsReceiptWarehouseAsync(workflow, matches[0], cancellationToken);
        }

        workflow.CandidateWarehouses.Clear();
        workflow.CandidateWarehouses.AddRange(matches);
        return new AssistantOutcome(
            $"I found multiple warehouses. Reply with the option number or warehouse code:{Environment.NewLine}{FormatOptions(matches)}");
    }

    private async Task<AssistantOutcome> CommitGoodsReceiptWarehouseAsync(
        AssistantGoodsReceiptWorkflow workflow,
        AssistantLookupOption warehouse,
        CancellationToken cancellationToken)
    {
        if (workflow.PurchaseOrderId is null)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingPurchaseOrder;
            return new AssistantOutcome("Tell me the purchase order number first.");
        }

        workflow.WarehouseId = warehouse.Id;
        workflow.WarehouseCode = warehouse.Code;
        workflow.WarehouseName = warehouse.Label;

        if (workflow.GoodsReceiptId is null)
        {
            var goodsReceiptId = await procurementService.CreateGoodsReceiptAsync(workflow.PurchaseOrderId.Value, warehouse.Id, cancellationToken);
            workflow.GoodsReceiptId = goodsReceiptId;
            var goodsReceipt = await dbContext.GoodsReceipts.AsNoTracking()
                .Where(x => x.Id == goodsReceiptId)
                .Select(x => new { x.Number, x.Status })
                .FirstAsync(cancellationToken);

            workflow.GoodsReceiptNumber = goodsReceipt.Number;
            workflow.CurrentStatus = goodsReceipt.Status;
        }

        var nextIndex = workflow.Lines.FindIndex(x => !x.IsAnswered);
        if (nextIndex < 0)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            return new AssistantOutcome(
                BuildGoodsReceiptVerificationReply(workflow),
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        workflow.CurrentLineIndex = nextIndex;
        workflow.Stage = AssistantGoodsReceiptStage.AwaitingQuantity;
        return new AssistantOutcome(
            $"GRN {workflow.GoodsReceiptNumber} is ready in warehouse {warehouse.DisplayText}. I opened it behind the chat box.{Environment.NewLine}{BuildGoodsReceiptCurrentLinePrompt(workflow.CurrentLine!)}",
            NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
    }

    private async Task<AssistantOutcome> HandleGoodsReceiptQuantityAsync(
        AssistantGoodsReceiptWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var line = workflow.CurrentLine;
        if (line is null)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            return new AssistantOutcome(BuildGoodsReceiptVerificationReply(workflow), NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        var normalized = Normalize(message);
        if (LooksLikeFinishCommand(normalized))
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            return new AssistantOutcome(
                BuildGoodsReceiptVerificationReply(workflow),
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        if (LooksLikeSkipReceiptCommand(normalized))
        {
            line.PlannedQuantity = 0m;
            line.BatchNumber = null;
            line.Serials.Clear();
            await SaveGoodsReceiptPlanAsync(workflow, cancellationToken);
            return MoveToNextGoodsReceiptStep(
                workflow,
                $"Marked {line.DisplayLabel} as not received on this GRN.",
                refreshCurrentPage: true);
        }

        var quantity = TryExtractDecimal(message);
        if (quantity is null)
        {
            var llm = await InterpretAsync("grn-quantity", message, provider, cancellationToken);
            quantity = llm?.Quantity;
            if (llm?.Intent == "finish")
            {
                workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
                return new AssistantOutcome(
                    BuildGoodsReceiptVerificationReply(workflow),
                    NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
            }
        }

        if (quantity is null || quantity < 0m)
        {
            return new AssistantOutcome("Send the received quantity for this line, or say `skip` if the item was not received.");
        }

        if (quantity > line.RemainingQuantity)
        {
            return new AssistantOutcome($"You can only receive up to {line.RemainingQuantity} for {line.DisplayLabel} on this PO line.");
        }

        line.PlannedQuantity = quantity.Value;
        if (quantity.Value <= 0m)
        {
            line.BatchNumber = null;
            line.Serials.Clear();
            await SaveGoodsReceiptPlanAsync(workflow, cancellationToken);
            return MoveToNextGoodsReceiptStep(
                workflow,
                $"Marked {line.DisplayLabel} as not received on this GRN.",
                refreshCurrentPage: true);
        }

        await SaveGoodsReceiptPlanAsync(workflow, cancellationToken);

        if (line.TrackingType == TrackingType.Batch)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingBatch;
            return new AssistantOutcome(
                $"Recorded {quantity.Value} for {line.DisplayLabel}. Send the batch number, or say `skip` if there is no batch to capture.",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value),
                RefreshCurrentPage: true);
        }

        if (line.TrackingType == TrackingType.Serial)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingSerials;
            return new AssistantOutcome(
                $"Recorded {quantity.Value} for {line.DisplayLabel}. Send the serial numbers one per line or comma-separated, or say `skip` if you want to leave serials blank for now.",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value),
                RefreshCurrentPage: true);
        }

        return MoveToNextGoodsReceiptStep(
            workflow,
            $"Recorded {quantity.Value} for {line.DisplayLabel}.",
            refreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> HandleGoodsReceiptBatchAsync(
        AssistantGoodsReceiptWorkflow workflow,
        string message,
        CancellationToken cancellationToken)
    {
        var line = workflow.CurrentLine;
        if (line is null)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            return new AssistantOutcome(BuildGoodsReceiptVerificationReply(workflow), NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        var normalized = Normalize(message);
        line.BatchNumber = LooksLikeSkipReceiptCommand(normalized) ? null : ParseBatchValue(message);
        await SaveGoodsReceiptPlanAsync(workflow, cancellationToken);
        return MoveToNextGoodsReceiptStep(
            workflow,
            string.IsNullOrWhiteSpace(line.BatchNumber)
                ? $"Left batch blank for {line.DisplayLabel}."
                : $"Set batch {line.BatchNumber} for {line.DisplayLabel}.",
            refreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> HandleGoodsReceiptSerialsAsync(
        AssistantGoodsReceiptWorkflow workflow,
        string message,
        CancellationToken cancellationToken)
    {
        var line = workflow.CurrentLine;
        if (line is null)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            return new AssistantOutcome(BuildGoodsReceiptVerificationReply(workflow), NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        line.Serials.Clear();
        if (!LooksLikeSkipReceiptCommand(Normalize(message)))
        {
            foreach (var serial in ParseSerialList(message))
            {
                line.Serials.Add(serial);
            }
        }

        await SaveGoodsReceiptPlanAsync(workflow, cancellationToken);
        return MoveToNextGoodsReceiptStep(
            workflow,
            line.Serials.Count == 0
                ? $"Left serials blank for {line.DisplayLabel}."
                : $"Captured {line.Serials.Count} serial number(s) for {line.DisplayLabel}.",
            refreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> HandleGoodsReceiptVerificationAsync(
        AssistantSession session,
        string message,
        CancellationToken cancellationToken)
    {
        var workflow = session.GoodsReceipt;
        var normalized = Normalize(message);

        var revisionOutcome = await TryHandleGoodsReceiptRevisionAsync(session, message, cancellationToken);
        if (revisionOutcome is not null)
        {
            return revisionOutcome;
        }

        if (LooksLikeConfirmCommand(normalized))
        {
            workflow.Stage = AssistantGoodsReceiptStage.Paused;
            return new AssistantOutcome(
                $"GRN {workflow.GoodsReceiptNumber} is saved as a draft and ready for review or posting. The full GRN stays open behind the chat box.",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value),
                RefreshCurrentPage: true);
        }

        if (LooksLikeResumeCommand(message))
        {
            var nextIndex = workflow.Lines.FindIndex(x => !x.IsAnswered);
            if (nextIndex >= 0)
            {
                workflow.CurrentLineIndex = nextIndex;
                workflow.Stage = AssistantGoodsReceiptStage.AwaitingQuantity;
                return new AssistantOutcome(
                    BuildGoodsReceiptCurrentLinePrompt(workflow.CurrentLine!),
                    NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
            }

            return new AssistantOutcome(
                BuildGoodsReceiptVerificationReply(workflow),
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        if (LooksLikePostGoodsReceiptCommand(message))
        {
            if (workflow.Lines.All(x => !x.HasPlannedReceipt))
            {
                return new AssistantOutcome(
                    $"GRN {workflow.GoodsReceiptNumber} does not have any received lines yet. Enter at least one received quantity before posting it.",
                    NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
            }

            workflow.AwaitingPostConfirmation = true;
            return new AssistantOutcome(
                BuildGoodsReceiptPostConfirmationReply(workflow),
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        if (LooksLikeRejectCommand(normalized) || LooksLikeCancelCommand(message))
        {
            workflow.Stage = AssistantGoodsReceiptStage.Paused;
            return new AssistantOutcome(
                $"Keeping GRN {workflow.GoodsReceiptNumber} as a draft. Say `resume grn` if you want to continue later.",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value));
        }

        return new AssistantOutcome(
            $"{BuildGoodsReceiptVerificationReply(workflow)}{Environment.NewLine}Reply `confirm` to keep the draft, say `post grn` to post it now, or say `change line 2 qty 5` / `change line 2 batch B1` / `change line 2 serials S1,S2`.");
    }

    private async Task<AssistantOutcome?> TryHandleGoodsReceiptRevisionAsync(
        AssistantSession session,
        string message,
        CancellationToken cancellationToken)
    {
        var workflow = session.GoodsReceipt;
        if (workflow.GoodsReceiptId is null)
        {
            return null;
        }

        if (TryParseGoodsReceiptLineQuantityChange(message, out var lineNumber, out var quantity))
        {
            var line = workflow.Lines.FirstOrDefault(x => x.DisplayIndex == lineNumber);
            if (line is null)
            {
                return new AssistantOutcome($"I could not find line {lineNumber} on this GRN plan.");
            }

            if (quantity < 0m || quantity > line.RemainingQuantity)
            {
                return new AssistantOutcome($"Line {lineNumber} can only receive between 0 and {line.RemainingQuantity}.");
            }

            line.PlannedQuantity = quantity;
            if (quantity <= 0m)
            {
                line.BatchNumber = null;
                line.Serials.Clear();
            }

            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            await SaveGoodsReceiptPlanAsync(workflow, cancellationToken);
            return new AssistantOutcome(
                $"{BuildGoodsReceiptRevisionReply(line, "quantity")}{Environment.NewLine}{BuildGoodsReceiptVerificationReply(workflow)}",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value),
                RefreshCurrentPage: true);
        }

        if (TryParseGoodsReceiptLineBatchChange(message, out lineNumber, out var batchNumber))
        {
            var line = workflow.Lines.FirstOrDefault(x => x.DisplayIndex == lineNumber);
            if (line is null)
            {
                return new AssistantOutcome($"I could not find line {lineNumber} on this GRN plan.");
            }

            line.BatchNumber = string.IsNullOrWhiteSpace(batchNumber) ? null : batchNumber;
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            await SaveGoodsReceiptPlanAsync(workflow, cancellationToken);
            return new AssistantOutcome(
                $"{BuildGoodsReceiptRevisionReply(line, "batch")}{Environment.NewLine}{BuildGoodsReceiptVerificationReply(workflow)}",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value),
                RefreshCurrentPage: true);
        }

        if (TryParseGoodsReceiptLineSerialChange(message, out lineNumber, out var serials))
        {
            var line = workflow.Lines.FirstOrDefault(x => x.DisplayIndex == lineNumber);
            if (line is null)
            {
                return new AssistantOutcome($"I could not find line {lineNumber} on this GRN plan.");
            }

            line.Serials.Clear();
            line.Serials.AddRange(serials);
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            await SaveGoodsReceiptPlanAsync(workflow, cancellationToken);
            return new AssistantOutcome(
                $"{BuildGoodsReceiptRevisionReply(line, "serials")}{Environment.NewLine}{BuildGoodsReceiptVerificationReply(workflow)}",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId.Value),
                RefreshCurrentPage: true);
        }

        return null;
    }

    private async Task SaveGoodsReceiptPlanAsync(
        AssistantGoodsReceiptWorkflow workflow,
        CancellationToken cancellationToken)
    {
        await procurementService.ReplaceGoodsReceiptReceiptPlanAsync(
            workflow.GoodsReceiptId!.Value,
            workflow.Lines
                .Where(x => x.PlannedQuantity is > 0m)
                .Select(x => new ProcurementService.GoodsReceiptReceiptPlanLineInput(
                    x.PurchaseOrderLineId,
                    x.PlannedQuantity!.Value,
                    x.UnitCost,
                    x.BatchNumber,
                    x.Serials.ToList()))
                .ToList(),
            cancellationToken);
    }

    private AssistantOutcome MoveToNextGoodsReceiptStep(
        AssistantGoodsReceiptWorkflow workflow,
        string prefix,
        bool refreshCurrentPage)
    {
        var nextIndex = workflow.Lines.FindIndex(workflow.CurrentLineIndex + 1, x => !x.IsAnswered);
        if (nextIndex < 0)
        {
            workflow.Stage = AssistantGoodsReceiptStage.AwaitingVerification;
            return new AssistantOutcome(
                $"{prefix}{Environment.NewLine}{Environment.NewLine}{BuildGoodsReceiptVerificationReply(workflow)}",
                NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value),
                RefreshCurrentPage: refreshCurrentPage);
        }

        workflow.CurrentLineIndex = nextIndex;
        workflow.Stage = AssistantGoodsReceiptStage.AwaitingQuantity;
        return new AssistantOutcome(
            $"{prefix}{Environment.NewLine}{Environment.NewLine}{BuildGoodsReceiptCurrentLinePrompt(workflow.CurrentLine!)}",
            NavigateTo: GoodsReceiptPath(workflow.GoodsReceiptId!.Value),
            RefreshCurrentPage: refreshCurrentPage);
    }

    private async Task<List<AssistantGoodsReceiptPlanLine>> LoadGoodsReceiptPlanLinesAsync(Guid purchaseOrderId, CancellationToken cancellationToken)
    {
        var lines = await (
            from po in dbContext.PurchaseOrders.AsNoTracking()
            where po.Id == purchaseOrderId
            from line in po.Lines
            join item in dbContext.Items.AsNoTracking() on line.ItemId equals item.Id
            let remainingQuantity = line.OrderedQuantity - line.ReceivedQuantity
            where remainingQuantity > 0m
            orderby line.Id
            select new AssistantGoodsReceiptPlanLine
            {
                PurchaseOrderLineId = line.Id,
                ItemId = item.Id,
                ItemCode = item.Sku,
                ItemName = item.Name,
                TrackingType = item.TrackingType,
                OrderedQuantity = line.OrderedQuantity,
                PreviouslyReceivedQuantity = line.ReceivedQuantity,
                RemainingQuantity = remainingQuantity,
                UnitCost = line.UnitPrice,
            })
            .ToListAsync(cancellationToken);

        for (var i = 0; i < lines.Count; i++)
        {
            lines[i].DisplayIndex = i + 1;
        }

        return lines;
    }

    private async Task<List<AssistantLookupOption>> FindReceivablePurchaseOrderMatchesAsync(string query, CancellationToken cancellationToken)
    {
        var normalized = Normalize(query);
        var purchaseOrders = await (
            from po in dbContext.PurchaseOrders.AsNoTracking()
            join supplier in dbContext.Suppliers.AsNoTracking() on po.SupplierId equals supplier.Id
            where po.Status == PurchaseOrderStatus.Approved || po.Status == PurchaseOrderStatus.PartiallyReceived
            where po.Lines.Any(line => line.ReceivedQuantity < line.OrderedQuantity)
            select new AssistantLookupOption(po.Id, po.Number, $"{supplier.Code} - {supplier.Name}"))
            .ToListAsync(cancellationToken);

        return purchaseOrders
            .Select(x => new { Option = x, Score = MatchScore(normalized, x.Code, x.Label, x.DisplayText) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Option.Code)
            .Take(MaxLookupOptions)
            .Select(x => x.Option)
            .ToList();
    }

    private async Task<List<AssistantLookupOption>> FindSupplierMatchesAsync(string query, CancellationToken cancellationToken)
    {
        var normalized = Normalize(query);
        var suppliers = await dbContext.Suppliers.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new AssistantLookupOption(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        return suppliers
            .Select(x => new { Option = x, Score = MatchScore(normalized, x.Code, x.Label) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Option.Code)
            .Take(MaxLookupOptions)
            .Select(x => x.Option)
            .ToList();
    }

    private async Task<List<AssistantLookupOption>> FindItemMatchesAsync(string query, CancellationToken cancellationToken)
    {
        var normalized = Normalize(query);
        var items = await dbContext.Items.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, x.Sku, x.Name, x.Barcode })
            .ToListAsync(cancellationToken);

        return items
            .Select(x => new
            {
                Option = new AssistantLookupOption(x.Id, x.Sku, x.Name),
                Score = MatchScore(normalized, x.Sku, x.Name, x.Barcode)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Option.Code)
            .Take(MaxLookupOptions)
            .Select(x => x.Option)
            .ToList();
    }

    private async Task<List<AssistantLookupOption>> FindWarehouseMatchesAsync(string query, CancellationToken cancellationToken)
    {
        var normalized = Normalize(query);
        var warehouses = await dbContext.Warehouses.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new AssistantLookupOption(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        return warehouses
            .Select(x => new { Option = x, Score = MatchScore(normalized, x.Code, x.Label, x.DisplayText) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Option.Code)
            .Take(MaxLookupOptions)
            .Select(x => x.Option)
            .ToList();
    }

    private async Task<AssistantInterpretation?> InterpretAsync(
        string stage,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        if (provider is null)
        {
            return null;
        }

        var prompt = stage switch
        {
            "idle" => (
                """
                You extract intent for an ERP assistant. Return JSON only with keys:
                intent, supplierText, itemText, warehouseText, referenceNumber, quantity, unitPrice.
                Allowed intent values:
                start_purchase_order, start_goods_receipt, start_stock_transfer, report_dashboard, report_stock_ledger, report_aging, report_costing, resume_purchase_order, unknown.
                Use null for unused fields.
                """,
                $"User message: {message}"),
            "grn-purchase-order" => (
                """
                Return JSON only with keys: intent, referenceNumber.
                Allowed intent values: provide_reference_number, unknown.
                Extract the purchase order number into referenceNumber.
                """,
                $"User message: {message}"),
            "grn-warehouse" => (
                """
                Return JSON only with keys: intent, warehouseText.
                Allowed intent values: select_warehouse, unknown.
                Extract the warehouse lookup text.
                """,
                $"User message: {message}"),
            "grn-quantity" => (
                """
                Return JSON only with keys: intent, quantity.
                Allowed intent values: provide_quantity, skip_line, finish, unknown.
                Extract a positive quantity when present.
                """,
                $"User message: {message}"),
            "po-requisition-choice" => (
                """
                Return JSON only with keys: intent, referenceNumber.
                Allowed intent values: use_requisition, no_requisition, unknown.
                If the user already typed a requisition number, set referenceNumber.
                """,
                $"User message: {message}"),
            "po-requisition-number" => (
                """
                Return JSON only with keys: intent, referenceNumber.
                Allowed intent values: provide_reference_number, unknown.
                Extract the requisition number into referenceNumber.
                """,
                $"User message: {message}"),
            "po-supplier" => (
                """
                Return JSON only with keys: intent, supplierText.
                Allowed intent values: select_supplier, unknown.
                Extract the supplier lookup text.
                """,
                $"User message: {message}"),
            "po-item" => (
                """
                Return JSON only with keys: intent, itemText, quantity, unitPrice.
                Allowed intent values: select_item, finish, update_last_line_quantity, update_last_line_price, remove_last_line, unknown.
                Extract itemText when the user is identifying an item.
                Extract quantity or unitPrice only if clearly present.
                """,
                $"User message: {message}"),
            "po-quantity" => (
                """
                Return JSON only with keys: intent, quantity.
                Allowed intent values: provide_quantity, unknown.
                Extract a positive quantity.
                """,
                $"User message: {message}"),
            "po-unit-price" => (
                """
                Return JSON only with keys: intent, unitPrice.
                Allowed intent values: provide_unit_price, unknown.
                Extract a unit price that is zero or greater.
                """,
                $"User message: {message}"),
            "po-line-confirmation" => (
                """
                Return JSON only with keys: intent, itemText, quantity, unitPrice.
                Allowed intent values: confirm, reject, update_current_quantity, update_current_unit_price, select_item, unknown.
                """,
                $"User message: {message}"),
            "po-paused" => (
                """
                Return JSON only with keys: intent, itemText.
                Allowed intent values: resume_purchase_order, select_item, finish, unknown.
                """,
                $"User message: {message}"),
            "report-stock-ledger" => (
                """
                Return JSON only with keys: intent, itemText, warehouseText.
                Allowed intent values: report_stock_ledger, unknown.
                Extract optional item and warehouse lookup text from the user message.
                """,
                $"User message: {message}"),
            "report-costing" => (
                """
                Return JSON only with keys: intent, itemText, warehouseText.
                Allowed intent values: report_costing, unknown.
                Extract optional item and warehouse lookup text from the user message.
                """,
                $"User message: {message}"),
            _ => (string.Empty, string.Empty),
        };

        if (string.IsNullOrWhiteSpace(prompt.Item1))
        {
            return null;
        }

        if (provider is null)
        {
            return null;
        }

        var resolvedProvider = new AssistantResolvedProvider(
            ProfileId: null,
            Name: provider.Kind ?? "assistant-provider",
            Kind: provider.Kind ?? "openai-compatible",
            BaseUrl: provider.BaseUrl ?? string.Empty,
            Model: provider.Model ?? string.Empty,
            ApiKey: provider.ApiKey);

        return await providerGateway.TryInterpretAsync(resolvedProvider, prompt.Item1, prompt.Item2, cancellationToken);
    }

    private async Task<string> ResolveIdleIntentAsync(
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(message);
        if (normalized.Contains("purchase order", StringComparison.Ordinal) ||
            Regex.IsMatch(normalized, @"\bpo\b", RegexOptions.CultureInvariant))
        {
            return "start_purchase_order";
        }

        if (normalized.Contains("goods receipt", StringComparison.Ordinal) ||
            normalized.Contains("grn", StringComparison.Ordinal) ||
            normalized.Contains("receive po", StringComparison.Ordinal) ||
            normalized.Contains("receive goods", StringComparison.Ordinal))
        {
            return "start_goods_receipt";
        }

        if (normalized.Contains("stock transfer", StringComparison.Ordinal) ||
            normalized.Contains("transfer stock", StringComparison.Ordinal) ||
            normalized.Contains("move stock", StringComparison.Ordinal) ||
            normalized.Contains("warehouse transfer", StringComparison.Ordinal))
        {
            return "start_stock_transfer";
        }

        if (normalized.Contains("dashboard", StringComparison.Ordinal))
        {
            return "report_dashboard";
        }

        if (normalized.Contains("stock ledger", StringComparison.Ordinal))
        {
            return "report_stock_ledger";
        }

        if (normalized.Contains("aging", StringComparison.Ordinal))
        {
            return "report_aging";
        }

        if (normalized.Contains("costing", StringComparison.Ordinal) ||
            normalized.Contains("inventory value", StringComparison.Ordinal))
        {
            return "report_costing";
        }

        if (LooksLikeResumeCommand(normalized))
        {
            return "resume_purchase_order";
        }

        var llm = await InterpretAsync("idle", message, provider, cancellationToken);
        return string.IsNullOrWhiteSpace(llm?.Intent) ? "unknown" : llm.Intent!;
    }

    private async Task<AssistantReportRequestDto?> ResolveReportRequestAsync(
        string intent,
        AssistantActor actor,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        if (!HasAnyRole(actor, Roles.Admin, Roles.Reporting))
        {
            return null;
        }

        return intent switch
        {
            "report_dashboard" => new AssistantReportRequestDto("dashboard", "Dashboard", "reporting/dashboard", "/", "High-level operational snapshot."),
            "report_stock_ledger" => await BuildInventoryReportRequestAsync("stock-ledger", "Stock Ledger", 50, 200, message, provider, cancellationToken),
            "report_aging" => new AssistantReportRequestDto("aging", "Aging", "reporting/aging", "/reporting/aging", "AR and AP aging as of today."),
            "report_costing" => await BuildInventoryReportRequestAsync("costing", "Costing", 50, 500, message, provider, cancellationToken),
            _ => null,
        };
    }

    private async Task<AssistantReportRequestDto> BuildInventoryReportRequestAsync(
        string kind,
        string title,
        int previewTake,
        int openTake,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var interpretation = await InterpretAsync(
            kind == "stock-ledger" ? "report-stock-ledger" : "report-costing",
            message,
            provider,
            cancellationToken);

        var warehouse = await ResolveWarehouseFilterAsync(message, interpretation?.WarehouseText, cancellationToken);
        var item = await ResolveItemFilterAsync(message, interpretation?.ItemText, cancellationToken);

        var previewQuery = new Dictionary<string, string?>
        {
            ["take"] = previewTake.ToString(CultureInfo.InvariantCulture),
        };
        var openQuery = new Dictionary<string, string?>
        {
            ["take"] = openTake.ToString(CultureInfo.InvariantCulture),
        };

        var summaryParts = new List<string>();
        if (warehouse is not null)
        {
            var warehouseId = warehouse.Id.ToString("D");
            previewQuery["warehouseId"] = warehouseId;
            openQuery["warehouseId"] = warehouseId;
            summaryParts.Add($"Warehouse: {warehouse.DisplayText}");
        }

        if (item is not null)
        {
            var itemId = item.Id.ToString("D");
            previewQuery["itemId"] = itemId;
            openQuery["itemId"] = itemId;
            summaryParts.Add($"Item: {item.DisplayText}");
        }

        return new AssistantReportRequestDto(
            kind,
            title,
            $"reporting/{kind}{BuildQueryString(previewQuery)}",
            $"/reporting/{kind}{BuildQueryString(openQuery)}",
            summaryParts.Count == 0 ? "Unfiltered preview." : string.Join(" | ", summaryParts));
    }

    private async Task<AssistantLookupOption?> ResolveWarehouseFilterAsync(
        string message,
        string? interpretedHint,
        CancellationToken cancellationToken)
    {
        foreach (var candidate in DistinctHintCandidates(
                     interpretedHint,
                     ExtractWarehouseHint(message),
                     message))
        {
            var match = (await FindWarehouseMatchesAsync(candidate, cancellationToken)).FirstOrDefault();
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private async Task<AssistantLookupOption?> ResolveItemFilterAsync(
        string message,
        string? interpretedHint,
        CancellationToken cancellationToken)
    {
        foreach (var candidate in DistinctHintCandidates(
                     interpretedHint,
                     ExtractItemHint(message),
                     message))
        {
            var match = (await FindItemMatchesAsync(candidate, cancellationToken)).FirstOrDefault();
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private async Task<AssistantChatResponse> BuildResponseAsync(
        AssistantSession session,
        AssistantTranscriptMessage _,
        AssistantReportRequestDto? reportRequest,
        string? navigateTo,
        bool refreshCurrentPage,
        CancellationToken cancellationToken)
    {
        var purchaseOrderDraft = await BuildPurchaseOrderDraftAsync(session, cancellationToken);
        var goodsReceiptDraft = await BuildGoodsReceiptDraftAsync(session, cancellationToken);
        var stockTransferDraft = await BuildStockTransferDraftAsync(session, cancellationToken);
        var status = BuildStatus(session, purchaseOrderDraft, goodsReceiptDraft, stockTransferDraft);
        return new AssistantChatResponse(
            session.SessionId,
            session.Transcript.Select(x => new AssistantMessageDto(x.Role, x.Content, x.OccurredAt)).ToList(),
            status,
            purchaseOrderDraft,
            goodsReceiptDraft,
            stockTransferDraft,
            reportRequest,
            navigateTo,
            refreshCurrentPage);
    }

    private AssistantTranscriptMessage AddAssistantReply(AssistantSession session, string content)
    {
        var reply = new AssistantTranscriptMessage("assistant", content, DateTimeOffset.UtcNow);
        session.Transcript.Add(reply);
        return reply;
    }

    private async Task<AssistantPurchaseOrderDraftDto?> BuildPurchaseOrderDraftAsync(
        AssistantSession session,
        CancellationToken cancellationToken)
    {
        var poId = session.PurchaseOrder.PurchaseOrderId;
        if (poId is null)
        {
            return null;
        }

        return await (
            from po in dbContext.PurchaseOrders.AsNoTracking()
            join supplier in dbContext.Suppliers.AsNoTracking() on po.SupplierId equals supplier.Id
            where po.Id == poId.Value
            select new AssistantPurchaseOrderDraftDto(
                po.Id,
                po.Number,
                po.Status.ToString(),
                supplier.Code,
                supplier.Name,
                po.Lines.Count,
                po.Lines.Sum(x => x.LineTotal),
                PurchaseOrderPath(po.Id),
                session.PurchaseOrder.CreatedFromRequisition))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AssistantGoodsReceiptDraftDto?> BuildGoodsReceiptDraftAsync(
        AssistantSession session,
        CancellationToken cancellationToken)
    {
        var goodsReceiptId = session.GoodsReceipt.GoodsReceiptId;
        if (goodsReceiptId is null)
        {
            return null;
        }

        var remainingLineCount = session.GoodsReceipt.Lines.Count(x => !x.HasPlannedReceipt);

        return await (
            from grn in dbContext.GoodsReceipts.AsNoTracking()
            join po in dbContext.PurchaseOrders.AsNoTracking() on grn.PurchaseOrderId equals po.Id
            join warehouse in dbContext.Warehouses.AsNoTracking() on grn.WarehouseId equals warehouse.Id
            where grn.Id == goodsReceiptId.Value
            select new AssistantGoodsReceiptDraftDto(
                grn.Id,
                grn.Number,
                grn.Status.ToString(),
                po.Number,
                warehouse.Code,
                warehouse.Name,
                grn.Lines.Count,
                grn.Lines.Sum(x => x.Quantity),
                remainingLineCount,
                GoodsReceiptPath(grn.Id)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AssistantStockTransferDraftDto?> BuildStockTransferDraftAsync(
        AssistantSession session,
        CancellationToken cancellationToken)
    {
        var transferId = session.StockTransfer.TransferId;
        if (transferId is null)
        {
            return null;
        }

        return await (
            from transfer in dbContext.StockTransfers.AsNoTracking()
            join fromWarehouse in dbContext.Warehouses.AsNoTracking() on transfer.FromWarehouseId equals fromWarehouse.Id
            join toWarehouse in dbContext.Warehouses.AsNoTracking() on transfer.ToWarehouseId equals toWarehouse.Id
            where transfer.Id == transferId.Value
            select new AssistantStockTransferDraftDto(
                transfer.Id,
                transfer.Number,
                transfer.Status.ToString(),
                fromWarehouse.Code,
                fromWarehouse.Name,
                toWarehouse.Code,
                toWarehouse.Name,
                transfer.Lines.Count,
                transfer.Lines.Sum(x => x.Quantity),
                StockTransferPath(transfer.Id)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static AssistantStatusDto BuildStatus(
        AssistantSession session,
        AssistantPurchaseOrderDraftDto? purchaseOrderDraft,
        AssistantGoodsReceiptDraftDto? goodsReceiptDraft,
        AssistantStockTransferDraftDto? stockTransferDraft)
    {
        var goodsReceiptWorkflow = session.GoodsReceipt;
        if (goodsReceiptDraft is not null)
        {
            var nextStep = goodsReceiptWorkflow.Stage switch
            {
                _ when goodsReceiptWorkflow.AwaitingPostConfirmation => "waiting for post confirmation",
                _ when !goodsReceiptWorkflow.CanEditDraft => $"{goodsReceiptDraft.Status} read-only",
                AssistantGoodsReceiptStage.AwaitingPurchaseOrder => "waiting for purchase order",
                AssistantGoodsReceiptStage.AwaitingWarehouse => "waiting for warehouse",
                AssistantGoodsReceiptStage.AwaitingQuantity => "waiting for received quantity",
                AssistantGoodsReceiptStage.AwaitingBatch => "waiting for batch",
                AssistantGoodsReceiptStage.AwaitingSerials => "waiting for serials",
                AssistantGoodsReceiptStage.AwaitingVerification => "waiting for verification",
                AssistantGoodsReceiptStage.Paused => "paused",
                _ => "ready",
            };

            return new AssistantStatusDto(
                "goods-receipt",
                $"GRN {goodsReceiptDraft.Number}",
                $"PO {goodsReceiptDraft.PurchaseOrderNumber}, warehouse {goodsReceiptDraft.WarehouseCode}, status {goodsReceiptDraft.Status}, {goodsReceiptDraft.LineCount} line(s), planned qty {goodsReceiptDraft.PlannedQuantity}, {goodsReceiptDraft.RemainingLineCount} line(s) still open, {nextStep}.");
        }

        var stockTransferWorkflow = session.StockTransfer;
        if (stockTransferDraft is not null)
        {
            var nextStep = stockTransferWorkflow.Stage switch
            {
                _ when stockTransferWorkflow.AwaitingPostConfirmation => "waiting for post confirmation",
                _ when !stockTransferWorkflow.CanEditDraft => $"{stockTransferDraft.Status} read-only",
                AssistantStockTransferStage.AwaitingFromWarehouse => "waiting for source warehouse",
                AssistantStockTransferStage.AwaitingToWarehouse => "waiting for destination warehouse",
                AssistantStockTransferStage.AwaitingItem => "waiting for item",
                AssistantStockTransferStage.AwaitingQuantity => "waiting for quantity",
                AssistantStockTransferStage.AwaitingBatch => "waiting for batch",
                AssistantStockTransferStage.AwaitingSerials => "waiting for serials",
                AssistantStockTransferStage.Paused => "paused",
                _ => "ready",
            };

            return new AssistantStatusDto(
                "stock-transfer",
                $"Transfer {stockTransferDraft.Number}",
                $"From {stockTransferDraft.FromWarehouseCode} to {stockTransferDraft.ToWarehouseCode}, status {stockTransferDraft.Status}, {stockTransferDraft.LineCount} line(s), total qty {stockTransferDraft.TotalQuantity}, {nextStep}.");
        }

        var workflow = session.PurchaseOrder;
        if (purchaseOrderDraft is not null)
        {
            var nextStep = workflow.Stage switch
            {
                _ when workflow.AwaitingApprovalConfirmation => "waiting for approval confirmation",
                _ when !workflow.CanEditDraft => $"{purchaseOrderDraft.Status} read-only",
                AssistantPurchaseOrderStage.AwaitingRequisitionChoice => "waiting for requisition choice",
                AssistantPurchaseOrderStage.AwaitingRequisitionNumber => "waiting for requisition number",
                AssistantPurchaseOrderStage.AwaitingSupplier => "waiting for supplier",
                AssistantPurchaseOrderStage.AwaitingItem => "waiting for item",
                AssistantPurchaseOrderStage.AwaitingQuantity => "waiting for quantity",
                AssistantPurchaseOrderStage.AwaitingUnitPrice => "waiting for unit price",
                AssistantPurchaseOrderStage.AwaitingLineConfirmation => "waiting for line confirmation",
                AssistantPurchaseOrderStage.Paused => "paused",
                _ => "ready",
            };

            return new AssistantStatusDto(
                "purchase-order",
                $"PO {purchaseOrderDraft.Number}",
                $"{purchaseOrderDraft.SupplierCode} - {purchaseOrderDraft.SupplierName}, status {purchaseOrderDraft.Status}, {purchaseOrderDraft.LineCount} line(s), total {purchaseOrderDraft.Total}, {nextStep}.");
        }

        return new AssistantStatusDto(
            "idle",
            "Assistant",
            "Ready. Phase 1 supports purchase-order drafting, GRN drafting, stock transfer drafting, and report previews.");
    }

    private static AssistantOutcome BuildReadOnlyGoodsReceiptOutcome(AssistantGoodsReceiptWorkflow workflow)
    {
        var status = workflow.CurrentStatus?.ToString() ?? "Draft";
        return new AssistantOutcome(
            $"GRN {workflow.GoodsReceiptNumber} is {status} and cannot be edited through the assistant any more. Open it if you need to review it, or start a new GRN if you need another receipt.",
            NavigateTo: workflow.GoodsReceiptId is null ? null : GoodsReceiptPath(workflow.GoodsReceiptId.Value));
    }

    private static string BuildGoodsReceiptRemainingLinesSummary(AssistantGoodsReceiptWorkflow workflow)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Remaining PO lines:");
        foreach (var line in workflow.Lines)
        {
            builder.Append("- ")
                .Append(line.DisplayIndex)
                .Append(". ")
                .Append(line.DisplayLabel)
                .Append(" | remaining ")
                .Append(line.RemainingQuantity)
                .Append(" | already received ")
                .Append(line.PreviouslyReceivedQuantity)
                .AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildGoodsReceiptPlanOverview(AssistantGoodsReceiptWorkflow workflow)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"GRN {workflow.GoodsReceiptNumber ?? "(draft)"} plan:");
        foreach (var line in workflow.Lines)
        {
            var status = line.PlannedQuantity switch
            {
                > 0m => $"receive {line.PlannedQuantity}",
                0m => "skip",
                _ => "pending"
            };

            builder.Append("- ")
                .Append(line.DisplayIndex)
                .Append(". ")
                .Append(line.DisplayLabel)
                .Append(" | ")
                .Append(status)
                .Append(" | remaining ")
                .Append(line.RemainingQuantity);

            if (!string.IsNullOrWhiteSpace(line.BatchNumber))
            {
                builder.Append(" | batch ").Append(line.BatchNumber);
            }

            if (line.Serials.Count > 0)
            {
                builder.Append(" | serials ").Append(string.Join(", ", line.Serials));
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildGoodsReceiptCurrentLinePrompt(AssistantGoodsReceiptPlanLine line)
    {
        var trackingPrompt = line.TrackingType switch
        {
            TrackingType.Batch => "I will ask for the batch after the quantity.",
            TrackingType.Serial => "I will ask for the serial numbers after the quantity.",
            _ => "Say `skip` if this item was not received."
        };

        return $"Line {line.DisplayIndex}: {line.DisplayLabel}.{Environment.NewLine}Ordered {line.OrderedQuantity}, already received {line.PreviouslyReceivedQuantity}, remaining {line.RemainingQuantity}.{Environment.NewLine}How much did you receive now? {trackingPrompt}";
    }

    private static string BuildGoodsReceiptVerificationReply(AssistantGoodsReceiptWorkflow workflow)
    {
        var receivedLines = workflow.Lines.Where(x => x.HasPlannedReceipt).ToList();
        var skippedLines = workflow.Lines.Where(x => !x.HasPlannedReceipt).ToList();

        var builder = new StringBuilder();
        builder.AppendLine($"Verification for GRN {workflow.GoodsReceiptNumber}:");
        builder.AppendLine("Receiving now:");
        if (receivedLines.Count == 0)
        {
            builder.AppendLine("- No items entered on this GRN yet.");
        }
        else
        {
            foreach (var line in receivedLines)
            {
                builder.Append("- ")
                    .Append(line.DisplayIndex)
                    .Append(". ")
                    .Append(line.DisplayLabel)
                    .Append(" | qty ")
                    .Append(line.PlannedQuantity);

                if (!string.IsNullOrWhiteSpace(line.BatchNumber))
                {
                    builder.Append(" | batch ").Append(line.BatchNumber);
                }

                if (line.Serials.Count > 0)
                {
                    builder.Append(" | serials ").Append(string.Join(", ", line.Serials));
                }

                builder.AppendLine();
            }
        }

        builder.AppendLine("Still open on the PO:");
        if (skippedLines.Count == 0)
        {
            builder.AppendLine("- Nothing left open from this receipt conversation.");
        }
        else
        {
            foreach (var line in skippedLines)
            {
                builder.Append("- ")
                    .Append(line.DisplayIndex)
                    .Append(". ")
                    .Append(line.DisplayLabel)
                    .Append(" | remaining ")
                    .Append(line.RemainingQuantity)
                    .AppendLine();
            }
        }

        builder.Append("Reply `confirm` to keep this draft, say `post grn` to post it now, or revise a line.");
        return builder.ToString();
    }

    private static string BuildGoodsReceiptPostConfirmationReply(AssistantGoodsReceiptWorkflow workflow)
        => $"Post GRN {workflow.GoodsReceiptNumber} now? Reply `confirm` to post it, or `cancel` to keep it as a draft.";

    private static string BuildGoodsReceiptRevisionReply(AssistantGoodsReceiptPlanLine line, string field)
        => field switch
        {
            "quantity" => $"Updated line {line.DisplayIndex} quantity to {line.PlannedQuantity}.",
            "batch" => string.IsNullOrWhiteSpace(line.BatchNumber)
                ? $"Cleared the batch on line {line.DisplayIndex}."
                : $"Updated line {line.DisplayIndex} batch to {line.BatchNumber}.",
            "serials" => line.Serials.Count == 0
                ? $"Cleared serials on line {line.DisplayIndex}."
                : $"Updated line {line.DisplayIndex} serials to {string.Join(", ", line.Serials)}.",
            _ => $"Updated line {line.DisplayIndex}."
        };

    private static bool TryResolveOptionSelection(string input, IReadOnlyList<AssistantLookupOption> options, out AssistantLookupOption option)
    {
        option = default!;
        if (int.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) &&
            index >= 1 &&
            index <= options.Count)
        {
            option = options[index - 1];
            return true;
        }

        var normalized = Normalize(input);
        option = options.FirstOrDefault(x => Normalize(x.Code) == normalized || Normalize(x.DisplayText) == normalized)!;
        return option is not null;
    }

    private static string FormatOptions(IReadOnlyList<AssistantLookupOption> options)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < options.Count; i++)
        {
            builder.Append(i + 1)
                .Append(". ")
                .Append(options[i].DisplayText)
                .AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static int MatchScore(string normalizedQuery, params string?[] candidates)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return 0;
        }

        var score = 0;
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var normalizedCandidate = Normalize(candidate);
            if (normalizedCandidate == normalizedQuery)
            {
                score = Math.Max(score, 100);
            }
            else if (normalizedQuery.Contains(normalizedCandidate, StringComparison.Ordinal))
            {
                score = Math.Max(score, 65);
            }
            else if (normalizedCandidate.StartsWith(normalizedQuery, StringComparison.Ordinal))
            {
                score = Math.Max(score, 75);
            }
            else if (normalizedCandidate.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                score = Math.Max(score, 50);
            }
            else if (TokenOverlap(normalizedQuery, normalizedCandidate) > 0)
            {
                score = Math.Max(score, 25 + (TokenOverlap(normalizedQuery, normalizedCandidate) * 10));
            }
        }

        return score;
    }

    private static int TokenOverlap(string left, string right)
    {
        var leftTokens = left.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var rightTokens = right.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (leftTokens.Length == 0 || rightTokens.Length == 0)
        {
            return 0;
        }

        var rightSet = rightTokens.ToHashSet(StringComparer.Ordinal);
        return leftTokens.Count(rightSet.Contains);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) || ch == '.' || ch == '-')
            {
                builder.Append(ch);
            }
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static IEnumerable<string> DistinctHintCandidates(params string?[] candidates)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            var normalized = Normalize(candidate);
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            {
                continue;
            }

            yield return candidate!.Trim();
        }
    }

    private static string? ExtractItemHint(string message)
        => ExtractHintAfterLabel(message, "item", "sku", "product");

    private static string? ExtractWarehouseHint(string message)
        => ExtractHintAfterLabel(message, "warehouse", "wh") ?? ExtractHintBeforeWarehouseLabel(message);

    private static string? ExtractHintAfterLabel(string message, params string[] labels)
    {
        var normalized = Normalize(message);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var joinedLabels = string.Join("|", labels.Select(Regex.Escape));
        var match = Regex.Match(
            normalized,
            $@"(?:\b(?:{joinedLabels})\b)\s+(?<value>[a-z0-9 .-]+?)(?=\s+\b(?:and|with|for|from|to|in|on|at|item|sku|product|warehouse|wh|report|ledger|costing|preview)\b|$)",
            RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? ExtractHintBeforeWarehouseLabel(string message)
    {
        var normalized = Normalize(message);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var match = Regex.Match(
            normalized,
            @"(?:in|at|from)\s+(?<value>[a-z0-9 .-]+?)\s+(?:warehouse|wh)\b",
            RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string BuildReportReply(AssistantReportRequestDto reportRequest)
    {
        var baseReply = $"I pulled the {reportRequest.Title.ToLowerInvariant()} preview below.";
        if (string.IsNullOrWhiteSpace(reportRequest.Summary))
        {
            return $"{baseReply} Use the open link if you want the full screen.";
        }

        return $"{baseReply} {reportRequest.Summary} Use the open link if you want the full screen.";
    }

    private static AssistantOutcome BuildReadOnlyPurchaseOrderOutcome(AssistantPurchaseOrderWorkflow workflow)
    {
        var status = workflow.CurrentStatus?.ToString() ?? "Draft";
        return new AssistantOutcome(
            $"PO {workflow.PurchaseOrderNumber} is {status} and cannot be edited through the assistant any more. Open it if you need to review it, or say `new PO` to start another draft.",
            NavigateTo: workflow.PurchaseOrderId is null ? null : PurchaseOrderPath(workflow.PurchaseOrderId.Value));
    }

    private static string BuildQueryString(IReadOnlyDictionary<string, string?> values)
    {
        var parts = values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}")
            .ToArray();

        return parts.Length == 0 ? string.Empty : $"?{string.Join("&", parts)}";
    }

    private static bool HasAnyRole(AssistantActor actor, params string[] roles) => roles.Any(actor.HasRole);

    private static decimal? TryExtractDecimal(string input)
    {
        var match = Regex.Match(input, @"-?\d+(?:\.\d+)?");
        if (!match.Success)
        {
            return null;
        }

        return decimal.TryParse(match.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static bool TryParseLastLineQuantityUpdate(string input, out decimal quantity)
    {
        quantity = 0;
        var match = Regex.Match(input, @"(?:change|update)\s+(?:the\s+)?last\s+line\s+(?:qty|quantity)\s+(?:to\s+)?(?<value>-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (!match.Success || !decimal.TryParse(match.Groups["value"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity))
        {
            return false;
        }

        return quantity > 0;
    }

    private static bool TryParseLastLinePriceUpdate(string input, out decimal unitPrice)
    {
        unitPrice = 0;
        var match = Regex.Match(input, @"(?:change|update)\s+(?:the\s+)?last\s+line\s+(?:price|unit price)\s+(?:to\s+)?(?<value>-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (!match.Success || !decimal.TryParse(match.Groups["value"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out unitPrice))
        {
            return false;
        }

        return unitPrice >= 0;
    }

    private static bool TryParseCurrentLineQuantityUpdate(string input, out decimal quantity)
    {
        quantity = 0;
        var match = Regex.Match(input, @"(?:qty|quantity)\s+(?<value>-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (!match.Success || !decimal.TryParse(match.Groups["value"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity))
        {
            return false;
        }

        return quantity > 0;
    }

    private static bool TryParseCurrentLinePriceUpdate(string input, out decimal unitPrice)
    {
        unitPrice = 0;
        var match = Regex.Match(input, @"(?:price|unit price)\s+(?<value>-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (!match.Success || !decimal.TryParse(match.Groups["value"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out unitPrice))
        {
            return false;
        }

        return unitPrice >= 0;
    }

    private static bool TryParseGoodsReceiptLineQuantityChange(string input, out int lineNumber, out decimal quantity)
    {
        lineNumber = 0;
        quantity = 0;
        var match = Regex.Match(
            input,
            @"(?:change|update)\s+(?:line\s+)?(?<line>\d+)\s+(?:qty|quantity)\s+(?:to\s+)?(?<value>-?\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase);
        if (!match.Success ||
            !int.TryParse(match.Groups["line"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineNumber) ||
            !decimal.TryParse(match.Groups["value"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity))
        {
            return false;
        }

        return lineNumber > 0 && quantity >= 0m;
    }

    private static bool TryParseGoodsReceiptLineBatchChange(string input, out int lineNumber, out string? batchNumber)
    {
        lineNumber = 0;
        batchNumber = null;
        var match = Regex.Match(
            input,
            @"(?:change|update)\s+(?:line\s+)?(?<line>\d+)\s+(?:batch|batch number)\s+(?:to\s+)?(?<value>.+)$",
            RegexOptions.IgnoreCase);
        if (!match.Success || !int.TryParse(match.Groups["line"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineNumber))
        {
            return false;
        }

        batchNumber = NormalizeNullableFreeText(match.Groups["value"].Value);
        return lineNumber > 0;
    }

    private static bool TryParseGoodsReceiptLineSerialChange(string input, out int lineNumber, out List<string> serials)
    {
        lineNumber = 0;
        serials = [];
        var match = Regex.Match(
            input,
            @"(?:change|update)\s+(?:line\s+)?(?<line>\d+)\s+serials?\s+(?:to\s+)?(?<value>.+)$",
            RegexOptions.IgnoreCase);
        if (!match.Success || !int.TryParse(match.Groups["line"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineNumber))
        {
            return false;
        }

        serials = ParseSerialList(match.Groups["value"].Value);
        return lineNumber > 0;
    }

    private static bool TryParseLastLineBatchUpdate(string input, out string? batchNumber)
    {
        batchNumber = null;
        var match = Regex.Match(
            input,
            @"(?:change|update)\s+(?:the\s+)?last\s+line\s+(?:batch|batch number)\s+(?:to\s+)?(?<value>.+)$",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        batchNumber = NormalizeNullableFreeText(match.Groups["value"].Value);
        return true;
    }

    private static bool TryParseLastLineSerialUpdate(string input, out List<string> serials)
    {
        serials = [];
        var match = Regex.Match(
            input,
            @"(?:change|update)\s+(?:the\s+)?last\s+line\s+serials?\s+(?:to\s+)?(?<value>.+)$",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        serials = ParseSerialList(match.Groups["value"].Value);
        return true;
    }

    private static bool LooksLikeReferenceNumber(string input)
        => Regex.IsMatch(input.Trim(), @"\bpr[-\s]?\d+\b", RegexOptions.IgnoreCase) ||
           Regex.IsMatch(input.Trim(), @"^[A-Za-z]{1,4}[-/]?\d+$", RegexOptions.IgnoreCase);

    private static bool LooksLikeNoRequisition(string normalized)
        => normalized.Contains("no requisition", StringComparison.Ordinal) ||
           normalized.Contains("from scratch", StringComparison.Ordinal) ||
           normalized == "no" ||
           normalized == "skip";

    private static bool LooksLikeYesCommand(string normalized)
        => normalized == "yes" || normalized.Contains("use requisition", StringComparison.Ordinal);

    private static bool LooksLikeCancelCommand(string input)
    {
        var normalized = Normalize(input);
        return normalized == "cancel" ||
               normalized == "stop" ||
               normalized == "abort";
    }

    private static bool LooksLikeConfirmCommand(string normalized)
        => normalized == "confirm" || normalized == "yes" || normalized == "add it";

    private static bool LooksLikeRejectCommand(string normalized)
        => normalized == "no" || normalized == "reject" || normalized == "discard";

    private static bool LooksLikeSkipReceiptCommand(string normalized)
        => normalized == "skip" ||
           normalized == "not present" ||
           normalized == "not received" ||
           normalized == "none" ||
           normalized == "not available";

    private static bool LooksLikeFinishCommand(string normalized)
        => normalized == "finish" ||
           normalized == "done" ||
           normalized == "complete" ||
           normalized == "en" ||
           normalized == "end" ||
           normalized == "save" ||
           normalized.Contains("end transaction", StringComparison.Ordinal);

    private static bool LooksLikePostGoodsReceiptCommand(string input)
    {
        var normalized = Normalize(input);
        return normalized == "post" ||
               normalized == "post grn" ||
               normalized == "post goods receipt" ||
               normalized == "post this grn" ||
               normalized == "post this goods receipt" ||
               normalized == "submit grn" ||
               normalized.Contains("post grn", StringComparison.Ordinal) ||
               normalized.Contains("post goods receipt", StringComparison.Ordinal) ||
               normalized.Contains("finalize grn", StringComparison.Ordinal) ||
               normalized.Contains("finalise grn", StringComparison.Ordinal);
    }

    private static bool LooksLikePostStockTransferCommand(string input)
    {
        var normalized = Normalize(input);
        return normalized == "post transfer" ||
               normalized == "post stock transfer" ||
               normalized == "post this transfer" ||
               normalized == "submit transfer" ||
               normalized.Contains("post transfer", StringComparison.Ordinal) ||
               normalized.Contains("post stock transfer", StringComparison.Ordinal) ||
               normalized.Contains("finalize transfer", StringComparison.Ordinal) ||
               normalized.Contains("finalise transfer", StringComparison.Ordinal);
    }

    private static bool LooksLikeResumeCommand(string input)
    {
        var normalized = Normalize(input);
        return normalized.Contains("resume", StringComparison.Ordinal) ||
               normalized.Contains("continue", StringComparison.Ordinal) ||
               normalized.Contains("add line", StringComparison.Ordinal) ||
               normalized.Contains("next item", StringComparison.Ordinal);
    }

    private static bool LooksLikeRemoveLastLineCommand(string input)
    {
        var normalized = Normalize(input);
        return normalized.Contains("remove last line", StringComparison.Ordinal) ||
               normalized.Contains("delete last line", StringComparison.Ordinal);
    }

    private static bool LooksLikeShowLinesCommand(string input)
    {
        var normalized = Normalize(input);
        return normalized == "show lines" ||
               normalized == "show items" ||
               normalized == "list lines" ||
               normalized == "list items" ||
               normalized.Contains("show line", StringComparison.Ordinal) ||
               normalized.Contains("show item", StringComparison.Ordinal);
    }

    private static bool LooksLikeNewPurchaseOrderCommand(string input)
    {
        var normalized = Normalize(input);
        return normalized.Contains("new po", StringComparison.Ordinal) ||
               normalized.Contains("new purchase order", StringComparison.Ordinal) ||
               normalized.Contains("another po", StringComparison.Ordinal) ||
               normalized.Contains("another purchase order", StringComparison.Ordinal) ||
               normalized.Contains("separate po", StringComparison.Ordinal) ||
               normalized.Contains("separate purchase order", StringComparison.Ordinal);
    }

    private static bool LooksLikeApprovePurchaseOrderCommand(string input)
    {
        var normalized = Normalize(input);
        return normalized == "approve" ||
               normalized == "approve po" ||
               normalized == "approve purchase order" ||
               normalized.Contains("approve po", StringComparison.Ordinal) ||
               normalized.Contains("approve purchase order", StringComparison.Ordinal) ||
               normalized.Contains("approve this po", StringComparison.Ordinal) ||
               normalized.Contains("approve this purchase order", StringComparison.Ordinal) ||
               normalized.Contains("finalize po", StringComparison.Ordinal) ||
               normalized.Contains("finalise po", StringComparison.Ordinal) ||
               normalized.Contains("finalize purchase order", StringComparison.Ordinal) ||
               normalized.Contains("finalise purchase order", StringComparison.Ordinal);
    }

    private static void SplitCodeAndName(AssistantLookupOption option, AssistantPurchaseOrderLineDraft draft)
    {
        draft.ItemId = option.Id;
        draft.ItemCode = option.Code;
        draft.ItemName = option.Label;
    }

    private static string? ParseBatchValue(string input)
        => NormalizeNullableFreeText(Regex.Replace(input.Trim(), @"^(batch|batch number)\s*", string.Empty, RegexOptions.IgnoreCase));

    private static List<string> ParseSerialList(string input)
        => NormalizeNullableFreeText(Regex.Replace(input.Trim(), @"^serials?\s*", string.Empty, RegexOptions.IgnoreCase))
            ?.Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
           ?? [];

    private static string? NormalizeNullableFreeText(string input)
    {
        var value = input.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = Normalize(value);
        return normalized is "none" or "skip" or "null" or "blank" ? null : value;
    }

    private static string PurchaseOrderPath(Guid purchaseOrderId) => $"/procurement/purchase-orders/{purchaseOrderId}";
    private static string GoodsReceiptPath(Guid goodsReceiptId) => $"/procurement/goods-receipts/{goodsReceiptId}";
    private static string StockTransferPath(Guid stockTransferId) => $"/inventory/stock-transfers/{stockTransferId}";

    private sealed record AssistantOutcome(
        string Reply,
        AssistantReportRequestDto? ReportRequest = null,
        string? NavigateTo = null,
        bool RefreshCurrentPage = false);
}
