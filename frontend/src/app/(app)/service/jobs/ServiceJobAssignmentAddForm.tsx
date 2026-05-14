"use client";

import { useMemo, useState } from "react";
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

export function ServiceJobAssignmentAddForm({
  serviceJobId,
  technicians,
  disabled,
}: {
  serviceJobId: string;
  technicians: TechnicianRef[];
  disabled?: boolean;
}) {
  const router = useRouter();
  const activeTechnicians = useMemo(
    () => technicians.filter((technician) => technician.isActive).sort((a, b) => a.code.localeCompare(b.code)),
    [technicians],
  );
  const [technicianId, setTechnicianId] = useState("");
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
      });

      setTechnicianId("");
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
      <div className="grid gap-3 lg:grid-cols-2 xl:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Technician</label>
          <Select value={technicianId} onChange={(event) => setTechnicianId(event.target.value)} disabled={disabled || busy}>
            <option value="">Manual employee</option>
            {activeTechnicians.map((technician) => (
              <option key={technician.id} value={technician.id}>
                {technician.code} - {technician.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Employee name</label>
          <Input value={employeeName} onChange={(event) => setEmployeeName(event.target.value)} disabled={disabled || busy || Boolean(technicianId)} />
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
