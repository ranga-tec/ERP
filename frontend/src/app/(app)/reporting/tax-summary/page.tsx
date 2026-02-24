import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";

type TaxSummaryReport = {
  from: string;
  to: string;
  salesTaxableSubtotal: number;
  salesTaxTotal: number;
  purchaseTaxableSubtotal: number;
  purchaseTaxTotal: number;
  netTaxPayable: number;
  salesInvoiceCount: number;
  supplierInvoiceCount: number;
};

function money(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 2,
  }).format(value);
}

export default async function TaxSummaryPage() {
  const report = await backendFetchJson<TaxSummaryReport>("/reporting/tax-summary");

  const rows = [
    {
      label: "Sales (Output Tax)",
      docs: report.salesInvoiceCount,
      taxable: report.salesTaxableSubtotal,
      tax: report.salesTaxTotal,
    },
    {
      label: "Purchases (Input Tax)",
      docs: report.supplierInvoiceCount,
      taxable: report.purchaseTaxableSubtotal,
      tax: report.purchaseTaxTotal,
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Tax Summary</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Posted sales and supplier invoice tax totals from{" "}
          {new Date(report.from).toLocaleDateString()} to{" "}
          {new Date(report.to).toLocaleDateString()}.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">
            Sales Tax
          </div>
          <div className="mt-2 text-2xl font-semibold">
            {money(report.salesTaxTotal)}
          </div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">
            Purchase Tax
          </div>
          <div className="mt-2 text-2xl font-semibold">
            {money(report.purchaseTaxTotal)}
          </div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">
            Net Tax Payable
          </div>
          <div
            className={`mt-2 text-2xl font-semibold ${
              report.netTaxPayable >= 0 ? "" : "text-emerald-600 dark:text-emerald-400"
            }`}
          >
            {money(report.netTaxPayable)}
          </div>
        </Card>
      </div>

      <Card className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">Category</th>
              <th className="py-2 pr-3 text-right">Documents</th>
              <th className="py-2 pr-3 text-right">Taxable</th>
              <th className="py-2 pr-3 text-right">Tax</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.label} className="border-b border-zinc-100 dark:border-zinc-900">
                <td className="py-2 pr-3 font-medium">{row.label}</td>
                <td className="py-2 pr-3 text-right">{row.docs}</td>
                <td className="py-2 pr-3 text-right">{money(row.taxable)}</td>
                <td className="py-2 pr-3 text-right">{money(row.tax)}</td>
              </tr>
            ))}
            <tr className="border-t border-zinc-300 font-semibold dark:border-zinc-700">
              <td className="py-2 pr-3">Net (Sales Tax - Purchase Tax)</td>
              <td className="py-2 pr-3 text-right">{report.salesInvoiceCount + report.supplierInvoiceCount}</td>
              <td className="py-2 pr-3 text-right">-</td>
              <td className="py-2 pr-3 text-right">{money(report.netTaxPayable)}</td>
            </tr>
          </tbody>
        </Table>
      </Card>
    </div>
  );
}
