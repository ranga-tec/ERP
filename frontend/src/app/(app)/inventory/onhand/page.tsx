import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { OnHandQuery } from "./OnHandQuery";

type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string };

export default async function OnHandPage() {
  const [warehouses, items] = await Promise.all([
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">On Hand</h1>
        <p className="mt-1 text-sm text-zinc-500">Query current inventory balance by warehouse + item (+ optional batch).</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Query</div>
        <OnHandQuery warehouses={warehouses} items={items} />
      </Card>
    </div>
  );
}

