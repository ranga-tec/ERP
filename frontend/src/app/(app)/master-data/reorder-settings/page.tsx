import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { ReorderSettingUpsertForm } from "./ReorderSettingUpsertForm";
import { ReorderSettingRow } from "./ReorderSettingRow";

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

  const warehouseById = new Map(warehouses.map((warehouse) => [warehouse.id, warehouse]));
  const itemById = new Map(items.map((item) => [item.id, item]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Reorder Settings</h1>
        <p className="mt-1 text-sm text-zinc-500">Reorder points and suggested reorder quantities per warehouse + item.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Upsert</div>
        <ReorderSettingUpsertForm
          warehouses={warehouses.map((warehouse) => ({ id: warehouse.id, code: warehouse.code, name: warehouse.name }))}
          items={items.map((item) => ({ id: item.id, sku: item.sku, name: item.name }))}
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
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {settings.map((setting) => {
                const warehouseLabel = warehouseById.get(setting.warehouseId)?.code ?? setting.warehouseId;
                const itemLabel = itemById.get(setting.itemId)?.sku ?? setting.itemId;

                return (
                  <ReorderSettingRow
                    key={setting.id}
                    setting={setting}
                    warehouseLabel={warehouseLabel}
                    itemLabel={itemLabel}
                  />
                );
              })}
              {settings.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
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
