"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Textarea } from "@/components/ui";
import { describeCategory, money, type ReallocationCandidateDto } from "./types";

type CreatedReallocationDto = { id: string };

function candidateLabel(candidate: ReallocationCandidateDto) {
  return `${describeCategory(candidate.category, candidate.serviceJobNumber, candidate.customCategoryName)} · ${candidate.requestNumber} · ${candidate.purpose}`;
}

export function PettyCashReallocationCreateForm({ candidates }: { candidates: ReallocationCandidateDto[] }) {
  const router = useRouter();
  const funds = useMemo(
    () => Array.from(new Map(candidates.map((candidate) => [candidate.pettyCashFundId, candidate.pettyCashFundCode])).entries()),
    [candidates],
  );
  const [fundId, setFundId] = useState(funds[0]?.[0] ?? "");
  const [sourceId, setSourceId] = useState("");
  const [destinationId, setDestinationId] = useState("");
  const [amountText, setAmountText] = useState("");
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fundCandidates = candidates.filter((candidate) => candidate.pettyCashFundId === fundId);
  const sourceOptions = fundCandidates.filter((candidate) => candidate.availableBalance > 0);
  const effectiveSourceId = sourceOptions.some((candidate) => candidate.pettyCashRequestLineId === sourceId)
    ? sourceId
    : sourceOptions[0]?.pettyCashRequestLineId ?? "";
  const destinationOptions = fundCandidates.filter((candidate) => candidate.pettyCashRequestLineId !== effectiveSourceId);
  const effectiveDestinationId = destinationOptions.some((candidate) => candidate.pettyCashRequestLineId === destinationId)
    ? destinationId
    : destinationOptions[0]?.pettyCashRequestLineId ?? "";
  const source = sourceOptions.find((candidate) => candidate.pettyCashRequestLineId === effectiveSourceId);
  const destination = destinationOptions.find((candidate) => candidate.pettyCashRequestLineId === effectiveDestinationId);
  const amount = Number(amountText);
  const valid = Boolean(
    source
    && destination
    && Number.isFinite(amount)
    && amount > 0
    && amount <= source.availableBalance
    && reason.trim().length > 0,
  );

  async function createReallocation() {
    if (!source || !destination || !valid) return;
    setError(null);
    setBusy(true);
    try {
      const created = await apiPost<CreatedReallocationDto>("finance/petty-cash-reallocations", {
        pettyCashFundId: fundId,
        sourcePettyCashRequestLineId: source.pettyCashRequestLineId,
        destinationPettyCashRequestLineId: destination.pettyCashRequestLineId,
        amount,
        reason: reason.trim(),
      });
      router.push(`/finance/petty-cash-reallocations/${created.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  if (funds.length === 0) {
    return <div className="text-sm text-zinc-500">No funded categories are available for reallocation.</div>;
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
            setSourceId("");
            setDestinationId("");
          }}
        >
          {funds.map(([id, code]) => <option key={id} value={id}>{code}</option>)}
        </select>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Move balance from</label>
        <select
          className="w-full rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-2 text-sm"
          value={effectiveSourceId}
          onChange={(event) => {
            setSourceId(event.target.value);
            if (event.target.value === effectiveDestinationId) setDestinationId("");
          }}
        >
          {sourceOptions.map((candidate) => (
            <option key={candidate.pettyCashRequestLineId} value={candidate.pettyCashRequestLineId}>
              {candidateLabel(candidate)} · available {money(candidate.availableBalance)}
            </option>
          ))}
        </select>
        {source ? (
          <div className="mt-1 text-xs text-zinc-500">
            Ledger {money(source.ledgerBalance)} · pending return {money(source.pendingReturnAmount)} · pending reallocation {money(source.pendingReallocationAmount)}
          </div>
        ) : null}
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Move balance to</label>
        <select
          className="w-full rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-2 text-sm"
          value={effectiveDestinationId}
          onChange={(event) => setDestinationId(event.target.value)}
        >
          {destinationOptions.map((candidate) => (
            <option key={candidate.pettyCashRequestLineId} value={candidate.pettyCashRequestLineId}>
              {candidateLabel(candidate)} · balance {money(candidate.ledgerBalance)}
            </option>
          ))}
        </select>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Amount *</label>
          <Input inputMode="decimal" value={amountText} onChange={(event) => setAmountText(event.target.value)} placeholder="0.00" />
          {source && amount > source.availableBalance ? (
            <div className="mt-1 text-xs text-red-700 dark:text-red-300">Maximum available is {money(source.availableBalance)}.</div>
          ) : null}
        </div>
        <div className="rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)] p-3 text-xs">
          <div>Source available after approval: <strong>{money(source && amount > 0 ? source.availableBalance - amount : source?.availableBalance ?? 0)}</strong></div>
          <div className="mt-1">Destination balance after approval: <strong>{money(destination && amount > 0 ? destination.ledgerBalance + amount : destination?.ledgerBalance ?? 0)}</strong></div>
          <div className="mt-1 text-zinc-500">Physical fund balance does not change.</div>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Business reason *</label>
        <Textarea
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          placeholder="Explain the new expense and why the original category now has spare balance"
        />
      </div>

      {(source?.category === 1 || destination?.category === 1) ? (
        <div className="rounded-md border border-amber-300 bg-amber-50 p-3 text-xs text-amber-900 dark:border-amber-900 dark:bg-amber-950/20 dark:text-amber-300">
          This changes a Job Wise allocation. Head office should confirm that the transfer is consistent with the affected job budget before approval.
        </div>
      ) : null}

      <div className="flex justify-end border-t border-[var(--card-border)] pt-3">
        <Button type="button" disabled={busy || !valid} onClick={() => void createReallocation()}>
          {busy ? "Preparing..." : "Prepare Reallocation"}
        </Button>
      </div>
      {destinationOptions.length === 0 ? <div className="text-sm text-amber-700 dark:text-amber-400">This fund needs at least two funded categories.</div> : null}
      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
