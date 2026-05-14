"use client";

import { useMemo, useState } from "react";
import { ItemInlineLink } from "@/components/InlineLink";
import { apiGet } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select, Table } from "@/components/ui";

type WarehouseRef = { id: string; code: string; name: string };
type WarehouseBinRef = { id: string; warehouseId: string; code: string; name: string; zone?: string | null; rack?: string | null; shelf?: string | null };
type ItemRef = { id: string; sku: string; name: string };
type InventoryAvailabilityDto = {
  warehouseId: string;
  warehouseBinId?: string | null;
  itemId: string;
  batchNumber?: string | null;
  serialNumber?: string | null;
  onHand: number;
  unitCost: number;
  inventoryValue: number;
};

function number(value: number) {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 4 }).format(value);
}

function money(value: number) {
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
}

function binLocation(bin?: WarehouseBinRef) {
  if (!bin) return "Unassigned";
  const detail = [bin.zone, bin.rack, bin.shelf].filter(Boolean).join(" / ");
  return detail ? `${bin.code} - ${detail}` : bin.code;
}

export function InventoryAvailabilityBrowser({
  warehouses,
  bins,
  items,
}: {
  warehouses: WarehouseRef[];
  bins: WarehouseBinRef[];
  items: ItemRef[];
}) {
  const [warehouseId, setWarehouseId] = useState("");
  const [warehouseBinId, setWarehouseBinId] = useState("");
  const [itemId, setItemId] = useState("");
  const [batchNumber, setBatchNumber] = useState("");
  const [serialNumber, setSerialNumber] = useState("");
  const [search, setSearch] = useState("");
  const [rows, setRows] = useState<InventoryAvailabilityDto[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const warehouseById = useMemo(() => new Map(warehouses.map((warehouse) => [warehouse.id, warehouse])), [warehouses]);
  const binById = useMemo(() => new Map(bins.map((bin) => [bin.id, bin])), [bins]);
  const itemById = useMemo(() => new Map(items.map((item) => [item.id, item])), [items]);
  const filteredBins = useMemo(() => bins.filter((bin) => !warehouseId || bin.warehouseId === warehouseId), [bins, warehouseId]);

  const displayedRows = useMemo(() => {
    const normalized = search.trim().toLowerCase();
    if (!rows || !normalized) return rows ?? [];

    return rows.filter((row) => {
      const item = itemById.get(row.itemId);
      const warehouse = warehouseById.get(row.warehouseId);
      const bin = row.warehouseBinId ? binById.get(row.warehouseBinId) : null;
      const haystack = [
        item?.sku,
        item?.name,
        warehouse?.code,
        warehouse?.name,
        bin?.code,
        bin?.name,
        bin?.zone,
        bin?.rack,
        bin?.shelf,
        row.batchNumber,
        row.serialNumber,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return haystack.includes(normalized);
    });
  }, [binById, itemById, rows, search, warehouseById]);

  const totalQuantity = displayedRows.reduce((sum, row) => sum + row.onHand, 0);
  const totalValue = displayedRows.reduce((sum, row) => sum + row.inventoryValue, 0);

  async function loadInventory(e?: React.FormEvent) {
    e?.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const qs = new URLSearchParams();
      if (warehouseId) qs.set("warehouseId", warehouseId);
      if (warehouseBinId) qs.set("warehouseBinId", warehouseBinId);
      if (itemId) qs.set("itemId", itemId);
      if (batchNumber.trim()) qs.set("batchNumber", batchNumber.trim());
      if (serialNumber.trim()) qs.set("serialNumber", serialNumber.trim());
      setRows(await apiGet<InventoryAvailabilityDto[]>(`inventory/availability?${qs.toString()}`));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  function clearFilters() {
    setWarehouseId("");
    setWarehouseBinId("");
    setItemId("");
    setBatchNumber("");
    setSerialNumber("");
    setSearch("");
    setRows(null);
    setError(null);
  }

  return (
    <div className="space-y-4">
      <form onSubmit={loadInventory} className="space-y-3">
        <div className="grid gap-3 lg:grid-cols-5">
          <div>
            <label className="mb-1 block text-sm font-medium">Warehouse</label>
            <Select
              value={warehouseId}
              onChange={(e) => {
                setWarehouseId(e.target.value);
                setWarehouseBinId("");
              }}
            >
              <option value="">All warehouses</option>
              {warehouses.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.code} - {warehouse.name}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Bin / Rack</label>
            <Select value={warehouseBinId} onChange={(e) => setWarehouseBinId(e.target.value)}>
              <option value="">All bins</option>
              {filteredBins.map((bin) => (
                <option key={bin.id} value={bin.id}>
                  {binLocation(bin)}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Item</label>
            <Select value={itemId} onChange={(e) => setItemId(e.target.value)}>
              <option value="">All items</option>
              {items.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.sku} - {item.name}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Batch / Lot</label>
            <Input value={batchNumber} onChange={(e) => setBatchNumber(e.target.value)} />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Serial</label>
            <Input value={serialNumber} onChange={(e) => setSerialNumber(e.target.value)} />
          </div>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button type="submit" disabled={busy}>
            {busy ? "Loading..." : "Load inventory"}
          </Button>
          <SecondaryButton type="button" onClick={clearFilters} disabled={busy}>
            Clear
          </SecondaryButton>
        </div>
      </form>

      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900">{error}</div> : null}

      {rows ? (
        <div className="space-y-3">
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-lg border border-[var(--card-border)] bg-[var(--surface)] p-3">
              <div className="text-xs uppercase tracking-wide text-zinc-500">Rows</div>
              <div className="mt-1 text-xl font-semibold">{displayedRows.length}</div>
            </div>
            <div className="rounded-lg border border-[var(--card-border)] bg-[var(--surface)] p-3">
              <div className="text-xs uppercase tracking-wide text-zinc-500">On Hand</div>
              <div className="mt-1 text-xl font-semibold">{number(totalQuantity)}</div>
            </div>
            <div className="rounded-lg border border-[var(--card-border)] bg-[var(--surface)] p-3">
              <div className="text-xs uppercase tracking-wide text-zinc-500">Inventory Value</div>
              <div className="mt-1 text-xl font-semibold">{money(totalValue)}</div>
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Search loaded inventory</label>
            <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search item, warehouse, bin, rack, batch, or serial" />
          </div>

          <div className="overflow-auto">
            <Table>
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="py-2 pr-3">Item</th>
                  <th className="py-2 pr-3">Warehouse</th>
                  <th className="py-2 pr-3">Bin / Rack</th>
                  <th className="py-2 pr-3">Batch / Lot</th>
                  <th className="py-2 pr-3">Serial</th>
                  <th className="py-2 pr-3">On Hand</th>
                  <th className="py-2 pr-3">Unit Cost</th>
                  <th className="py-2 pr-3">Value</th>
                </tr>
              </thead>
              <tbody>
                {displayedRows.map((row) => {
                  const item = itemById.get(row.itemId);
                  return (
                    <tr
                      key={[row.warehouseId, row.warehouseBinId ?? "unassigned", row.itemId, row.batchNumber ?? "no-batch", row.serialNumber ?? "no-serial"].join(":")}
                      className="border-b border-zinc-100 dark:border-zinc-900"
                    >
                      <td className="py-2 pr-3">
                        <ItemInlineLink itemId={row.itemId}>{item ? `${item.sku} - ${item.name}` : row.itemId}</ItemInlineLink>
                      </td>
                      <td className="py-2 pr-3">{warehouseById.get(row.warehouseId)?.code ?? row.warehouseId}</td>
                      <td className="py-2 pr-3">{binLocation(row.warehouseBinId ? binById.get(row.warehouseBinId) : undefined)}</td>
                      <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{row.batchNumber?.trim() ? row.batchNumber : "-"}</td>
                      <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{row.serialNumber?.trim() ? row.serialNumber : "-"}</td>
                      <td className="py-2 pr-3 font-medium">{number(row.onHand)}</td>
                      <td className="py-2 pr-3">{money(row.unitCost)}</td>
                      <td className="py-2 pr-3">{money(row.inventoryValue)}</td>
                    </tr>
                  );
                })}
                {displayedRows.length === 0 ? (
                  <tr>
                    <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                      No inventory rows match the selected filters.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </Table>
          </div>
        </div>
      ) : (
        <div className="rounded-lg border border-dashed border-zinc-300 bg-zinc-50/80 p-4 text-sm text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950/60 dark:text-zinc-300">
          Load all inventory, or narrow by warehouse, bin, item, batch, or serial before loading.
        </div>
      )}
    </div>
  );
}
