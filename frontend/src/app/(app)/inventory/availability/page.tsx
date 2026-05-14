import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { InventoryAvailabilityBrowser } from "./InventoryAvailabilityBrowser";

type WarehouseDto = { id: string; code: string; name: string };
type WarehouseBinDto = { id: string; warehouseId: string; code: string; name: string; zone?: string | null; rack?: string | null; shelf?: string | null };
type ItemDto = { id: string; sku: string; name: string };

export default async function InventoryAvailabilityPage() {
  const [warehouses, bins, items] = await Promise.all([
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<WarehouseBinDto[]>("/warehouses/bins"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Inventory Availability</h1>
        <p className="mt-1 text-sm text-zinc-500">Load and search current stock by item, warehouse, bin/rack, batch, and serial.</p>
      </div>

      <Card>
        <InventoryAvailabilityBrowser warehouses={warehouses} bins={bins} items={items} />
      </Card>
    </div>
  );
}
