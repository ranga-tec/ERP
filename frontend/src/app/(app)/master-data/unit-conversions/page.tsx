import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { UnitConversionCreateForm } from "./UnitConversionCreateForm";
import { UnitConversionRow } from "./UnitConversionRow";

type UomDto = { id: string; code: string; name: string; isActive: boolean };
type UnitConversionDto = {
  id: string;
  fromUnitOfMeasureId: string;
  fromUnitOfMeasureCode: string;
  fromUnitOfMeasureName: string;
  toUnitOfMeasureId: string;
  toUnitOfMeasureCode: string;
  toUnitOfMeasureName: string;
  factor: number;
  notes?: string | null;
  isActive: boolean;
};

export default async function UnitConversionsPage() {
  const [conversions, uoms] = await Promise.all([
    backendFetchJson<UnitConversionDto[]>("/uom-conversions"),
    backendFetchJson<UomDto[]>("/uoms"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">UoM Conversions</h1>
        <p className="mt-1 text-sm text-zinc-500">Maintain conversion factors between operational and base units.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <UnitConversionCreateForm uoms={uoms} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">From</th>
                <th className="py-2 pr-3">To</th>
                <th className="py-2 pr-3">Factor</th>
                <th className="py-2 pr-3">Notes</th>
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {conversions.map((conversion) => (
                <UnitConversionRow key={conversion.id} conversion={conversion} uoms={uoms} />
              ))}
              {conversions.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No UoM conversions yet.
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
