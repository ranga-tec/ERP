import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { CurrencyRateCreateForm } from "./CurrencyRateCreateForm";
import { CurrencyRateRow } from "./CurrencyRateRow";

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
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {currencyRates.map((r) => (
                <CurrencyRateRow key={r.id} rate={r} currencies={currencies} />
              ))}
              {currencyRates.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
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
