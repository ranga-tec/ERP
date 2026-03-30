"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type SupplierRef = { id: string; code: string; name: string };
type WarehouseRef = { id: string; code: string; name: string };
type ServiceJobRef = { id: string; number: string; kind: number };
type DirectPurchaseDto = { id: string; number: string };

export function DirectPurchaseCreateForm({
  suppliers,
  warehouses,
  serviceJobs,
}: {
  suppliers: SupplierRef[];
  warehouses: WarehouseRef[];
  serviceJobs: ServiceJobRef[];
}) {
  const router = useRouter();
  const [supplierId, setSupplierId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [serviceJobId, setServiceJobId] = useState("");
  const [purchasedAt, setPurchasedAt] = useState("");
  const [remarks, setRemarks] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const body = {
        supplierId,
        warehouseId,
        serviceJobId: serviceJobId || null,
        purchasedAt: purchasedAt ? new Date(purchasedAt).toISOString() : null,
        remarks: remarks.trim() || null,
      };

      const dp = await apiPost<DirectPurchaseDto>("procurement/direct-purchases", body);
      router.push(`/procurement/direct-purchases/${dp.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  const sortedSuppliers = suppliers.slice().sort((a, b) => a.code.localeCompare(b.code));
  const sortedWarehouses = warehouses.slice().sort((a, b) => a.code.localeCompare(b.code));
  const sortedServiceJobs = serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number));

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Supplier</label>
          <Select value={supplierId} onChange={(e) => setSupplierId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {sortedSuppliers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.code} - {s.name}
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
            {sortedWarehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.code} - {w.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Service job (optional)</label>
          <Select value={serviceJobId} onChange={(e) => setServiceJobId(e.target.value)}>
            <option value="">Not linked</option>
            {sortedServiceJobs.map((job) => (
              <option key={job.id} value={job.id}>
                {job.number} - {job.kind === 1 ? "Repair" : "Service"}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Date/Time (optional)</label>
          <Input type="datetime-local" value={purchasedAt} onChange={(e) => setPurchasedAt(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Remarks (optional)</label>
          <Input value={remarks} onChange={(e) => setRemarks(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Direct Purchase"}
      </Button>
    </form>
  );
}
