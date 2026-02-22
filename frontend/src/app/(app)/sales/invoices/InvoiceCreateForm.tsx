"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type CustomerRef = { id: string; code: string; name: string };
type InvoiceDto = { id: string; number: string };

export function InvoiceCreateForm({ customers }: { customers: CustomerRef[] }) {
  const router = useRouter();
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );

  const [customerId, setCustomerId] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const invoice = await apiPost<InvoiceDto>("sales/invoices", {
        customerId,
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
      });
      router.push(`/sales/invoices/${invoice.id}`);
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
        <div>
          <label className="mb-1 block text-sm font-medium">Due date (optional)</label>
          <Input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Invoice"}
      </Button>
    </form>
  );
}

