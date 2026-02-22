import { backendFetchJson } from "@/lib/backend.server";
import { Button, Card, Select, Table } from "@/components/ui";

type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };
type ReorderAlertDto = {
  warehouseId: string;
  itemId: string;
  reorderPoint: number;
  reorderQuantity: number;
  onHand: number;
};

export default async function ReorderAlertsPage({ searchParams }: { searchParams?: Promise<{ warehouseId?: string }> }) {
  const sp = await searchParams;
  const warehouseId = sp?.warehouseId?.trim() || "";

  const [warehouses, items, alerts] = await Promise.all([
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<ReorderAlertDto[]>(
      warehouseId ? `/inventory/reorder-alerts?warehouseId=${encodeURIComponent(warehouseId)}` : "/inventory/reorder-alerts",
    ),
  ]);

  const warehouseById = new Map(warehouses.map((w) => [w.id, w]));
  const itemById = new Map(items.map((i) => [i.id, i]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Reorder Alerts</h1>
        <p className="mt-1 text-sm text-zinc-500">Items at/below reorder points.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Filter</div>
        <form method="GET" className="flex flex-wrap items-end gap-3">
          <div className="min-w-64">
            <label className="mb-1 block text-sm font-medium">Warehouse</label>
            <Select name="warehouseId" defaultValue={warehouseId}>
              <option value="">All warehouses</option>
              {warehouses
                .slice()
                .sort((a, b) => a.code.localeCompare(b.code))
                .map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.code} — {w.name}
                  </option>
                ))}
            </Select>
          </div>
          <Button type="submit">Apply</Button>
        </form>
      </Card>

      <Card>
        <div className="mb-3 flex items-center justify-between">
          <div className="text-sm font-semibold">Alerts</div>
          <div className="text-xs text-zinc-500">{alerts.length} item(s)</div>
        </div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Warehouse</th>
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3">On Hand</th>
                <th className="py-2 pr-3">Reorder Point</th>
                <th className="py-2 pr-3">Reorder Qty</th>
              </tr>
            </thead>
            <tbody>
              {alerts.map((a) => (
                <tr key={`${a.warehouseId}-${a.itemId}`} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">{warehouseById.get(a.warehouseId)?.code ?? a.warehouseId}</td>
                  <td className="py-2 pr-3">{itemById.get(a.itemId)?.sku ?? a.itemId}</td>
                  <td className="py-2 pr-3">{a.onHand}</td>
                  <td className="py-2 pr-3">{a.reorderPoint}</td>
                  <td className="py-2 pr-3">{a.reorderQuantity}</td>
                </tr>
              ))}
              {alerts.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No reorder alerts.
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
