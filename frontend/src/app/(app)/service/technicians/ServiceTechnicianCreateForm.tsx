"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input } from "@/components/ui";

export function ServiceTechnicianCreateForm() {
  const router = useRouter();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [defaultCostRate, setDefaultCostRate] = useState("0");
  const [defaultBillingRate, setDefaultBillingRate] = useState("0");
  const [phone, setPhone] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPost("service/technicians", {
        code: code.trim(),
        name: name.trim(),
        defaultCostRate: Number(defaultCostRate),
        defaultBillingRate: Number(defaultBillingRate),
        phone: phone.trim() || null,
        notes: notes.trim() || null,
      });
      setCode("");
      setName("");
      setDefaultCostRate("0");
      setDefaultBillingRate("0");
      setPhone("");
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
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(event) => setCode(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(event) => setName(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Default cost rate</label>
          <Input value={defaultCostRate} onChange={(event) => setDefaultCostRate(event.target.value)} inputMode="decimal" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Default billing rate</label>
          <Input value={defaultBillingRate} onChange={(event) => setDefaultBillingRate(event.target.value)} inputMode="decimal" />
        </div>
      </div>
      <div className="grid gap-3 md:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Phone</label>
          <Input value={phone} onChange={(event) => setPhone(event.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Notes</label>
          <Input value={notes} onChange={(event) => setNotes(event.target.value)} />
        </div>
      </div>
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900">{error}</div> : null}
      <Button type="submit" disabled={busy}>{busy ? "Creating..." : "Create Technician"}</Button>
    </form>
  );
}
