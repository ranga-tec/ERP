import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { TaxConversionCreateForm } from "./TaxConversionCreateForm";
import { TaxConversionRow } from "./TaxConversionRow";

type TaxDto = { id: string; code: string; name: string; isActive: boolean };
type TaxConversionDto = {
  id: string;
  sourceTaxCodeId: string;
  sourceTaxCode: string;
  sourceTaxName: string;
  targetTaxCodeId: string;
  targetTaxCode: string;
  targetTaxName: string;
  multiplier: number;
  notes?: string | null;
  isActive: boolean;
};

export default async function TaxConversionsPage() {
  const [taxConversions, taxes] = await Promise.all([
    backendFetchJson<TaxConversionDto[]>("/tax-conversions"),
    backendFetchJson<TaxDto[]>("/taxes"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Tax Conversions</h1>
        <p className="mt-1 text-sm text-zinc-500">Map one tax regime amount to another using maintained multipliers.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <TaxConversionCreateForm taxes={taxes} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Source</th>
                <th className="py-2 pr-3">Target</th>
                <th className="py-2 pr-3">Multiplier</th>
                <th className="py-2 pr-3">Notes</th>
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {taxConversions.map((conversion) => (
                <TaxConversionRow key={conversion.id} conversion={conversion} taxes={taxes} />
              ))}
              {taxConversions.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No tax conversion rules yet.
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
