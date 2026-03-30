"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPostNoContent, apiPutNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Textarea } from "@/components/ui";

type WorkOrderTimeEntryDto = {
  id: string;
  technicianName: string;
  workDate: string;
  workDescription: string;
  hoursWorked: number;
  costRate: number;
  laborCost: number;
  billableToCustomer: boolean;
  billableHours: number;
  billingRate: number;
  taxPercent: number;
  billableTotal: number;
  effectiveBillableTotal: number;
  notes?: string | null;
  status: number;
  rejectionReason?: string | null;
  salesInvoiceId?: string | null;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Invoiced",
};

export function WorkOrderTimeEntryRow({
  workOrderId,
  entry,
}: {
  workOrderId: string;
  entry: WorkOrderTimeEntryDto;
}) {
  const router = useRouter();
  const isDraft = entry.status === 0;
  const isSubmitted = entry.status === 1;

  const [isEditing, setIsEditing] = useState(false);
  const [technicianName, setTechnicianName] = useState(entry.technicianName);
  const [workDate, setWorkDate] = useState(entry.workDate ? new Date(entry.workDate).toISOString().slice(0, 16) : "");
  const [workDescription, setWorkDescription] = useState(entry.workDescription);
  const [hoursWorked, setHoursWorked] = useState(entry.hoursWorked.toString());
  const [costRate, setCostRate] = useState(entry.costRate.toString());
  const [billableToCustomer, setBillableToCustomer] = useState(entry.billableToCustomer);
  const [billableHours, setBillableHours] = useState(entry.billableHours.toString());
  const [billingRate, setBillingRate] = useState(entry.billingRate.toString());
  const [taxPercent, setTaxPercent] = useState(entry.taxPercent.toString());
  const [notes, setNotes] = useState(entry.notes ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function resetForm() {
    setTechnicianName(entry.technicianName);
    setWorkDate(entry.workDate ? new Date(entry.workDate).toISOString().slice(0, 16) : "");
    setWorkDescription(entry.workDescription);
    setHoursWorked(entry.hoursWorked.toString());
    setCostRate(entry.costRate.toString());
    setBillableToCustomer(entry.billableToCustomer);
    setBillableHours(entry.billableHours.toString());
    setBillingRate(entry.billingRate.toString());
    setTaxPercent(entry.taxPercent.toString());
    setNotes(entry.notes ?? "");
  }

  async function saveEdit() {
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

      await apiPutNoContent(`service/work-orders/${workOrderId}/time-entries/${entry.id}`, {
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

      setIsEditing(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    if (!window.confirm("Delete this labor entry?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`service/work-orders/${workOrderId}/time-entries/${entry.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  async function submit() {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`service/work-orders/${workOrderId}/time-entries/${entry.id}/submit`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  async function approve() {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`service/work-orders/${workOrderId}/time-entries/${entry.id}/approve`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  async function reject() {
    const rejectionReason = window.prompt("Rejection reason (optional):", entry.rejectionReason ?? "");
    if (rejectionReason === null) return;

    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`service/work-orders/${workOrderId}/time-entries/${entry.id}/reject`, {
        rejectionReason: rejectionReason.trim() || null,
      });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  const previewLaborCost = Number(hoursWorked) * Number(costRate);
  const previewBillableTotal = billableToCustomer
    ? Number(billableHours) * Number(billingRate) * (1 + Number(taxPercent) / 100)
    : 0;

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={workDate} onChange={(event) => setWorkDate(event.target.value)} type="datetime-local" className="min-w-40" />
        ) : (
          new Date(entry.workDate).toLocaleString()
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={technicianName} onChange={(event) => setTechnicianName(event.target.value)} className="min-w-40" />
        ) : (
          entry.technicianName
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? (
          <div className="space-y-2">
            <Input value={workDescription} onChange={(event) => setWorkDescription(event.target.value)} className="min-w-56" />
            <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} className="min-h-16 min-w-56" />
          </div>
        ) : (
          <>
            <div>{entry.workDescription}</div>
            {entry.notes ? <div className="mt-1 text-xs text-zinc-400">{entry.notes}</div> : null}
            {entry.rejectionReason ? <div className="mt-1 text-xs text-red-600 dark:text-red-300">Rejected: {entry.rejectionReason}</div> : null}
          </>
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={hoursWorked} onChange={(event) => setHoursWorked(event.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          entry.hoursWorked
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={costRate} onChange={(event) => setCostRate(event.target.value)} inputMode="decimal" className="min-w-24" />
        ) : (
          entry.costRate.toFixed(2)
        )}
      </td>
      <td className="py-2 pr-3">{isEditing && Number.isFinite(previewLaborCost) ? previewLaborCost.toFixed(2) : entry.laborCost.toFixed(2)}</td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <div className="space-y-2">
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={billableToCustomer}
                onChange={(event) => setBillableToCustomer(event.target.checked)}
                className="h-4 w-4 rounded border-zinc-300"
              />
              Billable
            </label>
            <div className="grid gap-2 sm:grid-cols-3">
              <Input
                value={billableHours}
                onChange={(event) => setBillableHours(event.target.value)}
                inputMode="decimal"
                disabled={!billableToCustomer}
                placeholder="Hours"
              />
              <Input
                value={billingRate}
                onChange={(event) => setBillingRate(event.target.value)}
                inputMode="decimal"
                disabled={!billableToCustomer}
                placeholder="Rate"
              />
              <Input
                value={taxPercent}
                onChange={(event) => setTaxPercent(event.target.value)}
                inputMode="decimal"
                disabled={!billableToCustomer}
                placeholder="Tax %"
              />
            </div>
          </div>
        ) : entry.billableToCustomer ? (
          <div className="text-sm">
            <div>{entry.billableHours} hr</div>
            <div className="text-xs text-zinc-500">
              {entry.billingRate.toFixed(2)} @ {entry.taxPercent.toFixed(2)}% tax
            </div>
          </div>
        ) : (
          "No"
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing && Number.isFinite(previewBillableTotal) ? previewBillableTotal.toFixed(2) : entry.effectiveBillableTotal.toFixed(2)}
        {!isEditing && entry.billableToCustomer && entry.effectiveBillableTotal !== entry.billableTotal ? (
          <div className="mt-1 text-xs text-zinc-500">Covered from {entry.billableTotal.toFixed(2)}</div>
        ) : null}
      </td>
      <td className="py-2 pr-3">{statusLabel[entry.status] ?? entry.status}</td>
      <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
        {entry.salesInvoiceId ? entry.salesInvoiceId.slice(0, 8) : "-"}
      </td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          {isDraft ? (
            isEditing ? (
              <>
                <Button type="button" className="px-2 py-1 text-xs" onClick={() => void saveEdit()} disabled={busy}>
                  {busy ? "Saving..." : "Save"}
                </Button>
                <SecondaryButton
                  type="button"
                  className="px-2 py-1 text-xs"
                  onClick={() => {
                    setError(null);
                    resetForm();
                    setIsEditing(false);
                  }}
                  disabled={busy}
                >
                  Cancel
                </SecondaryButton>
              </>
            ) : (
              <>
                <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => setIsEditing(true)} disabled={busy}>
                  Edit
                </SecondaryButton>
                <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => void submit()} disabled={busy}>
                  Submit
                </SecondaryButton>
                <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => void remove()} disabled={busy}>
                  Delete
                </SecondaryButton>
              </>
            )
          ) : null}

          {isSubmitted ? (
            <>
              <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => void approve()} disabled={busy}>
                Approve
              </SecondaryButton>
              <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => void reject()} disabled={busy}>
                Reject
              </SecondaryButton>
            </>
          ) : null}
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
