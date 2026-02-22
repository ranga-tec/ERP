"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Select } from "@/components/ui";

type SupplierRef = { id: string; code: string; name: string };
type RfqDto = { id: string; number: string; supplierId: string };

export function RfqCreateForm({ suppliers }: { suppliers: SupplierRef[] }) {
  const router = useRouter();
  const supplierOptions = useMemo(
    () => suppliers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [suppliers],
  );

  const [supplierId, setSupplierId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const rfq = await apiPost<RfqDto>("procurement/rfqs", { supplierId });
      router.push(`/procurement/rfqs/${rfq.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Supplier</label>
        <Select value={supplierId} onChange={(e) => setSupplierId(e.target.value)} required>
          <option value="" disabled>
            Select…
          </option>
          {supplierOptions.map((s) => (
            <option key={s.id} value={s.id}>
              {s.code} — {s.name}
            </option>
          ))}
        </Select>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating…" : "Create RFQ"}
      </Button>
    </form>
  );
}

