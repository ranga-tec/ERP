export const ALL_ROLES = [
  "Admin",
  "Procurement",
  "Inventory",
  "Sales",
  "Service",
  "Finance",
  "Reporting",
] as const;

type AllowedRoles = readonly string[];

type RouteAccessRule = {
  prefix: string;
  roles: AllowedRoles;
};

type PermissionAccessRule = {
  prefix: string;
  permissions: readonly string[];
};

const ALL_BUSINESS_ROLES: AllowedRoles = ALL_ROLES;
const ADMIN_ONLY: AllowedRoles = ["Admin"];
const SALES_CORE: AllowedRoles = ["Admin", "Sales", "Finance"];
const SALES_STOCK: AllowedRoles = ["Admin", "Sales", "Inventory", "Finance"];
const DIRECT_DISPATCH: AllowedRoles = ["Admin", "Sales", "Inventory", "Service"];
const SERVICE_CORE: AllowedRoles = ["Admin", "Service", "Sales"];
const FINANCE_ONLY: AllowedRoles = ["Admin", "Finance"];
const REPORTING_ONLY: AllowedRoles = ["Admin", "Reporting"];
const PROCUREMENT_CORE: AllowedRoles = ["Admin", "Procurement", "Finance"];
const INVENTORY_CORE: AllowedRoles = ["Admin", "Inventory", "Reporting"];

const permissionAccessRules: PermissionAccessRule[] = [
  { prefix: "/finance/payments", permissions: ["Finance.Payment.View"] },
  { prefix: "/finance/petty-cash-ious", permissions: ["Finance.PettyCashIou.View"] },
  { prefix: "/finance/petty-cash", permissions: ["Finance.PettyCashFund.View"] },
  { prefix: "/finance/ar-credit-notes", permissions: ["Finance.CreditNote.View"] },
  { prefix: "/finance/ap-credit-notes", permissions: ["Finance.CreditNote.View"] },
  { prefix: "/finance/credit-notes", permissions: ["Finance.CreditNote.View"] },
  { prefix: "/finance/debit-notes", permissions: ["Finance.DebitNote.View"] },
  { prefix: "/inventory/stock-adjustments", permissions: ["Inventory.StockAdjustment.View"] },
  { prefix: "/inventory/stock-transfers", permissions: ["Inventory.StockTransfer.View"] },
  { prefix: "/procurement/purchase-requisitions", permissions: ["Procurement.PurchaseRequisition.View"] },
  { prefix: "/procurement/rfqs", permissions: ["Procurement.Rfq.View"] },
  { prefix: "/procurement/purchase-orders", permissions: ["Procurement.PurchaseOrder.View"] },
  { prefix: "/procurement/goods-receipts", permissions: ["Procurement.GoodsReceipt.View"] },
  { prefix: "/procurement/direct-purchases", permissions: ["Procurement.DirectPurchase.View"] },
  { prefix: "/procurement/supplier-invoices", permissions: ["Procurement.SupplierInvoice.View"] },
  { prefix: "/procurement/supplier-returns", permissions: ["Procurement.SupplierReturn.View"] },
  { prefix: "/sales/quotes", permissions: ["Sales.Quote.View"] },
  { prefix: "/sales/orders", permissions: ["Sales.Order.View"] },
  { prefix: "/sales/dispatches", permissions: ["Sales.Dispatch.View"] },
  { prefix: "/sales/direct-dispatches", permissions: ["Sales.DirectDispatch.View"] },
  { prefix: "/sales/invoices", permissions: ["Sales.Invoice.View"] },
  { prefix: "/sales/customer-returns", permissions: ["Sales.CustomerReturn.View"] },
  { prefix: "/service/estimates", permissions: ["Service.Estimate.View"] },
  { prefix: "/service/expense-claims", permissions: ["Service.ExpenseClaim.View"] },
  { prefix: "/service/material-requisitions", permissions: ["Service.MaterialRequisition.View"] },
];

const routeAccessRules: RouteAccessRule[] = [
  { prefix: "/admin", roles: ADMIN_ONLY },
  { prefix: "/audit-logs", roles: ADMIN_ONLY },
  { prefix: "/finance/petty-cash-ious", roles: ["Admin", "Finance", "Service"] },
  { prefix: "/finance", roles: FINANCE_ONLY },
  { prefix: "/inventory", roles: INVENTORY_CORE },
  { prefix: "/procurement", roles: PROCUREMENT_CORE },
  { prefix: "/reporting", roles: REPORTING_ONLY },
  { prefix: "/sales/customer-returns", roles: SALES_STOCK },
  { prefix: "/sales/direct-dispatches", roles: DIRECT_DISPATCH },
  { prefix: "/sales/dispatches", roles: SALES_STOCK },
  { prefix: "/sales/invoices", roles: ["Admin", "Sales", "Finance", "Inventory"] },
  { prefix: "/sales/orders", roles: SALES_CORE },
  { prefix: "/sales/quotes", roles: SALES_CORE },
  { prefix: "/service/command-center", roles: SERVICE_CORE },
  { prefix: "/service/dispatch-board", roles: SERVICE_CORE },
  { prefix: "/service/technician-workbench", roles: SERVICE_CORE },
  { prefix: "/service/contracts", roles: SERVICE_CORE },
  { prefix: "/service/equipment-units", roles: SERVICE_CORE },
  { prefix: "/service/estimates", roles: ["Admin", "Service", "Sales", "Finance"] },
  { prefix: "/service/expense-claims", roles: ["Admin", "Service", "Finance"] },
  { prefix: "/service/handovers", roles: ["Admin", "Service", "Sales", "Finance"] },
  { prefix: "/service/jobs", roles: SERVICE_CORE },
  { prefix: "/service/material-requisitions", roles: ["Admin", "Service", "Inventory"] },
  { prefix: "/service/quality-checks", roles: SERVICE_CORE },
  { prefix: "/service/technicians", roles: ["Admin", "Service"] },
  { prefix: "/service/work-orders", roles: SERVICE_CORE },
  { prefix: "/master-data", roles: ALL_BUSINESS_ROLES },
  { prefix: "/settings", roles: ALL_BUSINESS_ROLES },
  { prefix: "/", roles: ALL_BUSINESS_ROLES },
];

function matchesPath(pathname: string, prefix: string): boolean {
  return pathname === prefix || pathname.startsWith(`${prefix}/`);
}

export function canAccessPath(roles: readonly string[], pathname: string): boolean {
  return canAccessPathWithPermissions(roles, pathname);
}

export function canAccessPathWithPermissions(
  roles: readonly string[],
  pathname: string,
  permissions?: readonly string[] | null,
): boolean {
  if (roles.includes("Admin")) {
    return true;
  }

  if (permissions) {
    const matchedPermissionRule = permissionAccessRules.find((rule) => matchesPath(pathname, rule.prefix));
    if (matchedPermissionRule) {
      return matchedPermissionRule.permissions.some((permission) => permissions.includes(permission));
    }
  }

  const matchedRule = routeAccessRules.find((rule) => matchesPath(pathname, rule.prefix));
  if (!matchedRule) {
    return true;
  }

  return matchedRule.roles.some((role) => roles.includes(role));
}
