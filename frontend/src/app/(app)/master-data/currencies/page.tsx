import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { CurrencyCreateForm } from "./CurrencyCreateForm";

type CurrencyDto = {
  id: string;
  code: string;
  name: string;
  symbol: string;
  minorUnits: number;
  isBase: boolean;
  isActive: boolean;
};

export default async function CurrenciesPage() {
  const currencies = await backendFetchJson<CurrencyDto[]>("/currencies");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Currencies</h1>
        <p className="mt-1 text-sm text-zinc-500">Currency master for transactions and costing valuation.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <CurrencyCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Symbol</th>
                <th className="py-2 pr-3">Minor</th>
                <th className="py-2 pr-3">Base</th>
                <th className="py-2 pr-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {currencies.map((c) => (
                <tr key={c.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{c.code}</td>
                  <td className="py-2 pr-3">{c.name}</td>
                  <td className="py-2 pr-3">{c.symbol}</td>
                  <td className="py-2 pr-3">{c.minorUnits}</td>
                  <td className="py-2 pr-3">{c.isBase ? "Yes" : "No"}</td>
                  <td className="py-2 pr-3">{c.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {currencies.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No currencies yet.
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
