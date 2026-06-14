namespace ISS.Api.Security;

public sealed record AppPermissionDefinition(string Key, string Module, string Action, string Label, string Description);

public static class AppPermissions
{
    public const string PettyCashIouView = "Finance.PettyCashIou.View";
    public const string PettyCashIouCreate = "Finance.PettyCashIou.Create";
    public const string PettyCashIouSubmit = "Finance.PettyCashIou.Submit";
    public const string PettyCashIouApprove = "Finance.PettyCashIou.Approve";
    public const string PettyCashIouReject = "Finance.PettyCashIou.Reject";
    public const string PettyCashIouRelease = "Finance.PettyCashIou.Release";
    public const string PettyCashIouSettle = "Finance.PettyCashIou.Settle";
    public const string ServiceExpenseClaimView = "Service.ExpenseClaim.View";
    public const string ServiceExpenseClaimCreate = "Service.ExpenseClaim.Create";
    public const string ServiceExpenseClaimEdit = "Service.ExpenseClaim.Edit";
    public const string ServiceExpenseClaimSubmit = "Service.ExpenseClaim.Submit";
    public const string ServiceExpenseClaimApprove = "Service.ExpenseClaim.Approve";
    public const string ServiceExpenseClaimReject = "Service.ExpenseClaim.Reject";
    public const string ServiceExpenseClaimSettle = "Service.ExpenseClaim.Settle";
    public const string ServiceExpenseClaimConvert = "Service.ExpenseClaim.Convert";
    public const string ServiceMaterialRequisitionView = "Service.MaterialRequisition.View";
    public const string ServiceMaterialRequisitionCreate = "Service.MaterialRequisition.Create";
    public const string ServiceMaterialRequisitionEdit = "Service.MaterialRequisition.Edit";
    public const string ServiceMaterialRequisitionPost = "Service.MaterialRequisition.Post";
    public const string ServiceMaterialRequisitionVoid = "Service.MaterialRequisition.Void";
    public const string ServiceDailySheetSubmit = "Service.DailySheet.Submit";
    public const string ServiceDailySheetApprove = "Service.DailySheet.Approve";
    public const string ServiceDailySheetReject = "Service.DailySheet.Reject";
    public const string ServiceJobAssignmentCreate = "Service.JobAssignment.Create";
    public const string ServiceJobAssignmentApprove = "Service.JobAssignment.Approve";
    public const string ServiceJobAssignmentReject = "Service.JobAssignment.Reject";
    public const string ServiceWorkOrderTimeEntrySubmit = "Service.WorkOrderTimeEntry.Submit";
    public const string ServiceWorkOrderTimeEntryApprove = "Service.WorkOrderTimeEntry.Approve";
    public const string ServiceWorkOrderTimeEntryReject = "Service.WorkOrderTimeEntry.Reject";
    public const string ServiceEstimateView = "Service.Estimate.View";
    public const string ServiceEstimateCreate = "Service.Estimate.Create";
    public const string ServiceEstimateEdit = "Service.Estimate.Edit";
    public const string ServiceEstimateApprove = "Service.Estimate.Approve";
    public const string ServiceEstimateReject = "Service.Estimate.Reject";
    public const string ServiceEstimateSend = "Service.Estimate.Send";
    public const string ServiceEstimateRevise = "Service.Estimate.Revise";
    public const string ProcurementPurchaseRequisitionView = "Procurement.PurchaseRequisition.View";
    public const string ProcurementPurchaseRequisitionCreate = "Procurement.PurchaseRequisition.Create";
    public const string ProcurementPurchaseRequisitionEdit = "Procurement.PurchaseRequisition.Edit";
    public const string ProcurementPurchaseRequisitionSubmit = "Procurement.PurchaseRequisition.Submit";
    public const string ProcurementPurchaseRequisitionApprove = "Procurement.PurchaseRequisition.Approve";
    public const string ProcurementPurchaseRequisitionReject = "Procurement.PurchaseRequisition.Reject";
    public const string ProcurementPurchaseRequisitionCancel = "Procurement.PurchaseRequisition.Cancel";
    public const string ProcurementPurchaseRequisitionConvert = "Procurement.PurchaseRequisition.Convert";

    public static readonly AppPermissionDefinition[] All =
    [
        new(PettyCashIouView, "Finance / Petty Cash IOU", "View", "View IOUs", "Open and review petty-cash IOU requests."),
        new(PettyCashIouCreate, "Finance / Petty Cash IOU", "Create", "Create IOUs", "Create petty-cash IOU requests."),
        new(PettyCashIouSubmit, "Finance / Petty Cash IOU", "Submit", "Submit IOUs", "Submit IOUs for approval."),
        new(PettyCashIouApprove, "Finance / Petty Cash IOU", "Approve", "Approve IOUs", "Approve submitted IOU requests."),
        new(PettyCashIouReject, "Finance / Petty Cash IOU", "Reject", "Reject IOUs", "Reject submitted IOU requests."),
        new(PettyCashIouRelease, "Finance / Petty Cash IOU", "Release", "Release cash", "Release approved petty cash to the requester."),
        new(PettyCashIouSettle, "Finance / Petty Cash IOU", "Settle", "Settle IOUs", "Record IOU settlement/accounting."),
        new(ServiceExpenseClaimView, "Service / Expense Claims", "View", "View claims", "Open and review service expense claims."),
        new(ServiceExpenseClaimCreate, "Service / Expense Claims", "Create", "Create claims", "Create service expense claims against job orders."),
        new(ServiceExpenseClaimEdit, "Service / Expense Claims", "Edit", "Edit claims", "Add, update, or remove draft service expense claim lines."),
        new(ServiceExpenseClaimSubmit, "Service / Expense Claims", "Submit", "Submit claims", "Submit service expense claims for finance approval."),
        new(ServiceExpenseClaimApprove, "Service / Expense Claims", "Approve", "Approve claims", "Approve submitted service expense claims."),
        new(ServiceExpenseClaimReject, "Service / Expense Claims", "Reject", "Reject claims", "Reject submitted service expense claims."),
        new(ServiceExpenseClaimSettle, "Service / Expense Claims", "Settle", "Settle claims", "Record settlement for approved service expense claims."),
        new(ServiceExpenseClaimConvert, "Service / Expense Claims", "Convert", "Convert billable lines", "Convert approved billable expense lines to service estimates."),
        new(ServiceMaterialRequisitionView, "Service / Material Requisitions", "View", "View MRNs", "Open and review material requisitions."),
        new(ServiceMaterialRequisitionCreate, "Service / Material Requisitions", "Create", "Create MRNs", "Create material requisitions for service jobs."),
        new(ServiceMaterialRequisitionEdit, "Service / Material Requisitions", "Edit", "Edit MRNs", "Add, update, or remove draft material requisition lines."),
        new(ServiceMaterialRequisitionPost, "Service / Material Requisitions", "Post", "Post MRNs", "Post material requisitions and issue stock to service jobs."),
        new(ServiceMaterialRequisitionVoid, "Service / Material Requisitions", "Void", "Void MRNs", "Void draft material requisitions."),
        new(ServiceDailySheetSubmit, "Service / Daily Sheets", "Submit", "Submit daily sheets", "Submit service job daily sheets for supervisor approval."),
        new(ServiceDailySheetApprove, "Service / Daily Sheets", "Approve", "Approve daily sheets", "Approve submitted service job daily sheets."),
        new(ServiceDailySheetReject, "Service / Daily Sheets", "Reject", "Reject daily sheets", "Reject submitted service job daily sheets."),
        new(ServiceJobAssignmentCreate, "Service / Job Assignments", "Create", "Create assignments", "Create labor/technician assignment records."),
        new(ServiceJobAssignmentApprove, "Service / Job Assignments", "Approve", "Approve assignments", "Approve pending labor/technician assignments."),
        new(ServiceJobAssignmentReject, "Service / Job Assignments", "Reject", "Reject assignments", "Reject pending labor/technician assignments."),
        new(ServiceWorkOrderTimeEntrySubmit, "Service / Work Orders", "Submit", "Submit time entries", "Submit labor time entries for approval."),
        new(ServiceWorkOrderTimeEntryApprove, "Service / Work Orders", "Approve", "Approve time entries", "Approve submitted labor time entries."),
        new(ServiceWorkOrderTimeEntryReject, "Service / Work Orders", "Reject", "Reject time entries", "Reject submitted labor time entries."),
        new(ServiceEstimateView, "Service / Estimates", "View", "View estimates", "Open and review service estimates."),
        new(ServiceEstimateCreate, "Service / Estimates", "Create", "Create estimates", "Create service estimates."),
        new(ServiceEstimateEdit, "Service / Estimates", "Edit", "Edit estimates", "Update service estimate headers and lines."),
        new(ServiceEstimateApprove, "Service / Estimates", "Approve", "Approve estimates", "Approve draft service estimates."),
        new(ServiceEstimateReject, "Service / Estimates", "Reject", "Reject estimates", "Reject draft service estimates."),
        new(ServiceEstimateSend, "Service / Estimates", "Send", "Send estimates", "Send service estimates to customers."),
        new(ServiceEstimateRevise, "Service / Estimates", "Revise", "Revise estimates", "Create revisions from existing service estimates."),
        new(ProcurementPurchaseRequisitionView, "Procurement / Purchase Requisitions", "View", "View PRs", "Open and review purchase requisitions."),
        new(ProcurementPurchaseRequisitionCreate, "Procurement / Purchase Requisitions", "Create", "Create PRs", "Create purchase requisitions."),
        new(ProcurementPurchaseRequisitionEdit, "Procurement / Purchase Requisitions", "Edit", "Edit PRs", "Add, update, or remove draft purchase requisition lines."),
        new(ProcurementPurchaseRequisitionSubmit, "Procurement / Purchase Requisitions", "Submit", "Submit PRs", "Submit purchase requisitions for approval."),
        new(ProcurementPurchaseRequisitionApprove, "Procurement / Purchase Requisitions", "Approve", "Approve PRs", "Approve submitted purchase requisitions."),
        new(ProcurementPurchaseRequisitionReject, "Procurement / Purchase Requisitions", "Reject", "Reject PRs", "Reject submitted purchase requisitions."),
        new(ProcurementPurchaseRequisitionCancel, "Procurement / Purchase Requisitions", "Cancel", "Cancel PRs", "Cancel draft or submitted purchase requisitions."),
        new(ProcurementPurchaseRequisitionConvert, "Procurement / Purchase Requisitions", "Convert", "Convert to PO", "Convert approved purchase requisitions to purchase orders.")
    ];

    public static readonly string[] AllKeys = All.Select(x => x.Key).ToArray();
}
