"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Input, SecondaryButton, Select } from "@/components/ui";

type EntryRef = {
  id: string;
  referenceType: string;
  referenceId: string;
  outstanding: number;
  postedAt: string;
};

export function CreditNoteActions({
  creditNoteId,
  allocateMode,
  entries,
}: {
  creditNoteId: string;
  allocateMode: "ar" | "ap";
  entries: EntryRef[];
}) {
  const router = useRouter();
  const [entryId, setEntryId] = useState("");
  const [amount, setAmount] = useState("0");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const outstanding = useMemo(
    () => entries.filter((e) => e.outstanding > 0).sort((a, b) => b.postedAt.localeCompare(a.postedAt)),
    [entries],
  );

  async function allocate(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const amt = Number(amount);
      if (Number.isNaN(amt) || amt <= 0) {
        throw new Error("Amount must be positive.");
      }

      await apiPostNoContent(`finance/credit-notes/${creditNoteId}/allocate/${allocateMode}`, {
        entryId,
        amount: amt,
      });
      router.refresh();
      setAmount("0");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function autoAllocate() {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`finance/credit-notes/${creditNoteId}/auto-allocate`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2">
        <SecondaryButton type="button" disabled={busy} onClick={autoAllocate}>
          {busy ? "Allocating..." : "Auto allocate"}
        </SecondaryButton>
      </div>

      <form onSubmit={allocate} className="space-y-2">
        <div className="grid gap-2 sm:grid-cols-3">
          <div className="sm:col-span-2">
            <label className="mb-1 block text-sm font-medium">Outstanding entry</label>
            <Select value={entryId} onChange={(e) => setEntryId(e.target.value)} required>
              <option value="" disabled>
                Select...
              </option>
              {outstanding.map((x) => (
                <option key={x.id} value={x.id}>
                  {x.referenceType}:{x.referenceId} (outstanding {x.outstanding})
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Amount</label>
            <Input value={amount} onChange={(e) => setAmount(e.target.value)} inputMode="decimal" required />
          </div>
        </div>
        <SecondaryButton type="submit" disabled={busy || outstanding.length === 0}>
          {busy ? "Allocating..." : "Allocate"}
        </SecondaryButton>
      </form>

      {error ? <div className="text-sm text-red-600 dark:text-red-400">{error}</div> : null}
    </div>
  );
}

