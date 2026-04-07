import Link from "next/link";
import { Card } from "@/components/ui";

const reports = [
  {
    href: "/reporting/stock-ledger",
    title: "Stock Ledger",
    description: "Inventory movement history by item/warehouse with running quantities.",
  },
  {
    href: "/reporting/aging",
    title: "AR/AP Aging",
    description: "Outstanding receivables and payables bucketed by aging periods.",
  },
  {
    href: "/reporting/tax-summary",
    title: "Tax Summary",
    description: "Input vs output tax summary for posted sales and supplier invoices.",
  },
  {
    href: "/reporting/service-kpis",
    title: "Service KPIs",
    description: "Service job throughput, completion timing, and parts-consumption indicators.",
  },
  {
    href: "/reporting/sales-analysis",
    title: "Sales Analysis",
    description: "Period comparison, 12-month trend, top customers, and top-selling items from posted invoices.",
  },
  {
    href: "/reporting/purchase-analysis",
    title: "Purchase Analysis",
    description: "Period comparison, 12-month trend, and supplier spend concentration from posted supplier invoices.",
  },
  {
    href: "/reporting/costing",
    title: "Costing",
    description: "Default vs weighted costs, last receipt rates, and current on-hand valuation.",
  },
];

export default function ReportingOverviewPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Reporting</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Operational and management reports for inventory, finance, tax, service performance, sales, and purchasing.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {reports.map((report) => (
          <Link key={report.href} href={report.href}>
            <Card className="h-full transition hover:bg-zinc-50 dark:hover:bg-zinc-900">
              <div className="text-base font-semibold">{report.title}</div>
              <p className="mt-2 text-sm text-zinc-500">{report.description}</p>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
