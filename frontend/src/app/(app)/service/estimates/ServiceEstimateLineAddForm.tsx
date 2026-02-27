"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string; defaultUnitCost: number };
type TaxRef = { id: string; code: string; name: string; ratePercent: number; isActive: boolean };

const KIND_PART = "1";
const KIND_LABOR = "2";

export function ServiceEstimateLineAddForm({
  estimateId,
  items,
  taxes,
}: {
  estimateId: string;
  items: ItemRef[];
  taxes: TaxRef[];
}) {
  const router = useRouter();
  const [kind, setKind] = useState<string>(KIND_PART);
  const [itemId, setItemId] = useState("");
  const [description, setDescription] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [unitPrice, setUnitPrice] = useState("");
  const [taxCodeId, setTaxCodeId] = useState("");
  const [taxPercent, setTaxPercent] = useState("0");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sortedItems = items.slice().sort((a, b) => a.sku.localeCompare(b.sku));
  const taxOptions = taxes
    .filter((t) => t.isActive)
    .slice()
    .sort((a, b) => a.code.localeCompare(b.code));
  const selectedItem = itemId ? items.find((i) => i.id === itemId) : undefined;
  const isPart = kind === KIND_PART;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const qty = Number(quantity);
      const price = Number(unitPrice || (isPart ? selectedItem?.defaultUnitCost ?? 0 : 0));
      const tax = Number(taxPercent || 0);

      if (Number.isNaN(qty) || qty <= 0) throw new Error("Quantity must be positive.");
      if (Number.isNaN(price) || price < 0) throw new Error("Unit price must be 0 or greater.");
      if (Number.isNaN(tax) || tax < 0) throw new Error("Tax % must be 0 or greater.");
      if (!description.trim()) throw new Error("Description is required.");
      if (isPart && !itemId) throw new Error("Item is required for part lines.");

      await apiPostNoContent(`service/estimates/${estimateId}/lines`, {
        kind: Number(kind),
        itemId: isPart ? itemId : null,
        description: description.trim(),
        quantity: qty,
        unitPrice: price,
        taxPercent: tax,
      });

      setItemId("");
      setDescription("");
      setQuantity("1");
      setUnitPrice("");
      setTaxCodeId("");
      setTaxPercent("0");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-6">
        <div>
          <label className="mb-1 block text-sm font-medium">Kind</label>
          <Select
            value={kind}
            onChange={(e) => {
              const nextKind = e.target.value;
              setKind(nextKind);
              if (nextKind === KIND_LABOR) {
                setItemId("");
              }
            }}
          >
            <option value={KIND_PART}>Part</option>
            <option value={KIND_LABOR}>Labor</option>
          </Select>
        </div>
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium">Item (parts only)</label>
          <Select
            value={itemId}
            onChange={(e) => {
              const next = e.target.value;
              setItemId(next);
              if (!description && next) {
                const picked = items.find((i) => i.id === next);
                if (picked) setDescription(picked.name);
              }
            }}
            disabled={!isPart}
          >
            <option value="">{isPart ? "Select..." : "N/A for labor"}</option>
            {sortedItems.map((i) => (
              <option key={i.id} value={i.id}>
                {i.sku} - {i.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Qty / Hrs</label>
          <Input value={quantity} onChange={(e) => setQuantity(e.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Unit Price</label>
          <Input
            value={unitPrice}
            onChange={(e) => setUnitPrice(e.target.value)}
            inputMode="decimal"
            placeholder={isPart && selectedItem ? selectedItem.defaultUnitCost.toString() : "0"}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Tax code</label>
          <Select
            value={taxCodeId}
            onChange={(e) => {
              const selectedId = e.target.value;
              setTaxCodeId(selectedId);
              const selectedTax = taxOptions.find((t) => t.id === selectedId);
              if (selectedTax) {
                setTaxPercent(String(selectedTax.ratePercent));
              }
            }}
          >
            <option value="">(None)</option>
            {taxOptions.map((tax) => (
              <option key={tax.id} value={tax.id}>
                {tax.code} - {tax.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Tax %</label>
          <Input value={taxPercent} onChange={(e) => setTaxPercent(e.target.value)} inputMode="decimal" />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Description</label>
        <Input
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder={isPart ? "Part/repair line description" : "Labor task description"}
          required
        />
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
