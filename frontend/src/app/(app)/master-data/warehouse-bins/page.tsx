import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { WarehouseBinsManager } from "../warehouses/WarehouseBinsManager";

type WarehouseDto = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

type WarehouseBinDto = {
  id: string;
  warehouseId: string;
  code: string;
  name: string;
  zone?: string | null;
  rack?: string | null;
  shelf?: string | null;
  isActive: boolean;
};

export default async function WarehouseBinsPage() {
  const [warehouses, bins] = await Promise.all([
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<WarehouseBinDto[]>("/warehouses/bins"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Warehouse Bins</h1>
        <p className="mt-1 text-sm text-zinc-500">Maintain rack, shelf, and bin locations separately from warehouse headers.</p>
      </div>

      <Card>
        <WarehouseBinsManager warehouses={warehouses} bins={bins} />
      </Card>
    </div>
  );
}
