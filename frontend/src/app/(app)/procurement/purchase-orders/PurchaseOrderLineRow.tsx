"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton } from "@/components/ui";

type PurchaseOrderLineDto = {
  id: string;
  orderedQuantity: number;
  receivedQuantity: number;
  unitPrice: number;
  lineTotal: number;
};

export function PurchaseOrderLineRow({
  purchaseOrderId,
  line,
  itemLabel,
  canEdit,
}: {
  purchaseOrderId: string;
  line: PurchaseOrderLineDto;
  itemLabel: string;
  canEdit: boolean;
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [quantity, setQuantity] = useState(line.orderedQuantity.toString());
  const [unitPrice, setUnitPrice] = useState(line.unitPrice.toString());
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setQuantity(line.orderedQuantity.toString());
    setUnitPrice(line.unitPrice.toString());
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
      if (qty < line.receivedQuantity) {
        throw new Error("Ordered quantity cannot be less than received quantity.");
      }

      const price = Number(unitPrice);
      if (Number.isNaN(price) || price < 0) {
        throw new Error("Unit price must be 0 or greater.");
      }

      await apiPutNoContent(`procurement/purchase-orders/${purchaseOrderId}/lines/${line.id}`, {
        quantity: qty,
        unitPrice: price,
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
    if (!window.confirm("Delete this PO line?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`procurement/purchase-orders/${purchaseOrderId}/lines/${line.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  const previewTotal = Number(quantity) * Number(unitPrice);

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">{itemLabel}</td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={quantity} onChange={(e) => setQuantity(e.target.value)} inputMode="decimal" className="min-w-24" />
        ) : (
          line.orderedQuantity
        )}
      </td>
      <td className="py-2 pr-3">{line.receivedQuantity}</td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={unitPrice} onChange={(e) => setUnitPrice(e.target.value)} inputMode="decimal" className="min-w-28" />
        ) : (
          line.unitPrice
        )}
      </td>
      <td className="py-2 pr-3">{isEditing && Number.isFinite(previewTotal) ? previewTotal : line.lineTotal}</td>
      {canEdit ? (
        <td className="py-2 pr-3">
          <div className="flex flex-wrap items-center gap-2">
            {isEditing ? (
              <>
                <Button type="button" onClick={saveEdit} disabled={busy}>
                  {busy ? "Saving..." : "Save"}
                </Button>
                <SecondaryButton
                  type="button"
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
              <SecondaryButton type="button" onClick={beginEdit} disabled={busy}>
                Edit
              </SecondaryButton>
            )}
            <SecondaryButton type="button" onClick={deleteLine} disabled={busy}>
              Delete
            </SecondaryButton>
          </div>
          {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
        </td>
      ) : null}
    </tr>
  );
}

