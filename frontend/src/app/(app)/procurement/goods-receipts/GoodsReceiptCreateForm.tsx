"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Select } from "@/components/ui";

type PurchaseOrderRef = { id: string; number: string };
type WarehouseRef = { id: string; code: string; name: string };
type GoodsReceiptDto = { id: string; number: string };

export function GoodsReceiptCreateForm({
  purchaseOrders,
  warehouses,
}: {
  purchaseOrders: PurchaseOrderRef[];
  warehouses: WarehouseRef[];
}) {
  const router = useRouter();
  const poOptions = useMemo(
    () => purchaseOrders.slice().sort((a, b) => b.number.localeCompare(a.number)),
    [purchaseOrders],
  );
  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );

  const [purchaseOrderId, setPurchaseOrderId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const grn = await apiPost<GoodsReceiptDto>("procurement/goods-receipts", { purchaseOrderId, warehouseId });
      router.push(`/procurement/goods-receipts/${grn.id}`);
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
          <label className="mb-1 block text-sm font-medium">Purchase Order</label>
          <Select value={purchaseOrderId} onChange={(e) => setPurchaseOrderId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {poOptions.map((p) => (
              <option key={p.id} value={p.id}>
                {p.number}
              </option>
            ))}
          </Select>
        </div>
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
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create GRN"}
      </Button>
    </form>
  );
}

