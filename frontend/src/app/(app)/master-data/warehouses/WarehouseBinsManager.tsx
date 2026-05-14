"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPost, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select, Table } from "@/components/ui";

type WarehouseDto = { id: string; code: string; name: string; isActive: boolean };
type WarehouseBinDto = {
  id: string;
  warehouseId: string;
  code: string;
  name: string;
  zone?: string | null;
  rack?: string | null;
  shelf?: string | null;
  isActive: boolean;
};

type Draft = {
  code: string;
  name: string;
  zone: string;
  rack: string;
  shelf: string;
  isActive: string;
};

const emptyDraft: Draft = { code: "", name: "", zone: "", rack: "", shelf: "", isActive: "true" };

function binLabel(bin: WarehouseBinDto) {
  return [bin.zone, bin.rack, bin.shelf].filter(Boolean).join(" / ") || "-";
}

export function WarehouseBinsManager({ warehouses, bins }: { warehouses: WarehouseDto[]; bins: WarehouseBinDto[] }) {
  const router = useRouter();
  const [warehouseId, setWarehouseId] = useState(warehouses[0]?.id ?? "");
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editDraft, setEditDraft] = useState<Draft>(emptyDraft);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const warehouseById = useMemo(() => new Map(warehouses.map((warehouse) => [warehouse.id, warehouse])), [warehouses]);
  const displayedBins = useMemo(
    () => bins.filter((bin) => !warehouseId || bin.warehouseId === warehouseId).sort((a, b) => a.code.localeCompare(b.code)),
    [bins, warehouseId],
  );

  async function createBin(e: React.FormEvent) {
    e.preventDefault();
    if (!warehouseId) return;

    setError(null);
    setBusy(true);
    try {
      await apiPost<WarehouseBinDto>(`warehouses/${warehouseId}/bins`, {
        code: draft.code,
        name: draft.name,
        zone: draft.zone.trim() || null,
        rack: draft.rack.trim() || null,
        shelf: draft.shelf.trim() || null,
      });
      setDraft(emptyDraft);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  function beginEdit(bin: WarehouseBinDto) {
    setError(null);
    setEditingId(bin.id);
    setEditDraft({
      code: bin.code,
      name: bin.name,
      zone: bin.zone ?? "",
      rack: bin.rack ?? "",
      shelf: bin.shelf ?? "",
      isActive: bin.isActive ? "true" : "false",
    });
  }

  async function saveEdit(bin: WarehouseBinDto) {
    setError(null);
    setBusy(true);
    try {
      await apiPut<WarehouseBinDto>(`warehouses/${bin.warehouseId}/bins/${bin.id}`, {
        code: editDraft.code,
        name: editDraft.name,
        zone: editDraft.zone.trim() || null,
        rack: editDraft.rack.trim() || null,
        shelf: editDraft.shelf.trim() || null,
        isActive: editDraft.isActive === "true",
      });
      setEditingId(null);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function deleteBin(bin: WarehouseBinDto) {
    if (!window.confirm(`Delete bin ${bin.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`warehouses/${bin.warehouseId}/bins/${bin.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <div className="space-y-4">
      <form onSubmit={createBin} className="space-y-3">
        <div className="grid gap-3 md:grid-cols-6">
          <div className="md:col-span-2">
            <label className="mb-1 block text-sm font-medium">Warehouse</label>
            <Select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)} required>
              {warehouses.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.code} - {warehouse.name}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Bin Code</label>
            <Input value={draft.code} onChange={(e) => setDraft((current) => ({ ...current, code: e.target.value }))} required />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Name</label>
            <Input value={draft.name} onChange={(e) => setDraft((current) => ({ ...current, name: e.target.value }))} required />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Rack</label>
            <Input value={draft.rack} onChange={(e) => setDraft((current) => ({ ...current, rack: e.target.value }))} />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Shelf</label>
            <Input value={draft.shelf} onChange={(e) => setDraft((current) => ({ ...current, shelf: e.target.value }))} />
          </div>
        </div>
        <div className="grid gap-3 md:grid-cols-6">
          <div>
            <label className="mb-1 block text-sm font-medium">Zone</label>
            <Input value={draft.zone} onChange={(e) => setDraft((current) => ({ ...current, zone: e.target.value }))} />
          </div>
          <div className="flex items-end">
            <Button type="submit" disabled={busy || !warehouseId}>
              {busy ? "Saving..." : "Add bin"}
            </Button>
          </div>
        </div>
      </form>

      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900">{error}</div> : null}

      <div className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">Warehouse</th>
              <th className="py-2 pr-3">Bin</th>
              <th className="py-2 pr-3">Name</th>
              <th className="py-2 pr-3">Location</th>
              <th className="py-2 pr-3">Active</th>
              <th className="py-2 pr-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {displayedBins.map((bin) => {
              const editing = editingId === bin.id;
              return (
                <tr key={bin.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3">{warehouseById.get(bin.warehouseId)?.code ?? bin.warehouseId}</td>
                  <td className="py-2 pr-3 font-mono text-xs">
                    {editing ? <Input value={editDraft.code} onChange={(e) => setEditDraft((current) => ({ ...current, code: e.target.value }))} /> : bin.code}
                  </td>
                  <td className="py-2 pr-3">
                    {editing ? <Input value={editDraft.name} onChange={(e) => setEditDraft((current) => ({ ...current, name: e.target.value }))} /> : bin.name}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {editing ? (
                      <div className="grid min-w-64 gap-2 sm:grid-cols-3">
                        <Input value={editDraft.zone} onChange={(e) => setEditDraft((current) => ({ ...current, zone: e.target.value }))} placeholder="Zone" />
                        <Input value={editDraft.rack} onChange={(e) => setEditDraft((current) => ({ ...current, rack: e.target.value }))} placeholder="Rack" />
                        <Input value={editDraft.shelf} onChange={(e) => setEditDraft((current) => ({ ...current, shelf: e.target.value }))} placeholder="Shelf" />
                      </div>
                    ) : (
                      binLabel(bin)
                    )}
                  </td>
                  <td className="py-2 pr-3">
                    {editing ? (
                      <Select value={editDraft.isActive} onChange={(e) => setEditDraft((current) => ({ ...current, isActive: e.target.value }))}>
                        <option value="true">Yes</option>
                        <option value="false">No</option>
                      </Select>
                    ) : bin.isActive ? (
                      "Yes"
                    ) : (
                      "No"
                    )}
                  </td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap gap-2">
                      {editing ? (
                        <>
                          <Button type="button" className="px-2 py-1 text-xs" onClick={() => saveEdit(bin)} disabled={busy}>
                            Save
                          </Button>
                          <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => setEditingId(null)} disabled={busy}>
                            Cancel
                          </SecondaryButton>
                        </>
                      ) : (
                        <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => beginEdit(bin)} disabled={busy}>
                          Edit
                        </SecondaryButton>
                      )}
                      <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => deleteBin(bin)} disabled={busy}>
                        Delete
                      </SecondaryButton>
                    </div>
                  </td>
                </tr>
              );
            })}
            {displayedBins.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                  No bins for the selected warehouse.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </div>
    </div>
  );
}
