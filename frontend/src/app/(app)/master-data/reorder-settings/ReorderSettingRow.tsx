"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPost } from "@/lib/api-client";
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
  itemLabel: string;
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [reorderPoint, setReorderPoint] = useState(setting.reorderPoint.toString());
  const [reorderQuantity, setReorderQuantity] = useState(setting.reorderQuantity.toString());
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setReorderPoint(setting.reorderPoint.toString());
    setReorderQuantity(setting.reorderQuantity.toString());
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      await apiPost("reorder-settings", {
        warehouseId: setting.warehouseId,
        itemId: setting.itemId,
        reorderPoint: Number(reorderPoint),
        reorderQuantity: Number(reorderQuantity),
      });
      setIsEditing(false);
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
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={reorderPoint} onChange={(e) => setReorderPoint(e.target.value)} inputMode="decimal" className="min-w-24" />
        ) : (
          setting.reorderPoint
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={reorderQuantity} onChange={(e) => setReorderQuantity(e.target.value)} inputMode="decimal" className="min-w-24" />
        ) : (
          setting.reorderQuantity
        )}
      </td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          {isEditing ? (
            <>
              <Button type="button" className={actionButtonClass} onClick={saveEdit} disabled={busy}>
                {busy ? "Saving..." : "Save"}
              </Button>
              <SecondaryButton
                type="button"
                className={actionButtonClass}
                onClick={() => {
                  setError(null);
                  setIsEditing(false);
                }}
                disabled={busy}
              >
                Cancel
              </SecondaryButton>
            </>
          ) : (
            <SecondaryButton type="button" className={actionButtonClass} onClick={beginEdit} disabled={busy}>
              Edit
            </SecondaryButton>
          )}
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
