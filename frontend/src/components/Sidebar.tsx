"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { usePathname } from "next/navigation";

type NavItem = { href: string; label: string };
type NavSection = { title: string; items: NavItem[] };

const SIDEBAR_SECTION_STORAGE_KEY = "iss_sidebar_sections_v1";

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

function sectionIsActive(pathname: string, section: NavSection): boolean {
  return section.items.some((item) => itemIsActive(pathname, item.href));
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

function allSectionTitles(): string[] {
  return sections.map((section) => section.title);
}

function normalizeSearch(value: string): string {
  return value.trim().toLowerCase();
}

function readExpandedSectionPreference(): string[] {
  const fallback = allSectionTitles();

  if (typeof window === "undefined") return fallback;

  try {
    const raw = window.localStorage.getItem(SIDEBAR_SECTION_STORAGE_KEY);
    if (!raw) return fallback;

    const allowed = new Set(fallback);
    const parsed = JSON.parse(raw);
    if (!Array.isArray(parsed)) return fallback;

    return parsed.filter((value): value is string => typeof value === "string" && allowed.has(value));
  } catch {
    return fallback;
  }
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

function ChevronIcon({ expanded }: { expanded: boolean }) {
  return (
    <svg
      viewBox="0 0 20 20"
      className={["h-4 w-4 transition-transform duration-200", expanded ? "rotate-0" : "-rotate-90"].join(" ")}
      fill="none"
      aria-hidden="true"
    >
      <path
        d="m5 7.5 5 5 5-5"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function Sidebar({ collapsed = false, onNavigate, onToggleCollapse }: SidebarProps) {
  const pathname = usePathname();
  const canToggle = typeof onToggleCollapse === "function";
  const pinned = !collapsed;
  const [expandedSections, setExpandedSections] = useState<string[]>(() => readExpandedSectionPreference());
  const [search, setSearch] = useState("");

  useEffect(() => {
    if (typeof window === "undefined") return;
    window.localStorage.setItem(
      SIDEBAR_SECTION_STORAGE_KEY,
      JSON.stringify(expandedSections),
    );
  }, [expandedSections]);

  function toggleSection(title: string) {
    setExpandedSections((current) =>
      current.includes(title)
        ? current.filter((value) => value !== title)
        : [...current, title],
    );
  }

  const expandedSectionSet = new Set(expandedSections);
  const normalizedSearch = normalizeSearch(search);
  const filteredSections = useMemo(() => {
    if (!normalizedSearch) {
      return sections;
    }

    return sections
      .map((section) => {
        const sectionMatches = normalizeSearch(section.title).includes(normalizedSearch);
        const items = sectionMatches
          ? section.items
          : section.items.filter((item) => normalizeSearch(item.label).includes(normalizedSearch));

        return {
          ...section,
          items,
        };
      })
      .filter((section) => section.items.length > 0);
  }, [normalizedSearch]);

  return (
    <aside
      className={[
        "flex h-full max-h-screen shrink-0 flex-col overflow-hidden border-r border-[var(--card-border)] bg-[var(--surface-soft)] p-4 shadow-[var(--shadow-soft)] backdrop-blur-xl transition-all duration-200",
        collapsed ? "w-20" : "w-[18.5rem]",
      ].join(" ")}
    >
      <div className="mb-4 flex shrink-0 items-start justify-between gap-2">
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
            className="inline-flex h-8 w-8 items-center justify-center rounded-xl border border-[var(--input-border)] bg-[var(--surface)] text-[var(--muted-foreground)] shadow-[var(--shadow-control)] transition-all duration-200 hover:-translate-y-px hover:text-[var(--foreground)] hover:shadow-[var(--shadow-soft)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]"
          >
            <PinIcon pinned={pinned} />
          </button>
        ) : null}
      </div>

      {!collapsed ? (
        <div className="mb-4 shrink-0">
          <label htmlFor="sidebar-search" className="sr-only">
            Search menu items
          </label>
          <div className="relative">
            <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[var(--muted-foreground)]">
              <svg viewBox="0 0 20 20" className="h-4 w-4" fill="none" aria-hidden="true">
                <path
                  d="m14.5 14.5 3 3"
                  stroke="currentColor"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                />
                <circle
                  cx="8.5"
                  cy="8.5"
                  r="5.5"
                  stroke="currentColor"
                  strokeWidth="1.8"
                />
              </svg>
            </span>
            <input
              id="sidebar-search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search menu items"
              className="w-full rounded-[1.15rem] border border-[var(--input-border)] bg-[linear-gradient(180deg,var(--surface)_0%,var(--surface-soft)_100%)] py-2.5 pl-9 pr-10 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80"
            />
            {search ? (
              <button
                type="button"
                onClick={() => setSearch("")}
                aria-label="Clear menu search"
                className="absolute right-2 top-1/2 inline-flex h-7 w-7 -translate-y-1/2 items-center justify-center rounded-lg text-[var(--muted-foreground)] transition-colors hover:bg-[var(--surface)] hover:text-[var(--foreground)]"
              >
                <svg viewBox="0 0 20 20" className="h-4 w-4" fill="none" aria-hidden="true">
                  <path
                    d="M5 5l10 10M15 5 5 15"
                    stroke="currentColor"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                  />
                </svg>
              </button>
            ) : null}
          </div>
        </div>
      ) : null}

      <nav
        className={[
          "sidebar-scrollbar flex-1 overflow-y-auto overscroll-contain pr-2",
          collapsed ? "space-y-3" : "space-y-4",
        ].join(" ")}
      >
        {filteredSections.map((section) => {
          const expanded = collapsed || normalizedSearch.length > 0 || expandedSectionSet.has(section.title);
          const activeSection = sectionIsActive(pathname, section);

          return (
            <div key={section.title} className={collapsed ? "" : "rounded-[1.65rem]"}>
              {!collapsed ? (
                <button
                  type="button"
                  onClick={() => toggleSection(section.title)}
                  aria-expanded={expanded}
                  className={[
                    "group relative flex w-full items-center justify-between gap-3 overflow-hidden rounded-[1.35rem] border px-3.5 py-3 text-left transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]",
                    activeSection
                      ? "border-[var(--accent)] bg-[linear-gradient(180deg,var(--accent-muted)_0%,var(--surface)_100%)] text-[var(--foreground)] shadow-[0_18px_30px_-24px_rgba(15,23,42,0.72),0_8px_0_0_rgba(29,78,216,0.18)]"
                      : "border-[var(--input-border)] bg-[linear-gradient(180deg,var(--surface)_0%,var(--surface-soft)_100%)] text-[var(--foreground)] shadow-[0_18px_30px_-24px_rgba(15,23,42,0.6),0_8px_0_0_rgba(148,163,184,0.12)] hover:-translate-y-px hover:shadow-[0_24px_34px_-24px_rgba(15,23,42,0.72),0_10px_0_0_rgba(29,78,216,0.14)]",
                    expanded ? "translate-y-0.5" : "",
                  ].join(" ")}
                >
                  <div
                    className={[
                      "flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border text-[11px] font-black uppercase tracking-[0.18em] shadow-[inset_0_1px_0_rgba(255,255,255,0.65)] transition-colors duration-200",
                      activeSection
                        ? "border-[var(--accent)] bg-[var(--accent)] text-[var(--accent-contrast)]"
                        : "border-[var(--card-border)] bg-[var(--surface)] text-[var(--link)] group-hover:text-[var(--foreground)]",
                    ].join(" ")}
                  >
                    {compactLabel(section.title)}
                  </div>

                  <div className="min-w-0 flex-1">
                    <div className="truncate text-sm font-semibold tracking-tight">{section.title}</div>
                    <div className="mt-0.5 text-[11px] text-[var(--muted-foreground)]">
                      {section.items.length} {section.items.length === 1 ? "menu item" : "menu items"}
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <span className="rounded-full border border-[var(--card-border)] bg-[var(--surface)] px-2 py-0.5 text-[10px] font-semibold text-[var(--muted-foreground)] shadow-[var(--shadow-control)]">
                      {section.items.length}
                    </span>
                    <span className="inline-flex h-8 w-8 items-center justify-center rounded-xl border border-[var(--card-border)] bg-[var(--surface)] text-[var(--muted-foreground)] shadow-[var(--shadow-control)] transition-colors duration-200 group-hover:text-[var(--foreground)]">
                      <ChevronIcon expanded={expanded} />
                    </span>
                  </div>
                </button>
              ) : (
                <div className="mb-2 border-t border-[var(--card-border)] pt-2" />
              )}

              <div
                className={
                  collapsed
                    ? "mt-2"
                    : [
                        "grid overflow-hidden transition-[grid-template-rows,opacity,margin] duration-200 ease-out",
                        expanded ? "mt-2 grid-rows-[1fr] opacity-100" : "mt-0 grid-rows-[0fr] opacity-0",
                      ].join(" ")
                }
              >
                <div className={collapsed ? "" : "min-h-0 overflow-hidden"}>
                  <ul
                    className={[
                      collapsed
                        ? "space-y-1"
                        : "space-y-1 rounded-[1.25rem] border border-[var(--card-border)] bg-[var(--card-bg)] p-2.5 shadow-[0_18px_30px_-28px_rgba(15,23,42,0.85),inset_0_1px_0_rgba(255,255,255,0.45)] backdrop-blur-md",
                    ].join(" ")}
                  >
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
                              "block rounded-xl border px-2.5 py-2.5 text-sm transition-all duration-200",
                              collapsed ? "text-center font-medium" : "px-3.5",
                              active
                                ? "border-[var(--accent)] bg-[var(--accent)] text-[var(--accent-contrast)] shadow-[var(--shadow-button)]"
                                : "border-transparent bg-transparent text-[var(--foreground)]/85 hover:-translate-y-px hover:border-[var(--card-border)] hover:bg-[var(--surface)] hover:text-[var(--foreground)] hover:shadow-[var(--shadow-control)]",
                            ].join(" ")}
                          >
                            {collapsed ? compactLabel(item.label) : item.label}
                          </Link>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              </div>
            </div>
          );
        })}

        {filteredSections.length === 0 && !collapsed ? (
          <div className="rounded-[1.4rem] border border-[var(--card-border)] bg-[var(--card-bg)] px-4 py-4 text-sm text-[var(--muted-foreground)] shadow-[var(--shadow-soft)]">
            No menu items match your search.
          </div>
        ) : null}
      </nav>
    </aside>
  );
}
