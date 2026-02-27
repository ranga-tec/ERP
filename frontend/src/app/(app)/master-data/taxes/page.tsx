import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { TaxCreateForm } from "./TaxCreateForm";

type TaxDto = {
  id: string;
  code: string;
  name: string;
  ratePercent: number;
  isInclusive: boolean;
  scope: number;
  description?: string | null;
  isActive: boolean;
};

const scopeLabel: Record<number, string> = { 1: "Sales", 2: "Purchase", 3: "Both" };

export default async function TaxesPage() {
  const taxes = await backendFetchJson<TaxDto[]>("/taxes");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Tax Codes</h1>
        <p className="mt-1 text-sm text-zinc-500">Centralized tax rates used by purchase/sales/service line entry.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <TaxCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Rate %</th>
                <th className="py-2 pr-3">Scope</th>
                <th className="py-2 pr-3">Inclusive</th>
                <th className="py-2 pr-3">Active</th>
              </tr>
            </thead>
            <tbody>
              {taxes.map((tax) => (
                <tr key={tax.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{tax.code}</td>
                  <td className="py-2 pr-3">{tax.name}</td>
                  <td className="py-2 pr-3">{tax.ratePercent}</td>
                  <td className="py-2 pr-3">{scopeLabel[tax.scope] ?? tax.scope}</td>
                  <td className="py-2 pr-3">{tax.isInclusive ? "Yes" : "No"}</td>
                  <td className="py-2 pr-3">{tax.isActive ? "Yes" : "No"}</td>
                </tr>
              ))}
              {taxes.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No tax codes yet.
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
