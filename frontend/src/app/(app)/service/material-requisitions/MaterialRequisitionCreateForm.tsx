"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Select } from "@/components/ui";

type ServiceJobRef = { id: string; number: string };
type WarehouseRef = { id: string; code: string; name: string };
type MaterialRequisitionDto = { id: string; number: string };

export function MaterialRequisitionCreateForm({
  serviceJobs,
  warehouses,
}: {
  serviceJobs: ServiceJobRef[];
  warehouses: WarehouseRef[];
}) {
  const router = useRouter();
  const jobOptions = useMemo(
    () => serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number)),
    [serviceJobs],
  );
  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );

  const [serviceJobId, setServiceJobId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const mr = await apiPost<MaterialRequisitionDto>("service/material-requisitions", { serviceJobId, warehouseId });
      router.push(`/service/material-requisitions/${mr.id}`);
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
          <label className="mb-1 block text-sm font-medium">Service job</label>
          <Select value={serviceJobId} onChange={(e) => setServiceJobId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {jobOptions.map((j) => (
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
        {busy ? "Creating..." : "Create Material Requisition"}
      </Button>
    </form>
  );
}

