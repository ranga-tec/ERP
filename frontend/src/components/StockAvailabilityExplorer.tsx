"use client";

import { useMemo, useState } from "react";
import { ItemInlineLink } from "@/components/InlineLink";
import { apiGet } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select, Table } from "@/components/ui";

type WarehouseRef = { id: string; code: string; name: string };
type ItemRef = { id: string; sku: string; name: string };
type OnHandRowDto = { warehouseId: string; itemId: string; batchNumber?: string | null; onHand: number };
type GroupMode = "total" | "warehouse" | "batch" | "warehouse-batch";
type DisplayRow = { key: string; warehouseId?: string; batchLabel?: string; onHand: number };

const defaultGroupMode: GroupMode = "warehouse-batch";

const groupModeLabel: Record<GroupMode, string> = {
  total: "All together",
  warehouse: "Warehouse wise",
  batch: "Batch wise",
  "warehouse-batch": "Warehouse + batch",
};

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

export function StockAvailabilityExplorer({
  warehouses,
  items,
  initialWarehouseId = "",
}: {
  warehouses: WarehouseRef[];
  items: ItemRef[];
  initialWarehouseId?: string;
}) {
  const warehouseOptions = useMemo(
    () => warehouses.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [warehouses],
  );
  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );
  const warehouseById = useMemo(() => new Map(warehouseOptions.map((warehouse) => [warehouse.id, warehouse])), [warehouseOptions]);

  const [warehouseId, setWarehouseId] = useState(initialWarehouseId);
  const [itemId, setItemId] = useState("");
  const [batchNumber, setBatchNumber] = useState("");
  const [groupMode, setGroupMode] = useState<GroupMode>(defaultGroupMode);
  const [results, setResults] = useState<OnHandRowDto[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedItem = itemId ? itemOptions.find((item) => item.id === itemId) ?? null : null;
  const selectedWarehouse = warehouseId ? warehouseById.get(warehouseId) ?? null : null;

  const displayedRows = useMemo<DisplayRow[]>(() => {
    if (!results || groupMode === "total") {
      return [];
    }

    if (groupMode === "warehouse-batch") {
      return results
        .map((row) => ({
          key: `${row.warehouseId}:${row.batchNumber ?? "no-batch"}`,
          warehouseId: row.warehouseId,
          batchLabel: batchLabel(row.batchNumber),
          onHand: row.onHand,
        }))
        .sort((left, right) => {
          const warehouseCompare = (warehouseById.get(left.warehouseId ?? "")?.code ?? left.warehouseId ?? "").localeCompare(
            warehouseById.get(right.warehouseId ?? "")?.code ?? right.warehouseId ?? "",
          );
          if (warehouseCompare !== 0) {
            return warehouseCompare;
          }

          return (left.batchLabel ?? "").localeCompare(right.batchLabel ?? "");
        });
    }

    if (groupMode === "warehouse") {
      const grouped = new Map<string, number>();
      for (const row of results) {
        grouped.set(row.warehouseId, (grouped.get(row.warehouseId) ?? 0) + row.onHand);
      }

      return Array.from(grouped.entries())
        .map(([groupedWarehouseId, onHand]) => ({
          key: groupedWarehouseId,
          warehouseId: groupedWarehouseId,
          onHand,
        }))
        .sort((left, right) =>
          (warehouseById.get(left.warehouseId ?? "")?.code ?? left.warehouseId ?? "").localeCompare(
            warehouseById.get(right.warehouseId ?? "")?.code ?? right.warehouseId ?? "",
          ),
        );
    }

    const grouped = new Map<string, number>();
    for (const row of results) {
      const key = row.batchNumber?.trim() || "__no-batch__";
      grouped.set(key, (grouped.get(key) ?? 0) + row.onHand);
    }

    return Array.from(grouped.entries())
      .map(([groupedBatch, onHand]) => ({
        key: groupedBatch,
        batchLabel: groupedBatch === "__no-batch__" ? "No batch" : groupedBatch,
        onHand,
      }))
      .sort((left, right) => (left.batchLabel ?? "").localeCompare(right.batchLabel ?? ""));
  }, [groupMode, results, warehouseById]);

  const totalOnHand = useMemo(() => (results ?? []).reduce((sum, row) => sum + row.onHand, 0), [results]);
  const warehouseCount = useMemo(() => new Set((results ?? []).map((row) => row.warehouseId)).size, [results]);
  const batchCount = useMemo(() => new Set((results ?? []).map((row) => batchLabel(row.batchNumber))).size, [results]);

  function clearFilters() {
    setWarehouseId(initialWarehouseId);
    setItemId("");
    setBatchNumber("");
    setGroupMode(defaultGroupMode);
    setResults(null);
    setError(null);
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    setResults(null);

    try {
      const qs = new URLSearchParams({ itemId });
      if (warehouseId) {
        qs.set("warehouseId", warehouseId);
      }
      if (batchNumber.trim()) {
        qs.set("batchNumber", batchNumber.trim());
      }

      const data = await apiGet<OnHandRowDto[]>(`inventory/onhand?${qs.toString()}`);
      setResults(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-4">
      <form onSubmit={onSubmit} className="space-y-3">
        <div className="grid gap-3 sm:grid-cols-4">
          <div>
            <label className="mb-1 block text-sm font-medium">Warehouse (optional)</label>
            <Select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)}>
              <option value="">All warehouses</option>
              {warehouseOptions.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.code} - {warehouse.name}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Item</label>
            <Select value={itemId} onChange={(e) => setItemId(e.target.value)} required>
              <option value="" disabled>
                Select...
              </option>
              {itemOptions.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.sku} - {item.name}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Batch (optional)</label>
            <Input value={batchNumber} onChange={(e) => setBatchNumber(e.target.value)} />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">View</label>
            <Select value={groupMode} onChange={(e) => setGroupMode(e.target.value as GroupMode)}>
              <option value="total">All together</option>
              <option value="warehouse">Warehouse wise</option>
              <option value="batch">Batch wise</option>
              <option value="warehouse-batch">Warehouse + batch</option>
            </Select>
          </div>
        </div>

        {error ? (
          <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
            {error}
          </div>
        ) : null}

        <div className="flex flex-wrap gap-2">
          <Button type="submit" disabled={busy}>
            {busy ? "Loading..." : "Load stock"}
          </Button>
          <SecondaryButton type="button" onClick={clearFilters} disabled={busy}>
            Clear
          </SecondaryButton>
        </div>
      </form>

      {results ? (
        <div className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="rounded-xl border border-zinc-200 bg-white p-4 text-sm shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
              <div className="text-xs uppercase tracking-wide text-zinc-500">Total On Hand</div>
              <div className="mt-2 text-3xl font-semibold">{number(totalOnHand)}</div>
            </div>
            <div className="rounded-xl border border-zinc-200 bg-white p-4 text-sm shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
              <div className="text-xs uppercase tracking-wide text-zinc-500">Warehouses</div>
              <div className="mt-2 text-3xl font-semibold">{warehouseCount}</div>
            </div>
            <div className="rounded-xl border border-zinc-200 bg-white p-4 text-sm shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
              <div className="text-xs uppercase tracking-wide text-zinc-500">Batches</div>
              <div className="mt-2 text-3xl font-semibold">{batchCount}</div>
            </div>
          </div>

          <div className="rounded-xl border border-zinc-200 bg-white p-4 text-sm shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
            <div>
              <div className="text-sm font-semibold">Stock visibility</div>
              <div className="mt-1 text-xs text-zinc-500">
                Item:{" "}
                {selectedItem ? (
                  <ItemInlineLink itemId={selectedItem.id}>
                    {selectedItem.sku} - {selectedItem.name}
                  </ItemInlineLink>
                ) : (
                  itemId
                )}{" "}
                - Warehouse filter: {selectedWarehouse ? `${selectedWarehouse.code} - ${selectedWarehouse.name}` : "All warehouses"} -
                Batch filter: {batchNumber.trim() || "All batches"} - View: {groupModeLabel[groupMode]}
              </div>
            </div>

            {results.length === 0 ? (
              <div className="mt-4 rounded-xl border border-dashed border-zinc-300 bg-zinc-50/80 p-4 text-sm text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950/60 dark:text-zinc-300">
                No stock rows found for the selected filters.
              </div>
            ) : groupMode === "total" ? (
              <div className="mt-4 rounded-xl border border-zinc-200 bg-zinc-50/80 p-4 dark:border-zinc-800 dark:bg-zinc-900/60">
                <div className="text-xs uppercase tracking-wide text-zinc-500">{groupModeLabel[groupMode]}</div>
                <div className="mt-2 text-3xl font-semibold">{number(totalOnHand)}</div>
              </div>
            ) : (
              <div className="mt-4 overflow-auto">
                <Table>
                  <thead>
                    <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                      {groupMode === "warehouse" || groupMode === "warehouse-batch" ? (
                        <th className="py-2 pr-3">Warehouse</th>
                      ) : null}
                      {groupMode === "batch" || groupMode === "warehouse-batch" ? (
                        <th className="py-2 pr-3">Batch</th>
                      ) : null}
                      <th className="py-2 pr-3">On Hand</th>
                    </tr>
                  </thead>
                  <tbody>
                    {displayedRows.map((row) => (
                      <tr key={row.key} className="border-b border-zinc-100 dark:border-zinc-900">
                        {groupMode === "warehouse" || groupMode === "warehouse-batch" ? (
                          <td className="py-2 pr-3">
                            {warehouseLabel(warehouseById, row.warehouseId)}
                          </td>
                        ) : null}
                        {groupMode === "batch" || groupMode === "warehouse-batch" ? (
                          <td className="py-2 pr-3">{row.batchLabel ?? "No batch"}</td>
                        ) : null}
                        <td className="py-2 pr-3 font-medium">{number(row.onHand)}</td>
                      </tr>
                    ))}
                  </tbody>
                </Table>
              </div>
            )}
          </div>
        </div>
      ) : (
        <div className="rounded-xl border border-dashed border-zinc-300 bg-zinc-50/80 p-4 text-sm text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950/60 dark:text-zinc-300">
          Select an item, choose a view, and run the query to see total, warehouse-wise, or batch-wise stock.
        </div>
      )}
    </div>
  );
}
