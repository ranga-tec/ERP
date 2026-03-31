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

const routeAccessRules: RouteAccessRule[] = [
  { prefix: "/admin", roles: ADMIN_ONLY },
  { prefix: "/audit-logs", roles: ADMIN_ONLY },
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
  { prefix: "/service/contracts", roles: SERVICE_CORE },
  { prefix: "/service/equipment-units", roles: SERVICE_CORE },
  { prefix: "/service/estimates", roles: ["Admin", "Service", "Sales", "Finance"] },
  { prefix: "/service/expense-claims", roles: ["Admin", "Service", "Finance"] },
  { prefix: "/service/handovers", roles: ["Admin", "Service", "Sales", "Finance"] },
  { prefix: "/service/jobs", roles: SERVICE_CORE },
  { prefix: "/service/material-requisitions", roles: ["Admin", "Service", "Inventory"] },
  { prefix: "/service/quality-checks", roles: SERVICE_CORE },
  { prefix: "/service/work-orders", roles: SERVICE_CORE },
  { prefix: "/master-data", roles: ALL_BUSINESS_ROLES },
  { prefix: "/settings", roles: ALL_BUSINESS_ROLES },
  { prefix: "/", roles: ALL_BUSINESS_ROLES },
];

function matchesPath(pathname: string, prefix: string): boolean {
  return pathname === prefix || pathname.startsWith(`${prefix}/`);
}

export function canAccessPath(roles: readonly string[], pathname: string): boolean {
  const matchedRule = routeAccessRules.find((rule) => matchesPath(pathname, rule.prefix));
  if (!matchedRule) {
    return true;
  }

  return matchedRule.roles.some((role) => roles.includes(role));
}
