"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { EquipmentUnitLookupField } from "@/components/EquipmentUnitLookupField";
import { Button, Select, Textarea } from "@/components/ui";

type EquipmentUnitRef = { id: string; serialNumber: string; customerId: string };
type CustomerRef = { id: string; code: string; name: string };
type ServiceJobDto = {
  id: string;
  equipmentUnitId: string;
  customerId: string;
  kind: number;
  problemDescription: string;
};

const KIND_SERVICE = "0";
const KIND_REPAIR = "1";

export function ServiceJobEditForm({
  job,
  equipmentUnits,
  customers,
}: {
  job: ServiceJobDto;
  equipmentUnits: EquipmentUnitRef[];
  customers: CustomerRef[];
}) {
  const router = useRouter();
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );

  const [equipmentUnitId, setEquipmentUnitId] = useState(job.equipmentUnitId);
  const [customerId, setCustomerId] = useState(job.customerId);
  const [kind, setKind] = useState(String(job.kind));
  const [problemDescription, setProblemDescription] = useState(job.problemDescription);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setEquipmentUnitId(job.equipmentUnitId);
    setCustomerId(job.customerId);
    setKind(String(job.kind));
    setProblemDescription(job.problemDescription);
  }, [job]);

  useEffect(() => {
    const selected = equipmentUnits.find((unit) => unit.id === equipmentUnitId);
    if (selected) {
      setCustomerId(selected.customerId);
    }
  }, [equipmentUnitId, equipmentUnits]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);

    if (!equipmentUnitId) {
      setError("Select an equipment unit.");
      return;
    }

    setBusy(true);
    try {
      await apiPut(`service/jobs/${job.id}`, {
        equipmentUnitId,
        customerId,
        kind: Number(kind),
        problemDescription: problemDescription.trim(),
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
          <label className="mb-1 block text-sm font-medium">Equipment unit</label>
          <EquipmentUnitLookupField equipmentUnits={equipmentUnits} value={equipmentUnitId} onChange={setEquipmentUnitId} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select value={customerId} onChange={(event) => setCustomerId(event.target.value)} required>
            {customerOptions.map((customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.code} - {customer.name}
              </option>
            ))}
          </Select>
          <div className="mt-1 text-xs text-zinc-500">
            Open jobs can be edited. Customer defaults from the selected unit and entitlement is recalculated on save.
          </div>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Job type</label>
          <Select value={kind} onChange={(event) => setKind(event.target.value)} required>
            <option value={KIND_SERVICE}>Service</option>
            <option value={KIND_REPAIR}>Repair</option>
          </Select>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Problem description</label>
        <Textarea value={problemDescription} onChange={(event) => setProblemDescription(event.target.value)} required />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving..." : "Save Job"}
      </Button>
    </form>
  );
}
