"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Select, Textarea } from "@/components/ui";

type EquipmentUnitRef = { id: string; serialNumber: string };
type CustomerRef = { id: string; code: string; name: string };
type ServiceJobDto = { id: string; number: string };

export function ServiceJobCreateForm({
  equipmentUnits,
  customers,
}: {
  equipmentUnits: EquipmentUnitRef[];
  customers: CustomerRef[];
}) {
  const router = useRouter();
  const unitOptions = useMemo(
    () => equipmentUnits.slice().sort((a, b) => a.serialNumber.localeCompare(b.serialNumber)),
    [equipmentUnits],
  );
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );

  const [equipmentUnitId, setEquipmentUnitId] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [problemDescription, setProblemDescription] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const job = await apiPost<ServiceJobDto>("service/jobs", {
        equipmentUnitId,
        customerId,
        problemDescription: problemDescription.trim(),
      });
      router.push(`/service/jobs/${job.id}`);
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
          <Select value={equipmentUnitId} onChange={(e) => setEquipmentUnitId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {unitOptions.map((u) => (
              <option key={u.id} value={u.id}>
                {u.serialNumber}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select value={customerId} onChange={(e) => setCustomerId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {customerOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} — {c.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Problem description</label>
        <Textarea value={problemDescription} onChange={(e) => setProblemDescription(e.target.value)} required />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Service Job"}
      </Button>
    </form>
  );
}

