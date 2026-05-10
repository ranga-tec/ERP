"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { EquipmentUnitLookupField } from "@/components/EquipmentUnitLookupField";
import { Button, Input, Select, Textarea } from "@/components/ui";

type EquipmentUnitRef = { id: string; serialNumber: string; customerId: string };
type CustomerRef = { id: string; code: string; name: string };
type ServiceJobDto = {
  id: string;
  equipmentUnitId: string;
  customerId: string;
  kind: number;
  expectedCompletionAt?: string | null;
  siteLocation?: string | null;
  problemDescription: string;
};

const KIND_SERVICE = "0";
const KIND_REPAIR = "1";
const KIND_PDI = "2";
const KIND_WARRANTY = "3";
const KIND_INSPECTION = "4";

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
  const [expectedCompletionAt, setExpectedCompletionAt] = useState(job.expectedCompletionAt?.slice(0, 10) ?? "");
  const [siteLocation, setSiteLocation] = useState(job.siteLocation ?? "");
  const [problemDescription, setProblemDescription] = useState(job.problemDescription);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setEquipmentUnitId(job.equipmentUnitId);
    setCustomerId(job.customerId);
    setKind(String(job.kind));
    setExpectedCompletionAt(job.expectedCompletionAt?.slice(0, 10) ?? "");
    setSiteLocation(job.siteLocation ?? "");
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
        expectedCompletionAt: expectedCompletionAt || null,
        siteLocation: siteLocation.trim() || null,
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
            <option value={KIND_PDI}>PDI</option>
            <option value={KIND_WARRANTY}>Warranty</option>
            <option value={KIND_INSPECTION}>Inspection</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Expected completion</label>
          <Input type="date" value={expectedCompletionAt} onChange={(event) => setExpectedCompletionAt(event.target.value)} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Site location</label>
        <Input value={siteLocation} onChange={(event) => setSiteLocation(event.target.value)} />
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
