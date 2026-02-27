"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton } from "@/components/ui";

type InvoiceLineDto = {
  id: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  taxPercent: number;
  lineTotal: number;
};

export function InvoiceLineRow({
  invoiceId,
  line,
  itemLabel,
  canEdit,
}: {
  invoiceId: string;
  line: InvoiceLineDto;
  itemLabel: string;
  canEdit: boolean;
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [quantity, setQuantity] = useState(line.quantity.toString());
  const [unitPrice, setUnitPrice] = useState(line.unitPrice.toString());
  const [discountPercent, setDiscountPercent] = useState(line.discountPercent.toString());
  const [taxPercent, setTaxPercent] = useState(line.taxPercent.toString());
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setQuantity(line.quantity.toString());
    setUnitPrice(line.unitPrice.toString());
    setDiscountPercent(line.discountPercent.toString());
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

      const discount = Number(discountPercent);
      if (Number.isNaN(discount) || discount < 0) {
        throw new Error("Discount percent must be 0 or greater.");
      }

      const tax = Number(taxPercent);
      if (Number.isNaN(tax) || tax < 0) {
        throw new Error("Tax percent must be 0 or greater.");
      }

      await apiPutNoContent(`sales/invoices/${invoiceId}/lines/${line.id}`, {
        quantity: qty,
        unitPrice: price,
        discountPercent: discount,
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
    if (!window.confirm("Delete this invoice line?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`sales/invoices/${invoiceId}/lines/${line.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  const qty = Number(quantity);
  const price = Number(unitPrice);
  const discount = Number(discountPercent);
  const tax = Number(taxPercent);
  const subtotal = Number.isFinite(qty) && Number.isFinite(price) && Number.isFinite(discount) ? qty * price * (1 - discount / 100) : NaN;
  const previewTotal = Number.isFinite(subtotal) && Number.isFinite(tax) ? subtotal * (1 + tax / 100) : NaN;

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">{itemLabel}</td>
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
          line.unitPrice
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={discountPercent} onChange={(e) => setDiscountPercent(e.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          line.discountPercent
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={taxPercent} onChange={(e) => setTaxPercent(e.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          line.taxPercent
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

