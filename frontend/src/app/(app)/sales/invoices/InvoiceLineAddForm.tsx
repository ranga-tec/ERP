"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string };
type TaxRef = { id: string; code: string; name: string; ratePercent: number; isActive: boolean };

export function InvoiceLineAddForm({
  invoiceId,
  items,
  taxes,
}: {
  invoiceId: string;
  items: ItemRef[];
  taxes: TaxRef[];
}) {
  const router = useRouter();
  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );
  const taxOptions = useMemo(
    () =>
      taxes
        .filter((t) => t.isActive)
        .slice()
        .sort((a, b) => a.code.localeCompare(b.code)),
    [taxes],
  );

  const [itemId, setItemId] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [unitPrice, setUnitPrice] = useState("0");
  const [discountPercent, setDiscountPercent] = useState("0");
  const [taxCodeId, setTaxCodeId] = useState("");
  const [taxPercent, setTaxPercent] = useState("0");
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
      const price = Number(unitPrice);
      if (Number.isNaN(price) || price < 0) {
        throw new Error("Unit price must be 0 or greater.");
      }
      const discount = Number(discountPercent);
      if (Number.isNaN(discount) || discount < 0) {
        throw new Error("Discount must be 0 or greater.");
      }
      const tax = Number(taxPercent);
      if (Number.isNaN(tax) || tax < 0) {
        throw new Error("Tax must be 0 or greater.");
      }

      await apiPostNoContent(`sales/invoices/${invoiceId}/lines`, {
        itemId,
        quantity: qty,
        unitPrice: price,
        discountPercent: discount,
        taxPercent: tax,
      });

      setItemId("");
      setQuantity("1");
      setUnitPrice("0");
      setDiscountPercent("0");
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
          <label className="mb-1 block text-sm font-medium">Unit price</label>
          <Input value={unitPrice} onChange={(e) => setUnitPrice(e.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Discount %</label>
          <Input value={discountPercent} onChange={(e) => setDiscountPercent(e.target.value)} inputMode="decimal" required />
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
          <Input value={taxPercent} onChange={(e) => setTaxPercent(e.target.value)} inputMode="decimal" required />
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
