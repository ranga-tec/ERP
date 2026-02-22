"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type WarehouseRef = { id: string; code: string; name: string };
type StockTransferDto = { id: string; number: string };

export function StockTransferCreateForm({ warehouses }: { warehouses: WarehouseRef[] }) {
  const router = useRouter();
  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );

  const [fromWarehouseId, setFromWarehouseId] = useState("");
  const [toWarehouseId, setToWarehouseId] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const transfer = await apiPost<StockTransferDto>("inventory/stock-transfers", {
        fromWarehouseId,
        toWarehouseId,
        notes: notes.trim() || null,
      });
      router.push(`/inventory/stock-transfers/${transfer.id}`);
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
          <label className="mb-1 block text-sm font-medium">From warehouse</label>
          <Select value={fromWarehouseId} onChange={(e) => setFromWarehouseId(e.target.value)} required>
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
          <label className="mb-1 block text-sm font-medium">To warehouse</label>
          <Select value={toWarehouseId} onChange={(e) => setToWarehouseId(e.target.value)} required>
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
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Input value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Transfer"}
      </Button>
    </form>
  );
}

