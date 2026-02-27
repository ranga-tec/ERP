"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string; unitOfMeasure: string };
type UomRef = { id: string; code: string; name: string; isActive: boolean };
type UnitConversionRef = {
  id: string;
  fromUnitOfMeasureCode: string;
  toUnitOfMeasureCode: string;
  factor: number;
  isActive: boolean;
};

function resolveConversionFactor(
  requestedUomCode: string,
  itemBaseUomCode: string,
  conversions: UnitConversionRef[],
): number | null {
  if (requestedUomCode === itemBaseUomCode) {
    return 1;
  }

  const direct = conversions.find(
    (c) =>
      c.isActive &&
      c.fromUnitOfMeasureCode === requestedUomCode &&
      c.toUnitOfMeasureCode === itemBaseUomCode,
  );
  if (direct) {
    return direct.factor;
  }

  const inverse = conversions.find(
    (c) =>
      c.isActive &&
      c.fromUnitOfMeasureCode === itemBaseUomCode &&
      c.toUnitOfMeasureCode === requestedUomCode,
  );
  if (inverse) {
    return 1 / inverse.factor;
  }

  return null;
}

export function PurchaseRequisitionLineAddForm({
  purchaseRequisitionId,
  items,
  uoms,
  conversions,
}: {
  purchaseRequisitionId: string;
  items: ItemRef[];
  uoms: UomRef[];
  conversions: UnitConversionRef[];
}) {
  const router = useRouter();
  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );
  const uomOptions = useMemo(
    () => uoms.filter((u) => u.isActive).slice().sort((a, b) => a.code.localeCompare(b.code)),
    [uoms],
  );

  const [itemId, setItemId] = useState("");
  const [requestedUomCode, setRequestedUomCode] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedItem = itemId ? items.find((i) => i.id === itemId) : undefined;

  const parsedQty = Number(quantity);
  const conversionFactor =
    selectedItem && requestedUomCode
      ? resolveConversionFactor(requestedUomCode, selectedItem.unitOfMeasure, conversions)
      : null;
  const convertedQty =
    selectedItem && conversionFactor !== null && !Number.isNaN(parsedQty)
      ? parsedQty * conversionFactor
      : null;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      if (!selectedItem) {
        throw new Error("Select an item.");
      }
      if (!requestedUomCode) {
        throw new Error("Select a request unit.");
      }

      const qty = Number(quantity);
      if (Number.isNaN(qty) || qty <= 0) {
        throw new Error("Quantity must be positive.");
      }

      const factor = resolveConversionFactor(requestedUomCode, selectedItem.unitOfMeasure, conversions);
      if (factor === null) {
        throw new Error(`No conversion rule found from ${requestedUomCode} to ${selectedItem.unitOfMeasure}.`);
      }

      const baseQty = qty * factor;
      const conversionNote = requestedUomCode === selectedItem.unitOfMeasure
        ? `Requested ${qty} ${requestedUomCode}.`
        : `Requested ${qty} ${requestedUomCode} converted to ${baseQty} ${selectedItem.unitOfMeasure} (factor ${factor}).`;

      const mergedNotes = [notes.trim(), conversionNote].filter((n) => n.length > 0).join(" | ");

      await apiPostNoContent(`procurement/purchase-requisitions/${purchaseRequisitionId}/lines`, {
        itemId,
        quantity: baseQty,
        notes: mergedNotes || null,
      });

      setItemId("");
      setRequestedUomCode("");
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
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium">Item</label>
          <Select
            value={itemId}
            onChange={(e) => {
              const selectedId = e.target.value;
              setItemId(selectedId);
              const item = items.find((i) => i.id === selectedId);
              setRequestedUomCode(item?.unitOfMeasure ?? "");
            }}
            required
          >
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
          <label className="mb-1 block text-sm font-medium">Requested UoM</label>
          <Select value={requestedUomCode} onChange={(e) => setRequestedUomCode(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {uomOptions.map((uom) => (
              <option key={uom.id} value={uom.code}>
                {uom.code} - {uom.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Requested Qty</label>
          <Input
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            inputMode="decimal"
            required
          />
        </div>
      </div>

      {selectedItem ? (
        <div className="rounded-md border border-zinc-200 bg-zinc-50 p-3 text-sm text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-200">
          Base item UoM: <span className="font-mono">{selectedItem.unitOfMeasure}</span>
          {requestedUomCode && conversionFactor !== null && convertedQty !== null ? (
            <span>
              {" "}
              | Converted Qty: <span className="font-semibold">{convertedQty}</span> {selectedItem.unitOfMeasure}
            </span>
          ) : requestedUomCode ? (
            <span className="text-red-600 dark:text-red-400"> | No conversion rule found.</span>
          ) : null}
        </div>
      ) : null}

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
