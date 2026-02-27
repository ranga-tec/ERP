import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { CurrencyRateCreateForm } from "./CurrencyRateCreateForm";

type CurrencyDto = { id: string; code: string; name: string; isActive: boolean };
type CurrencyRateDto = {
  id: string;
  fromCurrencyId: string;
  fromCurrencyCode: string;
  toCurrencyId: string;
  toCurrencyCode: string;
  rate: number;
  rateType: number;
  effectiveFrom: string;
  source?: string | null;
  isActive: boolean;
};

const rateTypeLabel: Record<number, string> = { 1: "Spot", 2: "Corporate", 3: "Manual" };

export default async function CurrencyRatesPage() {
  const [currencyRates, currencies] = await Promise.all([
    backendFetchJson<CurrencyRateDto[]>("/currency-rates"),
    backendFetchJson<CurrencyDto[]>("/currencies"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Currency Rates</h1>
        <p className="mt-1 text-sm text-zinc-500">Maintain FX rates for currency conversion and valuation.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <CurrencyRateCreateForm currencies={currencies} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Pair</th>
                <th className="py-2 pr-3">Rate</th>
                <th className="py-2 pr-3">Type</th>
                <th className="py-2 pr-3">Effective</th>
                <th className="py-2 pr-3">Source</th>
                <th className="py-2 pr-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {currencyRates.map((r) => (
                <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    {r.fromCurrencyCode}/{r.toCurrencyCode}
                  </td>
                  <td className="py-2 pr-3">{r.rate}</td>
                  <td className="py-2 pr-3">{rateTypeLabel[r.rateType] ?? r.rateType}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(r.effectiveFrom).toLocaleString()}</td>
                  <td className="py-2 pr-3 text-zinc-500">{r.source ?? "-"}</td>
                  <td className="py-2 pr-3">{r.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {currencyRates.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No currency rates yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
