"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { EquipmentUnitLookupField } from "@/components/EquipmentUnitLookupField";
import { Button, Input, Select, Textarea } from "@/components/ui";

type EquipmentUnitRef = {
  id: string;
  serialNumber: string;
  customerId: string;
  itemSku?: string | null;
  itemName?: string | null;
  customerCode?: string | null;
};
type CustomerRef = { id: string; code: string; name: string };
type ServiceJobDto = { id: string; number: string };

const KIND_SERVICE = "0";
const KIND_REPAIR = "1";
const KIND_PDI = "2";
const KIND_WARRANTY = "3";
const KIND_INSPECTION = "4";

export function ServiceJobCreateForm({
  equipmentUnits,
  customers,
}: {
  equipmentUnits: EquipmentUnitRef[];
  customers: CustomerRef[];
}) {
  const router = useRouter();
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );

  const [equipmentUnitId, setEquipmentUnitId] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [kind, setKind] = useState(KIND_SERVICE);
  const [estimatedStartAt, setEstimatedStartAt] = useState("");
  const [expectedCompletionAt, setExpectedCompletionAt] = useState("");
  const [siteLocation, setSiteLocation] = useState("");
  const [responsibleOfficerName, setResponsibleOfficerName] = useState("");
  const [jobDescription, setJobDescription] = useState("");
  const [customerComplaint, setCustomerComplaint] = useState("");
  const [internalRemarks, setInternalRemarks] = useState("");
  const [problemDescription, setProblemDescription] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!equipmentUnitId) {
      return;
    }

    const selected = equipmentUnits.find((unit) => unit.id === equipmentUnitId);
    if (selected) {
      setCustomerId(selected.customerId);
    }
  }, [equipmentUnitId, equipmentUnits]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!equipmentUnitId) {
      setError("Select an equipment unit.");
      return;
    }

    setBusy(true);
    try {
      const job = await apiPost<ServiceJobDto>("service/jobs", {
        equipmentUnitId,
        customerId,
        kind: Number(kind),
        estimatedStartAt: estimatedStartAt || null,
        expectedCompletionAt: expectedCompletionAt || null,
        siteLocation: siteLocation.trim() || null,
        responsibleOfficerName: responsibleOfficerName.trim() || null,
        jobDescription: jobDescription.trim() || null,
        customerComplaint: customerComplaint.trim() || null,
        internalRemarks: internalRemarks.trim() || null,
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
          <EquipmentUnitLookupField equipmentUnits={equipmentUnits} value={equipmentUnitId} onChange={setEquipmentUnitId} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select value={customerId} onChange={(e) => setCustomerId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {customerOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} - {c.name}
              </option>
            ))}
          </Select>
          <div className="mt-1 text-xs text-zinc-500">
            Customer defaults from the selected unit. Entitlement is evaluated automatically when the job is created.
          </div>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Job type</label>
          <Select value={kind} onChange={(e) => setKind(e.target.value)} required>
            <option value={KIND_SERVICE}>Service</option>
            <option value={KIND_REPAIR}>Repair</option>
            <option value={KIND_PDI}>PDI</option>
            <option value={KIND_WARRANTY}>Warranty</option>
            <option value={KIND_INSPECTION}>Inspection</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Estimated start</label>
          <Input type="date" value={estimatedStartAt} onChange={(e) => setEstimatedStartAt(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Expected completion</label>
          <Input type="date" value={expectedCompletionAt} onChange={(e) => setExpectedCompletionAt(e.target.value)} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Responsible officer / supervisor</label>
        <Input value={responsibleOfficerName} onChange={(e) => setResponsibleOfficerName(e.target.value)} placeholder="Supervisor or service advisor" />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Site location</label>
        <Input value={siteLocation} onChange={(e) => setSiteLocation(e.target.value)} placeholder="Workshop or customer site" />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Job description</label>
        <Textarea value={jobDescription} onChange={(e) => setJobDescription(e.target.value)} placeholder="Scope of work, PDI checklist summary, installation task, or service plan" />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Customer complaint / service requirement</label>
        <Textarea value={customerComplaint} onChange={(e) => setCustomerComplaint(e.target.value)} />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Problem description / intake note</label>
        <Textarea value={problemDescription} onChange={(e) => setProblemDescription(e.target.value)} required />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Internal remarks</label>
        <Textarea value={internalRemarks} onChange={(e) => setInternalRemarks(e.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Job Order"}
      </Button>
    </form>
  );
}
