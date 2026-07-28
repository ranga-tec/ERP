"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type FundRef = { id: string; code: string; name: string };
type PettyCashRequestDto = { id: string; number: string };

export function PettyCashRequestCreateForm({ funds }: { funds: FundRef[] }) {
  const router = useRouter();
  const [pettyCashFundId, setPettyCashFundId] = useState(funds[0]?.id ?? "");
  const [neededByAt, setNeededByAt] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const request = await apiPost<PettyCashRequestDto>("finance/petty-cash-requests", {
        pettyCashFundId,
        neededByAt: neededByAt ? new Date(neededByAt).toISOString() : null,
        notes: notes.trim() || null,
      });
      // Straight to the detail page: a request is nothing until it has category lines, and that is
      // where they are added.
      router.push(`/finance/petty-cash-requests/${request.id}`);
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
          <label className="mb-1 block text-sm font-medium">Pay into fund</label>
          <Select value={pettyCashFundId} onChange={(e) => setPettyCashFundId(e.target.value)} required>
            <option value="" disabled>Select...</option>
            {funds.map((fund) => (
              <option key={fund.id} value={fund.id}>{fund.code} - {fund.name}</option>
            ))}
          </Select>
          <p className="mt-1 text-xs text-zinc-500">
            The float the approved money is paid into. Each category is tracked as a sub-account of it.
          </p>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Needed by (optional)</label>
          <Input type="date" value={neededByAt} onChange={(e) => setNeededByAt(e.target.value)} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Anything head office needs to know about this batch of requests."
        />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      {funds.length === 0 ? (
        <div className="text-xs text-amber-700 dark:text-amber-400">
          Create an active petty cash fund first - a request has to be paid into one.
        </div>
      ) : null}

      <Button type="submit" disabled={busy || !pettyCashFundId}>
        {busy ? "Creating..." : "Create Request"}
      </Button>
    </form>
  );
}
