"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

type NavItem = { href: string; label: string };
type NavSection = { title: string; items: NavItem[] };

const sections: NavSection[] = [
  {
    title: "Overview",
    items: [{ href: "/", label: "Dashboard" }],
  },
  {
    title: "Master Data",
    items: [
      { href: "/master-data/brands", label: "Brands" },
      { href: "/master-data/uoms", label: "UoMs" },
      { href: "/master-data/unit-conversions", label: "UoM Conversions" },
      { href: "/master-data/item-categories", label: "Item Categories" },
      { href: "/master-data/items", label: "Items" },
      { href: "/master-data/customers", label: "Customers" },
      { href: "/master-data/suppliers", label: "Suppliers" },
      { href: "/master-data/warehouses", label: "Warehouses" },
      { href: "/master-data/reorder-settings", label: "Reorder Settings" },
      { href: "/master-data/payment-types", label: "Payment Types" },
      { href: "/master-data/taxes", label: "Tax Codes" },
      { href: "/master-data/tax-conversions", label: "Tax Conversions" },
      { href: "/master-data/currencies", label: "Currencies" },
      { href: "/master-data/currency-rates", label: "Currency Rates" },
      { href: "/master-data/reference-forms", label: "Reference Forms" },
    ],
  },
  {
    title: "Procurement",
    items: [
      { href: "/procurement/purchase-requisitions", label: "Purchase Reqs" },
      { href: "/procurement/rfqs", label: "RFQs" },
      { href: "/procurement/purchase-orders", label: "Purchase Orders" },
      { href: "/procurement/goods-receipts", label: "Goods Receipts" },
      { href: "/procurement/direct-purchases", label: "Direct Purchases" },
      { href: "/procurement/supplier-invoices", label: "Supplier Invoices" },
      { href: "/procurement/supplier-returns", label: "Supplier Returns" },
    ],
  },
  {
    title: "Sales",
    items: [
      { href: "/sales/quotes", label: "Quotes" },
      { href: "/sales/orders", label: "Orders" },
      { href: "/sales/dispatches", label: "Dispatches" },
      { href: "/sales/direct-dispatches", label: "Direct Dispatches" },
      { href: "/sales/invoices", label: "Invoices" },
      { href: "/sales/customer-returns", label: "Customer Returns" },
    ],
  },
  {
    title: "Service",
    items: [
      { href: "/service/equipment-units", label: "Equipment Units" },
      { href: "/service/jobs", label: "Jobs" },
      { href: "/service/estimates", label: "Estimates" },
      { href: "/service/work-orders", label: "Work Orders" },
      { href: "/service/material-requisitions", label: "Material Reqs" },
      { href: "/service/quality-checks", label: "Quality Checks" },
      { href: "/service/handovers", label: "Handovers" },
    ],
  },
  {
    title: "Inventory",
    items: [
      { href: "/inventory/onhand", label: "On Hand" },
      { href: "/inventory/reorder-alerts", label: "Reorder Alerts" },
      { href: "/inventory/stock-adjustments", label: "Stock Adjustments" },
      { href: "/inventory/stock-transfers", label: "Stock Transfers" },
    ],
  },
  {
    title: "Finance",
    items: [
      { href: "/finance/ar", label: "Accounts Receivable" },
      { href: "/finance/ap", label: "Accounts Payable" },
      { href: "/finance/payments", label: "Payments" },
      { href: "/finance/credit-notes", label: "Credit Notes" },
      { href: "/finance/debit-notes", label: "Debit Notes" },
    ],
  },
  {
    title: "Audit",
    items: [{ href: "/audit-logs", label: "Audit Logs" }],
  },
  {
    title: "Reporting",
    items: [
      { href: "/reporting", label: "Reports Overview" },
      { href: "/reporting/stock-ledger", label: "Stock Ledger" },
      { href: "/reporting/aging", label: "AR/AP Aging" },
      { href: "/reporting/tax-summary", label: "Tax Summary" },
      { href: "/reporting/service-kpis", label: "Service KPIs" },
      { href: "/reporting/costing", label: "Costing" },
    ],
  },
  {
    title: "Admin",
    items: [
      { href: "/admin/import", label: "Import (Excel)" },
      { href: "/admin/notifications", label: "Notifications" },
      { href: "/admin/users", label: "Users" },
      { href: "/settings", label: "Settings" },
    ],
  },
];

type SidebarProps = {
  collapsed?: boolean;
  onNavigate?: () => void;
  onToggleCollapse?: () => void;
};

function itemIsActive(pathname: string, href: string): boolean {
  if (href === "/") return pathname === "/";
  return pathname === href || pathname.startsWith(`${href}/`);
}

function compactLabel(label: string): string {
  const words = label
    .split(/[\s/().-]+/)
    .map((w) => w.trim())
    .filter((w) => w.length > 0);

  if (words.length === 0) return "?";
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return `${words[0][0]}${words[1][0]}`.toUpperCase();
}

function PinIcon({ pinned }: { pinned: boolean }) {
  return (
    <svg
      viewBox="0 0 24 24"
      className={["h-4 w-4", pinned ? "" : "-rotate-45"].join(" ")}
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M9 5h6l-1 7 3 3H7l3-3-1-7Z"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M12 15v6"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      />
    </svg>
  );
}

export function Sidebar({ collapsed = false, onNavigate, onToggleCollapse }: SidebarProps) {
  const pathname = usePathname();
  const canToggle = typeof onToggleCollapse === "function";
  const pinned = !collapsed;

  return (
    <aside
      className={[
        "h-full shrink-0 border-r border-[var(--card-border)] bg-[var(--surface-soft)] p-4 shadow-[0_26px_44px_-38px_rgba(15,23,42,0.85)] backdrop-blur-xl transition-all duration-200",
        collapsed ? "w-20" : "w-[18.5rem]",
      ].join(" ")}
    >
      <div className="mb-4 flex items-start justify-between gap-2">
        <div>
          <div className="text-sm font-semibold tracking-tight text-[var(--foreground)]">
            {collapsed ? "ISS" : "ISS ERP"}
          </div>
          {!collapsed ? (
            <div className="text-xs text-[var(--muted-foreground)]">Service + Inventory + Sales</div>
          ) : null}
        </div>
        {canToggle ? (
          <button
            type="button"
            onClick={onToggleCollapse}
            aria-pressed={pinned}
            aria-label={pinned ? "Unpin sidebar" : "Pin sidebar"}
            title={pinned ? "Unpin sidebar" : "Pin sidebar"}
            className="inline-flex h-8 w-8 items-center justify-center rounded-xl border border-[var(--input-border)] bg-[var(--surface)] text-[var(--muted-foreground)] transition-all duration-200 hover:-translate-y-px hover:text-[var(--foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]"
          >
            <PinIcon pinned={pinned} />
          </button>
        ) : null}
      </div>

      <nav className={collapsed ? "space-y-3" : "space-y-6"}>
        {sections.map((section) => (
          <div key={section.title}>
            {!collapsed ? (
              <div className="mb-2 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--muted-foreground)]">
                {section.title}
              </div>
            ) : (
              <div className="mb-2 border-t border-[var(--card-border)] pt-2" />
            )}
            <ul className="space-y-1">
              {section.items.map((item) => {
                const active = itemIsActive(pathname, item.href);

                return (
                  <li key={item.href}>
                    <Link
                      href={item.href}
                      onClick={onNavigate}
                      title={collapsed ? item.label : undefined}
                      aria-label={item.label}
                      className={[
                        "block rounded-xl px-2.5 py-2 text-sm transition-all duration-200",
                        collapsed ? "text-center font-medium" : "",
                        active
                          ? "bg-gradient-to-r from-cyan-600 to-sky-600 text-white shadow-[0_14px_24px_-16px_rgba(2,132,199,0.95)]"
                          : "text-[var(--foreground)]/85 hover:-translate-y-px hover:bg-white/65 hover:text-[var(--foreground)] dark:hover:bg-slate-900/55",
                      ].join(" ")}
                    >
                      {collapsed ? compactLabel(item.label) : item.label}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>
    </aside>
  );
}
