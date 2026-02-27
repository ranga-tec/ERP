"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Textarea } from "@/components/ui";

type StockTransferLineDto = {
  id: string;
  quantity: number;
  unitCost: number;
  batchNumber?: string | null;
  serials: string[];
};

function parseList(text: string): string[] {
  return text
    .split(/[\n,]/g)
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

export function StockTransferLineRow({
  transferId,
  line,
  itemLabel,
  canEdit,
}: {
  transferId: string;
  line: StockTransferLineDto;
  itemLabel: string;
  canEdit: boolean;
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [quantity, setQuantity] = useState(line.quantity.toString());
  const [unitCost, setUnitCost] = useState(line.unitCost.toString());
  const [batchNumber, setBatchNumber] = useState(line.batchNumber ?? "");
  const [serials, setSerials] = useState(line.serials.join("\n"));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setQuantity(line.quantity.toString());
    setUnitCost(line.unitCost.toString());
    setBatchNumber(line.batchNumber ?? "");
    setSerials(line.serials.join("\n"));
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

      const cost = Number(unitCost);
      if (Number.isNaN(cost) || cost < 0) {
        throw new Error("Unit cost must be 0 or greater.");
      }

      const serialList = parseList(serials);

      await apiPutNoContent(`inventory/stock-transfers/${transferId}/lines/${line.id}`, {
        quantity: qty,
        unitCost: cost,
        batchNumber: batchNumber.trim() || null,
        serials: serialList.length ? serialList : null,
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
    if (!window.confirm("Delete this stock transfer line?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`inventory/stock-transfers/${transferId}/lines/${line.id}`);
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
          <Input value={quantity} onChange={(e) => setQuantity(e.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          line.quantity
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={unitCost} onChange={(e) => setUnitCost(e.target.value)} inputMode="decimal" className="min-w-24" />
        ) : (
          line.unitCost
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={batchNumber} onChange={(e) => setBatchNumber(e.target.value)} className="min-w-24" />
        ) : (
          <span className="font-mono text-xs text-zinc-500">{line.batchNumber ?? "-"}</span>
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Textarea
            value={serials}
            onChange={(e) => setSerials(e.target.value)}
            placeholder="One per line or comma-separated"
            className="min-h-20 min-w-56"
          />
        ) : (
          <span className="font-mono text-xs text-zinc-500">{line.serials.length ? line.serials.join(", ") : "-"}</span>
        )}
      </td>
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

