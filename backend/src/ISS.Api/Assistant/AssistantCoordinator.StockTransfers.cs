using System.Text.RegularExpressions;
using System.Text;
using ISS.Api.Security;
using ISS.Domain.Inventory;
using ISS.Domain.MasterData;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Assistant;

public sealed partial class AssistantCoordinator
{
    private async Task<AssistantOutcome?> TryHandlePausedStockTransferAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        if (workflow.Stage != AssistantStockTransferStage.Paused && workflow.Stage != AssistantStockTransferStage.Idle)
        {
            return null;
        }

        if (!HasAnyRole(actor, Roles.Admin, Roles.Inventory))
        {
            return null;
        }

        if (!workflow.CanEditDraft &&
            (LooksLikeResumeCommand(message) ||
             TryParseLastLineQuantityUpdate(message, out _) ||
             TryParseLastLineBatchUpdate(message, out _) ||
             TryParseLastLineSerialUpdate(message, out _) ||
             LooksLikeRemoveLastLineCommand(message) ||
             LooksLikeShowLinesCommand(message) ||
             LooksLikePostStockTransferCommand(message)))
        {
            return BuildReadOnlyStockTransferOutcome(workflow);
        }

        var revisionOutcome = await TryHandleStockTransferLastLineRevisionAsync(session, message, cancellationToken);
        if (revisionOutcome is not null)
        {
            return revisionOutcome;
        }

        if (LooksLikeShowLinesCommand(message))
        {
            return new AssistantOutcome(
                BuildStockTransferLinesOverview(workflow),
                NavigateTo: StockTransferPath(workflow.TransferId!.Value));
        }

        if (workflow.HasDraft && LooksLikePostStockTransferCommand(message))
        {
            if (!await HasStockTransferLinesAsync(workflow.TransferId!.Value, cancellationToken))
            {
                return new AssistantOutcome(
                    $"Transfer {workflow.TransferNumber} does not have any lines yet. Add at least one line before posting it.",
                    NavigateTo: StockTransferPath(workflow.TransferId!.Value));
            }

            workflow.AwaitingPostConfirmation = true;
            return new AssistantOutcome(
                BuildStockTransferPostConfirmationReply(workflow),
                NavigateTo: StockTransferPath(workflow.TransferId!.Value));
        }

        if (LooksLikeResumeCommand(message))
        {
            workflow.Stage = AssistantStockTransferStage.AwaitingItem;
            return new AssistantOutcome(
                $"Transfer {workflow.TransferNumber} is still open. Tell me the next item to move.",
                NavigateTo: StockTransferPath(workflow.TransferId!.Value));
        }

        if (LooksLikeFinishCommand(Normalize(message)))
        {
            return new AssistantOutcome(
                $"Transfer {workflow.TransferNumber} is still open. Review it behind the chat window, add more lines with `resume transfer`, or say `post transfer` when you are ready.",
                NavigateTo: StockTransferPath(workflow.TransferId!.Value));
        }

        return null;
    }

    private async Task<AssistantOutcome> HandleStockTransferWorkflowAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;

        if (!HasAnyRole(actor, Roles.Admin, Roles.Inventory))
        {
            workflow.ResetConversation();
            return new AssistantOutcome("You no longer have Inventory access in this session, so I stopped the guided stock-transfer flow.");
        }

        if (!workflow.CanEditDraft)
        {
            workflow.Stage = AssistantStockTransferStage.Paused;
            workflow.ResetCurrentLine();
            return BuildReadOnlyStockTransferOutcome(workflow);
        }

        if (LooksLikeCancelCommand(message))
        {
            workflow.Stage = workflow.HasDraft ? AssistantStockTransferStage.Paused : AssistantStockTransferStage.Idle;
            workflow.ResetCurrentLine();
            return workflow.HasDraft
                ? new AssistantOutcome(
                    $"I stopped prompting. Draft transfer {workflow.TransferNumber} stays open. Say `resume transfer` when you want to continue.",
                    NavigateTo: StockTransferPath(workflow.TransferId!.Value))
                : new AssistantOutcome("I cancelled the current stock-transfer flow.");
        }

        var revisionOutcome = await TryHandleStockTransferLastLineRevisionAsync(session, message, cancellationToken);
        if (revisionOutcome is not null)
        {
            return revisionOutcome;
        }

        if (LooksLikeShowLinesCommand(message))
        {
            return new AssistantOutcome(
                BuildStockTransferLinesOverview(workflow),
                NavigateTo: workflow.TransferId is null ? null : StockTransferPath(workflow.TransferId.Value));
        }

        if (workflow.HasDraft && LooksLikePostStockTransferCommand(message))
        {
            if (HasPendingStockTransferLine(workflow))
            {
                return new AssistantOutcome(
                    $"There is still a pending transfer line for {workflow.CurrentLine.DisplayLabel}. Finish it or cancel it before posting the transfer.",
                    NavigateTo: StockTransferPath(workflow.TransferId!.Value));
            }

            if (!await HasStockTransferLinesAsync(workflow.TransferId!.Value, cancellationToken))
            {
                return new AssistantOutcome(
                    $"Transfer {workflow.TransferNumber} does not have any lines yet. Add at least one line before posting it.",
                    NavigateTo: StockTransferPath(workflow.TransferId!.Value));
            }

            workflow.AwaitingPostConfirmation = true;
            return new AssistantOutcome(
                BuildStockTransferPostConfirmationReply(workflow),
                NavigateTo: StockTransferPath(workflow.TransferId!.Value));
        }

        return workflow.Stage switch
        {
            AssistantStockTransferStage.AwaitingFromWarehouse => await HandleStockTransferFromWarehouseAsync(workflow, message, provider, cancellationToken),
            AssistantStockTransferStage.AwaitingToWarehouse => await HandleStockTransferToWarehouseAsync(workflow, message, provider, cancellationToken),
            AssistantStockTransferStage.AwaitingItem => await HandleStockTransferItemAsync(workflow, message, provider, cancellationToken),
            AssistantStockTransferStage.AwaitingQuantity => await HandleStockTransferQuantityAsync(session, message, provider, cancellationToken),
            AssistantStockTransferStage.AwaitingBatch => await HandleStockTransferBatchAsync(session, message, cancellationToken),
            AssistantStockTransferStage.AwaitingSerials => await HandleStockTransferSerialsAsync(session, message, cancellationToken),
            AssistantStockTransferStage.Paused => new AssistantOutcome(
                $"Draft transfer {workflow.TransferNumber} is paused. Say `resume transfer`, `show lines`, `change last line qty 5`, or `post transfer`.",
                NavigateTo: StockTransferPath(workflow.TransferId!.Value)),
            _ => new AssistantOutcome("Tell me the source warehouse for the stock transfer."),
        };
    }

    private async Task SyncStockTransferWorkflowAsync(
        AssistantStockTransferWorkflow workflow,
        CancellationToken cancellationToken)
    {
        if (workflow.TransferId is null)
        {
            return;
        }

        var transfer = await dbContext.StockTransfers.AsNoTracking()
            .Where(x => x.Id == workflow.TransferId.Value)
            .Select(x => new { x.Number, x.Status, x.FromWarehouseId, x.ToWarehouseId })
            .FirstOrDefaultAsync(cancellationToken);

        if (transfer is null)
        {
            workflow.ResetConversation();
            return;
        }

        workflow.TransferNumber = transfer.Number;
        workflow.CurrentStatus = transfer.Status;
        workflow.FromWarehouseId ??= transfer.FromWarehouseId;
        workflow.ToWarehouseId ??= transfer.ToWarehouseId;
    }

    private async Task<AssistantOutcome> HandleStockTransferFromWarehouseAsync(
        AssistantStockTransferWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        if (workflow.CandidateFromWarehouses.Count > 0 &&
            TryResolveOptionSelection(message, workflow.CandidateFromWarehouses, out var selectedWarehouse))
        {
            workflow.CandidateFromWarehouses.Clear();
            return await CommitStockTransferFromWarehouseAsync(workflow, selectedWarehouse, cancellationToken);
        }

        var llm = await InterpretAsync("transfer-from-warehouse", message, provider, cancellationToken);
        var warehouseQuery = llm?.WarehouseText ?? message;
        var matches = await FindWarehouseMatchesAsync(warehouseQuery, cancellationToken);
        if (matches.Count == 0)
        {
            return new AssistantOutcome("I could not match that source warehouse. Send the warehouse code or a clearer warehouse name.");
        }

        if (matches.Count == 1)
        {
            return await CommitStockTransferFromWarehouseAsync(workflow, matches[0], cancellationToken);
        }

        workflow.CandidateFromWarehouses.Clear();
        workflow.CandidateFromWarehouses.AddRange(matches);
        return new AssistantOutcome(
            $"I found multiple source warehouses. Reply with the option number or warehouse code:{Environment.NewLine}{FormatOptions(matches)}");
    }

    private Task<AssistantOutcome> CommitStockTransferFromWarehouseAsync(
        AssistantStockTransferWorkflow workflow,
        AssistantLookupOption warehouse,
        CancellationToken _)
    {
        workflow.FromWarehouseId = warehouse.Id;
        workflow.FromWarehouseCode = warehouse.Code;
        workflow.FromWarehouseName = warehouse.Label;
        workflow.Stage = AssistantStockTransferStage.AwaitingToWarehouse;

        return Task.FromResult(new AssistantOutcome(
            $"Source warehouse set to {warehouse.DisplayText}. Now tell me the destination warehouse."));
    }

    private async Task<AssistantOutcome> HandleStockTransferToWarehouseAsync(
        AssistantStockTransferWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        if (workflow.CandidateToWarehouses.Count > 0 &&
            TryResolveOptionSelection(message, workflow.CandidateToWarehouses, out var selectedWarehouse))
        {
            workflow.CandidateToWarehouses.Clear();
            return await CommitStockTransferToWarehouseAsync(workflow, selectedWarehouse, cancellationToken);
        }

        var llm = await InterpretAsync("transfer-to-warehouse", message, provider, cancellationToken);
        var warehouseQuery = llm?.WarehouseText ?? message;
        var matches = await FindWarehouseMatchesAsync(warehouseQuery, cancellationToken);
        if (matches.Count == 0)
        {
            return new AssistantOutcome("I could not match that destination warehouse. Send the warehouse code or a clearer warehouse name.");
        }

        if (matches.Count == 1)
        {
            return await CommitStockTransferToWarehouseAsync(workflow, matches[0], cancellationToken);
        }

        workflow.CandidateToWarehouses.Clear();
        workflow.CandidateToWarehouses.AddRange(matches);
        return new AssistantOutcome(
            $"I found multiple destination warehouses. Reply with the option number or warehouse code:{Environment.NewLine}{FormatOptions(matches)}");
    }

    private async Task<AssistantOutcome> CommitStockTransferToWarehouseAsync(
        AssistantStockTransferWorkflow workflow,
        AssistantLookupOption warehouse,
        CancellationToken cancellationToken)
    {
        if (workflow.FromWarehouseId is null)
        {
            workflow.Stage = AssistantStockTransferStage.AwaitingFromWarehouse;
            return new AssistantOutcome("Tell me the source warehouse first.");
        }

        if (workflow.FromWarehouseId == warehouse.Id)
        {
            return new AssistantOutcome("Source and destination warehouses must be different. Send a different destination warehouse.");
        }

        workflow.ToWarehouseId = warehouse.Id;
        workflow.ToWarehouseCode = warehouse.Code;
        workflow.ToWarehouseName = warehouse.Label;

        if (workflow.TransferId is null)
        {
            var transferId = await inventoryOperationsService.CreateStockTransferAsync(
                workflow.FromWarehouseId.Value,
                warehouse.Id,
                notes: null,
                cancellationToken);

            workflow.TransferId = transferId;
            var transfer = await dbContext.StockTransfers.AsNoTracking()
                .Where(x => x.Id == transferId)
                .Select(x => new { x.Number, x.Status })
                .FirstAsync(cancellationToken);

            workflow.TransferNumber = transfer.Number;
            workflow.CurrentStatus = transfer.Status;
        }

        workflow.Stage = AssistantStockTransferStage.AwaitingItem;
        return new AssistantOutcome(
            $"Transfer {workflow.TransferNumber} is ready from {workflow.FromWarehouseCode} to {warehouse.Code}. I opened it behind the chat box. Tell me the first item to move.",
            NavigateTo: StockTransferPath(workflow.TransferId!.Value));
    }

    private async Task<AssistantOutcome> HandleStockTransferItemAsync(
        AssistantStockTransferWorkflow workflow,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(message);
        if (LooksLikeFinishCommand(normalized))
        {
            workflow.Stage = AssistantStockTransferStage.Paused;
            workflow.ResetCurrentLine();
            return new AssistantOutcome(
                $"I stopped the guided flow. Draft transfer {workflow.TransferNumber} stays open. Say `resume transfer` if you want more lines, or `post transfer` when you are ready.",
                NavigateTo: StockTransferPath(workflow.TransferId!.Value));
        }

        if (workflow.CandidateItems.Count > 0 &&
            TryResolveOptionSelection(message, workflow.CandidateItems, out var selectedItem))
        {
            workflow.CandidateItems.Clear();
            return await CommitStockTransferItemAsync(workflow, selectedItem, cancellationToken);
        }

        var llm = await InterpretAsync("transfer-item", message, provider, cancellationToken);
        if (llm?.Intent == "finish")
        {
            workflow.Stage = AssistantStockTransferStage.Paused;
            workflow.ResetCurrentLine();
            return new AssistantOutcome(
                $"I stopped the guided flow. Draft transfer {workflow.TransferNumber} stays open. Say `resume transfer` if you want more lines, or `post transfer` when you are ready.",
                NavigateTo: StockTransferPath(workflow.TransferId!.Value));
        }

        var itemQuery = llm?.ItemText ?? message;
        var matches = await FindItemMatchesAsync(itemQuery, cancellationToken);
        if (matches.Count == 0)
        {
            return new AssistantOutcome("I could not match that item. Send the item SKU, barcode, or a clearer part of the item name.");
        }

        if (matches.Count == 1)
        {
            return await CommitStockTransferItemAsync(workflow, matches[0], cancellationToken);
        }

        workflow.CandidateItems.Clear();
        workflow.CandidateItems.AddRange(matches);
        return new AssistantOutcome(
            $"I found multiple items. Reply with the option number or item code:{Environment.NewLine}{FormatOptions(matches)}");
    }

    private async Task<AssistantOutcome> CommitStockTransferItemAsync(
        AssistantStockTransferWorkflow workflow,
        AssistantLookupOption itemOption,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.AsNoTracking()
            .Where(x => x.Id == itemOption.Id)
            .Select(x => new { x.Id, x.Sku, x.Name, x.TrackingType, x.DefaultUnitCost })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return new AssistantOutcome("That item is no longer available. Send the item again.");
        }

        workflow.ResetCurrentLine();
        workflow.CurrentLine.ItemId = item.Id;
        workflow.CurrentLine.ItemCode = item.Sku;
        workflow.CurrentLine.ItemName = item.Name;
        workflow.CurrentLine.TrackingType = item.TrackingType;
        workflow.CurrentLine.UnitCost = item.DefaultUnitCost;
        workflow.Stage = AssistantStockTransferStage.AwaitingQuantity;

        return new AssistantOutcome(
            $"Selected {item.Sku} - {item.Name}. How much do you want to move out of {workflow.FromWarehouseCode}? I will use unit cost {item.DefaultUnitCost} unless you change it later.");
    }

    private async Task<AssistantOutcome> HandleStockTransferQuantityAsync(
        AssistantSession session,
        string message,
        AssistantProviderConfigDto? provider,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        if (!workflow.CurrentLine.ItemId.HasValue)
        {
            workflow.Stage = AssistantStockTransferStage.AwaitingItem;
            return new AssistantOutcome("Tell me the item you want to move.");
        }

        var quantity = TryExtractDecimal(message);
        if (quantity is null)
        {
            var llm = await InterpretAsync("transfer-quantity", message, provider, cancellationToken);
            quantity = llm?.Quantity;
        }

        if (quantity is null || quantity <= 0m)
        {
            return new AssistantOutcome("Move quantity must be greater than 0. Send the quantity for this transfer line.");
        }

        workflow.CurrentLine.Quantity = quantity.Value;

        if (workflow.CurrentLine.TrackingType == TrackingType.Batch)
        {
            workflow.Stage = AssistantStockTransferStage.AwaitingBatch;
            return new AssistantOutcome(
                $"Recorded move qty {quantity.Value} for {workflow.CurrentLine.DisplayLabel}. Send the batch number, or say `skip` if you want to leave it blank.");
        }

        if (workflow.CurrentLine.TrackingType == TrackingType.Serial)
        {
            workflow.Stage = AssistantStockTransferStage.AwaitingSerials;
            return new AssistantOutcome(
                $"Recorded move qty {quantity.Value} for {workflow.CurrentLine.DisplayLabel}. Send the serial numbers one per line or comma-separated, or say `skip` if you want to leave them blank for now.");
        }

        return await CommitStockTransferCurrentLineAsync(session, cancellationToken);
    }

    private async Task<AssistantOutcome> HandleStockTransferBatchAsync(
        AssistantSession session,
        string message,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        workflow.CurrentLine.BatchNumber = LooksLikeSkipReceiptCommand(Normalize(message)) ? null : ParseBatchValue(message);
        return await CommitStockTransferCurrentLineAsync(session, cancellationToken);
    }

    private async Task<AssistantOutcome> HandleStockTransferSerialsAsync(
        AssistantSession session,
        string message,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        workflow.CurrentLine.Serials.Clear();
        if (!LooksLikeSkipReceiptCommand(Normalize(message)))
        {
            workflow.CurrentLine.Serials.AddRange(ParseSerialList(message));
        }

        return await CommitStockTransferCurrentLineAsync(session, cancellationToken);
    }

    private async Task<AssistantOutcome> CommitStockTransferCurrentLineAsync(
        AssistantSession session,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        var lineDraft = workflow.CurrentLine;
        if (workflow.TransferId is null || !lineDraft.ItemId.HasValue || !lineDraft.Quantity.HasValue)
        {
            workflow.Stage = AssistantStockTransferStage.AwaitingItem;
            workflow.ResetCurrentLine();
            return new AssistantOutcome("The transfer line is incomplete. Tell me the item you want to move.");
        }

        await inventoryOperationsService.AddStockTransferLineAsync(
            workflow.TransferId.Value,
            lineDraft.ItemId.Value,
            lineDraft.Quantity.Value,
            lineDraft.UnitCost,
            lineDraft.BatchNumber,
            lineDraft.Serials.Count == 0 ? null : lineDraft.Serials.ToList(),
            cancellationToken);

        var latestLine = await ResolveLatestAddedStockTransferLineAsync(session, cancellationToken);
        if (latestLine is not null)
        {
            workflow.AddedLines.Add(latestLine);
        }

        var itemLabel = lineDraft.DisplayLabel;
        var quantity = lineDraft.Quantity.Value;
        var batchNumber = lineDraft.BatchNumber;
        var serials = lineDraft.Serials.ToList();

        workflow.ResetCurrentLine();
        workflow.Stage = AssistantStockTransferStage.AwaitingItem;

        var detailParts = new List<string> { $"qty {quantity}" };
        if (!string.IsNullOrWhiteSpace(batchNumber))
        {
            detailParts.Add($"batch {batchNumber}");
        }

        if (serials.Count > 0)
        {
            detailParts.Add($"serials {string.Join(", ", serials)}");
        }

        return new AssistantOutcome(
            $"Added {itemLabel} to transfer {workflow.TransferNumber} ({string.Join(" | ", detailParts)}). Tell me the next item, say `finish`, or ask me to change/remove the last line.",
            NavigateTo: StockTransferPath(workflow.TransferId.Value),
            RefreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> HandleStockTransferPostConfirmationAsync(
        AssistantSession session,
        AssistantActor actor,
        string message,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        if (!HasAnyRole(actor, Roles.Admin, Roles.Inventory))
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = workflow.HasDraft ? AssistantStockTransferStage.Paused : AssistantStockTransferStage.Idle;
            return new AssistantOutcome("You no longer have Inventory access in this session, so I stopped the stock-transfer posting flow.");
        }

        if (workflow.TransferId is null)
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantStockTransferStage.Idle;
            return new AssistantOutcome("There is no stock transfer draft open to post.");
        }

        if (!workflow.CanEditDraft)
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantStockTransferStage.Paused;
            return BuildReadOnlyStockTransferOutcome(workflow);
        }

        var revisionOutcome = await TryHandleStockTransferLastLineRevisionAsync(session, message, cancellationToken);
        if (revisionOutcome is not null)
        {
            workflow.AwaitingPostConfirmation = false;
            return revisionOutcome;
        }

        if (LooksLikeShowLinesCommand(message))
        {
            return new AssistantOutcome(
                $"{BuildStockTransferLinesOverview(workflow)}{Environment.NewLine}{Environment.NewLine}{BuildStockTransferPostConfirmationReply(workflow)}",
                NavigateTo: StockTransferPath(workflow.TransferId.Value));
        }

        if (LooksLikeResumeCommand(message))
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantStockTransferStage.AwaitingItem;
            return new AssistantOutcome(
                "Posting cancelled for now. Tell me the next item to move.",
                NavigateTo: StockTransferPath(workflow.TransferId.Value));
        }

        var normalized = Normalize(message);
        if (LooksLikeConfirmCommand(normalized))
        {
            await inventoryOperationsService.PostStockTransferAsync(workflow.TransferId.Value, cancellationToken);
            await SyncStockTransferWorkflowAsync(workflow, cancellationToken);
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantStockTransferStage.Paused;
            return new AssistantOutcome(
                $"Transfer {workflow.TransferNumber} has been posted. The document behind the chat is now read-only.",
                NavigateTo: StockTransferPath(workflow.TransferId.Value),
                RefreshCurrentPage: true);
        }

        if (LooksLikeRejectCommand(normalized) || LooksLikeCancelCommand(message))
        {
            workflow.AwaitingPostConfirmation = false;
            workflow.Stage = AssistantStockTransferStage.Paused;
            return new AssistantOutcome(
                $"Keeping transfer {workflow.TransferNumber} as a draft. Say `post transfer` when you want to post it.",
                NavigateTo: StockTransferPath(workflow.TransferId.Value));
        }

        return new AssistantOutcome(
            BuildStockTransferPostConfirmationReply(workflow),
            NavigateTo: StockTransferPath(workflow.TransferId.Value));
    }

    private static AssistantOutcome BuildReadOnlyStockTransferOutcome(AssistantStockTransferWorkflow workflow)
    {
        var status = workflow.CurrentStatus?.ToString() ?? "Draft";
        return new AssistantOutcome(
            $"Transfer {workflow.TransferNumber} is {status} and cannot be edited through the assistant any more. Open it if you need to review it, or start a new transfer if you need another move.",
            NavigateTo: workflow.TransferId is null ? null : StockTransferPath(workflow.TransferId.Value));
    }

    private static string BuildStockTransferLinesOverview(AssistantStockTransferWorkflow workflow)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Transfer {workflow.TransferNumber ?? "(draft)"} lines:");

        if (workflow.AddedLines.Count == 0)
        {
            builder.AppendLine("- No assistant-added lines tracked in this session yet.");
            return builder.ToString().TrimEnd();
        }

        foreach (var line in workflow.AddedLines)
        {
            builder.Append("- ")
                .Append(line.ItemCode)
                .Append(" - ")
                .Append(line.ItemName)
                .Append(" | qty ")
                .Append(line.Quantity)
                .Append(" | unit cost ")
                .Append(line.UnitCost);

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

    private static string BuildStockTransferPostConfirmationReply(AssistantStockTransferWorkflow workflow)
        => $"{BuildStockTransferLinesOverview(workflow)}{Environment.NewLine}{Environment.NewLine}Reply `confirm` to post it, say `resume transfer` to add more lines, or say `cancel` to keep it as a draft.";

    private bool HasPendingStockTransferLine(AssistantStockTransferWorkflow workflow)
    {
        var currentLine = workflow.CurrentLine;
        return currentLine.ItemId.HasValue ||
               currentLine.Quantity.HasValue ||
               !string.IsNullOrWhiteSpace(currentLine.BatchNumber) ||
               currentLine.Serials.Count > 0;
    }

    private async Task<bool> HasStockTransferLinesAsync(Guid stockTransferId, CancellationToken cancellationToken)
    {
        return await dbContext.StockTransfers.AsNoTracking()
            .Where(x => x.Id == stockTransferId)
            .Select(x => x.Lines.Any())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AssistantStockTransferLineSnapshot?> ResolveLatestAddedStockTransferLineAsync(
        AssistantSession session,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        var lineDraft = workflow.CurrentLine;
        var knownIds = workflow.AddedLines.Select(x => x.LineId).ToHashSet();

        var lines = await dbContext.StockTransfers.AsNoTracking()
            .Where(x => x.Id == workflow.TransferId)
            .SelectMany(x => x.Lines)
            .Select(x => new
            {
                x.Id,
                x.ItemId,
                x.Quantity,
                x.UnitCost,
                x.BatchNumber,
                Serials = x.Serials.Select(s => s.SerialNumber).ToList()
            })
            .ToListAsync(cancellationToken);

        var latest = lines
            .Where(x => !knownIds.Contains(x.Id)
                        && x.ItemId == lineDraft.ItemId
                        && x.Quantity == lineDraft.Quantity
                        && x.UnitCost == lineDraft.UnitCost
                        && string.Equals(x.BatchNumber ?? string.Empty, lineDraft.BatchNumber ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                        && x.Serials.SequenceEqual(lineDraft.Serials, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        return latest is null
            ? null
            : new AssistantStockTransferLineSnapshot(
                latest.Id,
                latest.ItemId,
                lineDraft.ItemCode ?? string.Empty,
                lineDraft.ItemName ?? string.Empty,
                latest.Quantity,
                latest.UnitCost,
                latest.BatchNumber,
                latest.Serials);
    }

    private async Task<AssistantOutcome> UpdateLastTransferLineQuantityAsync(
        AssistantSession session,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        var lastLine = workflow.AddedLines.LastOrDefault();
        if (lastLine is null)
        {
            return new AssistantOutcome("There is no assistant-added transfer line to change yet.");
        }

        await inventoryOperationsService.UpdateStockTransferLineAsync(
            workflow.TransferId!.Value,
            lastLine.LineId,
            quantity,
            lastLine.UnitCost,
            lastLine.BatchNumber,
            lastLine.Serials,
            cancellationToken);

        workflow.AddedLines[^1] = lastLine with { Quantity = quantity };
        workflow.Stage = workflow.Stage == AssistantStockTransferStage.Paused
            ? AssistantStockTransferStage.Paused
            : AssistantStockTransferStage.AwaitingItem;

        return new AssistantOutcome(
            $"Updated the last transfer line quantity to {quantity}.",
            NavigateTo: StockTransferPath(workflow.TransferId.Value),
            RefreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> UpdateLastTransferLineBatchAsync(
        AssistantSession session,
        string? batchNumber,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        var lastLine = workflow.AddedLines.LastOrDefault();
        if (lastLine is null)
        {
            return new AssistantOutcome("There is no assistant-added transfer line to change yet.");
        }

        await inventoryOperationsService.UpdateStockTransferLineAsync(
            workflow.TransferId!.Value,
            lastLine.LineId,
            lastLine.Quantity,
            lastLine.UnitCost,
            batchNumber,
            lastLine.Serials,
            cancellationToken);

        workflow.AddedLines[^1] = lastLine with { BatchNumber = batchNumber };
        workflow.Stage = workflow.Stage == AssistantStockTransferStage.Paused
            ? AssistantStockTransferStage.Paused
            : AssistantStockTransferStage.AwaitingItem;

        return new AssistantOutcome(
            $"Updated the last transfer line batch to {(string.IsNullOrWhiteSpace(batchNumber) ? "blank" : batchNumber)}.",
            NavigateTo: StockTransferPath(workflow.TransferId.Value),
            RefreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> UpdateLastTransferLineSerialsAsync(
        AssistantSession session,
        IReadOnlyList<string> serials,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        var lastLine = workflow.AddedLines.LastOrDefault();
        if (lastLine is null)
        {
            return new AssistantOutcome("There is no assistant-added transfer line to change yet.");
        }

        await inventoryOperationsService.UpdateStockTransferLineAsync(
            workflow.TransferId!.Value,
            lastLine.LineId,
            lastLine.Quantity,
            lastLine.UnitCost,
            lastLine.BatchNumber,
            serials,
            cancellationToken);

        workflow.AddedLines[^1] = lastLine with { Serials = serials.ToList() };
        workflow.Stage = workflow.Stage == AssistantStockTransferStage.Paused
            ? AssistantStockTransferStage.Paused
            : AssistantStockTransferStage.AwaitingItem;

        return new AssistantOutcome(
            serials.Count == 0
                ? "Cleared the last transfer line serials."
                : $"Updated the last transfer line serials to {string.Join(", ", serials)}.",
            NavigateTo: StockTransferPath(workflow.TransferId.Value),
            RefreshCurrentPage: true);
    }

    private async Task<AssistantOutcome> RemoveLastTransferLineAsync(
        AssistantSession session,
        CancellationToken cancellationToken)
    {
        var workflow = session.StockTransfer;
        var lastLine = workflow.AddedLines.LastOrDefault();
        if (lastLine is null)
        {
            return new AssistantOutcome("There is no assistant-added transfer line to remove yet.");
        }

        await inventoryOperationsService.RemoveStockTransferLineAsync(
            workflow.TransferId!.Value,
            lastLine.LineId,
            cancellationToken);

        workflow.AddedLines.RemoveAt(workflow.AddedLines.Count - 1);
        workflow.Stage = workflow.Stage == AssistantStockTransferStage.Paused
            ? AssistantStockTransferStage.Paused
            : AssistantStockTransferStage.AwaitingItem;

        return new AssistantOutcome(
            $"Removed the last transfer line: {lastLine.ItemCode} - {lastLine.ItemName}.",
            NavigateTo: StockTransferPath(workflow.TransferId.Value),
            RefreshCurrentPage: true);
    }

    private async Task<AssistantOutcome?> TryHandleStockTransferLastLineRevisionAsync(
        AssistantSession session,
        string message,
        CancellationToken cancellationToken)
    {
        if (TryParseLastLineQuantityUpdate(message, out var quantity))
        {
            return await UpdateLastTransferLineQuantityAsync(session, quantity, cancellationToken);
        }

        if (TryParseLastLineBatchUpdate(message, out var batchNumber))
        {
            return await UpdateLastTransferLineBatchAsync(session, batchNumber, cancellationToken);
        }

        if (TryParseLastLineSerialUpdate(message, out var serials))
        {
            return await UpdateLastTransferLineSerialsAsync(session, serials, cancellationToken);
        }

        if (LooksLikeRemoveLastLineCommand(message))
        {
            return await RemoveLastTransferLineAsync(session, cancellationToken);
        }

        return null;
    }
}
