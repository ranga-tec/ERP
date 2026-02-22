"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type WarehouseRef = { id: string; code: string; name: string };
type ItemRef = { id: string; sku: string; name: string };

type ReorderSettingDto = {
  id: string;
  warehouseId: string;
  itemId: string;
  reorderPoint: number;
  reorderQuantity: number;
};

export function ReorderSettingUpsertForm({
  warehouses,
  items,
}: {
  warehouses: WarehouseRef[];
  items: ItemRef[];
}) {
  const router = useRouter();

  const [warehouseId, setWarehouseId] = useState("");
  const [itemId, setItemId] = useState("");
  const [reorderPoint, setReorderPoint] = useState("0");
  const [reorderQuantity, setReorderQuantity] = useState("0");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );
  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const rp = Number(reorderPoint);
      const rq = Number(reorderQuantity);
      if (Number.isNaN(rp) || Number.isNaN(rq)) {
        throw new Error("Reorder values must be numbers.");
      }

      await apiPost<ReorderSettingDto>("reorder-settings", {
        warehouseId,
        itemId,
        reorderPoint: rp,
        reorderQuantity: rq,
      });

      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Warehouse</label>
          <Select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)} required>
            <option value="" disabled>
              Select…
            </option>
            {warehouseOptions.map((w) => (
              <option key={w.id} value={w.id}>
                {w.code} — {w.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Item</label>
          <Select value={itemId} onChange={(e) => setItemId(e.target.value)} required>
            <option value="" disabled>
              Select…
            </option>
            {itemOptions.map((i) => (
              <option key={i.id} value={i.id}>
                {i.sku} — {i.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Reorder point</label>
          <Input value={reorderPoint} onChange={(e) => setReorderPoint(e.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Reorder quantity</label>
          <Input value={reorderQuantity} onChange={(e) => setReorderQuantity(e.target.value)} inputMode="decimal" required />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving…" : "Upsert Reorder Setting"}
      </Button>
    </form>
  );
}

