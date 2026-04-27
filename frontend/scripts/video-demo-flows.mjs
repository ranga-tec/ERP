export const videoFlows = {
  orientation: {
    title: "Login and App Orientation",
    steps: [
      { type: "goto", path: "/", heading: "Dashboard", pauseMs: 2500 },
      { type: "goto", path: "/settings", heading: "Settings", pauseMs: 2600 },
    ],
  },
  overview: {
    title: "Overview",
    steps: [
      { type: "goto", path: "/", heading: "Dashboard", pauseMs: 4500 },
    ],
  },
  "master-data": {
    title: "Master Data",
    steps: [
      { type: "goto", path: "/master-data/warehouses", heading: "Warehouses" },
      { type: "goto", path: "/master-data/items", heading: "Items" },
      { type: "goto", path: "/master-data/currencies", heading: "Currencies" },
      { type: "goto", path: "/master-data/currency-rates", heading: "Currency Rates" },
      { type: "goto", path: "/master-data/taxes", heading: "Tax Codes" },
      { type: "goto", path: "/master-data/tax-conversions", heading: "Tax Conversions" },
      { type: "goto", path: "/master-data/payment-types", heading: "Payment Types" },
      { type: "goto", path: "/master-data/reference-forms", heading: "Reference Forms" },
    ],
  },
  procurement: {
    title: "Procurement",
    steps: [
      { type: "goto", path: "/procurement/purchase-requisitions", heading: "Purchase Requisitions" },
      { type: "goto", path: "/procurement/rfqs", heading: "RFQs" },
      { type: "goto", path: "/procurement/purchase-orders", heading: "Purchase Orders" },
      { type: "goto", path: "/procurement/goods-receipts", heading: "Goods Receipts (GRN)" },
      { type: "goto", path: "/procurement/direct-purchases", heading: "Direct Purchases" },
      { type: "goto", path: "/procurement/supplier-invoices", heading: "Supplier Invoices" },
      { type: "goto", path: "/procurement/supplier-returns", heading: "Supplier Returns" },
    ],
  },
  sales: {
    title: "Sales",
    steps: [
      { type: "goto", path: "/sales/quotes", heading: "Sales Quotes" },
      { type: "goto", path: "/sales/orders", heading: "Sales Orders" },
      { type: "goto", path: "/sales/dispatches", heading: "Dispatch Notes" },
      { type: "goto", path: "/sales/direct-dispatches", heading: "Direct Dispatches" },
      { type: "goto", path: "/sales/invoices", heading: "Sales Invoices" },
      { type: "goto", path: "/sales/customer-returns", heading: "Customer Returns" },
    ],
  },
  service: {
    title: "Service",
    steps: [
      { type: "goto", path: "/service/equipment-units", heading: "Equipment Units" },
      { type: "goto", path: "/service/contracts", heading: "Service Contracts" },
      { type: "goto", path: "/service/jobs", heading: "Service Jobs" },
      { type: "goto", path: "/service/estimates", heading: "Service Estimates" },
      { type: "goto", path: "/service/expense-claims", heading: "Service Expense Claims" },
      { type: "goto", path: "/service/work-orders", heading: "Work Orders" },
      { type: "goto", path: "/service/material-requisitions", heading: "Material Requisitions" },
      { type: "goto", path: "/service/quality-checks", heading: "Quality Checks" },
      { type: "goto", path: "/service/handovers", heading: "Service Handovers" },
    ],
  },
  inventory: {
    title: "Inventory",
    steps: [
      { type: "goto", path: "/inventory/onhand", heading: "On Hand" },
      { type: "goto", path: "/inventory/reorder-alerts", heading: "Reorder Alerts" },
      { type: "goto", path: "/inventory/stock-adjustments", heading: "Stock Adjustments" },
      { type: "goto", path: "/inventory/stock-transfers", heading: "Stock Transfers" },
    ],
  },
  finance: {
    title: "Finance",
    steps: [
      { type: "goto", path: "/finance/accounts", heading: "Chart of Accounts" },
      { type: "goto", path: "/finance/ar", heading: "Accounts Receivable" },
      { type: "goto", path: "/finance/ap", heading: "Accounts Payable" },
      { type: "goto", path: "/finance/payments", heading: "Payments" },
      { type: "goto", path: "/finance/petty-cash", heading: "Petty Cash" },
      { type: "goto", path: "/finance/credit-notes", heading: "Credit Notes" },
      { type: "goto", path: "/finance/debit-notes", heading: "Debit Notes" },
    ],
  },
  audit: {
    title: "Audit",
    steps: [
      { type: "goto", path: "/audit-logs", heading: "Audit Logs", pauseMs: 3500 },
    ],
  },
  reporting: {
    title: "Reporting",
    steps: [
      { type: "goto", path: "/reporting", heading: "Reporting" },
      { type: "goto", path: "/reporting/stock-ledger", heading: "Stock Ledger" },
      { type: "goto", path: "/reporting/aging", heading: "AR/AP Aging" },
      { type: "goto", path: "/reporting/tax-summary", heading: "Tax Summary" },
      { type: "goto", path: "/reporting/service-kpis", heading: "Service KPIs" },
      { type: "goto", path: "/reporting/sales-analysis", heading: "Sales Analysis" },
      { type: "goto", path: "/reporting/purchase-analysis", heading: "Purchase Analysis" },
      { type: "goto", path: "/reporting/supplier-performance", heading: "Supplier Performance" },
      { type: "goto", path: "/reporting/costing", heading: "Costing" },
    ],
  },
  admin: {
    title: "Admin",
    steps: [
      { type: "goto", path: "/admin/import", heading: "Admin · Import" },
      { type: "goto", path: "/admin/notifications", heading: "Admin - Notifications" },
      { type: "goto", path: "/admin/users", heading: "Admin · Users" },
      { type: "goto", path: "/settings", heading: "Settings" },
    ],
  },
  "full-tour": {
    title: "Full ISS Guided Tour",
    steps: [
      { type: "goto", path: "/", heading: "Dashboard", pauseMs: 3000 },
      { type: "goto", path: "/master-data/warehouses", heading: "Warehouses" },
      { type: "goto", path: "/procurement/purchase-orders", heading: "Purchase Orders" },
      { type: "goto", path: "/inventory/onhand", heading: "On Hand" },
      { type: "goto", path: "/sales/invoices", heading: "Sales Invoices" },
      { type: "goto", path: "/finance/ar", heading: "Accounts Receivable" },
      { type: "goto", path: "/reporting/costing", heading: "Costing" },
      { type: "goto", path: "/service/jobs", heading: "Service Jobs" },
      { type: "goto", path: "/audit-logs", heading: "Audit Logs" },
      { type: "goto", path: "/admin/users", heading: "Admin · Users" },
      { type: "goto", path: "/settings", heading: "Settings" },
    ],
  },
};

export function listFlowNames() {
  return Object.keys(videoFlows).sort();
}
