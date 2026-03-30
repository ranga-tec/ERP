"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string };

export function ServiceExpenseClaimLineAddForm({
  claimId,
  items,
}: {
  claimId: string;
  items: ItemRef[];
}) {
  const router = useRouter();
  const [itemId, setItemId] = useState("");
  const [description, setDescription] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [unitCost, setUnitCost] = useState("");
  const [billableToCustomer, setBillableToCustomer] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const parsedQuantity = Number(quantity);
      if (!Number.isFinite(parsedQuantity) || parsedQuantity <= 0) {
        throw new Error("Quantity must be positive.");
      }

      const parsedUnitCost = Number(unitCost);
      if (!Number.isFinite(parsedUnitCost) || parsedUnitCost < 0) {
        throw new Error("Unit cost must be 0 or greater.");
      }

      await apiPostNoContent(`service/expense-claims/${claimId}/lines`, {
        itemId: itemId || null,
        description: description.trim(),
        quantity: parsedQuantity,
        unitCost: parsedUnitCost,
        billableToCustomer,
      });

      setDescription("");
      setQuantity("1");
      setUnitCost("");
      setBillableToCustomer(true);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  const sortedItems = items.slice().sort((a, b) => a.sku.localeCompare(b.sku));

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Item (optional)</label>
          <Select value={itemId} onChange={(event) => setItemId(event.target.value)}>
            <option value="">Ad-hoc / outside buy</option>
            {sortedItems.map((item) => (
              <option key={item.id} value={item.id}>
                {item.sku} - {item.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Description</label>
          <Input
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="Fuse, courier charge, outside machining, wiring, etc."
            required
          />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Quantity</label>
          <Input value={quantity} onChange={(event) => setQuantity(event.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Unit cost</label>
          <Input value={unitCost} onChange={(event) => setUnitCost(event.target.value)} inputMode="decimal" required />
        </div>
        <label className="flex items-center gap-2 rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm shadow-[var(--shadow-control)]">
          <input
            type="checkbox"
            checked={billableToCustomer}
            onChange={(event) => setBillableToCustomer(event.target.checked)}
            className="h-4 w-4 rounded border-zinc-300"
          />
          Billable to customer
        </label>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Adding..." : "Add Expense Line"}
      </Button>
    </form>
  );
}
