"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, DecimalInput, Input, Select, Textarea } from "@/components/ui";

type WorkOrderTimeEntryDto = { id: string };
type TechnicianRef = {
  id: string;
  code: string;
  name: string;
  defaultCostRate: number;
  defaultBillingRate: number;
  isActive: boolean;
};

export function WorkOrderTimeEntryAddForm({
  workOrderId,
  technicians,
  disabled,
}: {
  workOrderId: string;
  technicians: TechnicianRef[];
  disabled?: boolean;
}) {
  const router = useRouter();
  const activeTechnicians = useMemo(
    () => technicians.filter((technician) => technician.isActive).sort((a, b) => a.code.localeCompare(b.code)),
    [technicians],
  );
  const [technicianId, setTechnicianId] = useState("");
  const [workDate, setWorkDate] = useState("");
  const [workDescription, setWorkDescription] = useState("");
  const [hoursWorked, setHoursWorked] = useState("1");
  const [costRate, setCostRate] = useState("0");
  const [billableToCustomer, setBillableToCustomer] = useState(true);
  const [billableHours, setBillableHours] = useState("1");
  // billable hours track hours worked until someone deliberately changes them, so a 12-hour day
  // cannot silently invoice as 1 hour
  const [billableHoursEdited, setBillableHoursEdited] = useState(false);
  const [billingRate, setBillingRate] = useState("0");
  const [taxPercent, setTaxPercent] = useState("0");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const previewLaborCost = useMemo(() => Number(hoursWorked) * Number(costRate), [hoursWorked, costRate]);
  const previewBillableTotal = useMemo(() => {
    if (!billableToCustomer) return 0;
    const hours = Number(billableHours);
    const rate = Number(billingRate);
    const tax = Number(taxPercent);
    return hours * rate * (1 + tax / 100);
  }, [billableHours, billingRate, billableToCustomer, taxPercent]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const parsedHoursWorked = Number(hoursWorked);
      const parsedCostRate = Number(costRate);
      const parsedBillableHours = Number(billableHours);
      const parsedBillingRate = Number(billingRate);
      const parsedTaxPercent = Number(taxPercent);

      const selectedTechnician = activeTechnicians.find((technician) => technician.id === technicianId);
      if (!selectedTechnician) {
        throw new Error("Technician is required.");
      }

      if (!workDescription.trim()) {
        throw new Error("Work description is required.");
      }

      if (!Number.isFinite(parsedHoursWorked) || parsedHoursWorked <= 0) {
        throw new Error("Hours worked must be positive.");
      }

      if (!Number.isFinite(parsedCostRate) || parsedCostRate < 0) {
        throw new Error("Cost rate must be 0 or greater.");
      }

      if (billableToCustomer) {
        if (!Number.isFinite(parsedBillableHours) || parsedBillableHours <= 0) {
          throw new Error("Billable hours must be positive.");
        }

        if (parsedBillableHours > parsedHoursWorked) {
          throw new Error("Billable hours cannot exceed worked hours.");
        }

        if (!Number.isFinite(parsedBillingRate) || parsedBillingRate < 0) {
          throw new Error("Billing rate must be 0 or greater.");
        }

        if (!Number.isFinite(parsedTaxPercent) || parsedTaxPercent < 0) {
          throw new Error("Tax percent must be 0 or greater.");
        }
      }

      await apiPost<WorkOrderTimeEntryDto>(`service/work-orders/${workOrderId}/time-entries`, {
        technicianUserId: selectedTechnician.id,
        technicianName: selectedTechnician.name,
        workDate: workDate ? new Date(workDate).toISOString() : null,
        workDescription: workDescription.trim(),
        hoursWorked: parsedHoursWorked,
        costRate: parsedCostRate,
        billableToCustomer,
        billableHours: billableToCustomer ? parsedBillableHours : null,
        billingRate: billableToCustomer ? parsedBillingRate : null,
        taxPercent: billableToCustomer ? parsedTaxPercent : null,
        notes: notes.trim() || null,
      });

      setTechnicianId("");
      setWorkDate("");
      setWorkDescription("");
      setHoursWorked("1");
      setBillableHoursEdited(false);
      setCostRate("0");
      setBillableToCustomer(true);
      setBillableHours("1");
      setBillingRate("0");
      setTaxPercent("0");
      setNotes("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 lg:grid-cols-2 xl:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Technician</label>
          <Select
            value={technicianId}
            onChange={(event) => {
              const nextId = event.target.value;
              const technician = activeTechnicians.find((candidate) => candidate.id === nextId);
              setTechnicianId(nextId);
              if (technician) {
                setCostRate(String(technician.defaultCostRate));
                setBillingRate(String(technician.defaultBillingRate));
              }
            }}
            disabled={disabled || busy}
            required
          >
            <option value="" disabled>
              Select...
            </option>
            {activeTechnicians.map((technician) => (
              <option key={technician.id} value={technician.id}>
                {technician.code} - {technician.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Work Date</label>
          <Input type="datetime-local" value={workDate} onChange={(event) => setWorkDate(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Hours Worked</label>
          <DecimalInput
            value={hoursWorked}
            onChange={(event) => {
              setHoursWorked(event.target.value);
              if (!billableHoursEdited) {
                setBillableHours(event.target.value);
              }
            }}
            disabled={disabled || busy}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Cost Rate</label>
          <DecimalInput value={costRate} onChange={(event) => setCostRate(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Work Description</label>
        <Textarea value={workDescription} onChange={(event) => setWorkDescription(event.target.value)} disabled={disabled || busy} className="min-h-20" />
      </div>

      <div className="grid gap-3 lg:grid-cols-2 xl:grid-cols-4">
        <label className="flex items-center gap-2 rounded-xl border border-[var(--card-border)] px-3 py-2 text-sm">
          <input
            type="checkbox"
            checked={billableToCustomer}
            onChange={(event) => {
              setBillableToCustomer(event.target.checked);
              if (event.target.checked && !billableHoursEdited) {
                setBillableHours(hoursWorked);
              }
            }}
            disabled={disabled || busy}
            className="h-4 w-4 rounded border-zinc-300"
          />
          Billable to customer
        </label>
        <div>
          <label className="mb-1 block text-sm font-medium">Billable Hours</label>
          <DecimalInput
            value={billableHours}
            onChange={(event) => {
              setBillableHoursEdited(true);
              setBillableHours(event.target.value);
            }}
            disabled={disabled || busy || !billableToCustomer}
          />
          {billableToCustomer && Number(billableHours) < Number(hoursWorked) ? (
            <button
              type="button"
              className="mt-1 text-xs text-amber-700 underline dark:text-amber-300"
              disabled={disabled || busy}
              onClick={() => {
                setBillableHoursEdited(false);
                setBillableHours(hoursWorked);
              }}
            >
              Only {billableHours} of {hoursWorked} hrs will be charged - bill all
            </button>
          ) : null}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Billing Rate</label>
          <DecimalInput
            value={billingRate}
            onChange={(event) => setBillingRate(event.target.value)}
            disabled={disabled || busy || !billableToCustomer}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Tax %</label>
          <DecimalInput
            value={taxPercent}
            onChange={(event) => setTaxPercent(event.target.value)}
            disabled={disabled || busy || !billableToCustomer}
          />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes</label>
        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} disabled={disabled || busy} className="min-h-20" />
      </div>

      <div className="flex flex-wrap items-center gap-3 text-sm text-zinc-500">
        <div>Labor cost: {Number.isFinite(previewLaborCost) ? previewLaborCost.toFixed(2) : "-"}</div>
        <div>Billable total: {Number.isFinite(previewBillableTotal) ? previewBillableTotal.toFixed(2) : "-"}</div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={disabled || busy}>
        {busy ? "Saving..." : "Add Labor Entry"}
      </Button>
    </form>
  );
}
