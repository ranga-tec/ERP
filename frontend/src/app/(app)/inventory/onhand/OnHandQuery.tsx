"use client";

import { StockAvailabilityExplorer } from "@/components/StockAvailabilityExplorer";

type WarehouseRef = { id: string; code: string; name: string };
type ItemRef = { id: string; sku: string; name: string };

export function OnHandQuery({ warehouses, items }: { warehouses: WarehouseRef[]; items: ItemRef[] }) {
  return <StockAvailabilityExplorer warehouses={warehouses} items={items} />;
}
