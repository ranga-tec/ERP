"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type TechnicianRef = {
  id: string;
  code: string;
  name: string;
  defaultCostRate: number;
  defaultBillingRate: number;
  isActive: boolean;
};

type AssignmentDto = { id: string };
type DailySheetRef = { id: string; number: string; status: number };

export function ServiceJobAssignmentAddForm({
  serviceJobId,
  technicians,
  dailySheets = [],
  defaultDailySheetId = "",
  requireDailySheet = false,
  disabled,
}: {
  serviceJobId: string;
  technicians: TechnicianRef[];
  dailySheets?: DailySheetRef[];
  defaultDailySheetId?: string;
  requireDailySheet?: boolean;
  disabled?: boolean;
}) {
  const router = useRouter();
  const activeTechnicians = useMemo(
    () => technicians.filter((technician) => technician.isActive).sort((a, b) => a.code.localeCompare(b.code)),
    [technicians],
  );
  const [technicianId, setTechnicianId] = useState("");
  const selectedTechnician = useMemo(
    () => activeTechnicians.find((technician) => technician.id === technicianId) ?? null,
    [activeTechnicians, technicianId],
  );
  const [dailySheetId, setDailySheetId] = useState(defaultDailySheetId);
  const [employeeName, setEmployeeName] = useState("");
  const [role, setRole] = useState("Technician");
  const [assignedTask, setAssignedTask] = useState("");
  const [assignedDate, setAssignedDate] = useState("");
  const [workStartAt, setWorkStartAt] = useState("");
  const [workEndAt, setWorkEndAt] = useState("");
  const [normalHours, setNormalHours] = useState("0");
  const [overtimeHours, setOvertimeHours] = useState("0");
  const [dailyWorkDescription, setDailyWorkDescription] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const selectedDailySheet = useMemo(
    () =>
      dailySheets.find((sheet) => sheet.id === dailySheetId) ??
      dailySheets.find((sheet) => sheet.id === defaultDailySheetId) ??
      null,
    [dailySheets, dailySheetId, defaultDailySheetId],
  );

  useEffect(() => {
    setDailySheetId(defaultDailySheetId);
  }, [defaultDailySheetId]);

  function onTechnicianChange(value: string) {
    setTechnicianId(value);
    if (value) {
      setEmployeeName("");
    }
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const parsedNormalHours = Number(normalHours);
      const parsedOvertimeHours = Number(overtimeHours);
      if (!technicianId && !employeeName.trim()) {
        throw new Error("Select a technician or enter an employee name.");
      }

      if (requireDailySheet && !dailySheetId) {
        throw new Error("Select a daily sheet before adding labor.");
      }

      if (!assignedTask.trim()) {
        throw new Error("Assigned task is required.");
      }

      if (!Number.isFinite(parsedNormalHours) || parsedNormalHours < 0) {
        throw new Error("Normal hours must be 0 or greater.");
      }

      if (!Number.isFinite(parsedOvertimeHours) || parsedOvertimeHours < 0) {
        throw new Error("Overtime hours must be 0 or greater.");
      }

      await apiPost<AssignmentDto>(`service/jobs/${serviceJobId}/assignments`, {
        technicianId: technicianId || null,
        employeeName: technicianId ? null : employeeName.trim(),
        role: role.trim(),
        assignedTask: assignedTask.trim(),
        assignedDate: assignedDate ? new Date(assignedDate).toISOString() : null,
        workStartAt: workStartAt ? new Date(workStartAt).toISOString() : null,
        workEndAt: workEndAt ? new Date(workEndAt).toISOString() : null,
        normalHours: parsedNormalHours,
        overtimeHours: parsedOvertimeHours,
        dailyWorkDescription: dailyWorkDescription.trim() || null,
        serviceJobDailySheetId: dailySheetId || null,
      });

      setTechnicianId("");
      setDailySheetId(defaultDailySheetId);
      setEmployeeName("");
      setRole("Technician");
      setAssignedTask("");
      setAssignedDate("");
      setWorkStartAt("");
      setWorkEndAt("");
      setNormalHours("0");
      setOvertimeHours("0");
      setDailyWorkDescription("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 lg:grid-cols-2 xl:grid-cols-5">
        <div>
          <label className="mb-1 block text-sm font-medium">Daily sheet</label>
          {requireDailySheet ? (
            <div className="min-h-10 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100">
              <div className="font-medium">{selectedDailySheet?.number ?? "No daily sheet selected"}</div>
              {selectedDailySheet ? <div className="text-xs text-slate-500 dark:text-slate-400">Selected daily sheet</div> : null}
            </div>
          ) : (
            <Select value={dailySheetId} onChange={(event) => setDailySheetId(event.target.value)} disabled={disabled || busy}>
              <option value="">Unlinked</option>
              {dailySheets
                .filter((sheet) => sheet.status !== 2)
                .map((sheet) => (
                  <option key={sheet.id} value={sheet.id}>
                    {sheet.number}
                  </option>
                ))}
            </Select>
          )}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Technician</label>
          <Select value={technicianId} onChange={(event) => onTechnicianChange(event.target.value)} disabled={disabled || busy}>
            <option value="">Manual employee</option>
            {activeTechnicians.map((technician) => (
              <option key={technician.id} value={technician.id}>
                {technician.code} - {technician.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Manual employee name</label>
          {selectedTechnician ? (
            <div className="min-h-10 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100">
              <div className="font-medium">{selectedTechnician.name}</div>
              <div className="text-xs text-slate-500 dark:text-slate-400">From Technician Master</div>
            </div>
          ) : (
            <Input
              value={employeeName}
              onChange={(event) => setEmployeeName(event.target.value)}
              disabled={disabled || busy}
              placeholder="Type name only for manual employee"
            />
          )}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Role</label>
          <Input value={role} onChange={(event) => setRole(event.target.value)} disabled={disabled || busy} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Assigned date</label>
          <Input type="datetime-local" value={assignedDate} onChange={(event) => setAssignedDate(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Assigned task</label>
        <Textarea value={assignedTask} onChange={(event) => setAssignedTask(event.target.value)} disabled={disabled || busy} required />
      </div>

      <div className="grid gap-3 lg:grid-cols-2 xl:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Work start</label>
          <Input type="datetime-local" value={workStartAt} onChange={(event) => setWorkStartAt(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Work end</label>
          <Input type="datetime-local" value={workEndAt} onChange={(event) => setWorkEndAt(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Normal hours</label>
          <Input value={normalHours} onChange={(event) => setNormalHours(event.target.value)} inputMode="decimal" disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Overtime hours</label>
          <Input value={overtimeHours} onChange={(event) => setOvertimeHours(event.target.value)} inputMode="decimal" disabled={disabled || busy} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Daily work description</label>
        <Textarea value={dailyWorkDescription} onChange={(event) => setDailyWorkDescription(event.target.value)} disabled={disabled || busy} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={disabled || busy}>
        {busy ? "Assigning..." : "Add Assignment"}
      </Button>
    </form>
  );
}
