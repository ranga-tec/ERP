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
        new(ServiceMaterialRequisitionVoid, "Service / Material Requisitions", "Void", "Void MRNs", "Void draft material requisitions.")
    ];

    public static readonly string[] AllKeys = All.Select(x => x.Key).ToArray();
}
