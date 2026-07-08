"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { AuditTrailButton } from "@/components/AuditTrailButton";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type WarehouseDto = {
  id: string;
  code: string;
  name: string;
  address?: string | null;
  isActive: boolean;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function WarehouseRow({ warehouse }: { warehouse: WarehouseDto }) {
  const router = useRouter();
  const [code, setCode] = useState(warehouse.code);
  const [name, setName] = useState(warehouse.name);
  const [address, setAddress] = useState(warehouse.address ?? "");
  const [isActive, setIsActive] = useState(warehouse.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCode(warehouse.code);
    setName(warehouse.name);
    setAddress(warehouse.address ?? "");
    setIsActive(warehouse.isActive ? "true" : "false");
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`warehouses/${warehouse.id}`, {
        code,
        name,
        address: address.trim() || null,
        isActive: isActive === "true",
      });
      close();
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function deleteRow() {
    if (!window.confirm(`Delete warehouse ${warehouse.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`warehouses/${warehouse.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">{warehouse.code}</td>
      <td className="py-2 pr-3">{warehouse.name}</td>
      <td className="py-2 pr-3 text-zinc-500">{warehouse.address ?? "-"}</td>
      <td className="py-2 pr-3">{warehouse.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit Warehouse ${warehouse.code}`} description="Update warehouse details and active state." buttonLabel="Edit" variant="secondary" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">Code</label><Input value={code} onChange={(e) => setCode(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Name</label><Input value={name} onChange={(e) => setName(e.target.value)} required /></div>
                  <div className="sm:col-span-2"><label className="mb-1 block text-sm font-medium">Address</label><Input value={address} onChange={(e) => setAddress(e.target.value)} /></div>
                  <div><label className="mb-1 block text-sm font-medium">Active</label><Select value={isActive} onChange={(e) => setIsActive(e.target.value)}><option value="true">Yes</option><option value="false">No</option></Select></div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>{busy ? "Saving..." : "Save Warehouse"}</Button>
              </form>
            )}
          </AppFormModal>
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
          <AuditTrailButton tableName="Warehouses" recordId={warehouse.id} />
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
