import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { ReorderSettingUpsertForm } from "./ReorderSettingUpsertForm";

type WarehouseDto = { id: string; code: string; name: string; address?: string | null; isActive: boolean };
type ItemDto = { id: string; sku: string; name: string };
type ReorderSettingDto = {
  id: string;
  warehouseId: string;
  itemId: string;
  reorderPoint: number;
  reorderQuantity: number;
};

export default async function ReorderSettingsPage() {
  const [settings, warehouses, items] = await Promise.all([
    backendFetchJson<ReorderSettingDto[]>("/reorder-settings"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Reorder Settings</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Reorder points and suggested reorder quantities per warehouse + item.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Upsert</div>
        <ReorderSettingUpsertForm
          warehouses={warehouses.map((w) => ({ id: w.id, code: w.code, name: w.name }))}
          items={items.map((i) => ({ id: i.id, sku: i.sku, name: i.name }))}
        />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">Reorder Point</th>
                <th className="py-2 pr-3">Reorder Qty</th>
              </tr>
            </thead>
            <tbody>
              {settings.map((s) => (
                <tr key={s.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">
                    {warehouseById.get(s.warehouseId)?.code ?? s.warehouseId}
                  </td>
                  <td className="py-2 pr-3">
                    {itemById.get(s.itemId)?.sku ?? s.itemId}
                  </td>
                  <td className="py-2 pr-3">{s.reorderPoint}</td>
                  <td className="py-2 pr-3">{s.reorderQuantity}</td>
                </tr>
              ))}
              {settings.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                    No reorder settings yet.
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

