"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string };

export function PurchaseRequisitionLineAddForm({
  purchaseRequisitionId,
  items,
}: {
  purchaseRequisitionId: string;
  items: ItemRef[];
}) {
  const router = useRouter();
  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );

  const [itemId, setItemId] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const qty = Number(quantity);
      if (Number.isNaN(qty) || qty <= 0) {
        throw new Error("Quantity must be positive.");
      }

      await apiPostNoContent(`procurement/purchase-requisitions/${purchaseRequisitionId}/lines`, {
        itemId,
        quantity: qty,
        notes: notes || null,
      });

      setItemId("");
      setQuantity("1");
      setNotes("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div className="sm:col-span-2">
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
          <label className="mb-1 block text-sm font-medium">Qty</label>
          <Input
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            inputMode="decimal"
            required
          />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes</label>
        <Input value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Adding..." : "Add line"}
      </Button>
    </form>
  );
}
