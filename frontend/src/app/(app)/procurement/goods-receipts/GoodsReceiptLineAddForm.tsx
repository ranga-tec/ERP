"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };

function parseList(text: string): string[] {
  return text
    .split(/[\n,]/g)
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

export function GoodsReceiptLineAddForm({ goodsReceiptId, items }: { goodsReceiptId: string; items: ItemRef[] }) {
  const router = useRouter();
  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );

  const [itemId, setItemId] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [unitCost, setUnitCost] = useState("");
  const [batchNumber, setBatchNumber] = useState("");
  const [serials, setSerials] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedItem = itemId ? items.find((i) => i.id === itemId) : undefined;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const qty = Number(quantity);
      if (Number.isNaN(qty) || qty <= 0) {
        throw new Error("Quantity must be positive.");
      }
      const cost = Number(unitCost || selectedItem?.defaultUnitCost || 0);
      if (Number.isNaN(cost) || cost < 0) {
        throw new Error("Unit cost must be 0 or greater.");
      }

      const serialList = parseList(serials);

      await apiPostNoContent(`procurement/goods-receipts/${goodsReceiptId}/lines`, {
        itemId,
        quantity: qty,
        unitCost: cost,
        batchNumber: batchNumber.trim() || null,
        serials: serialList.length ? serialList : null,
      });

      setItemId("");
      setQuantity("1");
      setUnitCost("");
      setBatchNumber("");
      setSerials("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-4">
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium">Item</label>
          <Select value={itemId} onChange={(e) => setItemId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {itemOptions.map((i) => (
              <option key={i.id} value={i.id}>
                {i.sku} — {i.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Qty</label>
          <Input value={quantity} onChange={(e) => setQuantity(e.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Unit cost</label>
          <Input
            value={unitCost}
            onChange={(e) => setUnitCost(e.target.value)}
            inputMode="decimal"
            placeholder={selectedItem ? selectedItem.defaultUnitCost.toString() : ""}
          />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Batch number (optional)</label>
          <Input value={batchNumber} onChange={(e) => setBatchNumber(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Serials (optional)</label>
          <Textarea value={serials} onChange={(e) => setSerials(e.target.value)} placeholder="One per line or comma-separated" />
        </div>
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

