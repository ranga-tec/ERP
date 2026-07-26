"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiGet, apiPost } from "@/lib/api-client";
import { Button, Select } from "@/components/ui";

type MaterialRequisitionSummary = {
  id: string;
  number: string;
  serviceJobId: string;
  warehouseId: string;
  requestedAt: string;
  purpose?: string | null;
  status: number;
  lineCount: number;
};

type LoadResult = { requisitionNumber: string; linesAdded: number; linesSkipped: number };

const statusLabel: Record<number, string> = { 0: "Draft", 1: "Posted", 2: "Voided" };

/**
 * Pulls the items requested on a job's MRN straight onto the dispatch, the same way a
 * goods receipt is filled from its purchase order, so they do not have to be re-keyed.
 */
export function DirectDispatchLoadFromMrnForm({
  directDispatchId,
  serviceJobId,
}: {
  directDispatchId: string;
  serviceJobId: string;
}) {
  const router = useRouter();
  const [requisitions, setRequisitions] = useState<MaterialRequisitionSummary[] | null>(null);
  const [selectedId, setSelectedId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<LoadResult | null>(null);

  useEffect(() => {
    let ignore = false;
    apiGet<MaterialRequisitionSummary[]>(`service/material-requisitions?serviceJobId=${serviceJobId}&take=200`)
      .then((rows) => {
        if (!ignore) setRequisitions(rows);
      })
      .catch((err) => {
        if (!ignore) {
          setRequisitions([]);
          setError(err instanceof Error ? err.message : String(err));
        }
      });
    return () => {
      ignore = true;
    };
  }, [serviceJobId]);

  const options = useMemo(
    () => (requisitions ?? []).slice().sort((a, b) => b.number.localeCompare(a.number)),
    [requisitions],
  );

  async function onLoad() {
    setError(null);
    setResult(null);
    setBusy(true);
    try {
      if (!selectedId) {
        throw new Error("Select a material requisition first.");
      }

      const loaded = await apiPost<LoadResult>(`sales/direct-dispatches/${directDispatchId}/load-from-mrn`, {
        materialRequisitionId: selectedId,
      });
      setResult(loaded);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-zinc-500">
        Requests for this job are raised as MRNs. Pick one to copy its requested items onto this
        dispatch instead of adding each line by hand. Items already on the dispatch are left alone.
      </p>

      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-0 flex-1 sm:max-w-md">
          <label className="mb-1 block text-sm font-medium">Material requisition</label>
          <Select value={selectedId} onChange={(event) => setSelectedId(event.target.value)}>
            <option value="">Select...</option>
            {options.map((mrn) => (
              <option key={mrn.id} value={mrn.id}>
                {`${mrn.number} — ${mrn.lineCount} line(s) — ${statusLabel[mrn.status] ?? mrn.status}`}
                {mrn.purpose ? ` — ${mrn.purpose}` : ""}
              </option>
            ))}
          </Select>
        </div>
        <Button type="button" onClick={onLoad} disabled={busy || !selectedId}>
          {busy ? "Loading..." : "Load requested items"}
        </Button>
      </div>

      {requisitions?.length === 0 ? (
        <div className="text-sm text-zinc-500">No material requisitions have been raised on this job yet.</div>
      ) : null}

      {result ? (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-900 dark:border-emerald-900/40 dark:bg-emerald-950/40 dark:text-emerald-100">
          {`Loaded ${result.linesAdded} line(s) from ${result.requisitionNumber}.`}
          {result.linesSkipped > 0 ? ` ${result.linesSkipped} skipped, already on this dispatch.` : ""}
        </div>
      ) : null}

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}
    </div>
  );
}
