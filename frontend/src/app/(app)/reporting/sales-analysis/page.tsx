import { backendFetchJson } from "@/lib/backend.server";
import { userSettingsFromCookies } from "@/lib/user-settings.server";
import { ItemInlineLink } from "@/components/InlineLink";
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

type SalesAnalysisCustomerDto = {
  customerId: string;
  customerCode: string;
  customerName: string;
  invoiceCount: number;
  netSales: number;
  taxTotal: number;
  grossSales: number;
};

type SalesAnalysisItemDto = {
  itemId: string;
  itemSku: string;
  itemName: string;
  quantity: number;
  netSales: number;
  taxTotal: number;
  grossSales: number;
};

type SalesAnalysisReportDto = {
  from: string;
  to: string;
  previousPeriod: PeriodWindowDto;
  samePeriodLastYear: PeriodWindowDto;
  baseCurrencyCode: string;
  netSales: number;
  taxTotal: number;
  grossSales: number;
  invoiceCount: number;
  customerCount: number;
  itemCount: number;
  grossSalesVsPrevious: AmountComparisonDto;
  grossSalesVsYearAgo: AmountComparisonDto;
  invoiceCountVsPrevious: CountComparisonDto;
  trend: TrendPointDto[];
  topCustomers: SalesAnalysisCustomerDto[];
  topItems: SalesAnalysisItemDto[];
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

export default async function SalesAnalysisPage({
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
  const pdfHref = `/api/backend/reporting/sales-analysis/pdf?${qs.toString()}`;

  const report = await backendFetchJson<SalesAnalysisReportDto>(`/reporting/sales-analysis?${qs.toString()}`);
  const locale = settings.locale;
  const currencyCode = report.baseCurrencyCode || settings.baseCurrencyCode;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Sales Analysis</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Posted and paid sales invoices summarized by period, trend, customer, and item.
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
            <SecondaryLink href="/reporting/sales-analysis">Reset</SecondaryLink>
          </div>
        </form>
      </Card>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Gross Sales</div>
          <div className="mt-2 text-2xl font-semibold">{formatMoney(report.grossSales, locale, currencyCode)}</div>
          <div className={`mt-2 text-xs font-medium ${comparisonTone(report.grossSalesVsPrevious.delta)}`}>
            vs previous period: {formatPercent(report.grossSalesVsPrevious.deltaPercent)}
          </div>
          <div className={`mt-1 text-xs font-medium ${comparisonTone(report.grossSalesVsYearAgo.delta)}`}>
            vs same period last year: {formatPercent(report.grossSalesVsYearAgo.deltaPercent)}
          </div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Net Sales</div>
          <div className="mt-2 text-2xl font-semibold">{formatMoney(report.netSales, locale, currencyCode)}</div>
          <div className="mt-2 text-xs text-zinc-500">Tax total: {formatMoney(report.taxTotal, locale, currencyCode)}</div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Invoices</div>
          <div className="mt-2 text-2xl font-semibold">{formatNumber(report.invoiceCount, locale)}</div>
          <div className={`mt-2 text-xs font-medium ${comparisonTone(report.invoiceCountVsPrevious.delta)}`}>
            vs previous period: {formatPercent(report.invoiceCountVsPrevious.deltaPercent)}
          </div>
        </Card>

        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Coverage</div>
          <div className="mt-2 text-2xl font-semibold">{formatNumber(report.customerCount, locale)} customers</div>
          <div className="mt-2 text-xs text-zinc-500">{formatNumber(report.itemCount, locale)} distinct items sold</div>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1.3fr)_minmax(18rem,0.7fr)]">
        <TrendChartCard
          title="12-month sales trend"
          description="Toggle between billed value and invoice volume for the trailing 12 calendar months."
          points={report.trend}
          locale={locale}
          currencyCode={currencyCode}
          amountLabel="Revenue"
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
                {formatMoney(report.grossSalesVsPrevious.previous, locale, currencyCode)}
              </div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-zinc-500">Same period last year</div>
              <div className="mt-1 font-medium">{formatRange(report.samePeriodLastYear, locale)}</div>
              <div className="mt-1 text-xs text-zinc-500">
                {formatMoney(report.grossSalesVsYearAgo.previous, locale, currencyCode)}
              </div>
            </div>
          </div>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-2">
        <Card className="overflow-auto">
          <div className="mb-3 text-sm font-semibold">Top Customers</div>
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3 text-right">Invoices</th>
                <th className="py-2 pr-3 text-right">Net</th>
                <th className="py-2 pr-3 text-right">Tax</th>
                <th className="py-2 pr-3 text-right">Gross</th>
              </tr>
            </thead>
            <tbody>
              {report.topCustomers.map((row) => (
                <tr key={row.customerId} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 text-xs">
                    <div className="font-medium">{row.customerCode}</div>
                    <div className="text-zinc-500">{row.customerName}</div>
                  </td>
                  <td className="py-2 pr-3 text-right">{formatNumber(row.invoiceCount, locale)}</td>
                  <td className="py-2 pr-3 text-right">{formatMoney(row.netSales, locale, currencyCode)}</td>
                  <td className="py-2 pr-3 text-right">{formatMoney(row.taxTotal, locale, currencyCode)}</td>
                  <td className="py-2 pr-3 text-right font-medium">{formatMoney(row.grossSales, locale, currencyCode)}</td>
                </tr>
              ))}
              {report.topCustomers.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No sales found for the selected period.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </Card>

        <Card className="overflow-auto">
          <div className="mb-3 text-sm font-semibold">Top Items</div>
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3 text-right">Qty</th>
                <th className="py-2 pr-3 text-right">Net</th>
                <th className="py-2 pr-3 text-right">Tax</th>
                <th className="py-2 pr-3 text-right">Gross</th>
              </tr>
            </thead>
            <tbody>
              {report.topItems.map((row) => (
                <tr key={row.itemId} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 text-xs">
                    <div className="font-medium">
                      <ItemInlineLink itemId={row.itemId}>{row.itemSku}</ItemInlineLink>
                    </div>
                    <div className="text-zinc-500">
                      <ItemInlineLink itemId={row.itemId}>{row.itemName}</ItemInlineLink>
                    </div>
                  </td>
                  <td className="py-2 pr-3 text-right">{formatNumber(row.quantity, locale, 2)}</td>
                  <td className="py-2 pr-3 text-right">{formatMoney(row.netSales, locale, currencyCode)}</td>
                  <td className="py-2 pr-3 text-right">{formatMoney(row.taxTotal, locale, currencyCode)}</td>
                  <td className="py-2 pr-3 text-right font-medium">{formatMoney(row.grossSales, locale, currencyCode)}</td>
                </tr>
              ))}
              {report.topItems.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No sales lines found for the selected period.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </Card>
      </div>
    </div>
  );
}
