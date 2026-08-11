"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiGet, apiPut } from "@/lib/api-client";
import { Button, Input, Select, Table, Textarea } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string; trackingType: number; unitOfMeasure?: string };

type MaterialRequisitionSummary = {
  id: string;
  number: string;
  status: number;
  purpose?: string | null;
  lineCount: number;
};

type PlanLine = {
  materialRequisitionLineId: string;
  itemId: string;
  requestedQuantity: number;
  previouslyDispatchedQuantity: number;
  reservedInOtherDraftsQuantity: number;
  outstandingQuantity: number;
  onHandQuantity: number;
  dispatchableQuantity: number;
  directDispatchLineId?: string | null;
  currentQuantity: number;
  batchNumber?: string | null;
  serials: string[];
  availableSerials: string[];
};

type Plan = { materialRequisitionId: string; requisitionNumber: string; lines: PlanLine[] };

type EditableLine = {
  materialRequisitionLineId: string;
  itemId: string;
  requestedQuantity: number;
  previouslyDispatchedQuantity: number;
  outstandingQuantity: number;
  onHandQuantity: number;
  dispatchableQuantity: number;
  quantity: string;
  batchNumber: string;
  serials: string;
  availableSerials: string[];
};

const TRACKING_SERIAL = 1;
const TRACKING_BATCH = 2;
const statusLabel: Record<number, string> = { 0: "Draft", 1: "Posted", 2: "Voided" };

function parseList(text: string): string[] {
  return text.split(/[\n,]/g).map((v) => v.trim()).filter(Boolean);
}

function num(value: string): number {
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

function toEditable(line: PlanLine): EditableLine {
  return {
    materialRequisitionLineId: line.materialRequisitionLineId,
    itemId: line.itemId,
    requestedQuantity: line.requestedQuantity,
    previouslyDispatchedQuantity: line.previouslyDispatchedQuantity,
    outstandingQuantity: line.outstandingQuantity,
    onHandQuantity: line.onHandQuantity,
    dispatchableQuantity: line.dispatchableQuantity,
    quantity: line.currentQuantity > 0 ? String(line.currentQuantity) : "",
    batchNumber: line.batchNumber ?? "",
    serials: line.serials.join("\n"),
    availableSerials: line.availableSerials ?? [],
  };
}

/**
 * The AOD working grid when the dispatch fulfils a job request. Picking the MRN shows its
 * requested items straight away; the user only enters how much is going out now, in full or
 * in part. Equivalent to receiving a goods receipt against its purchase order.
 */
export function DirectDispatchMrnPlanForm({
  directDispatchId,
  serviceJobId,
  linkedRequisitionId,
  items,
}: {
  directDispatchId: string;
  serviceJobId: string;
  linkedRequisitionId?: string | null;
  items: ItemRef[];
}) {
  const router = useRouter();
  const itemById = useMemo(() => new Map(items.map((i) => [i.id, i])), [items]);

  const [requisitions, setRequisitions] = useState<MaterialRequisitionSummary[]>([]);
  const [selectedId, setSelectedId] = useState(linkedRequisitionId ?? "");
  const [lines, setLines] = useState<EditableLine[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState<string | null>(null);

  useEffect(() => {
    let ignore = false;
    apiGet<MaterialRequisitionSummary[]>(`service/material-requisitions?serviceJobId=${serviceJobId}&take=200`)
      .then((rows) => {
        if (!ignore) setRequisitions(rows);
      })
      .catch(() => {
        if (!ignore) setRequisitions([]);
      });
    return () => {
      ignore = true;
    };
  }, [serviceJobId]);

  const loadPlan = useCallback(
    (requisitionId: string) => {
      if (!requisitionId) {
        setLines(null);
        return;
      }

      setLoading(true);
      setError(null);
      setSaved(null);
      apiGet<Plan>(`sales/direct-dispatches/${directDispatchId}/mrn-plan?materialRequisitionId=${requisitionId}`)
        .then((plan) => setLines((plan?.lines ?? []).map(toEditable)))
        .catch((err) => {
          setLines([]);
          setError(err instanceof Error ? err.message : String(err));
        })
        .finally(() => setLoading(false));
    },
    [directDispatchId],
  );

  // requested items appear as soon as an MRN is chosen - no separate load step
  useEffect(() => {
    if (selectedId) {
      loadPlan(selectedId);
    }
  }, [selectedId, loadPlan]);

  function patch(id: string, changes: Partial<EditableLine>) {
    setLines((prev) => prev?.map((l) => (l.materialRequisitionLineId === id ? { ...l, ...changes } : l)) ?? prev);
  }

  // Fills what the warehouse can actually supply, not what the requisition asked for. Filling the
  // outstanding figure produced plans that only failed at post time.
  function fillAllAvailable() {
    setLines((prev) => prev?.map((l) => ({ ...l, quantity: l.dispatchableQuantity > 0 ? String(l.dispatchableQuantity) : "" })) ?? prev);
  }

  async function onSave() {
    setError(null);
    setSaved(null);
    setSaving(true);
    try {
      for (const line of lines ?? []) {
        const qty = num(line.quantity);
        if (qty <= 0) continue;

        const item = itemById.get(line.itemId);
        if (item?.trackingType === TRACKING_SERIAL && parseList(line.serials).length !== Math.trunc(qty)) {
          throw new Error(`${item.sku}: enter ${Math.trunc(qty)} serial number(s) for a serial-tracked item.`);
        }
        if (item?.trackingType === TRACKING_BATCH && !line.batchNumber.trim()) {
          throw new Error(`${item.sku}: batch number is required for a batch-tracked item.`);
        }
      }

      const plan = await apiPut<Plan>(`sales/direct-dispatches/${directDispatchId}/mrn-plan`, {
        materialRequisitionId: selectedId,
        lines: (lines ?? []).map((l) => ({
          materialRequisitionLineId: l.materialRequisitionLineId,
          quantity: num(l.quantity),
          batchNumber: l.batchNumber.trim() || null,
          serials: parseList(l.serials).length ? parseList(l.serials) : null,
        })),
      });

      setLines((plan?.lines ?? []).map(toEditable));
      setSaved(`Saved against ${plan.requisitionNumber}.`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  }

  const options = useMemo(
    () => requisitions.slice().sort((a, b) => b.number.localeCompare(a.number)),
    [requisitions],
  );
  const totalToDispatch = (lines ?? []).reduce((sum, l) => sum + num(l.quantity), 0);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="min-w-0 flex-1 sm:max-w-md">
          <label className="mb-1 block text-sm font-medium">Material requisition</label>
          <Select value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
            <option value="">Select the request being dispatched...</option>
            {options.map((mrn) => (
              <option key={mrn.id} value={mrn.id}>
                {`${mrn.number} — ${mrn.lineCount} item(s) — ${statusLabel[mrn.status] ?? mrn.status}`}
                {mrn.purpose ? ` — ${mrn.purpose}` : ""}
              </option>
            ))}
          </Select>
        </div>
        {lines && lines.length > 0 ? (
          <div className="flex items-center gap-2">
            <Button type="button" onClick={fillAllAvailable} disabled={saving}>
              Fill all available
            </Button>
            <Button type="button" onClick={onSave} disabled={saving}>
              {saving ? "Saving..." : "Save dispatch quantities"}
            </Button>
          </div>
        ) : null}
      </div>

      {requisitions.length === 0 ? (
        <div className="text-sm text-zinc-500">No material requisitions have been raised on this job yet.</div>
      ) : null}
      {loading ? <div className="text-sm text-zinc-500">Loading requested items...</div> : null}
      {lines && lines.length === 0 && !loading ? (
        <div className="text-sm text-zinc-500">That requisition has no lines.</div>
      ) : null}

      {lines && lines.length > 0 ? (
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Item</th>
                <th className="py-2 pr-3 text-right">Requested</th>
                <th className="py-2 pr-3 text-right">Already out</th>
                <th className="py-2 pr-3 text-right">Outstanding</th>
                <th className="py-2 pr-3 text-right">In stock</th>
                <th className="py-2 pr-3">Dispatch now</th>
                <th className="py-2 pr-3">Batch</th>
                <th className="py-2 pr-3">Serials</th>
              </tr>
            </thead>
            <tbody>
              {lines.map((line) => {
                const item = itemById.get(line.itemId);
                const qty = num(line.quantity);
                const selectedSerials = parseList(line.serials);
                const unusedAvailableSerials = line.availableSerials.filter(
                  (serial) => !selectedSerials.some((selected) => selected.toLowerCase() === serial.toLowerCase()),
                );
                const overOutstanding = qty > line.outstandingQuantity;
                const overStock = qty > line.onHandQuantity;
                const over = overOutstanding || overStock;
                return (
                  <tr key={line.materialRequisitionLineId} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                    <td className="py-2 pr-3">
                      <div className="font-mono text-xs">{item?.sku ?? line.itemId}</div>
                      <div className="text-xs text-zinc-500">{item?.name}</div>
                    </td>
                    <td className="py-2 pr-3 text-right text-sm">{line.requestedQuantity}</td>
                    <td className="py-2 pr-3 text-right text-sm text-zinc-500">{line.previouslyDispatchedQuantity}</td>
                    <td className="py-2 pr-3 text-right text-sm">{line.outstandingQuantity}</td>
                    <td
                      className={
                        line.onHandQuantity < line.outstandingQuantity
                          ? "py-2 pr-3 text-right text-sm font-semibold text-amber-700 dark:text-amber-400"
                          : "py-2 pr-3 text-right text-sm"
                      }
                    >
                      {line.onHandQuantity}
                      {item?.unitOfMeasure ? <span className="ml-1 text-xs font-normal text-zinc-500">{item.unitOfMeasure}</span> : null}
                    </td>
                    <td className="py-2 pr-3">
                      <Input
                        value={line.quantity}
                        inputMode="decimal"
                        placeholder="0"
                        className={over ? "border-red-400" : undefined}
                        onChange={(e) => patch(line.materialRequisitionLineId, { quantity: e.target.value })}
                      />
                      {overStock ? (
                        <div className="mt-1 text-[11px] text-red-700 dark:text-red-300">
                          Only {line.onHandQuantity}{item?.unitOfMeasure ? ` ${item.unitOfMeasure}` : ""} in stock - posting will fail.
                        </div>
                      ) : overOutstanding ? (
                        <div className="mt-1 text-[11px] text-red-700 dark:text-red-300">
                          More than the {line.outstandingQuantity} still outstanding.
                        </div>
                      ) : null}
                    </td>
                    <td className="py-2 pr-3">
                      <Input
                        value={line.batchNumber}
                        placeholder={item?.trackingType === TRACKING_BATCH ? "Required" : "-"}
                        onChange={(e) => patch(line.materialRequisitionLineId, { batchNumber: e.target.value })}
                      />
                    </td>
                    <td className="py-2 pr-3">
                      {item?.trackingType === TRACKING_SERIAL ? (
                        <div className="space-y-2">
                          <Textarea
                            value={line.serials}
                            rows={2}
                            placeholder="One per line"
                            onChange={(e) => patch(line.materialRequisitionLineId, { serials: e.target.value })}
                          />
                          {unusedAvailableSerials.length > 0 ? (
                            <Select
                              value=""
                              aria-label={`Add an available serial for ${item?.sku ?? "item"}`}
                              onChange={(e) => {
                                if (!e.target.value) return;
                                patch(line.materialRequisitionLineId, {
                                  serials: [...selectedSerials, e.target.value].join("\n"),
                                });
                              }}
                            >
                              <option value="">Add existing serial...</option>
                              {unusedAvailableSerials.map((serial) => (
                                <option key={serial} value={serial}>{serial}</option>
                              ))}
                            </Select>
                          ) : (
                            <div className="text-[11px] text-zinc-500">No other serials are currently available.</div>
                          )}
                          <div className="text-[11px] text-zinc-500">Available MRN serials are filled automatically.</div>
                        </div>
                      ) : (
                        <span className="text-xs text-zinc-400">-</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </Table>
        </div>
      ) : null}

      {lines && lines.length > 0 ? (
        <div className="text-xs text-zinc-500">
          {`Dispatching ${totalToDispatch} unit(s) across ${lines.filter((l) => num(l.quantity) > 0).length} line(s).`}
          {" Leave a quantity blank or zero to keep it off this dispatch."}
        </div>
      ) : null}

      {saved ? (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-900 dark:border-emerald-900/40 dark:bg-emerald-950/40 dark:text-emerald-100">
          {saved}
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
