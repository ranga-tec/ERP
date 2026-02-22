import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { EquipmentUnitCreateForm } from "./EquipmentUnitCreateForm";

type EquipmentUnitDto = {
  id: string;
  itemId: string;
  serialNumber: string;
  customerId: string;
  purchasedAt?: string | null;
  warrantyUntil?: string | null;
};

type ItemDto = { id: string; sku: string; name: string; type: number };
type CustomerDto = { id: string; code: string; name: string };

export default async function EquipmentUnitsPage() {
  const [units, items, customers] = await Promise.all([
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=100"),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const itemById = new Map(items.map((i) => [i.id, i]));
  const customerById = new Map(customers.map((c) => [c.id, c]));

  const equipmentItems = items.filter((i) => i.type === 1).map((i) => ({ id: i.id, sku: i.sku, name: i.name }));

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
              </tr>
            </thead>
            <tbody>
              {units.map((u) => (
                <tr key={u.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/equipment-units/${u.id}`}>
                      {u.serialNumber}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{itemById.get(u.itemId)?.sku ?? u.itemId}</td>
                  <td className="py-2 pr-3">{customerById.get(u.customerId)?.code ?? u.customerId}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {u.purchasedAt ? new Date(u.purchasedAt).toLocaleDateString() : "—"}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {u.warrantyUntil ? new Date(u.warrantyUntil).toLocaleDateString() : "—"}
                  </td>
                </tr>
              ))}
              {units.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
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

