"use client";

import { useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { Select, Table } from "@/components/ui";

type WarehouseRef = { id: string; code: string; name: string };
type OnHandRowDto = { warehouseId: string; itemId: string; batchNumber?: string | null; onHand: number };
type ViewMode = "total" | "warehouse" | "batch" | "warehouse-batch";
type DisplayRow = { key: string; warehouseId?: string; batchLabel?: string; onHand: number };

function number(value: number) {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 4 }).format(value);
}

function batchLabel(value?: string | null) {
  return value?.trim() ? value : "No batch";
}

function warehouseLabel(warehouseById: Map<string, WarehouseRef>, warehouseId?: string) {
  if (!warehouseId) {
    return "-";
  }

  const warehouse = warehouseById.get(warehouseId);
  if (!warehouse) {
    return warehouseId;
  }

  return `${warehouse.code} - ${warehouse.name}`;
}

export function LineStockInsight({
  warehouses,
  warehouseId,
  itemId,
  batchNumber,
  countedQuantity,
}: {
  warehouses: WarehouseRef[];
  warehouseId?: string;
  itemId?: string;
  batchNumber?: string;
  countedQuantity?: string;
}) {
  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );
  const warehouseById = useMemo(() => new Map(warehouseOptions.map((warehouse) => [warehouse.id, warehouse])), [warehouseOptions]);

  const [viewMode, setViewMode] = useState<ViewMode>("warehouse");
  const [rows, setRows] = useState<OnHandRowDto[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const normalizedBatch = batchNumber?.trim() ?? "";

  useEffect(() => {
    if (!itemId) {
      setRows(null);
      setBusy(false);
      setError(null);
      return;
    }

    let ignore = false;
    const handle = window.setTimeout(async () => {
      setBusy(true);
      setError(null);

      try {
        const qs = new URLSearchParams({ itemId });
        if (normalizedBatch) {
          qs.set("batchNumber", normalizedBatch);
        }

        const data = await apiGet<OnHandRowDto[]>(`inventory/onhand?${qs.toString()}`);
        if (!ignore) {
          setRows(data);
        }
      } catch (err) {
        if (!ignore) {
          setRows([]);
          setError(err instanceof Error ? err.message : String(err));
        }
      } finally {
        if (!ignore) {
          setBusy(false);
        }
      }
    }, 250);

    return () => {
      ignore = true;
      window.clearTimeout(handle);
    };
  }, [itemId, normalizedBatch]);

  const totalOnHand = useMemo(() => (rows ?? []).reduce((sum, row) => sum + row.onHand, 0), [rows]);
  const warehouseOnHand = useMemo(
    () => (rows ?? []).filter((row) => row.warehouseId === warehouseId).reduce((sum, row) => sum + row.onHand, 0),
    [rows, warehouseId],
  );
  const locationCount = useMemo(() => new Set((rows ?? []).map((row) => row.warehouseId)).size, [rows]);
  const parsedCountedQuantity = countedQuantity == null || countedQuantity.trim() === "" ? null : Number(countedQuantity);
  const expectedVariance =
    parsedCountedQuantity == null || Number.isNaN(parsedCountedQuantity) ? null : parsedCountedQuantity - warehouseOnHand;

  const displayedRows = useMemo<DisplayRow[]>(() => {
    if (!rows || viewMode === "total") {
      return [];
    }

    if (viewMode === "warehouse-batch") {
      return rows
        .map((row) => ({
          key: `${row.warehouseId}:${row.batchNumber ?? "no-batch"}`,
          warehouseId: row.warehouseId,
          batchLabel: batchLabel(row.batchNumber),
          onHand: row.onHand,
        }))
        .sort((left, right) => {
          const warehouseCompare = warehouseLabel(warehouseById, left.warehouseId).localeCompare(
            warehouseLabel(warehouseById, right.warehouseId),
          );
          if (warehouseCompare !== 0) {
            return warehouseCompare;
          }

          return (left.batchLabel ?? "").localeCompare(right.batchLabel ?? "");
        });
    }

    if (viewMode === "warehouse") {
      const grouped = new Map<string, number>();
      for (const row of rows) {
        grouped.set(row.warehouseId, (grouped.get(row.warehouseId) ?? 0) + row.onHand);
      }

      return Array.from(grouped.entries())
        .map(([groupedWarehouseId, onHand]) => ({
          key: groupedWarehouseId,
          warehouseId: groupedWarehouseId,
          onHand,
        }))
        .sort((left, right) =>
          warehouseLabel(warehouseById, left.warehouseId).localeCompare(warehouseLabel(warehouseById, right.warehouseId)),
        );
    }

    const grouped = new Map<string, number>();
    for (const row of rows) {
      const key = batchLabel(row.batchNumber);
      grouped.set(key, (grouped.get(key) ?? 0) + row.onHand);
    }

    return Array.from(grouped.entries())
      .map(([groupedBatch, onHand]) => ({
        key: groupedBatch,
        batchLabel: groupedBatch,
        onHand,
      }))
      .sort((left, right) => (left.batchLabel ?? "").localeCompare(right.batchLabel ?? ""));
  }, [rows, viewMode, warehouseById]);

  if (!itemId) {
    return (
      <div className="rounded-xl border border-dashed border-zinc-300 bg-zinc-50/80 p-3 text-xs text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950/60 dark:text-zinc-300">
        Select an item to see live stock.
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-zinc-200 bg-zinc-50/80 p-3 dark:border-zinc-800 dark:bg-zinc-950/60">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Live Stock</div>
          <div className="mt-1 text-xs text-zinc-500">
            Warehouse: {warehouseLabel(warehouseById, warehouseId)} - Batch filter: {normalizedBatch || "All batches"}
          </div>
        </div>
        <div className="w-full sm:w-52">
          <Select value={viewMode} onChange={(e) => setViewMode(e.target.value as ViewMode)} className="text-xs">
            <option value="total">All together</option>
            <option value="warehouse">Warehouse wise</option>
            <option value="batch">Batch wise</option>
            <option value="warehouse-batch">Warehouse + batch</option>
          </Select>
        </div>
      </div>

      <div className="mt-3 grid gap-3 sm:grid-cols-4">
        <div className="rounded-lg border border-zinc-200 bg-white px-3 py-2 dark:border-zinc-800 dark:bg-zinc-950">
          <div className="text-[11px] uppercase tracking-wide text-zinc-500">This Warehouse</div>
          <div className="mt-1 text-lg font-semibold">{number(warehouseOnHand)}</div>
        </div>
        <div className="rounded-lg border border-zinc-200 bg-white px-3 py-2 dark:border-zinc-800 dark:bg-zinc-950">
          <div className="text-[11px] uppercase tracking-wide text-zinc-500">All Warehouses</div>
          <div className="mt-1 text-lg font-semibold">{number(totalOnHand)}</div>
        </div>
        <div className="rounded-lg border border-zinc-200 bg-white px-3 py-2 dark:border-zinc-800 dark:bg-zinc-950">
          <div className="text-[11px] uppercase tracking-wide text-zinc-500">Locations</div>
          <div className="mt-1 text-lg font-semibold">{locationCount}</div>
        </div>
        <div className="rounded-lg border border-zinc-200 bg-white px-3 py-2 dark:border-zinc-800 dark:bg-zinc-950">
          <div className="text-[11px] uppercase tracking-wide text-zinc-500">Expected Variance</div>
          <div className="mt-1 text-lg font-semibold">
            {expectedVariance == null ? "-" : `${expectedVariance > 0 ? "+" : ""}${number(expectedVariance)}`}
          </div>
        </div>
      </div>

      {busy ? <div className="mt-3 text-xs text-zinc-500">Loading stock...</div> : null}
      {error ? <div className="mt-3 text-xs text-red-700 dark:text-red-300">{error}</div> : null}

      {!busy && rows ? (
        rows.length === 0 ? (
          <div className="mt-3 text-xs text-zinc-500">No stock found for the selected item and batch.</div>
        ) : viewMode === "total" ? null : (
          <div className="mt-3 overflow-auto">
            <Table className="text-xs">
              <thead>
                <tr className="border-b border-zinc-200 text-left uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  {viewMode === "warehouse" || viewMode === "warehouse-batch" ? (
                    <th className="py-2 pr-3">Warehouse</th>
                  ) : null}
                  {viewMode === "batch" || viewMode === "warehouse-batch" ? (
                    <th className="py-2 pr-3">Batch</th>
                  ) : null}
                  <th className="py-2 pr-3">On Hand</th>
                </tr>
              </thead>
              <tbody>
                {displayedRows.map((row) => (
                  <tr
                    key={row.key}
                    className={[
                      "border-b border-zinc-100 dark:border-zinc-900",
                      row.warehouseId && row.warehouseId === warehouseId ? "bg-amber-50/70 dark:bg-amber-500/10" : "",
                    ].join(" ")}
                  >
                    {viewMode === "warehouse" || viewMode === "warehouse-batch" ? (
                      <td className="py-2 pr-3">{warehouseLabel(warehouseById, row.warehouseId)}</td>
                    ) : null}
                    {viewMode === "batch" || viewMode === "warehouse-batch" ? (
                      <td className="py-2 pr-3">{row.batchLabel ?? "No batch"}</td>
                    ) : null}
                    <td className="py-2 pr-3 font-medium">{number(row.onHand)}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </div>
        )
      ) : null}
    </div>
  );
}
