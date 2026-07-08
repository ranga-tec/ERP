"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input } from "@/components/ui";

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

export function ServiceTechnicianEditForm({ technician }: { technician: ServiceTechnicianDto }) {
  const router = useRouter();
  const [code, setCode] = useState(technician.code);
  const [name, setName] = useState(technician.name);
  const [defaultCostRate, setDefaultCostRate] = useState(String(technician.defaultCostRate));
  const [defaultBillingRate, setDefaultBillingRate] = useState(String(technician.defaultBillingRate));
  const [phone, setPhone] = useState(technician.phone ?? "");
  const [notes, setNotes] = useState(technician.notes ?? "");
  const [isActive, setIsActive] = useState(technician.isActive);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
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
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(event) => setCode(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(event) => setName(event.target.value)} required />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Cost rate</label>
          <Input value={defaultCostRate} onChange={(event) => setDefaultCostRate(event.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Billing rate</label>
          <Input value={defaultBillingRate} onChange={(event) => setDefaultBillingRate(event.target.value)} inputMode="decimal" required />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Phone</label>
          <Input value={phone} onChange={(event) => setPhone(event.target.value)} />
        </div>
        <label className="mt-6 flex items-center gap-2 text-sm">
          <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
          Active
        </label>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes</label>
        <Input value={notes} onChange={(event) => setNotes(event.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving..." : "Save Technician"}
      </Button>
    </form>
  );
}
