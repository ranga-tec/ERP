"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton } from "@/components/ui";

type PurchaseRequisitionLineDto = {
  id: string;
  quantity: number;
  notes?: string | null;
};

export function PurchaseRequisitionLineRow({
  purchaseRequisitionId,
  line,
  itemLabel,
  baseUom,
  canEdit,
}: {
  purchaseRequisitionId: string;
  line: PurchaseRequisitionLineDto;
  itemLabel: string;
  baseUom: string;
  canEdit: boolean;
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [quantity, setQuantity] = useState(line.quantity.toString());
  const [notes, setNotes] = useState(line.notes ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setQuantity(line.quantity.toString());
    setNotes(line.notes ?? "");
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      const qty = Number(quantity);
      if (Number.isNaN(qty) || qty <= 0) {
        throw new Error("Quantity must be positive.");
      }

      await apiPutNoContent(`procurement/purchase-requisitions/${purchaseRequisitionId}/lines/${line.id}`, {
        quantity: qty,
        notes: notes.trim() || null,
      });

      setIsEditing(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function deleteLine() {
    if (!window.confirm("Delete this purchase requisition line?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`procurement/purchase-requisitions/${purchaseRequisitionId}/lines/${line.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">{itemLabel}</td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={quantity} onChange={(e) => setQuantity(e.target.value)} inputMode="decimal" className="min-w-24" />
        ) : (
          line.quantity
        )}
      </td>
      <td className="py-2 pr-3">{baseUom}</td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? (
          <Input value={notes} onChange={(e) => setNotes(e.target.value)} className="min-w-56" />
        ) : (
          line.notes ?? "-"
        )}
      </td>
      {canEdit ? (
        <td className="py-2 pr-3">
          <div className="flex flex-wrap items-center gap-2">
            {isEditing ? (
              <>
                <Button type="button" className="px-2 py-1 text-xs" onClick={saveEdit} disabled={busy}>
                  {busy ? "Saving..." : "Save"}
                </Button>
                <SecondaryButton
                  type="button"
                  className="px-2 py-1 text-xs"
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
              <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={beginEdit} disabled={busy}>
                Edit
              </SecondaryButton>
            )}
            <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={deleteLine} disabled={busy}>
              Delete
            </SecondaryButton>
          </div>
          {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
        </td>
      ) : null}
    </tr>
  );
}




