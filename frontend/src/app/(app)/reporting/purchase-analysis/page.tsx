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

type PurchaseAnalysisSupplierDto = {
  supplierId: string;
  supplierCode: string;
  supplierName: string;
  invoiceCount: number;
  subtotal: number;
  taxTotal: number;
  grossSpend: number;
};

type PurchaseAnalysisReportDto = {
  from: string;
  to: string;
  previousPeriod: PeriodWindowDto;
  samePeriodLastYear: PeriodWindowDto;
  baseCurrencyCode: string;
  purchaseSubtotal: number;
  taxTotal: number;
  grossSpend: number;
  invoiceCount: number;
  supplierCount: number;
  grossSpendVsPrevious: AmountComparisonDto;
  grossSpendVsYearAgo: AmountComparisonDto;
  invoiceCountVsPrevious: CountComparisonDto;
  trend: TrendPointDto[];
  topSuppliers: PurchaseAnalysisSupplierDto[];
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

function formatRange(range: PeriodWindowDto, locale: string) {
  return `${new Date(range.from).toLocaleDateString(locale)} - ${new Date(range.to).toLocaleDateString(locale)}`;
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

  return `${value >= 0 ? "+" : ""}${value.toFixed(1)}%`;
}

export default async function PurchaseAnalysisPage({
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

  const report = await backendFetchJson<PurchaseAnalysisReportDto>(`/reporting/purchase-analysis?${qs.toString()}`);
  const locale = settings.locale;
  const currencyCode = report.baseCurrencyCode || settings.baseCurrencyCode;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Purchase Analysis</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Posted supplier invoices summarized by period, trend, and supplier concentration.
        </p>
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
            <SecondaryLink href="/reporting/purchase-analysis">Reset</SecondaryLink>
          </div>
        </form>
      </Card>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Gross Spend</div>
          <div className="mt-2 text-2xl font-semibold">{formatMoney(report.grossSpend, locale, currencyCode)}</div>
          <div className={`mt-2 text-xs font-medium ${comparisonTone(report.grossSpendVsPrevious.delta)}`}>
            vs previous period: {formatPercent(report.grossSpendVsPrevious.deltaPercent)}
          </div>
          <div className={`mt-1 text-xs font-medium ${comparisonTone(report.grossSpendVsYearAgo.delta)}`}>
            vs same period last year: {formatPercent(report.grossSpendVsYearAgo.deltaPercent)}
          </div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Subtotal</div>
          <div className="mt-2 text-2xl font-semibold">{formatMoney(report.purchaseSubtotal, locale, currencyCode)}</div>
          <div className="mt-2 text-xs text-zinc-500">Tax total: {formatMoney(report.taxTotal, locale, currencyCode)}</div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Supplier Invoices</div>
          <div className="mt-2 text-2xl font-semibold">{formatNumber(report.invoiceCount, locale)}</div>
          <div className={`mt-2 text-xs font-medium ${comparisonTone(report.invoiceCountVsPrevious.delta)}`}>
            vs previous period: {formatPercent(report.invoiceCountVsPrevious.deltaPercent)}
          </div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Supplier Coverage</div>
          <div className="mt-2 text-2xl font-semibold">{formatNumber(report.supplierCount, locale)} suppliers</div>
          <div className="mt-2 text-xs text-zinc-500">Based on posted supplier invoices only</div>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1.3fr)_minmax(18rem,0.7fr)]">
        <TrendChartCard
          title="12-month purchase trend"
          description="Toggle between billed spend and supplier-invoice volume for the trailing 12 calendar months."
          points={report.trend}
          locale={locale}
          currencyCode={currencyCode}
          amountLabel="Spend"
          countLabel="Invoices"
        />

        <Card>
          <div className="text-sm font-semibold">Comparison windows</div>
          <div className="mt-4 space-y-4 text-sm">
            <div>
              <div className="text-xs uppercase tracking-wide text-zinc-500">Selected period</div>
              <div className="mt-1 font-medium">{formatRange({ from: report.from, to: report.to }, locale)}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-zinc-500">Previous period</div>
              <div className="mt-1 font-medium">{formatRange(report.previousPeriod, locale)}</div>
              <div className="mt-1 text-xs text-zinc-500">
                {formatMoney(report.grossSpendVsPrevious.previous, locale, currencyCode)}
              </div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-zinc-500">Same period last year</div>
              <div className="mt-1 font-medium">{formatRange(report.samePeriodLastYear, locale)}</div>
              <div className="mt-1 text-xs text-zinc-500">
                {formatMoney(report.grossSpendVsYearAgo.previous, locale, currencyCode)}
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
              <th className="py-2 pr-3 text-right">Invoices</th>
              <th className="py-2 pr-3 text-right">Subtotal</th>
              <th className="py-2 pr-3 text-right">Tax</th>
              <th className="py-2 pr-3 text-right">Gross</th>
            </tr>
          </thead>
          <tbody>
            {report.topSuppliers.map((row) => (
              <tr key={row.supplierId} className="border-b border-zinc-100 dark:border-zinc-900">
                <td className="py-2 pr-3 text-xs">
                  <div className="font-medium">{row.supplierCode}</div>
                  <div className="text-zinc-500">{row.supplierName}</div>
                </td>
                <td className="py-2 pr-3 text-right">{formatNumber(row.invoiceCount, locale)}</td>
                <td className="py-2 pr-3 text-right">{formatMoney(row.subtotal, locale, currencyCode)}</td>
                <td className="py-2 pr-3 text-right">{formatMoney(row.taxTotal, locale, currencyCode)}</td>
                <td className="py-2 pr-3 text-right font-medium">{formatMoney(row.grossSpend, locale, currencyCode)}</td>
              </tr>
            ))}
            {report.topSuppliers.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                  No posted supplier invoices found for the selected period.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </Card>
    </div>
  );
}
