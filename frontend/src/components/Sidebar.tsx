import Link from "next/link";

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
      { href: "/master-data/item-categories", label: "Item Categories" },
      { href: "/master-data/items", label: "Items" },
      { href: "/master-data/customers", label: "Customers" },
      { href: "/master-data/suppliers", label: "Suppliers" },
      { href: "/master-data/warehouses", label: "Warehouses" },
      { href: "/master-data/reorder-settings", label: "Reorder Settings" },
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
    title: "Admin",
    items: [
      { href: "/admin/import", label: "Import (Excel)" },
      { href: "/admin/notifications", label: "Notifications" },
      { href: "/admin/users", label: "Users" },
    ],
  },
];

export function Sidebar() {
  return (
    <aside className="w-72 shrink-0 border-r border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mb-4">
        <div className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
          ISS ERP
        </div>
        <div className="text-xs text-zinc-500">Service + Inventory + Sales</div>
      </div>

      <nav className="space-y-6">
        {sections.map((section) => (
          <div key={section.title}>
            <div className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500">
              {section.title}
            </div>
            <ul className="space-y-1">
              {section.items.map((item) => (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    className="block rounded-md px-2 py-1.5 text-sm text-zinc-700 hover:bg-zinc-100 hover:text-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-900 dark:hover:text-zinc-100"
                  >
                    {item.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>
    </aside>
  );
}
