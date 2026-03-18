"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";
import { LineStockInsight } from "@/components/LineStockInsight";

type ItemRef = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };
type WarehouseRef = { id: string; code: string; name: string };

function parseList(text: string): string[] {
  return text
    .split(/[\n,]/g)
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

export function StockAdjustmentLineAddForm({
  adjustmentId,
  items,
  warehouses,
  warehouseId,
}: {
  adjustmentId: string;
  items: ItemRef[];
  warehouses: WarehouseRef[];
  warehouseId: string;
}) {
  const router = useRouter();
  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );

  const [itemId, setItemId] = useState("");
  const [countedQuantity, setCountedQuantity] = useState("");
  const [unitCost, setUnitCost] = useState("");
  const [batchNumber, setBatchNumber] = useState("");
  const [serials, setSerials] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedItem = itemId ? items.find((i) => i.id === itemId) : undefined;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const counted = Number(countedQuantity);
      if (countedQuantity.trim() === "" || Number.isNaN(counted) || counted < 0) {
        throw new Error("Counted quantity must be 0 or greater.");
      }
      const cost = Number(unitCost || selectedItem?.defaultUnitCost || 0);
      if (Number.isNaN(cost) || cost < 0) {
        throw new Error("Unit cost must be 0 or greater.");
      }

      const serialList = parseList(serials);

      await apiPostNoContent(`inventory/stock-adjustments/${adjustmentId}/lines`, {
        itemId,
        countedQuantity: counted,
        unitCost: cost,
        batchNumber: batchNumber.trim() || null,
        serials: serialList.length ? serialList : null,
      });

      setItemId("");
      setCountedQuantity("");
      setUnitCost("");
      setBatchNumber("");
      setSerials("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-4">
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium">Item</label>
          <Select value={itemId} onChange={(e) => setItemId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {itemOptions.map((i) => (
              <option key={i.id} value={i.id}>
                {i.sku} - {i.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Counted qty</label>
          <Input value={countedQuantity} onChange={(e) => setCountedQuantity(e.target.value)} inputMode="decimal" required />
          <div className="mt-1 text-xs text-zinc-500">Enter the real counted stock. The system will calculate the variance.</div>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Unit cost</label>
          <Input
            value={unitCost}
            onChange={(e) => setUnitCost(e.target.value)}
            inputMode="decimal"
            placeholder={selectedItem ? selectedItem.defaultUnitCost.toString() : ""}
          />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Batch (optional)</label>
          <Input value={batchNumber} onChange={(e) => setBatchNumber(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Serials (optional)</label>
          <Textarea value={serials} onChange={(e) => setSerials(e.target.value)} placeholder="One per line or comma-separated" />
        </div>
      </div>

      <LineStockInsight
        warehouses={warehouses}
        warehouseId={warehouseId}
        itemId={itemId}
        batchNumber={batchNumber}
        countedQuantity={countedQuantity}
      />

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Adding..." : "Add line"}
      </Button>
    </form>
  );
}
