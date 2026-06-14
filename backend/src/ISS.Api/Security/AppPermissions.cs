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
    public const string InventoryStockAdjustmentView = "Inventory.StockAdjustment.View";
    public const string InventoryStockAdjustmentCreate = "Inventory.StockAdjustment.Create";
    public const string InventoryStockAdjustmentEdit = "Inventory.StockAdjustment.Edit";
    public const string InventoryStockAdjustmentPost = "Inventory.StockAdjustment.Post";
    public const string InventoryStockAdjustmentVoid = "Inventory.StockAdjustment.Void";
    public const string InventoryStockTransferView = "Inventory.StockTransfer.View";
    public const string InventoryStockTransferCreate = "Inventory.StockTransfer.Create";
    public const string InventoryStockTransferEdit = "Inventory.StockTransfer.Edit";
    public const string InventoryStockTransferPost = "Inventory.StockTransfer.Post";
    public const string InventoryStockTransferVoid = "Inventory.StockTransfer.Void";
    public const string ProcurementRfqView = "Procurement.Rfq.View";
    public const string ProcurementRfqCreate = "Procurement.Rfq.Create";
    public const string ProcurementRfqEdit = "Procurement.Rfq.Edit";
    public const string ProcurementRfqSend = "Procurement.Rfq.Send";
    public const string ProcurementPurchaseOrderView = "Procurement.PurchaseOrder.View";
    public const string ProcurementPurchaseOrderCreate = "Procurement.PurchaseOrder.Create";
    public const string ProcurementPurchaseOrderEdit = "Procurement.PurchaseOrder.Edit";
    public const string ProcurementPurchaseOrderApprove = "Procurement.PurchaseOrder.Approve";
    public const string ProcurementGoodsReceiptView = "Procurement.GoodsReceipt.View";
    public const string ProcurementGoodsReceiptCreate = "Procurement.GoodsReceipt.Create";
    public const string ProcurementGoodsReceiptEdit = "Procurement.GoodsReceipt.Edit";
    public const string ProcurementGoodsReceiptPost = "Procurement.GoodsReceipt.Post";
    public const string ProcurementDirectPurchaseView = "Procurement.DirectPurchase.View";
    public const string ProcurementDirectPurchaseCreate = "Procurement.DirectPurchase.Create";
    public const string ProcurementDirectPurchaseEdit = "Procurement.DirectPurchase.Edit";
    public const string ProcurementDirectPurchasePost = "Procurement.DirectPurchase.Post";
    public const string ProcurementSupplierInvoiceView = "Procurement.SupplierInvoice.View";
    public const string ProcurementSupplierInvoiceCreate = "Procurement.SupplierInvoice.Create";
    public const string ProcurementSupplierInvoiceEdit = "Procurement.SupplierInvoice.Edit";
    public const string ProcurementSupplierInvoicePost = "Procurement.SupplierInvoice.Post";
    public const string ProcurementSupplierReturnView = "Procurement.SupplierReturn.View";
    public const string ProcurementSupplierReturnCreate = "Procurement.SupplierReturn.Create";
    public const string ProcurementSupplierReturnEdit = "Procurement.SupplierReturn.Edit";
    public const string ProcurementSupplierReturnPost = "Procurement.SupplierReturn.Post";
    public const string SalesQuoteView = "Sales.Quote.View";
    public const string SalesQuoteCreate = "Sales.Quote.Create";
    public const string SalesQuoteEdit = "Sales.Quote.Edit";
    public const string SalesQuoteSend = "Sales.Quote.Send";
    public const string SalesOrderView = "Sales.Order.View";
    public const string SalesOrderCreate = "Sales.Order.Create";
    public const string SalesOrderEdit = "Sales.Order.Edit";
    public const string SalesOrderConfirm = "Sales.Order.Confirm";
    public const string SalesDispatchView = "Sales.Dispatch.View";
    public const string SalesDispatchCreate = "Sales.Dispatch.Create";
    public const string SalesDispatchEdit = "Sales.Dispatch.Edit";
    public const string SalesDispatchPost = "Sales.Dispatch.Post";
    public const string SalesDirectDispatchView = "Sales.DirectDispatch.View";
    public const string SalesDirectDispatchCreate = "Sales.DirectDispatch.Create";
    public const string SalesDirectDispatchEdit = "Sales.DirectDispatch.Edit";
    public const string SalesDirectDispatchPost = "Sales.DirectDispatch.Post";
    public const string SalesInvoiceView = "Sales.Invoice.View";
    public const string SalesInvoiceCreate = "Sales.Invoice.Create";
    public const string SalesInvoiceEdit = "Sales.Invoice.Edit";
    public const string SalesInvoicePost = "Sales.Invoice.Post";
    public const string SalesCustomerReturnView = "Sales.CustomerReturn.View";
    public const string SalesCustomerReturnCreate = "Sales.CustomerReturn.Create";
    public const string SalesCustomerReturnEdit = "Sales.CustomerReturn.Edit";
    public const string SalesCustomerReturnPost = "Sales.CustomerReturn.Post";

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
        new(ProcurementPurchaseRequisitionConvert, "Procurement / Purchase Requisitions", "Convert", "Convert to PO", "Convert approved purchase requisitions to purchase orders."),
        new(InventoryStockAdjustmentView, "Inventory / Stock Adjustments", "View", "View adjustments", "Open and review stock adjustments."),
        new(InventoryStockAdjustmentCreate, "Inventory / Stock Adjustments", "Create", "Create adjustments", "Create stock adjustments."),
        new(InventoryStockAdjustmentEdit, "Inventory / Stock Adjustments", "Edit", "Edit adjustments", "Add, update, or remove draft stock adjustment lines."),
        new(InventoryStockAdjustmentPost, "Inventory / Stock Adjustments", "Post", "Post adjustments", "Post stock adjustments to inventory."),
        new(InventoryStockAdjustmentVoid, "Inventory / Stock Adjustments", "Void", "Void adjustments", "Void draft stock adjustments."),
        new(InventoryStockTransferView, "Inventory / Stock Transfers", "View", "View transfers", "Open and review stock transfers."),
        new(InventoryStockTransferCreate, "Inventory / Stock Transfers", "Create", "Create transfers", "Create stock transfers."),
        new(InventoryStockTransferEdit, "Inventory / Stock Transfers", "Edit", "Edit transfers", "Add, update, or remove draft stock transfer lines."),
        new(InventoryStockTransferPost, "Inventory / Stock Transfers", "Post", "Post transfers", "Post stock transfers to inventory."),
        new(InventoryStockTransferVoid, "Inventory / Stock Transfers", "Void", "Void transfers", "Void draft stock transfers."),
        new(ProcurementRfqView, "Procurement / RFQs", "View", "View RFQs", "Open and review requests for quotation."),
        new(ProcurementRfqCreate, "Procurement / RFQs", "Create", "Create RFQs", "Create requests for quotation."),
        new(ProcurementRfqEdit, "Procurement / RFQs", "Edit", "Edit RFQs", "Add, update, or remove draft RFQ lines."),
        new(ProcurementRfqSend, "Procurement / RFQs", "Send", "Send RFQs", "Mark RFQs as sent to suppliers."),
        new(ProcurementPurchaseOrderView, "Procurement / Purchase Orders", "View", "View POs", "Open and review purchase orders."),
        new(ProcurementPurchaseOrderCreate, "Procurement / Purchase Orders", "Create", "Create POs", "Create purchase orders."),
        new(ProcurementPurchaseOrderEdit, "Procurement / Purchase Orders", "Edit", "Edit POs", "Add, update, or remove draft purchase order lines."),
        new(ProcurementPurchaseOrderApprove, "Procurement / Purchase Orders", "Approve", "Approve POs", "Approve draft purchase orders."),
        new(ProcurementGoodsReceiptView, "Procurement / Goods Receipts", "View", "View GRNs", "Open and review goods receipts."),
        new(ProcurementGoodsReceiptCreate, "Procurement / Goods Receipts", "Create", "Create GRNs", "Create goods receipts."),
        new(ProcurementGoodsReceiptEdit, "Procurement / Goods Receipts", "Edit", "Edit GRNs", "Add, update, or remove draft goods receipt lines."),
        new(ProcurementGoodsReceiptPost, "Procurement / Goods Receipts", "Post", "Post GRNs", "Post goods receipts to inventory and payables."),
        new(ProcurementDirectPurchaseView, "Procurement / Direct Purchases", "View", "View direct purchases", "Open and review direct purchases."),
        new(ProcurementDirectPurchaseCreate, "Procurement / Direct Purchases", "Create", "Create direct purchases", "Create direct purchases."),
        new(ProcurementDirectPurchaseEdit, "Procurement / Direct Purchases", "Edit", "Edit direct purchases", "Add, update, or remove draft direct purchase lines."),
        new(ProcurementDirectPurchasePost, "Procurement / Direct Purchases", "Post", "Post direct purchases", "Post direct purchases to inventory and payables."),
        new(ProcurementSupplierInvoiceView, "Procurement / Supplier Invoices", "View", "View supplier invoices", "Open and review supplier invoices."),
        new(ProcurementSupplierInvoiceCreate, "Procurement / Supplier Invoices", "Create", "Create supplier invoices", "Create supplier invoices."),
        new(ProcurementSupplierInvoiceEdit, "Procurement / Supplier Invoices", "Edit", "Edit supplier invoices", "Update draft supplier invoices."),
        new(ProcurementSupplierInvoicePost, "Procurement / Supplier Invoices", "Post", "Post supplier invoices", "Post supplier invoices to accounts payable."),
        new(ProcurementSupplierReturnView, "Procurement / Supplier Returns", "View", "View supplier returns", "Open and review supplier returns."),
        new(ProcurementSupplierReturnCreate, "Procurement / Supplier Returns", "Create", "Create supplier returns", "Create supplier returns."),
        new(ProcurementSupplierReturnEdit, "Procurement / Supplier Returns", "Edit", "Edit supplier returns", "Add, update, or remove draft supplier return lines."),
        new(ProcurementSupplierReturnPost, "Procurement / Supplier Returns", "Post", "Post supplier returns", "Post supplier returns to inventory and credit notes."),
        new(SalesQuoteView, "Sales / Quotes", "View", "View quotes", "Open and review sales quotes."),
        new(SalesQuoteCreate, "Sales / Quotes", "Create", "Create quotes", "Create sales quotes."),
        new(SalesQuoteEdit, "Sales / Quotes", "Edit", "Edit quotes", "Add, update, or remove draft quote lines."),
        new(SalesQuoteSend, "Sales / Quotes", "Send", "Send quotes", "Mark sales quotes as sent."),
        new(SalesOrderView, "Sales / Orders", "View", "View orders", "Open and review sales orders."),
        new(SalesOrderCreate, "Sales / Orders", "Create", "Create orders", "Create sales orders."),
        new(SalesOrderEdit, "Sales / Orders", "Edit", "Edit orders", "Add, update, or remove draft order lines."),
        new(SalesOrderConfirm, "Sales / Orders", "Confirm", "Confirm orders", "Confirm sales orders."),
        new(SalesDispatchView, "Sales / Dispatches", "View", "View dispatches", "Open and review dispatch notes."),
        new(SalesDispatchCreate, "Sales / Dispatches", "Create", "Create dispatches", "Create dispatch notes."),
        new(SalesDispatchEdit, "Sales / Dispatches", "Edit", "Edit dispatches", "Add, update, or remove draft dispatch lines."),
        new(SalesDispatchPost, "Sales / Dispatches", "Post", "Post dispatches", "Post dispatches to inventory."),
        new(SalesDirectDispatchView, "Sales / Direct Dispatches", "View", "View direct dispatches", "Open and review direct dispatches."),
        new(SalesDirectDispatchCreate, "Sales / Direct Dispatches", "Create", "Create direct dispatches", "Create direct dispatches."),
        new(SalesDirectDispatchEdit, "Sales / Direct Dispatches", "Edit", "Edit direct dispatches", "Add, update, or remove draft direct dispatch lines."),
        new(SalesDirectDispatchPost, "Sales / Direct Dispatches", "Post", "Post direct dispatches", "Post direct dispatches to inventory."),
        new(SalesInvoiceView, "Sales / Invoices", "View", "View invoices", "Open and review sales invoices."),
        new(SalesInvoiceCreate, "Sales / Invoices", "Create", "Create invoices", "Create sales invoices."),
        new(SalesInvoiceEdit, "Sales / Invoices", "Edit", "Edit invoices", "Add, update, or remove draft invoice lines."),
        new(SalesInvoicePost, "Sales / Invoices", "Post", "Post invoices", "Post sales invoices to accounts receivable."),
        new(SalesCustomerReturnView, "Sales / Customer Returns", "View", "View returns", "Open and review customer returns."),
        new(SalesCustomerReturnCreate, "Sales / Customer Returns", "Create", "Create returns", "Create customer returns."),
        new(SalesCustomerReturnEdit, "Sales / Customer Returns", "Edit", "Edit returns", "Add, update, or remove draft customer return lines."),
        new(SalesCustomerReturnPost, "Sales / Customer Returns", "Post", "Post returns", "Post customer returns to inventory and credit notes.")
    ];

    public static readonly string[] AllKeys = All.Select(x => x.Key).ToArray();
}
