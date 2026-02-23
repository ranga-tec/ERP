"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type CustomerRef = { id: string; code: string; name: string };
type ServiceJobRef = { id: string; number: string; customerId: string };
type WarehouseRef = { id: string; code: string; name: string };
type DirectDispatchDto = { id: string; number: string };

export function DirectDispatchCreateForm({
  customers,
  serviceJobs,
  warehouses,
}: {
  customers: CustomerRef[];
  serviceJobs: ServiceJobRef[];
  warehouses: WarehouseRef[];
}) {
  const router = useRouter();
  const [customerId, setCustomerId] = useState("");
  const [serviceJobId, setServiceJobId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const customerOptions = customers.slice().sort((a, b) => a.code.localeCompare(b.code));
  const serviceJobOptions = serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number));
  const warehouseOptions = warehouses.slice().sort((a, b) => a.code.localeCompare(b.code));

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      if (!customerId && !serviceJobId) {
        throw new Error("Select a customer or a service job.");
      }

      const dd = await apiPost<DirectDispatchDto>("sales/direct-dispatches", {
        warehouseId,
        customerId: customerId || null,
        serviceJobId: serviceJobId || null,
        reason: reason.trim() || null,
      });

      router.push(`/sales/direct-dispatches/${dd.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Customer (optional)</label>
          <Select
            value={customerId}
            onChange={(e) => {
              setCustomerId(e.target.value);
            }}
          >
            <option value="">None</option>
            {customerOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} - {c.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Service Job (optional)</label>
          <Select
            value={serviceJobId}
            onChange={(e) => {
              const id = e.target.value;
              setServiceJobId(id);
              if (id && !customerId) {
                const job = serviceJobs.find((j) => j.id === id);
                if (job) setCustomerId(job.customerId);
              }
            }}
          >
            <option value="">None</option>
            {serviceJobOptions.map((j) => (
              <option key={j.id} value={j.id}>
                {j.number}
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
                {w.code} - {w.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Reason (optional)</label>
        <Input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Urgent, replacement, sample..." />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Direct Dispatch"}
      </Button>
    </form>
  );
}
