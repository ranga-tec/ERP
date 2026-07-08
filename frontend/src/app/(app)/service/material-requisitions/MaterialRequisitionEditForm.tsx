"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type ServiceJobRef = { id: string; number: string };
type WarehouseRef = { id: string; code: string; name: string };
type MaterialRequisitionEditable = {
  id: string;
  serviceJobId: string;
  serviceJobDailySheetId?: string | null;
  warehouseId: string;
  purpose?: string | null;
};

export function MaterialRequisitionEditForm({
  requisition,
  serviceJobs,
  warehouses,
}: {
  requisition: MaterialRequisitionEditable;
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

  const [serviceJobId, setServiceJobId] = useState(requisition.serviceJobId);
  const [warehouseId, setWarehouseId] = useState(requisition.warehouseId);
  const [purpose, setPurpose] = useState(requisition.purpose ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPut(`service/material-requisitions/${requisition.id}`, {
        serviceJobId,
        warehouseId,
        purpose: purpose.trim() || null,
        serviceJobDailySheetId: requisition.serviceJobDailySheetId ?? null,
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
          <label className="mb-1 block text-sm font-medium">Job Order</label>
          <Select value={serviceJobId} onChange={(event) => setServiceJobId(event.target.value)} required>
            {jobOptions.map((job) => (
              <option key={job.id} value={job.id}>
                {job.number}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Warehouse</label>
          <Select value={warehouseId} onChange={(event) => setWarehouseId(event.target.value)} required>
            {warehouseOptions.map((warehouse) => (
              <option key={warehouse.id} value={warehouse.id}>
                {warehouse.code} - {warehouse.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Need / purpose</label>
        <Input value={purpose} onChange={(event) => setPurpose(event.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving..." : "Save MRN"}
      </Button>
    </form>
  );
}
