import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";

type EquipmentUnitDto = {
  id: string;
  itemId: string;
  serialNumber: string;
  customerId: string;
  purchasedAt?: string | null;
  warrantyUntil?: string | null;
};

type ItemDto = { id: string; sku: string; name: string };
type CustomerDto = { id: string; code: string; name: string };

export default async function EquipmentUnitDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [unit, items, customers] = await Promise.all([
    backendFetchJson<EquipmentUnitDto>(`/service/equipment-units/${id}`),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const itemById = new Map(items.map((i) => [i.id, i]));
  const customerById = new Map(customers.map((c) => [c.id, c]));

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/equipment-units" className="hover:underline">
            Equipment Units
          </Link>{" "}
          / <span className="font-mono text-xs">{unit.serialNumber}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Unit {unit.serialNumber}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Item: {itemById.get(unit.itemId)?.sku ?? unit.itemId}</div>
          <div>Customer: {customerById.get(unit.customerId)?.code ?? unit.customerId}</div>
          <div>Purchased: {unit.purchasedAt ? new Date(unit.purchasedAt).toLocaleDateString() : "—"}</div>
          <div>Warranty: {unit.warrantyUntil ? new Date(unit.warrantyUntil).toLocaleDateString() : "—"}</div>
        </div>
      </div>

      <Card>
        <div className="text-sm text-zinc-500">
          Create a service job from <Link className="underline" href="/service/jobs">Service Jobs</Link>.
        </div>
      </Card>
    </div>
  );
}

