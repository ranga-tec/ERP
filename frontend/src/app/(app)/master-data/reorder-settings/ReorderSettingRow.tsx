"use client";

import { useRouter } from "next/navigation";
import { useState, type ReactNode } from "react";
import { apiDeleteNoContent, apiPost } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { Button, Input, SecondaryButton } from "@/components/ui";

type ReorderSettingDto = {
  id: string;
  warehouseId: string;
  itemId: string;
  reorderPoint: number;
  reorderQuantity: number;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function ReorderSettingRow({
  setting,
  warehouseLabel,
  itemLabel,
}: {
  setting: ReorderSettingDto;
  warehouseLabel: string;
  itemLabel: ReactNode;
}) {
  const router = useRouter();
  const [reorderPoint, setReorderPoint] = useState(setting.reorderPoint.toString());
  const [reorderQuantity, setReorderQuantity] = useState(setting.reorderQuantity.toString());
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setReorderPoint(setting.reorderPoint.toString());
    setReorderQuantity(setting.reorderQuantity.toString());
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      await apiPost("reorder-settings", {
        warehouseId: setting.warehouseId,
        itemId: setting.itemId,
        reorderPoint: Number(reorderPoint),
        reorderQuantity: Number(reorderQuantity),
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
    if (!window.confirm(`Delete reorder setting ${warehouseLabel} / ${itemLabel}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`reorder-settings/${setting.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">{warehouseLabel}</td>
      <td className="py-2 pr-3">{itemLabel}</td>
      <td className="py-2 pr-3">{setting.reorderPoint}</td>
      <td className="py-2 pr-3">{setting.reorderQuantity}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title="Edit Reorder Setting" description="Update reorder point and reorder quantity." buttonLabel="Edit" variant="secondary" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">Reorder Point</label><Input value={reorderPoint} onChange={(e) => setReorderPoint(e.target.value)} inputMode="decimal" required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Reorder Quantity</label><Input value={reorderQuantity} onChange={(e) => setReorderQuantity(e.target.value)} inputMode="decimal" required /></div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>{busy ? "Saving..." : "Save Reorder Setting"}</Button>
              </form>
            )}
          </AppFormModal>
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
