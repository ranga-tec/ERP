import { backendFetchJson } from "@/lib/backend.server";
import { userSettingsFromCookies } from "@/lib/user-settings.server";
import { Button, Card, Input, SecondaryLink, Select, Table } from "@/components/ui";
import { TrendChartCard } from "@/components/reporting/TrendChartCard";

type PeriodWindowDto = {
  from: string;
  to: string;
};

type AmountComparisonDto = {
  current: number;
  previous: number;
  delta: number;
  deltaPercent?: number | null;
};

type CountComparisonDto = {
  current: number;
  previous: number;
  delta: number;
  deltaPercent?: number | null;
};

type TrendPointDto = {
  periodStart: string;
  label: string;
  amount: number;
  count: number;
};

type SupplierPerformanceSupplierDto = {
  supplierId: string;
  supplierCode: string;
  supplierName: string;
  purchaseOrderCount: number;
  closedPurchaseOrderCount: number;
  orderedAmount: number;
  receivedAmount: number;
  receiptFillPercent: number;
  averageDaysToFirstReceipt?: number | null;
  supplierInvoiceCount: number;
  invoicedSpend: number;
};

type SupplierPerformanceReportDto = {
  from: string;
  to: string;
  previousPeriod: PeriodWindowDto;
  baseCurrencyCode: string;
  orderedAmount: number;
  receivedAmount: number;
  receiptFillPercent: number;
  averageDaysToFirstReceipt?: number | null;
  purchaseOrderCount: number;
  activeSupplierCount: number;
  openPurchaseOrderCount: number;
  orderedAmountVsPrevious: AmountComparisonDto;
  purchaseOrderCountVsPrevious: CountComparisonDto;
  trend: TrendPointDto[];
  suppliers: SupplierPerformanceSupplierDto[];
};

function normalizeDateInput(value?: string | null) {
  if (!value) {
    return "";
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return "";
  }

  return parsed.toISOString().slice(0, 10);
}

function formatMoney(value: number, locale: string, currencyCode: string) {
  try {
    return new Intl.NumberFormat(locale, {
      style: "currency",
      currency: currencyCode,
      maximumFractionDigits: 2,
    }).format(value);
  } catch {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
      maximumFractionDigits: 2,
    }).format(value);
  }
}

function formatNumber(value: number, locale: string, maximumFractionDigits = 0) {
  return new Intl.NumberFormat(locale, { maximumFractionDigits }).format(value);
}

function comparisonTone(value: number) {
  if (value > 0) return "text-emerald-600";
  if (value < 0) return "text-rose-600";
  return "text-zinc-500";
}

function formatPercent(value?: number | null) {
  if (value == null) {
    return "n/a";
  }

  return `${value.toFixed(1)}%`;
}

function formatDays(value?: number | null) {
  if (value == null) {
    return "n/a";
  }

  return `${value.toFixed(1)} days`;
}

function formatRange(range: PeriodWindowDto, locale: string) {
  return `${new Date(range.from).toLocaleDateString(locale)} - ${new Date(range.to).toLocaleDateString(locale)}`;
}

export default async function SupplierPerformancePage({
  searchParams,
}: {
  searchParams?: Promise<{ from?: string; to?: string; take?: string }>;
}) {
  const sp = await searchParams;
  const settings = await userSettingsFromCookies();
  const from = normalizeDateInput(sp?.from ?? null);
  const to = normalizeDateInput(sp?.to ?? null);
  const take = Math.min(25, Math.max(3, Number(sp?.take ?? "10") || 10));

  const qs = new URLSearchParams({ take: String(take) });
  if (from) qs.set("from", from);
  if (to) qs.set("to", to);
  const pdfHref = `/api/backend/reporting/supplier-performance/pdf?${qs.toString()}`;

  const report = await backendFetchJson<SupplierPerformanceReportDto>(`/reporting/supplier-performance?${qs.toString()}`);
  const locale = settings.locale;
  const currencyCode = report.baseCurrencyCode || settings.baseCurrencyCode;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Supplier Performance</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Purchase-order execution by supplier, with receipt progress and first-receipt speed.
          </p>
        </div>
        <SecondaryLink href={pdfHref} target="_blank" rel="noopener noreferrer">Download PDF</SecondaryLink>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Filter</div>
        <form method="GET" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <div>
            <label className="mb-1 block text-sm font-medium">From</label>
            <Input name="from" type="date" defaultValue={from} />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">To</label>
            <Input name="to" type="date" defaultValue={to} />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Top rows</label>
            <Select name="take" defaultValue={String(take)}>
              <option value="5">5</option>
              <option value="10">10</option>
              <option value="15">15</option>
              <option value="20">20</option>
              <option value="25">25</option>
            </Select>
          </div>

          <div className="flex flex-wrap items-end gap-2">
            <Button type="submit">Apply</Button>
            <SecondaryLink href="/reporting/supplier-performance">Reset</SecondaryLink>
          </div>
        </form>
      </Card>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Ordered Amount</div>
          <div className="mt-2 text-2xl font-semibold">{formatMoney(report.orderedAmount, locale, currencyCode)}</div>
          <div className={`mt-2 text-xs font-medium ${comparisonTone(report.orderedAmountVsPrevious.delta)}`}>
            vs previous period: {report.orderedAmountVsPrevious.deltaPercent == null ? "n/a" : `${report.orderedAmountVsPrevious.deltaPercent >= 0 ? "+" : ""}${report.orderedAmountVsPrevious.deltaPercent.toFixed(1)}%`}
          </div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Received Amount</div>
          <div className="mt-2 text-2xl font-semibold">{formatMoney(report.receivedAmount, locale, currencyCode)}</div>
          <div className="mt-2 text-xs text-zinc-500">Receipt fill: {formatPercent(report.receiptFillPercent)}</div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Purchase Orders</div>
          <div className="mt-2 text-2xl font-semibold">{formatNumber(report.purchaseOrderCount, locale)}</div>
          <div className={`mt-2 text-xs font-medium ${comparisonTone(report.purchaseOrderCountVsPrevious.delta)}`}>
            vs previous period: {report.purchaseOrderCountVsPrevious.deltaPercent == null ? "n/a" : `${report.purchaseOrderCountVsPrevious.deltaPercent >= 0 ? "+" : ""}${report.purchaseOrderCountVsPrevious.deltaPercent.toFixed(1)}%`}
          </div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Execution</div>
          <div className="mt-2 text-2xl font-semibold">{formatNumber(report.activeSupplierCount, locale)} suppliers</div>
          <div className="mt-2 text-xs text-zinc-500">Open POs: {formatNumber(report.openPurchaseOrderCount, locale)}</div>
          <div className="mt-1 text-xs text-zinc-500">Avg first receipt: {formatDays(report.averageDaysToFirstReceipt)}</div>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1.3fr)_minmax(18rem,0.7fr)]">
        <TrendChartCard
          title="12-month PO trend"
          description="Toggle between ordered value and purchase-order count for the trailing 12 calendar months."
          points={report.trend}
          locale={locale}
          currencyCode={currencyCode}
          amountLabel="Ordered"
          countLabel="PO count"
        />

        <Card>
          <div className="text-sm font-semibold">Comparison window</div>
          <div className="mt-4 space-y-4 text-sm">
            <div>
              <div className="text-xs uppercase tracking-wide text-zinc-500">Selected period</div>
              <div className="mt-1 font-medium">{formatRange({ from: report.from, to: report.to }, locale)}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-zinc-500">Previous period</div>
              <div className="mt-1 font-medium">{formatRange(report.previousPeriod, locale)}</div>
              <div className="mt-1 text-xs text-zinc-500">
                {formatMoney(report.orderedAmountVsPrevious.previous, locale, currencyCode)}
              </div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-zinc-500">What this report measures</div>
              <div className="mt-1 text-xs text-zinc-500">
                Purchase orders are grouped by supplier and compared against posted receipts and posted supplier invoices in the same selected period.
              </div>
            </div>
          </div>
        </Card>
      </div>

      <Card className="overflow-auto">
        <div className="mb-3 text-sm font-semibold">Top Suppliers</div>
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">Supplier</th>
              <th className="py-2 pr-3 text-right">POs</th>
              <th className="py-2 pr-3 text-right">Closed</th>
              <th className="py-2 pr-3 text-right">Ordered</th>
              <th className="py-2 pr-3 text-right">Received</th>
              <th className="py-2 pr-3 text-right">Fill %</th>
              <th className="py-2 pr-3 text-right">First Receipt</th>
              <th className="py-2 pr-3 text-right">Inv. Count</th>
              <th className="py-2 pr-3 text-right">Inv. Spend</th>
            </tr>
          </thead>
          <tbody>
            {report.suppliers.map((row) => (
              <tr key={row.supplierId} className="border-b border-zinc-100 dark:border-zinc-900">
                <td className="py-2 pr-3 text-xs">
                  <div className="font-medium">{row.supplierCode}</div>
                  <div className="text-zinc-500">{row.supplierName}</div>
                </td>
                <td className="py-2 pr-3 text-right">{formatNumber(row.purchaseOrderCount, locale)}</td>
                <td className="py-2 pr-3 text-right">{formatNumber(row.closedPurchaseOrderCount, locale)}</td>
                <td className="py-2 pr-3 text-right">{formatMoney(row.orderedAmount, locale, currencyCode)}</td>
                <td className="py-2 pr-3 text-right">{formatMoney(row.receivedAmount, locale, currencyCode)}</td>
                <td className="py-2 pr-3 text-right">{formatPercent(row.receiptFillPercent)}</td>
                <td className="py-2 pr-3 text-right">{formatDays(row.averageDaysToFirstReceipt)}</td>
                <td className="py-2 pr-3 text-right">{formatNumber(row.supplierInvoiceCount, locale)}</td>
                <td className="py-2 pr-3 text-right font-medium">{formatMoney(row.invoicedSpend, locale, currencyCode)}</td>
              </tr>
            ))}
            {report.suppliers.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={9}>
                  No supplier performance rows found for the selected period.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </Card>
    </div>
  );
}
