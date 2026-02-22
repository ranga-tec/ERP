import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";

type DashboardDto = {
  openServiceJobs: number;
  arOutstanding: number;
  apOutstanding: number;
  reorderAlerts: number;
};

function money(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 2,
  }).format(value);
}

export default async function DashboardPage() {
  const dashboard = await backendFetchJson<DashboardDto>("/reporting/dashboard");

  const cards = [
    {
      label: "Open Service Jobs",
      value: dashboard.openServiceJobs.toString(),
      href: "/service/jobs",
    },
    { label: "AR Outstanding", value: money(dashboard.arOutstanding), href: "/finance/ar" },
    { label: "AP Outstanding", value: money(dashboard.apOutstanding), href: "/finance/ap" },
    {
      label: "Reorder Alerts",
      value: dashboard.reorderAlerts.toString(),
      href: "/inventory/reorder-alerts",
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Real-time snapshot across service, finance, and inventory.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map((c) => (
          <Link
            key={c.label}
            href={c.href}
            className="rounded-xl border border-zinc-200 bg-white p-4 shadow-sm hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950 dark:hover:bg-zinc-900"
          >
            <div className="text-sm text-zinc-500">{c.label}</div>
            <div className="mt-2 text-2xl font-semibold">{c.value}</div>
          </Link>
        ))}
      </div>

      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950">
        <div className="text-sm font-semibold">Quick actions</div>
        <div className="mt-3 flex flex-wrap gap-2">
          <Link className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm hover:bg-zinc-50 dark:border-zinc-700 dark:hover:bg-zinc-900" href="/procurement/purchase-orders">
            New Purchase Order
          </Link>
          <Link className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm hover:bg-zinc-50 dark:border-zinc-700 dark:hover:bg-zinc-900" href="/sales/orders">
            New Sales Order
          </Link>
          <Link className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm hover:bg-zinc-50 dark:border-zinc-700 dark:hover:bg-zinc-900" href="/inventory/stock-adjustments">
            Stock Adjustment
          </Link>
          <Link className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm hover:bg-zinc-50 dark:border-zinc-700 dark:hover:bg-zinc-900" href="/service/jobs">
            New Service Job
          </Link>
        </div>
      </div>
    </div>
  );
}
