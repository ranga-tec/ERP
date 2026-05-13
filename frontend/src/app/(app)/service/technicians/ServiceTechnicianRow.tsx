"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton } from "@/components/ui";

type ServiceTechnicianDto = {
  id: string;
  code: string;
  name: string;
  defaultCostRate: number;
  defaultBillingRate: number;
  phone?: string | null;
  notes?: string | null;
  isActive: boolean;
};

export function ServiceTechnicianRow({ technician }: { technician: ServiceTechnicianDto }) {
  const router = useRouter();
  const [editing, setEditing] = useState(false);
  const [code, setCode] = useState(technician.code);
  const [name, setName] = useState(technician.name);
  const [defaultCostRate, setDefaultCostRate] = useState(String(technician.defaultCostRate));
  const [defaultBillingRate, setDefaultBillingRate] = useState(String(technician.defaultBillingRate));
  const [phone, setPhone] = useState(technician.phone ?? "");
  const [notes, setNotes] = useState(technician.notes ?? "");
  const [isActive, setIsActive] = useState(technician.isActive);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function reset() {
    setCode(technician.code);
    setName(technician.name);
    setDefaultCostRate(String(technician.defaultCostRate));
    setDefaultBillingRate(String(technician.defaultBillingRate));
    setPhone(technician.phone ?? "");
    setNotes(technician.notes ?? "");
    setIsActive(technician.isActive);
    setError(null);
  }

  async function save() {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`service/technicians/${technician.id}`, {
        code: code.trim(),
        name: name.trim(),
        defaultCostRate: Number(defaultCostRate),
        defaultBillingRate: Number(defaultBillingRate),
        phone: phone.trim() || null,
        notes: notes.trim() || null,
        isActive,
      });
      setEditing(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    if (!window.confirm("Delete this technician?")) return;
    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`service/technicians/${technician.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">
        {editing ? <Input value={code} onChange={(event) => setCode(event.target.value)} className="min-w-28" /> : technician.code}
      </td>
      <td className="py-2 pr-3">
        {editing ? <Input value={name} onChange={(event) => setName(event.target.value)} className="min-w-44" /> : technician.name}
      </td>
      <td className="py-2 pr-3">
        {editing ? <Input value={defaultCostRate} onChange={(event) => setDefaultCostRate(event.target.value)} inputMode="decimal" className="min-w-24" /> : technician.defaultCostRate.toFixed(2)}
      </td>
      <td className="py-2 pr-3">
        {editing ? <Input value={defaultBillingRate} onChange={(event) => setDefaultBillingRate(event.target.value)} inputMode="decimal" className="min-w-24" /> : technician.defaultBillingRate.toFixed(2)}
      </td>
      <td className="py-2 pr-3">
        {editing ? <Input value={phone} onChange={(event) => setPhone(event.target.value)} className="min-w-36" /> : technician.phone ?? "-"}
      </td>
      <td className="py-2 pr-3">
        {editing ? <Input value={notes} onChange={(event) => setNotes(event.target.value)} className="min-w-52" /> : technician.notes ?? "-"}
      </td>
      <td className="py-2 pr-3">
        {editing ? (
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
            Active
          </label>
        ) : technician.isActive ? (
          "Active"
        ) : (
          "Inactive"
        )}
      </td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap gap-2">
          {editing ? (
            <>
              <Button type="button" className="px-2 py-1 text-xs" onClick={() => void save()} disabled={busy}>
                Save
              </Button>
              <SecondaryButton
                type="button"
                className="px-2 py-1 text-xs"
                onClick={() => {
                  reset();
                  setEditing(false);
                }}
                disabled={busy}
              >
                Cancel
              </SecondaryButton>
            </>
          ) : (
            <>
              <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => setEditing(true)} disabled={busy}>
                Edit
              </SecondaryButton>
              <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => void remove()} disabled={busy}>
                Delete
              </SecondaryButton>
            </>
          )}
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
