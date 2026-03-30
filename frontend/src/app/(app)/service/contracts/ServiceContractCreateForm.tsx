"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type CustomerRef = { id: string; code: string; name: string };
type EquipmentUnitRef = { id: string; serialNumber: string; customerId: string };
type ServiceContractDto = { id: string };

const contractTypeOptions = [
  { value: "0", label: "Annual Maintenance" },
  { value: "1", label: "Service Level Agreement" },
  { value: "2", label: "Warranty Extension" },
];

const coverageOptions = [
  { value: "1", label: "Inspection Only" },
  { value: "2", label: "Labor Only" },
  { value: "3", label: "Parts Only" },
  { value: "4", label: "Labor and Parts" },
];

export function ServiceContractCreateForm({
  customers,
  equipmentUnits,
}: {
  customers: CustomerRef[];
  equipmentUnits: EquipmentUnitRef[];
}) {
  const router = useRouter();
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );
  const equipmentOptions = useMemo(
    () => equipmentUnits.slice().sort((a, b) => a.serialNumber.localeCompare(b.serialNumber)),
    [equipmentUnits],
  );

  const [customerId, setCustomerId] = useState("");
  const [equipmentUnitId, setEquipmentUnitId] = useState("");
  const [contractType, setContractType] = useState("0");
  const [coverage, setCoverage] = useState("4");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [notes, setNotes] = useState("");
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

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const contract = await apiPost<ServiceContractDto>("service/contracts", {
        customerId,
        equipmentUnitId,
        contractType: Number(contractType),
        coverage: Number(coverage),
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        notes: notes.trim() || null,
      });
      router.push(`/service/contracts/${contract.id}`);
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
          <Select value={equipmentUnitId} onChange={(event) => setEquipmentUnitId(event.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {equipmentOptions.map((unit) => (
              <option key={unit.id} value={unit.id}>
                {unit.serialNumber}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select value={customerId} onChange={(event) => setCustomerId(event.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {customerOptions.map((customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.code} - {customer.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Contract type</label>
          <Select value={contractType} onChange={(event) => setContractType(event.target.value)} required>
            {contractTypeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Coverage</label>
          <Select value={coverage} onChange={(event) => setCoverage(event.target.value)} required>
            {coverageOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Start date</label>
          <Input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">End date</label>
          <Input type="date" value={endDate} onChange={(event) => setEndDate(event.target.value)} required />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Contract"}
      </Button>
    </form>
  );
}
