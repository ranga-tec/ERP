"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton } from "@/components/ui";

type ServiceEstimateLineDto = {
  id: string;
  kind: number;
  itemId?: string | null;
  description: string;
  quantity: number;
  unitPrice: number;
  taxPercent: number;
  lineTotal: number;
};

export function ServiceEstimateLineRow({
  estimateId,
  line,
  kindLabel,
  itemLabel,
  canEdit,
}: {
  estimateId: string;
  line: ServiceEstimateLineDto;
  kindLabel: string;
  itemLabel: string;
  canEdit: boolean;
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [description, setDescription] = useState(line.description);
  const [quantity, setQuantity] = useState(line.quantity.toString());
  const [unitPrice, setUnitPrice] = useState(line.unitPrice.toString());
  const [taxPercent, setTaxPercent] = useState(line.taxPercent.toString());
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setDescription(line.description);
    setQuantity(line.quantity.toString());
    setUnitPrice(line.unitPrice.toString());
    setTaxPercent(line.taxPercent.toString());
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

      const price = Number(unitPrice);
      if (Number.isNaN(price) || price < 0) {
        throw new Error("Unit price must be 0 or greater.");
      }

      const tax = Number(taxPercent);
      if (Number.isNaN(tax) || tax < 0) {
        throw new Error("Tax percent must be 0 or greater.");
      }

      if (!description.trim()) {
        throw new Error("Description is required.");
      }

      await apiPutNoContent(`service/estimates/${estimateId}/lines/${line.id}`, {
        kind: line.kind,
        itemId: line.itemId ?? null,
        description: description.trim(),
        quantity: qty,
        unitPrice: price,
        taxPercent: tax,
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
    if (!window.confirm("Delete this estimate line?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`service/estimates/${estimateId}/lines/${line.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  const qty = Number(quantity);
  const price = Number(unitPrice);
  const tax = Number(taxPercent);
  const previewTotal = Number.isFinite(qty) && Number.isFinite(price) && Number.isFinite(tax)
    ? qty * price * (1 + tax / 100)
    : NaN;

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">{kindLabel}</td>
      <td className="py-2 pr-3">{itemLabel}</td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? (
          <Input value={description} onChange={(e) => setDescription(e.target.value)} className="min-w-56" />
        ) : (
          line.description
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={quantity} onChange={(e) => setQuantity(e.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          line.quantity
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={unitPrice} onChange={(e) => setUnitPrice(e.target.value)} inputMode="decimal" className="min-w-24" />
        ) : (
          line.unitPrice.toFixed(2)
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={taxPercent} onChange={(e) => setTaxPercent(e.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          line.taxPercent.toFixed(2)
        )}
      </td>
      <td className="py-2 pr-3">{isEditing && Number.isFinite(previewTotal) ? previewTotal.toFixed(2) : line.lineTotal.toFixed(2)}</td>
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




