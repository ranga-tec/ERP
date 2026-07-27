"use client";

import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

type OnHandDto = { batchNumber?: string | null; onHand: number; unitOfMeasure?: string | null };
type BatchOnHand = { batchNumber: string; onHand: number; unitOfMeasure?: string | null };

function formatQty(value: number): string {
  return Number.isInteger(value) ? String(value) : value.toFixed(4).replace(/0+$/, "").replace(/\.$/, "");
}

/**
 * Lists the batches actually in stock for an item/warehouse so a user can pick one
 * instead of having to look the number up elsewhere and retype it.
 * Selecting is single-choice: a stock line is issued from one batch.
 */
export function AvailableBatchPicker({
  warehouseId,
  itemId,
  value,
  onChange,
  requiredQuantity,
}: {
  warehouseId: string;
  itemId: string;
  value: string;
  onChange: (value: string) => void;
  requiredQuantity?: string;
}) {
  const [batches, setBatches] = useState<BatchOnHand[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const hasLookup = Boolean(warehouseId && itemId);

  useEffect(() => {
    if (!hasLookup) {
      return;
    }

    let ignore = false;
    // deferred like AvailableSerialPicker: setState in an effect body triggers
    // cascading renders and is rejected by react-hooks/set-state-in-effect
    const resetHandle = window.setTimeout(() => {
      if (!ignore) {
        setBatches(null);
        setBusy(true);
        setError(null);
      }
    }, 0);

    const qs = new URLSearchParams({ warehouseId, itemId });
    apiGet<OnHandDto[]>(`inventory/onhand?${qs.toString()}`)
      .then((rows) => {
        if (ignore) return;
        // roll bins up per batch: the picker only needs batch + total quantity
        const totals = new Map<string, number>();
        const unitOfMeasure = rows.find((row) => row.unitOfMeasure)?.unitOfMeasure;
        for (const row of rows) {
          const batch = row.batchNumber?.trim();
          if (!batch) continue;
          totals.set(batch, (totals.get(batch) ?? 0) + row.onHand);
        }
        setBatches(
          [...totals.entries()]
            .filter(([, onHand]) => onHand > 0)
            .map(([batchNumber, onHand]) => ({ batchNumber, onHand, unitOfMeasure }))
            .sort((a, b) => a.batchNumber.localeCompare(b.batchNumber)),
        );
      })
      .catch((err) => {
        if (ignore) return;
        setBatches([]);
        setError(err instanceof Error ? err.message : String(err));
      })
      .finally(() => {
        if (!ignore) setBusy(false);
      });

    return () => {
      ignore = true;
      window.clearTimeout(resetHandle);
    };
  }, [hasLookup, warehouseId, itemId]);

  // derived rather than cleared from the effect, so no setState during render sync
  const visibleBatches = hasLookup ? batches : null;
  const required = Number(requiredQuantity);
  const needed = Number.isFinite(required) && required > 0 ? required : 0;
  const selected = value.trim();

  return (
    <div className="rounded-lg border border-[var(--input-border)] bg-[var(--surface-soft)] p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Available batches</div>
          <div className="mt-1 text-xs text-zinc-500">
            {selected ? `Selected ${selected}.` : "Pick the batch to issue from."}
          </div>
        </div>
        {selected ? (
          <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => onChange("")}>
            Clear
          </SecondaryButton>
        ) : null}
      </div>

      {!hasLookup ? <div className="mt-3 text-xs text-zinc-500">Select an item to see its batches.</div> : null}
      {hasLookup && busy ? <div className="mt-3 text-xs text-zinc-500">Loading batches...</div> : null}
      {error ? <div className="mt-3 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      {hasLookup && !busy && visibleBatches?.length === 0 ? (
        <div className="mt-3 text-xs text-red-700 dark:text-red-300">No batch stock is available in this warehouse.</div>
      ) : null}

      {visibleBatches && visibleBatches.length > 0 ? (
        <div className="mt-3 grid max-h-44 gap-2 overflow-auto sm:grid-cols-2 lg:grid-cols-3">
          {visibleBatches.map((batch) => {
            const isSelected = selected.toLowerCase() === batch.batchNumber.toLowerCase();
            const short = needed > 0 && batch.onHand < needed;
            return (
              <button
                key={batch.batchNumber}
                type="button"
                onClick={() => onChange(isSelected ? "" : batch.batchNumber)}
                className={[
                  "flex items-center justify-between gap-2 rounded-md border px-2 py-1.5 text-left text-xs transition-colors",
                  isSelected
                    ? "border-[var(--link)] bg-[var(--accent-muted)]"
                    : "border-[var(--input-border)] bg-[var(--surface)] hover:border-[var(--link)]",
                ].join(" ")}
              >
                <span className="truncate font-mono">{batch.batchNumber}</span>
                <span className={short ? "shrink-0 text-red-700 dark:text-red-300" : "shrink-0 text-zinc-500"}>
                  {formatQty(batch.onHand)} {batch.unitOfMeasure ?? ""}
                  {short ? " short" : ""}
                </span>
              </button>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}
