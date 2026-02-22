"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type SupplierRef = { id: string; code: string; name: string };
type WarehouseRef = { id: string; code: string; name: string };
type SupplierReturnDto = { id: string; number: string };

export function SupplierReturnCreateForm({
  suppliers,
  warehouses,
}: {
  suppliers: SupplierRef[];
  warehouses: WarehouseRef[];
}) {
  const router = useRouter();
  const supplierOptions = useMemo(
    () => suppliers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [suppliers],
  );
  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );

  const [supplierId, setSupplierId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const sr = await apiPost<SupplierReturnDto>("procurement/supplier-returns", {
        supplierId,
        warehouseId,
        reason: reason.trim() || null,
      });
      router.push(`/procurement/supplier-returns/${sr.id}`);
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
          <label className="mb-1 block text-sm font-medium">Supplier</label>
          <Select value={supplierId} onChange={(e) => setSupplierId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {supplierOptions.map((s) => (
              <option key={s.id} value={s.id}>
                {s.code} — {s.name}
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

      <div>
        <label className="mb-1 block text-sm font-medium">Reason (optional)</label>
        <Input value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Supplier Return"}
      </Button>
    </form>
  );
}

