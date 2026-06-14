"use client";

import { useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

type SerialOnHandDto = { serialNumber: string };

function parseList(text: string): string[] {
  return text
    .split(/[\n,]/g)
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

export function AvailableSerialPicker({
  warehouseId,
  itemId,
  quantity,
  value,
  onChange,
}: {
  warehouseId: string;
  itemId: string;
  quantity: string;
  value: string;
  onChange: (value: string) => void;
}) {
  const [serials, setSerials] = useState<string[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const hasLookup = Boolean(warehouseId && itemId);

  useEffect(() => {
    if (!hasLookup) {
      return;
    }

    let ignore = false;
    const resetHandle = window.setTimeout(() => {
      if (!ignore) {
        setSerials(null);
        setBusy(true);
        setError(null);
      }
    }, 0);

    const qs = new URLSearchParams({ warehouseId, itemId });
    apiGet<SerialOnHandDto[]>(`inventory/serials-on-hand?${qs.toString()}`)
      .then((rows) => {
        if (!ignore) {
          setSerials(rows.map((row) => row.serialNumber));
        }
      })
      .catch((err) => {
        if (!ignore) {
          setSerials([]);
          setError(err instanceof Error ? err.message : String(err));
        }
      })
      .finally(() => {
        if (!ignore) {
          setBusy(false);
        }
      });

    return () => {
      ignore = true;
      window.clearTimeout(resetHandle);
    };
  }, [hasLookup, warehouseId, itemId]);

  const selected = useMemo(() => parseList(value), [value]);
  const selectedSet = useMemo(() => new Set(selected.map((serial) => serial.toLowerCase())), [selected]);
  const visibleSerials = hasLookup ? serials : null;
  const visibleError = hasLookup ? error : null;
  const qty = Number(quantity);
  const requestedCount = Number.isFinite(qty) && qty > 0 ? Math.trunc(qty) : 0;

  function setSelected(next: string[]) {
    onChange(next.join("\n"));
  }

  function toggle(serial: string) {
    const exists = selectedSet.has(serial.toLowerCase());
    setSelected(exists ? selected.filter((current) => current.toLowerCase() !== serial.toLowerCase()) : [...selected, serial]);
  }

  function selectFirstAvailable() {
    setSelected((visibleSerials ?? []).slice(0, requestedCount || 1));
  }

  return (
    <div className="rounded-lg border border-[var(--input-border)] bg-[var(--surface-soft)] p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Available serials</div>
          <div className="mt-1 text-xs text-zinc-500">
            Selected {selected.length}
            {requestedCount ? ` of ${requestedCount}` : ""}.
          </div>
        </div>
        <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={selectFirstAvailable} disabled={!hasLookup || busy || !visibleSerials?.length}>
          Select first available
        </SecondaryButton>
      </div>

      {hasLookup && busy ? <div className="mt-3 text-xs text-zinc-500">Loading serials...</div> : null}
      {visibleError ? <div className="mt-3 text-xs text-red-700 dark:text-red-300">{visibleError}</div> : null}
      {hasLookup && !busy && visibleSerials?.length === 0 ? <div className="mt-3 text-xs text-red-700 dark:text-red-300">No serial stock is available in this warehouse.</div> : null}

      {visibleSerials && visibleSerials.length > 0 ? (
        <div className="mt-3 grid max-h-44 gap-2 overflow-auto sm:grid-cols-2 lg:grid-cols-3">
          {visibleSerials.map((serial) => (
            <label
              key={serial}
              className="flex items-center gap-2 rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2 py-1.5 text-xs"
            >
              <input type="checkbox" checked={selectedSet.has(serial.toLowerCase())} onChange={() => toggle(serial)} />
              <span className="font-mono">{serial}</span>
            </label>
          ))}
        </div>
      ) : null}
    </div>
  );
}
