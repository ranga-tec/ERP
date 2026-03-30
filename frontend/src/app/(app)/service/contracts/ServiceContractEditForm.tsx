"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type CustomerRef = { id: string; code: string; name: string };
type EquipmentUnitRef = { id: string; serialNumber: string; customerId: string };
type ServiceContractDto = {
  id: string;
  customerId: string;
  equipmentUnitId: string;
  contractType: number;
  coverage: number;
  startDate: string;
  endDate: string;
  notes?: string | null;
  isActive: boolean;
};

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

function toDateInput(value: string): string {
  return value ? new Date(value).toISOString().slice(0, 10) : "";
}

export function ServiceContractEditForm({
  contract,
  customers,
  equipmentUnits,
}: {
  contract: ServiceContractDto;
  customers: CustomerRef[];
  equipmentUnits: EquipmentUnitRef[];
}) {
  const router = useRouter();
  const [customerId, setCustomerId] = useState(contract.customerId);
  const [equipmentUnitId, setEquipmentUnitId] = useState(contract.equipmentUnitId);
  const [contractType, setContractType] = useState(String(contract.contractType));
  const [coverage, setCoverage] = useState(String(contract.coverage));
  const [startDate, setStartDate] = useState(toDateInput(contract.startDate));
  const [endDate, setEndDate] = useState(toDateInput(contract.endDate));
  const [notes, setNotes] = useState(contract.notes ?? "");
  const [isActive, setIsActive] = useState(contract.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
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
      await apiPut(`service/contracts/${contract.id}`, {
        customerId,
        equipmentUnitId,
        contractType: Number(contractType),
        coverage: Number(coverage),
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        notes: notes.trim() || null,
        isActive: isActive === "true",
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
          <Select value={equipmentUnitId} onChange={(event) => setEquipmentUnitId(event.target.value)} required>
            {equipmentUnits.map((unit) => (
              <option key={unit.id} value={unit.id}>
                {unit.serialNumber}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select value={customerId} onChange={(event) => setCustomerId(event.target.value)} required>
            {customers.map((customer) => (
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

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Status</label>
          <Select value={isActive} onChange={(event) => setIsActive(event.target.value)}>
            <option value="true">Active</option>
            <option value="false">Inactive</option>
          </Select>
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
        {busy ? "Saving..." : "Save Contract"}
      </Button>
    </form>
  );
}
