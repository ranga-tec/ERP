"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Select } from "@/components/ui";

type CustomerRef = { id: string; code: string; name: string };
type SalesOrderDto = { id: string; number: string };

export function SalesOrderCreateForm({ customers }: { customers: CustomerRef[] }) {
  const router = useRouter();
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );

  const [customerId, setCustomerId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const so = await apiPost<SalesOrderDto>("sales/orders", { customerId });
      router.push(`/sales/orders/${so.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Customer</label>
        <Select value={customerId} onChange={(e) => setCustomerId(e.target.value)} required>
          <option value="" disabled>
            Select...
          </option>
          {customerOptions.map((c) => (
            <option key={c.id} value={c.id}>
              {c.code} — {c.name}
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
        {busy ? "Creating..." : "Create Sales Order"}
      </Button>
    </form>
  );
}

