"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Select } from "@/components/ui";

type SalesOrderRef = { id: string; number: string };
type WarehouseRef = { id: string; code: string; name: string };
type DispatchDto = { id: string; number: string };

export function DispatchCreateForm({
  salesOrders,
  warehouses,
}: {
  salesOrders: SalesOrderRef[];
  warehouses: WarehouseRef[];
}) {
  const router = useRouter();
  const orderOptions = useMemo(
    () => salesOrders.slice().sort((a, b) => b.number.localeCompare(a.number)),
    [salesOrders],
  );
  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );

  const [salesOrderId, setSalesOrderId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const dn = await apiPost<DispatchDto>("sales/dispatches", { salesOrderId, warehouseId });
      router.push(`/sales/dispatches/${dn.id}`);
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
          <label className="mb-1 block text-sm font-medium">Sales Order</label>
          <Select value={salesOrderId} onChange={(e) => setSalesOrderId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {orderOptions.map((o) => (
              <option key={o.id} value={o.id}>
                {o.number}
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
        {busy ? "Creating..." : "Create Dispatch"}
      </Button>
    </form>
  );
}

