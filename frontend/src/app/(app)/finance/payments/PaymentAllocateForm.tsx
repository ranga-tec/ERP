"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type EntryRef = { id: string; referenceType: string; referenceId: string; outstanding: number };

export function PaymentAllocateForm({
  paymentId,
  kind,
  entries,
  maxAmount,
}: {
  paymentId: string;
  kind: "ar" | "ap";
  entries: EntryRef[];
  maxAmount: number;
}) {
  const router = useRouter();
  const entryOptions = useMemo(() => entries.slice().sort((a, b) => b.outstanding - a.outstanding), [entries]);

  const [entryId, setEntryId] = useState("");
  const [amount, setAmount] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const amt = Number(amount);
      if (Number.isNaN(amt) || amt <= 0) {
        throw new Error("Amount must be positive.");
      }
      await apiPostNoContent(`finance/payments/${paymentId}/allocate/${kind}`, { entryId, amount: amt });
      setEntryId("");
      setAmount("");
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
          <label className="mb-1 block text-sm font-medium">{kind.toUpperCase()} entry</label>
          <Select value={entryId} onChange={(e) => setEntryId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {entryOptions.map((e) => (
              <option key={e.id} value={e.id}>
                {e.referenceType}:{e.referenceId.slice(0, 8)} (out {e.outstanding})
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Amount</label>
          <Input value={amount} onChange={(e) => setAmount(e.target.value)} inputMode="decimal" placeholder={maxAmount.toString()} required />
          <div className="mt-1 text-xs text-zinc-500">Unallocated remaining: {maxAmount}</div>
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Allocating..." : "Allocate"}
      </Button>
    </form>
  );
}

