import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { Card, Table } from "@/components/ui";
import { EquipmentUnitCreateForm } from "./EquipmentUnitCreateForm";

type EquipmentUnitDto = {
  id: string;
  itemId: string;
  serialNumber: string;
  customerId: string;
  purchasedAt?: string | null;
  warrantyUntil?: string | null;
  warrantyCoverage: number;
  hasActiveWarranty: boolean;
};

type ItemDto = { id: string; sku: string; name: string; type: number };
type CustomerDto = { id: string; code: string; name: string };

export default async function EquipmentUnitsPage() {
  const [units, items, customers] = await Promise.all([
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=100"),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const itemById = new Map(items.map((item) => [item.id, item]));
  const customerById = new Map(customers.map((customer) => [customer.id, customer]));

  const equipmentItems = items.filter((item) => item.type === 1).map((item) => ({ id: item.id, sku: item.sku, name: item.name }));
  const coverageLabel: Record<number, string> = {
    0: "No Warranty",
    1: "Inspection Only",
    2: "Labor Only",
    3: "Parts Only",
    4: "Labor and Parts",
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Equipment Units</h1>
        <p className="mt-1 text-sm text-zinc-500">Track sold equipment by serial number for service jobs.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <EquipmentUnitCreateForm equipmentItems={equipmentItems} customers={customers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Serial</th>
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Purchased</th>
                <th className="py-2 pr-3">Warranty</th>
                <th className="py-2 pr-3">Coverage</th>
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {units.map((unit) => (
                <tr key={unit.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/equipment-units/${unit.id}`}>
                      {unit.serialNumber}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">
                    <ItemInlineLink itemId={unit.itemId}>
                      {itemById.get(unit.itemId)?.sku ?? unit.itemId}
                    </ItemInlineLink>
                  </td>
                  <td className="py-2 pr-3">{customerById.get(unit.customerId)?.code ?? unit.customerId}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {unit.purchasedAt ? new Date(unit.purchasedAt).toLocaleDateString() : "-"}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {unit.warrantyUntil ? new Date(unit.warrantyUntil).toLocaleDateString() : "-"}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{coverageLabel[unit.warrantyCoverage] ?? unit.warrantyCoverage}</td>
                  <td className="py-2 pr-3">{unit.hasActiveWarranty ? "Yes" : "No"}</td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap gap-3 text-xs">
                      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/equipment-units/${unit.id}`}>
                        View
                      </Link>
                      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/equipment-units/${unit.id}`}>
                        Edit
                      </Link>
                    </div>
                  </td>
                </tr>
              ))}
              {units.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                    No equipment units yet.
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
