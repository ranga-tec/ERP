"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Textarea } from "@/components/ui";
import { describeReturnCategory, money, type ReturnCandidateDto } from "./types";

type CreatedReturnDto = { id: string };

export function PettyCashReturnCreateForm({ candidates }: { candidates: ReturnCandidateDto[] }) {
  const router = useRouter();
  const funds = useMemo(
    () => Array.from(new Map(candidates.map((candidate) => [candidate.pettyCashFundId, candidate.pettyCashFundCode])).entries()),
    [candidates],
  );
  const [fundId, setFundId] = useState(funds[0]?.[0] ?? "");
  const [amounts, setAmounts] = useState<Record<string, string>>({});
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const visible = candidates.filter((candidate) => candidate.pettyCashFundId === fundId);
  const selectedLines = visible.flatMap((candidate) => {
    const amount = Number(amounts[candidate.pettyCashRequestLineId] ?? 0);
    return amount > 0 ? [{ candidate, amount }] : [];
  });
  const invalid = selectedLines.some(({ candidate, amount }) =>
    !Number.isFinite(amount) || amount <= 0 || amount > candidate.availableToReturn || candidate.openIouCount > 0,
  );
  const total = selectedLines.reduce((sum, line) => sum + line.amount, 0);

  function selectFullBalance(candidate: ReturnCandidateDto, checked: boolean) {
    setAmounts((current) => ({
      ...current,
      [candidate.pettyCashRequestLineId]: checked ? String(candidate.availableToReturn) : "",
    }));
  }

  async function createReturn() {
    setError(null);
    setBusy(true);
    try {
      const created = await apiPost<CreatedReturnDto>("finance/petty-cash-returns", {
        pettyCashFundId: fundId,
        notes: notes.trim() || null,
        lines: selectedLines.map(({ candidate, amount }) => ({
          pettyCashRequestLineId: candidate.pettyCashRequestLineId,
          amount,
        })),
      });
      router.push(`/finance/petty-cash-returns/${created.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  if (funds.length === 0) {
    return <div className="text-sm text-zinc-500">No funded category currently has cash available to return.</div>;
  }

  return (
    <div className="space-y-4">
      <div>
        <label className="mb-1 block text-sm font-medium">Petty cash fund</label>
        <select
          className="w-full rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-2 text-sm"
          value={fundId}
          onChange={(event) => {
            setFundId(event.target.value);
            setAmounts({});
          }}
        >
          {funds.map(([id, code]) => <option key={id} value={id}>{code}</option>)}
        </select>
      </div>

      <div className="max-h-[26rem] space-y-2 overflow-y-auto pr-1">
        {visible.map((candidate) => {
          const isBlocked = candidate.openIouCount > 0;
          const amount = amounts[candidate.pettyCashRequestLineId] ?? "";
          return (
            <div key={candidate.pettyCashRequestLineId} className="rounded-md border border-[var(--card-border)] p-3">
              <div className="flex items-start gap-3">
                <input
                  className="mt-1 h-4 w-4"
                  type="checkbox"
                  checked={Number(amount) > 0}
                  disabled={isBlocked}
                  onChange={(event) => selectFullBalance(candidate, event.target.checked)}
                  aria-label={`Select ${candidate.purpose}`}
                />
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-baseline justify-between gap-2">
                    <div className="text-sm font-semibold">
                      {describeReturnCategory(candidate.category, candidate.serviceJobNumber, candidate.customCategoryName)}
                    </div>
                    <div className="font-mono text-xs text-zinc-500">{candidate.requestNumber}</div>
                  </div>
                  <div className="mt-1 text-xs text-zinc-500">{candidate.purpose}</div>
                  <div className="mt-2 flex flex-wrap items-end gap-3">
                    <div className="text-xs text-zinc-500">
                      Available<br /><span className="font-semibold text-zinc-800 dark:text-zinc-200">{money(candidate.availableToReturn)}</span>
                    </div>
                    {candidate.pendingReturnAmount > 0 ? (
                      <div className="text-xs text-amber-700 dark:text-amber-400">
                        Pending elsewhere<br />{money(candidate.pendingReturnAmount)}
                      </div>
                    ) : null}
                    {candidate.pendingReallocationAmount > 0 ? (
                      <div className="text-xs text-amber-700 dark:text-amber-400">
                        Pending reallocation<br />{money(candidate.pendingReallocationAmount)}
                      </div>
                    ) : null}
                    <div className="ml-auto w-36">
                      <label className="mb-1 block text-xs font-medium">Amount to return</label>
                      <Input
                        inputMode="decimal"
                        value={amount}
                        disabled={isBlocked}
                        onChange={(event) => setAmounts((current) => ({ ...current, [candidate.pettyCashRequestLineId]: event.target.value }))}
                        placeholder="0.00"
                      />
                    </div>
                  </div>
                  {isBlocked ? (
                    <div className="mt-2 text-xs text-amber-700 dark:text-amber-400">
                      {candidate.openIouCount} IOU{candidate.openIouCount === 1 ? " is" : "s are"} still released or awaiting head-office settlement approval.
                    </div>
                  ) : null}
                </div>
              </div>
            </div>
          );
        })}
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Reconciliation notes</label>
        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Cash count, period, handover details, or explanation" />
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[var(--card-border)] pt-3">
        <div className="text-sm">Selected total: <span className="font-semibold">{money(total)}</span></div>
        <Button type="button" disabled={busy || selectedLines.length === 0 || invalid} onClick={() => void createReturn()}>
          {busy ? "Preparing..." : "Prepare Return"}
        </Button>
      </div>
      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
