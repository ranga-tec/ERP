using System.Collections.Concurrent;
using ISS.Domain.Inventory;
using ISS.Domain.MasterData;
using ISS.Domain.Procurement;

namespace ISS.Api.Assistant;

public sealed record AssistantProviderConfigDto(string? Kind, string? BaseUrl, string? Model, string? ApiKey);

public sealed record AssistantChatRequest(Guid? SessionId, string Message, Guid? ProviderProfileId, AssistantProviderConfigDto? Provider);

public sealed record AssistantPolicyDto(bool IsEnabled, bool AllowUserManagedProviders, IReadOnlyList<string> AllowedRoles);

public sealed record AssistantUserPreferenceDto(bool AssistantEnabled, Guid? ActiveProviderProfileId);

public sealed record AssistantProviderProfileDto(
    Guid Id,
    string Name,
    string Kind,
    string BaseUrl,
    string Model,
    bool HasApiKey,
    bool IsActive,
    DateTimeOffset UpdatedAt);

public sealed record AssistantSettingsDto(
    bool CanManagePolicy,
    bool CanManageProviders,
    bool IsAllowed,
    string? DisabledReason,
    AssistantPolicyDto Policy,
    AssistantUserPreferenceDto Preference,
    IReadOnlyList<AssistantProviderProfileDto> Providers,
    IReadOnlyList<string> AvailableRoles,
    IReadOnlyList<string> UserRoles);

public sealed record AssistantProviderProfileUpsertRequest(
    string Name,
    string Kind,
    string? BaseUrl,
    string? Model,
    string? ApiKey,
    bool ActivateAfterSave);

public sealed record AssistantProviderTestRequest(string Kind, string? BaseUrl, string? Model, string? ApiKey);

public sealed record AssistantConnectionTestDto(bool Success, string Message);

public sealed record AssistantModelOptionDto(string Id, string Label);

public sealed record AssistantMessageDto(string Role, string Content, DateTimeOffset OccurredAt);

public sealed record AssistantStatusDto(string Mode, string Title, string Summary);

public sealed record AssistantPurchaseOrderDraftDto(
    Guid Id,
    string Number,
    string Status,
    string SupplierCode,
    string SupplierName,
    int LineCount,
    decimal Total,
    string Path,
    bool CreatedFromRequisition);

public sealed record AssistantGoodsReceiptDraftDto(
    Guid Id,
    string Number,
    string Status,
    string PurchaseOrderNumber,
    string WarehouseCode,
    string WarehouseName,
    int LineCount,
    decimal PlannedQuantity,
    int RemainingLineCount,
    string Path);

public sealed record AssistantStockTransferDraftDto(
    Guid Id,
    string Number,
    string Status,
    string FromWarehouseCode,
    string FromWarehouseName,
    string ToWarehouseCode,
    string ToWarehouseName,
    int LineCount,
    decimal TotalQuantity,
    string Path);

public sealed record AssistantReportRequestDto(string Kind, string Title, string ApiPath, string OpenPath, string? Summary);

public sealed record AssistantChatResponse(
    Guid SessionId,
    IReadOnlyList<AssistantMessageDto> Messages,
    AssistantStatusDto Status,
    AssistantPurchaseOrderDraftDto? PurchaseOrderDraft,
    AssistantGoodsReceiptDraftDto? GoodsReceiptDraft,
    AssistantStockTransferDraftDto? StockTransferDraft,
    AssistantReportRequestDto? ReportRequest,
    string? NavigateTo,
    bool RefreshCurrentPage);

internal sealed record AssistantActor(Guid UserId, HashSet<string> Roles)
{
    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

internal sealed record AssistantInterpretation(
    string? Intent,
    string? SupplierText,
    string? ItemText,
    string? WarehouseText,
    string? ReferenceNumber,
    decimal? Quantity,
    decimal? UnitPrice);

internal sealed record AssistantResolvedProvider(
    Guid? ProfileId,
    string Name,
    string Kind,
    string BaseUrl,
    string Model,
    string? ApiKey);

internal sealed record AssistantLookupOption(Guid Id, string Code, string Label)
{
    public string DisplayText => $"{Code} - {Label}";
}

internal sealed record AssistantTranscriptMessage(string Role, string Content, DateTimeOffset OccurredAt);

internal enum AssistantPurchaseOrderStage
{
    Idle = 0,
    AwaitingRequisitionChoice = 1,
    AwaitingRequisitionNumber = 2,
    AwaitingSupplier = 3,
    AwaitingItem = 4,
    AwaitingQuantity = 5,
    AwaitingUnitPrice = 6,
    AwaitingLineConfirmation = 7,
    Paused = 8,
}

internal enum AssistantGoodsReceiptStage
{
    Idle = 0,
    AwaitingPurchaseOrder = 1,
    AwaitingWarehouse = 2,
    AwaitingQuantity = 3,
    AwaitingBatch = 4,
    AwaitingSerials = 5,
    AwaitingVerification = 6,
    Paused = 7,
}

internal enum AssistantStockTransferStage
{
    Idle = 0,
    AwaitingFromWarehouse = 1,
    AwaitingToWarehouse = 2,
    AwaitingItem = 3,
    AwaitingQuantity = 4,
    AwaitingBatch = 5,
    AwaitingSerials = 6,
    Paused = 7,
}

internal sealed class AssistantPurchaseOrderLineDraft
{
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }

    public string DisplayLabel => string.IsNullOrWhiteSpace(ItemCode)
        ? (ItemName ?? "(item)")
        : string.IsNullOrWhiteSpace(ItemName)
            ? ItemCode!
            : $"{ItemCode} - {ItemName}";

    public void Reset()
    {
        ItemId = null;
        ItemCode = null;
        ItemName = null;
        Quantity = null;
        UnitPrice = null;
    }
}

internal sealed record AssistantPurchaseOrderLineSnapshot(Guid LineId, Guid ItemId, string ItemCode, string ItemName, decimal Quantity, decimal UnitPrice);

internal sealed class AssistantGoodsReceiptPlanLine
{
    public int DisplayIndex { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public TrackingType TrackingType { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal PreviouslyReceivedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal? PlannedQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public List<string> Serials { get; } = [];

    public bool HasPlannedReceipt => PlannedQuantity is > 0m;
    public bool IsAnswered => PlannedQuantity.HasValue;
    public string DisplayLabel => $"{ItemCode} - {ItemName}";
}

internal sealed class AssistantStockTransferLineDraft
{
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public TrackingType TrackingType { get; set; }
    public decimal UnitCost { get; set; }
    public decimal? Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public List<string> Serials { get; } = [];

    public string DisplayLabel => string.IsNullOrWhiteSpace(ItemCode)
        ? (ItemName ?? "(item)")
        : string.IsNullOrWhiteSpace(ItemName)
            ? ItemCode!
            : $"{ItemCode} - {ItemName}";

    public void Reset()
    {
        ItemId = null;
        ItemCode = null;
        ItemName = null;
        TrackingType = TrackingType.None;
        UnitCost = 0m;
        Quantity = null;
        BatchNumber = null;
        Serials.Clear();
    }
}

internal sealed record AssistantStockTransferLineSnapshot(
    Guid LineId,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    decimal Quantity,
    decimal UnitCost,
    string? BatchNumber,
    IReadOnlyList<string> Serials);

internal sealed class AssistantGoodsReceiptWorkflow
{
    public AssistantGoodsReceiptStage Stage { get; set; } = AssistantGoodsReceiptStage.Idle;
    public Guid? GoodsReceiptId { get; set; }
    public string? GoodsReceiptNumber { get; set; }
    public GoodsReceiptStatus? CurrentStatus { get; set; }
    public bool AwaitingPostConfirmation { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
    public int CurrentLineIndex { get; set; }
    public List<AssistantLookupOption> CandidatePurchaseOrders { get; } = [];
    public List<AssistantLookupOption> CandidateWarehouses { get; } = [];
    public List<AssistantGoodsReceiptPlanLine> Lines { get; } = [];

    public bool IsActive => Stage != AssistantGoodsReceiptStage.Idle && Stage != AssistantGoodsReceiptStage.Paused;
    public bool HasDraft => GoodsReceiptId.HasValue;
    public bool CanEditDraft => !CurrentStatus.HasValue || CurrentStatus == GoodsReceiptStatus.Draft;

    public AssistantGoodsReceiptPlanLine? CurrentLine =>
        CurrentLineIndex >= 0 && CurrentLineIndex < Lines.Count ? Lines[CurrentLineIndex] : null;

    public void ResetTransientSelections()
    {
        CandidatePurchaseOrders.Clear();
        CandidateWarehouses.Clear();
    }

    public void ResetConversation()
    {
        Stage = AssistantGoodsReceiptStage.Idle;
        GoodsReceiptId = null;
        GoodsReceiptNumber = null;
        CurrentStatus = null;
        AwaitingPostConfirmation = false;
        PurchaseOrderId = null;
        PurchaseOrderNumber = null;
        WarehouseId = null;
        WarehouseCode = null;
        WarehouseName = null;
        CurrentLineIndex = 0;
        CandidatePurchaseOrders.Clear();
        CandidateWarehouses.Clear();
        Lines.Clear();
    }
}

internal sealed class AssistantStockTransferWorkflow
{
    public AssistantStockTransferStage Stage { get; set; } = AssistantStockTransferStage.Idle;
    public Guid? TransferId { get; set; }
    public string? TransferNumber { get; set; }
    public StockTransferStatus? CurrentStatus { get; set; }
    public bool AwaitingPostConfirmation { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public string? FromWarehouseCode { get; set; }
    public string? FromWarehouseName { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public string? ToWarehouseCode { get; set; }
    public string? ToWarehouseName { get; set; }
    public List<AssistantLookupOption> CandidateFromWarehouses { get; } = [];
    public List<AssistantLookupOption> CandidateToWarehouses { get; } = [];
    public List<AssistantLookupOption> CandidateItems { get; } = [];
    public AssistantStockTransferLineDraft CurrentLine { get; } = new();
    public List<AssistantStockTransferLineSnapshot> AddedLines { get; } = [];

    public bool IsActive => Stage != AssistantStockTransferStage.Idle && Stage != AssistantStockTransferStage.Paused;
    public bool HasDraft => TransferId.HasValue;
    public bool CanEditDraft => !CurrentStatus.HasValue || CurrentStatus == StockTransferStatus.Draft;

    public void ResetCurrentLine()
    {
        CandidateItems.Clear();
        CurrentLine.Reset();
    }

    public void ResetConversation()
    {
        Stage = AssistantStockTransferStage.Idle;
        TransferId = null;
        TransferNumber = null;
        CurrentStatus = null;
        AwaitingPostConfirmation = false;
        FromWarehouseId = null;
        FromWarehouseCode = null;
        FromWarehouseName = null;
        ToWarehouseId = null;
        ToWarehouseCode = null;
        ToWarehouseName = null;
        CandidateFromWarehouses.Clear();
        CandidateToWarehouses.Clear();
        CandidateItems.Clear();
        CurrentLine.Reset();
        AddedLines.Clear();
    }
}

internal sealed class AssistantPurchaseOrderWorkflow
{
    public AssistantPurchaseOrderStage Stage { get; set; } = AssistantPurchaseOrderStage.Idle;
    public Guid? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public PurchaseOrderStatus? CurrentStatus { get; set; }
    public Guid? PurchaseRequisitionId { get; set; }
    public string? PurchaseRequisitionNumber { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public bool CreatedFromRequisition { get; set; }
    public bool AwaitingApprovalConfirmation { get; set; }
    public List<AssistantLookupOption> CandidateSuppliers { get; } = [];
    public List<AssistantLookupOption> CandidateItems { get; } = [];
    public AssistantPurchaseOrderLineDraft CurrentLine { get; } = new();
    public List<AssistantPurchaseOrderLineSnapshot> AddedLines { get; } = [];

    public bool IsActive => Stage != AssistantPurchaseOrderStage.Idle && Stage != AssistantPurchaseOrderStage.Paused;
    public bool HasDraft => PurchaseOrderId.HasValue;
    public bool CanEditDraft => !CurrentStatus.HasValue || CurrentStatus == PurchaseOrderStatus.Draft;

    public void ResetCurrentLine()
    {
        CandidateItems.Clear();
        CurrentLine.Reset();
    }

    public void ResetConversation()
    {
        Stage = AssistantPurchaseOrderStage.Idle;
        PurchaseOrderId = null;
        PurchaseOrderNumber = null;
        CurrentStatus = null;
        PurchaseRequisitionId = null;
        PurchaseRequisitionNumber = null;
        SupplierId = null;
        SupplierCode = null;
        SupplierName = null;
        CreatedFromRequisition = false;
        AwaitingApprovalConfirmation = false;
        CandidateSuppliers.Clear();
        CandidateItems.Clear();
        CurrentLine.Reset();
        AddedLines.Clear();
    }
}

internal sealed class AssistantSession
{
    public AssistantSession(Guid sessionId, Guid userId)
    {
        SessionId = sessionId;
        UserId = userId;
    }

    public Guid SessionId { get; }
    public Guid UserId { get; }
    public AssistantPurchaseOrderWorkflow PurchaseOrder { get; } = new();
    public AssistantGoodsReceiptWorkflow GoodsReceipt { get; } = new();
    public AssistantStockTransferWorkflow StockTransfer { get; } = new();
    public List<AssistantTranscriptMessage> Transcript { get; } = [];
    public SemaphoreSlim Gate { get; } = new(1, 1);
}

public sealed class AssistantSessionStore
{
    private readonly ConcurrentDictionary<Guid, AssistantSession> sessions = new();

    internal AssistantSession GetOrCreate(Guid userId, Guid? sessionId)
    {
        var resolvedSessionId = sessionId.GetValueOrDefault(Guid.NewGuid());
        return sessions.AddOrUpdate(
            resolvedSessionId,
            _ => new AssistantSession(resolvedSessionId, userId),
            (_, existing) => existing.UserId == userId ? existing : new AssistantSession(resolvedSessionId, userId));
    }
}
