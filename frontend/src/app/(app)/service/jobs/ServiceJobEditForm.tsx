"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
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
type ServiceJobDto = {
  id: string;
  equipmentUnitId: string;
  customerId: string;
  kind: number;
  estimatedStartAt?: string | null;
  expectedCompletionAt?: string | null;
  siteLocation?: string | null;
  jobDescription?: string | null;
  customerComplaint?: string | null;
  internalRemarks?: string | null;
  responsibleOfficerName?: string | null;
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
  const [estimatedStartAt, setEstimatedStartAt] = useState(job.estimatedStartAt?.slice(0, 10) ?? "");
  const [expectedCompletionAt, setExpectedCompletionAt] = useState(job.expectedCompletionAt?.slice(0, 10) ?? "");
  const [siteLocation, setSiteLocation] = useState(job.siteLocation ?? "");
  const [responsibleOfficerName, setResponsibleOfficerName] = useState(job.responsibleOfficerName ?? "");
  const [jobDescription, setJobDescription] = useState(job.jobDescription ?? "");
  const [customerComplaint, setCustomerComplaint] = useState(job.customerComplaint ?? "");
  const [internalRemarks, setInternalRemarks] = useState(job.internalRemarks ?? "");
  const [problemDescription, setProblemDescription] = useState(job.problemDescription);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setEquipmentUnitId(job.equipmentUnitId);
    setCustomerId(job.customerId);
    setKind(String(job.kind));
    setEstimatedStartAt(job.estimatedStartAt?.slice(0, 10) ?? "");
    setExpectedCompletionAt(job.expectedCompletionAt?.slice(0, 10) ?? "");
    setSiteLocation(job.siteLocation ?? "");
    setResponsibleOfficerName(job.responsibleOfficerName ?? "");
    setJobDescription(job.jobDescription ?? "");
    setCustomerComplaint(job.customerComplaint ?? "");
    setInternalRemarks(job.internalRemarks ?? "");
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
        estimatedStartAt: estimatedStartAt || null,
        expectedCompletionAt: expectedCompletionAt || null,
        siteLocation: siteLocation.trim() || null,
        responsibleOfficerName: responsibleOfficerName.trim() || null,
        jobDescription: jobDescription.trim() || null,
        customerComplaint: customerComplaint.trim() || null,
        internalRemarks: internalRemarks.trim() || null,
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
          <label className="mb-1 block text-sm font-medium">Estimated start</label>
          <Input type="date" value={estimatedStartAt} onChange={(event) => setEstimatedStartAt(event.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Expected completion</label>
          <Input type="date" value={expectedCompletionAt} onChange={(event) => setExpectedCompletionAt(event.target.value)} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Responsible officer / supervisor</label>
        <Input value={responsibleOfficerName} onChange={(event) => setResponsibleOfficerName(event.target.value)} />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Site location</label>
        <Input value={siteLocation} onChange={(event) => setSiteLocation(event.target.value)} />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Job description</label>
        <Textarea value={jobDescription} onChange={(event) => setJobDescription(event.target.value)} />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Customer complaint / service requirement</label>
        <Textarea value={customerComplaint} onChange={(event) => setCustomerComplaint(event.target.value)} />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Problem description / intake note</label>
        <Textarea value={problemDescription} onChange={(event) => setProblemDescription(event.target.value)} required />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Internal remarks</label>
        <Textarea value={internalRemarks} onChange={(event) => setInternalRemarks(event.target.value)} />
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
