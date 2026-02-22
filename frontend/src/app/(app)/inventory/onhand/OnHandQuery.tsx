"use client";

import { useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type WarehouseRef = { id: string; code: string; name: string };
type ItemRef = { id: string; sku: string; name: string };

type OnHandDto = { warehouseId: string; itemId: string; batchNumber?: string | null; onHand: number };

export function OnHandQuery({ warehouses, items }: { warehouses: WarehouseRef[]; items: ItemRef[] }) {
  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );
  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );

  const [warehouseId, setWarehouseId] = useState("");
  const [itemId, setItemId] = useState("");
  const [batchNumber, setBatchNumber] = useState("");
  const [result, setResult] = useState<OnHandDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    setResult(null);
    try {
      const qs = new URLSearchParams({ warehouseId, itemId });
      if (batchNumber.trim()) {
        qs.set("batchNumber", batchNumber.trim());
      }
      const data = await apiGet<OnHandDto>(`inventory/onhand?${qs.toString()}`);
      setResult(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-4">
      <form onSubmit={onSubmit} className="space-y-3">
        <div className="grid gap-3 sm:grid-cols-3">
          <div>
            <label className="mb-1 block text-sm font-medium">Warehouse</label>
            <Select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)} required>
              <option value="" disabled>
                Select...
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
                Select...
              </option>
              {itemOptions.map((i) => (
                <option key={i.id} value={i.id}>
                  {i.sku} — {i.name}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Batch (optional)</label>
            <Input value={batchNumber} onChange={(e) => setBatchNumber(e.target.value)} />
          </div>
        </div>

        {error ? (
          <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
            {error}
          </div>
        ) : null}

        <Button type="submit" disabled={busy}>
          {busy ? "Checking..." : "Check on hand"}
        </Button>
      </form>

      {result ? (
        <div className="rounded-xl border border-zinc-200 bg-white p-4 text-sm shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
          <div className="text-xs uppercase tracking-wide text-zinc-500">On hand</div>
          <div className="mt-2 text-3xl font-semibold">{result.onHand}</div>
          <div className="mt-2 text-xs text-zinc-500">
            Warehouse: {result.warehouseId} · Item: {result.itemId} · Batch: {result.batchNumber ?? "—"}
          </div>
        </div>
      ) : null}
    </div>
  );
}

