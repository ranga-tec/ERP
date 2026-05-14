"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Textarea } from "@/components/ui";

type DailySheetDto = { id: string };

export function ServiceJobDailySheetCreateForm({ serviceJobId, disabled }: { serviceJobId: string; disabled?: boolean }) {
  const router = useRouter();
  const [sheetDate, setSheetDate] = useState("");
  const [preparedByName, setPreparedByName] = useState("");
  const [siteLocation, setSiteLocation] = useState("");
  const [shiftName, setShiftName] = useState("");
  const [weatherOrSiteCondition, setWeatherOrSiteCondition] = useState("");
  const [workPlanned, setWorkPlanned] = useState("");
  const [workCompleted, setWorkCompleted] = useState("");
  const [workPending, setWorkPending] = useState("");
  const [problemsFound, setProblemsFound] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      if (!workPlanned.trim()) throw new Error("Planned work is required.");
      await apiPost<DailySheetDto>(`service/jobs/${serviceJobId}/daily-sheets`, {
        sheetDate: sheetDate ? new Date(sheetDate).toISOString() : null,
        preparedByName: preparedByName.trim() || null,
        siteLocation: siteLocation.trim() || null,
        shiftName: shiftName.trim() || null,
        weatherOrSiteCondition: weatherOrSiteCondition.trim() || null,
        workPlanned: workPlanned.trim(),
        workCompleted: workCompleted.trim() || null,
        workPending: workPending.trim() || null,
        problemsFound: problemsFound.trim() || null,
        customerInstructions: null,
        technicianNotes: null,
        supervisorNotes: null,
      });
      setSheetDate("");
      setPreparedByName("");
      setSiteLocation("");
      setShiftName("");
      setWeatherOrSiteCondition("");
      setWorkPlanned("");
      setWorkCompleted("");
      setWorkPending("");
      setProblemsFound("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 lg:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Date / time</label>
          <Input type="datetime-local" value={sheetDate} onChange={(event) => setSheetDate(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Prepared by</label>
          <Input value={preparedByName} onChange={(event) => setPreparedByName(event.target.value)} disabled={disabled || busy} placeholder="Defaults to signed-in user" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Site / location</label>
          <Input value={siteLocation} onChange={(event) => setSiteLocation(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Shift</label>
          <Input value={shiftName} onChange={(event) => setShiftName(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium">Site condition</label>
        <Input value={weatherOrSiteCondition} onChange={(event) => setWeatherOrSiteCondition(event.target.value)} disabled={disabled || busy} />
      </div>
      <div className="grid gap-3 lg:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Planned work</label>
          <Textarea value={workPlanned} onChange={(event) => setWorkPlanned(event.target.value)} disabled={disabled || busy} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Completed work</label>
          <Textarea value={workCompleted} onChange={(event) => setWorkCompleted(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Pending work</label>
          <Textarea value={workPending} onChange={(event) => setWorkPending(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Problems found</label>
          <Textarea value={problemsFound} onChange={(event) => setProblemsFound(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
      <Button type="submit" disabled={disabled || busy}>{busy ? "Creating..." : "Create Daily Sheet"}</Button>
    </form>
  );
}
