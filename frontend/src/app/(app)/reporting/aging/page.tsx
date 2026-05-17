import { backendFetchJson } from "@/lib/backend.server";
import { Button, Card, Input, SecondaryLink, Select, Table } from "@/components/ui";
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

type AgingEntity = "both" | "ar" | "ap";
type BucketFilter = "all" | "current" | "1-30" | "31-60" | "61-90" | "90+" | "overdue";

function emptyBuckets(): AgingBuckets {
  return {
    current: 0,
    days1To30: 0,
    days31To60: 0,
    days61To90: 0,
    daysOver90: 0,
    total: 0,
  };
}

function sumBuckets(rows: Array<{ buckets: AgingBuckets }>): AgingBuckets {
  return rows.reduce<AgingBuckets>(
    (acc, row) => ({
      current: acc.current + row.buckets.current,
      days1To30: acc.days1To30 + row.buckets.days1To30,
      days31To60: acc.days31To60 + row.buckets.days31To60,
      days61To90: acc.days61To90 + row.buckets.days61To90,
      daysOver90: acc.daysOver90 + row.buckets.daysOver90,
      total: acc.total + row.buckets.total,
    }),
    emptyBuckets(),
  );
}

function matchesBucket(buckets: AgingBuckets, filter: BucketFilter) {
  switch (filter) {
    case "current":
      return buckets.current > 0;
    case "1-30":
      return buckets.days1To30 > 0;
    case "31-60":
      return buckets.days31To60 > 0;
    case "61-90":
      return buckets.days61To90 > 0;
    case "90+":
      return buckets.daysOver90 > 0;
    case "overdue":
      return buckets.days1To30 + buckets.days31To60 + buckets.days61To90 + buckets.daysOver90 > 0;
    default:
      return buckets.total > 0;
  }
}

function normalizeDateInput(value?: string | null) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toISOString().slice(0, 10);
}

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

export default async function AgingPage({
  searchParams,
}: {
  searchParams?: Promise<{ asOf?: string; q?: string; bucket?: string; entity?: string }>;
}) {
  const sp = await searchParams;
  const settings = await userSettingsFromCookies();
  const searchQuery = (sp?.q ?? "").trim();
  const normalizedQuery = searchQuery.toLowerCase();
  const bucket = (sp?.bucket ?? "all") as BucketFilter;
  const entity = (sp?.entity ?? "both") as AgingEntity;
  const asOfInput = normalizeDateInput(sp?.asOf ?? null);

  const qs = new URLSearchParams();
  if (asOfInput) {
    qs.set("asOf", asOfInput);
  }
  const pdfHref = `/api/backend/reporting/aging/pdf${qs.size > 0 ? `?${qs.toString()}` : ""}`;

  const report = await backendFetchJson<AgingReport>(`/reporting/aging${qs.size > 0 ? `?${qs.toString()}` : ""}`);
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

  const matchesSearch = (code: string, name: string) =>
    normalizedQuery.length === 0 ||
    code.toLowerCase().includes(normalizedQuery) ||
    name.toLowerCase().includes(normalizedQuery);

  const filteredAr = report.accountsReceivable.filter(
    (row) => matchesSearch(row.customerCode, row.customerName) && matchesBucket(row.buckets, bucket),
  );
  const filteredAp = report.accountsPayable.filter(
    (row) => matchesSearch(row.supplierCode, row.supplierName) && matchesBucket(row.buckets, bucket),
  );

  const arTotals = sumBuckets(filteredAr);
  const apTotals = sumBuckets(filteredAp);
  const hasFilters = normalizedQuery.length > 0 || bucket !== "all" || asOfInput.length > 0;
  const summarySuffix = hasFilters ? " (filtered)" : "";
  const asOf = new Date(report.asOf).toLocaleString(settings.locale, {
    timeZone: settings.timeZone,
  });

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">AR/AP Aging</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Outstanding balances bucketed by entry age as of {asOf}.
          </p>
        </div>
        <SecondaryLink href={pdfHref} target="_blank" rel="noopener noreferrer">Download PDF</SecondaryLink>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Filter</div>
        <form method="GET" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
          <div className="xl:col-span-2">
            <label className="mb-1 block text-sm font-medium">Search</label>
            <Input
              name="q"
              defaultValue={searchQuery}
              placeholder="Customer / supplier code or name"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">As of date</label>
            <Input name="asOf" type="date" defaultValue={asOfInput} />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Bucket</label>
            <Select name="bucket" defaultValue={bucket}>
              <option value="all">All balances</option>
              <option value="overdue">Overdue only</option>
              <option value="current">Current only</option>
              <option value="1-30">1-30 days</option>
              <option value="31-60">31-60 days</option>
              <option value="61-90">61-90 days</option>
              <option value="90+">Over 90 days</option>
            </Select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Show</label>
            <Select name="entity" defaultValue={entity}>
              <option value="both">AR and AP</option>
              <option value="ar">AR only</option>
              <option value="ap">AP only</option>
            </Select>
          </div>

          <div className="flex flex-wrap items-end gap-2 xl:col-span-5">
            <Button type="submit">Apply</Button>
            <SecondaryLink href="/reporting/aging">Reset</SecondaryLink>
          </div>
        </form>
      </Card>

      <div className="grid gap-4 sm:grid-cols-2">
        {entity !== "ap" ? (
          <Card>
            <div className="text-xs uppercase tracking-wide text-zinc-500">AR Total{summarySuffix}</div>
            <div className="mt-2 text-2xl font-semibold">{money(arTotals.total)}</div>
            <div className="mt-1 text-xs text-zinc-500">{filteredAr.length} customer row(s)</div>
          </Card>
        ) : null}
        {entity !== "ar" ? (
          <Card>
            <div className="text-xs uppercase tracking-wide text-zinc-500">AP Total{summarySuffix}</div>
            <div className="mt-2 text-2xl font-semibold">{money(apTotals.total)}</div>
            <div className="mt-1 text-xs text-zinc-500">{filteredAp.length} supplier row(s)</div>
          </Card>
        ) : null}
      </div>

      {entity !== "ap" ? (
        <AgingTable
          title="Accounts Receivable"
          codeLabel="Customer"
          rows={filteredAr.map((row) => ({ code: row.customerCode, name: row.customerName, buckets: row.buckets }))}
          totals={arTotals}
          money={money}
        />
      ) : null}

      {entity !== "ar" ? (
        <AgingTable
          title="Accounts Payable"
          codeLabel="Supplier"
          rows={filteredAp.map((row) => ({ code: row.supplierCode, name: row.supplierName, buckets: row.buckets }))}
          totals={apTotals}
          money={money}
        />
      ) : null}
    </div>
  );
}
