"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Textarea } from "@/components/ui";

type ProgressUpdateDto = { id: string };

export function ServiceJobProgressUpdateAddForm({ serviceJobId, disabled }: { serviceJobId: string; disabled?: boolean }) {
  const router = useRouter();
  const [progressDate, setProgressDate] = useState("");
  const [workCompleted, setWorkCompleted] = useState("");
  const [workPending, setWorkPending] = useState("");
  const [problemsFound, setProblemsFound] = useState("");
  const [additionalPartsRequired, setAdditionalPartsRequired] = useState("");
  const [additionalLaborRequired, setAdditionalLaborRequired] = useState("");
  const [customerInstructions, setCustomerInstructions] = useState("");
  const [siteIssues, setSiteIssues] = useState("");
  const [technicianNotes, setTechnicianNotes] = useState("");
  const [supervisorNotes, setSupervisorNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      if (!workCompleted.trim()) {
        throw new Error("Work completed is required.");
      }

      await apiPost<ProgressUpdateDto>(`service/jobs/${serviceJobId}/progress-updates`, {
        progressDate: progressDate ? new Date(progressDate).toISOString() : null,
        workCompleted: workCompleted.trim(),
        workPending: workPending.trim() || null,
        problemsFound: problemsFound.trim() || null,
        additionalPartsRequired: additionalPartsRequired.trim() || null,
        additionalLaborRequired: additionalLaborRequired.trim() || null,
        customerInstructions: customerInstructions.trim() || null,
        siteIssues: siteIssues.trim() || null,
        technicianNotes: technicianNotes.trim() || null,
        supervisorNotes: supervisorNotes.trim() || null,
      });

      setProgressDate("");
      setWorkCompleted("");
      setWorkPending("");
      setProblemsFound("");
      setAdditionalPartsRequired("");
      setAdditionalLaborRequired("");
      setCustomerInstructions("");
      setSiteIssues("");
      setTechnicianNotes("");
      setSupervisorNotes("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Progress date</label>
        <Input type="datetime-local" value={progressDate} onChange={(event) => setProgressDate(event.target.value)} disabled={disabled || busy} />
      </div>
      <div className="grid gap-3 lg:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Work completed</label>
          <Textarea value={workCompleted} onChange={(event) => setWorkCompleted(event.target.value)} disabled={disabled || busy} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Work pending</label>
          <Textarea value={workPending} onChange={(event) => setWorkPending(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Problems found</label>
          <Textarea value={problemsFound} onChange={(event) => setProblemsFound(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Additional parts required</label>
          <Textarea value={additionalPartsRequired} onChange={(event) => setAdditionalPartsRequired(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Additional labor required</label>
          <Textarea value={additionalLaborRequired} onChange={(event) => setAdditionalLaborRequired(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Customer instructions</label>
          <Textarea value={customerInstructions} onChange={(event) => setCustomerInstructions(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Site issues</label>
          <Textarea value={siteIssues} onChange={(event) => setSiteIssues(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Technician notes</label>
          <Textarea value={technicianNotes} onChange={(event) => setTechnicianNotes(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Supervisor notes</label>
          <Textarea value={supervisorNotes} onChange={(event) => setSupervisorNotes(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={disabled || busy}>
        {busy ? "Saving..." : "Add Progress Update"}
      </Button>
    </form>
  );
}
