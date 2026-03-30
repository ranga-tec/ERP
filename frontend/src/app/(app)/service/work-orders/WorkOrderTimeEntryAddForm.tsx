"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Textarea } from "@/components/ui";

type WorkOrderTimeEntryDto = { id: string };

export function WorkOrderTimeEntryAddForm({
  workOrderId,
  disabled,
}: {
  workOrderId: string;
  disabled?: boolean;
}) {
  const router = useRouter();
  const [technicianName, setTechnicianName] = useState("");
  const [workDate, setWorkDate] = useState("");
  const [workDescription, setWorkDescription] = useState("");
  const [hoursWorked, setHoursWorked] = useState("1");
  const [costRate, setCostRate] = useState("0");
  const [billableToCustomer, setBillableToCustomer] = useState(true);
  const [billableHours, setBillableHours] = useState("1");
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

      if (!technicianName.trim()) {
        throw new Error("Technician name is required.");
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
        technicianUserId: null,
        technicianName: technicianName.trim(),
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

      setTechnicianName("");
      setWorkDate("");
      setWorkDescription("");
      setHoursWorked("1");
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
          <Input value={technicianName} onChange={(event) => setTechnicianName(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Work Date</label>
          <Input type="datetime-local" value={workDate} onChange={(event) => setWorkDate(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Hours Worked</label>
          <Input value={hoursWorked} onChange={(event) => setHoursWorked(event.target.value)} inputMode="decimal" disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Cost Rate</label>
          <Input value={costRate} onChange={(event) => setCostRate(event.target.value)} inputMode="decimal" disabled={disabled || busy} />
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
              if (event.target.checked && (!billableHours || Number(billableHours) <= 0)) {
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
          <Input
            value={billableHours}
            onChange={(event) => setBillableHours(event.target.value)}
            inputMode="decimal"
            disabled={disabled || busy || !billableToCustomer}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Billing Rate</label>
          <Input
            value={billingRate}
            onChange={(event) => setBillingRate(event.target.value)}
            inputMode="decimal"
            disabled={disabled || busy || !billableToCustomer}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Tax %</label>
          <Input
            value={taxPercent}
            onChange={(event) => setTaxPercent(event.target.value)}
            inputMode="decimal"
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
