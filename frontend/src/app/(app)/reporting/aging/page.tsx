import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { userSettingsFromCookies } from "@/lib/user-settings.server";

type AgingBuckets = {
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  daysOver90: number;
  total: number;
};

type ArRow = {
  customerId: string;
  customerCode: string;
  customerName: string;
  buckets: AgingBuckets;
};

type ApRow = {
  supplierId: string;
  supplierCode: string;
  supplierName: string;
  buckets: AgingBuckets;
};

type AgingReport = {
  asOf: string;
  accountsReceivable: ArRow[];
  arTotals: AgingBuckets;
  accountsPayable: ApRow[];
  apTotals: AgingBuckets;
};

function AgingTable({
  title,
  codeLabel,
  rows,
  totals,
  money,
}: {
  title: string;
  codeLabel: string;
  rows: Array<{ code: string; name: string; buckets: AgingBuckets }>;
  totals: AgingBuckets;
  money: (value: number) => string;
}) {
  const renderBuckets = (b: AgingBuckets) => (
    <>
      <td className="py-2 pr-3 text-right">{money(b.current)}</td>
      <td className="py-2 pr-3 text-right">{money(b.days1To30)}</td>
      <td className="py-2 pr-3 text-right">{money(b.days31To60)}</td>
      <td className="py-2 pr-3 text-right">{money(b.days61To90)}</td>
      <td className="py-2 pr-3 text-right">{money(b.daysOver90)}</td>
      <td className="py-2 pr-3 text-right font-semibold">{money(b.total)}</td>
    </>
  );

  return (
    <Card className="overflow-auto">
      <div className="mb-3 text-sm font-semibold">{title}</div>
      <Table>
        <thead>
          <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
            <th className="py-2 pr-3">{codeLabel}</th>
            <th className="py-2 pr-3">Name</th>
            <th className="py-2 pr-3 text-right">Current</th>
            <th className="py-2 pr-3 text-right">1-30</th>
            <th className="py-2 pr-3 text-right">31-60</th>
            <th className="py-2 pr-3 text-right">61-90</th>
            <th className="py-2 pr-3 text-right">{">"}90</th>
            <th className="py-2 pr-3 text-right">Total</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.code} className="border-b border-zinc-100 dark:border-zinc-900">
              <td className="py-2 pr-3 font-medium">{row.code}</td>
              <td className="py-2 pr-3 text-zinc-600 dark:text-zinc-300">{row.name}</td>
              {renderBuckets(row.buckets)}
            </tr>
          ))}
          <tr className="border-t border-zinc-300 font-semibold dark:border-zinc-700">
            <td className="py-2 pr-3" colSpan={2}>Totals</td>
            {renderBuckets(totals)}
          </tr>
          {rows.length === 0 ? (
            <tr>
              <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                No outstanding entries.
              </td>
            </tr>
          ) : null}
        </tbody>
      </Table>
    </Card>
  );
}

export default async function AgingPage() {
  const settings = await userSettingsFromCookies();
  const report = await backendFetchJson<AgingReport>("/reporting/aging");
  const money = (value: number) => {
    try {
      return new Intl.NumberFormat(settings.locale, {
        style: "currency",
        currency: settings.baseCurrencyCode,
        maximumFractionDigits: 2,
      }).format(value);
    } catch {
      return new Intl.NumberFormat("en-LK", {
        style: "currency",
        currency: "LKR",
        maximumFractionDigits: 2,
      }).format(value);
    }
  };

  const asOf = new Date(report.asOf).toLocaleString(settings.locale, {
    timeZone: settings.timeZone,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">AR/AP Aging</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Outstanding balances bucketed by entry age as of {asOf}.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">AR Total</div>
          <div className="mt-2 text-2xl font-semibold">{money(report.arTotals.total)}</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">AP Total</div>
          <div className="mt-2 text-2xl font-semibold">{money(report.apTotals.total)}</div>
        </Card>
      </div>

      <AgingTable
        title="Accounts Receivable"
        codeLabel="Customer"
        rows={report.accountsReceivable.map((row) => ({ code: row.customerCode, name: row.customerName, buckets: row.buckets }))}
        totals={report.arTotals}
        money={money}
      />

      <AgingTable
        title="Accounts Payable"
        codeLabel="Supplier"
        rows={report.accountsPayable.map((row) => ({ code: row.supplierCode, name: row.supplierName, buckets: row.buckets }))}
        totals={report.apTotals}
        money={money}
      />
    </div>
  );
}
