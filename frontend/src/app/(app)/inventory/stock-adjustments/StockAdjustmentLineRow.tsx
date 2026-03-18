"use client";

import { useRouter } from "next/navigation";
import { useState, type ReactNode } from "react";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Textarea } from "@/components/ui";
import { LineStockInsight } from "@/components/LineStockInsight";

type StockAdjustmentLineDto = {
  id: string;
  countedQuantity?: number | null;
  systemQuantity?: number | null;
  quantityDelta: number;
  unitCost: number;
  batchNumber?: string | null;
  serials: string[];
};
type WarehouseRef = { id: string; code: string; name: string };

function parseList(text: string): string[] {
  return text
    .split(/[\n,]/g)
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

function formatNumber(value: number) {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 4 }).format(value);
}

function formatSignedNumber(value: number) {
  const formatted = formatNumber(Math.abs(value));
  if (value > 0) {
    return `+${formatted}`;
  }

  if (value < 0) {
    return `-${formatted}`;
  }

  return formatted;
}

export function StockAdjustmentLineRow({
  adjustmentId,
  line,
  itemId,
  warehouseId,
  warehouses,
  itemLabel,
  canEdit,
}: {
  adjustmentId: string;
  line: StockAdjustmentLineDto;
  itemId: string;
  warehouseId: string;
  warehouses: WarehouseRef[];
  itemLabel: ReactNode;
  canEdit: boolean;
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [countedQuantity, setCountedQuantity] = useState(
    line.countedQuantity != null ? line.countedQuantity.toString() : line.quantityDelta.toString(),
  );
  const [unitCost, setUnitCost] = useState(line.unitCost.toString());
  const [batchNumber, setBatchNumber] = useState(line.batchNumber ?? "");
  const [serials, setSerials] = useState(line.serials.join("\n"));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCountedQuantity(line.countedQuantity != null ? line.countedQuantity.toString() : line.quantityDelta.toString());
    setUnitCost(line.unitCost.toString());
    setBatchNumber(line.batchNumber ?? "");
    setSerials(line.serials.join("\n"));
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      const counted = Number(countedQuantity);
      if (countedQuantity.trim() === "" || Number.isNaN(counted) || counted < 0) {
        throw new Error("Counted quantity must be 0 or greater.");
      }

      const cost = Number(unitCost);
      if (Number.isNaN(cost) || cost < 0) {
        throw new Error("Unit cost must be 0 or greater.");
      }

      const serialList = parseList(serials);

      await apiPutNoContent(`inventory/stock-adjustments/${adjustmentId}/lines/${line.id}`, {
        countedQuantity: counted,
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
    if (!window.confirm("Delete this stock adjustment line?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`inventory/stock-adjustments/${adjustmentId}/lines/${line.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  const colSpan = canEdit ? 8 : 7;

  return (
    <>
      <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
        <td className="py-2 pr-3">{itemLabel}</td>
        <td className="py-2 pr-3">
          {isEditing ? (
            <Input value={countedQuantity} onChange={(e) => setCountedQuantity(e.target.value)} inputMode="decimal" className="min-w-20" />
          ) : (
            line.countedQuantity == null ? "-" : formatNumber(line.countedQuantity)
          )}
        </td>
        <td className="py-2 pr-3">
          {line.systemQuantity == null ? "-" : formatNumber(line.systemQuantity)}
        </td>
        <td className="py-2 pr-3">
          {isEditing ? (
            <span className="text-xs text-zinc-500">Calculated on save/post</span>
          ) : (
            formatSignedNumber(line.quantityDelta)
          )}
        </td>
        <td className="py-2 pr-3">
          {isEditing ? (
            <Input value={unitCost} onChange={(e) => setUnitCost(e.target.value)} inputMode="decimal" className="min-w-24" />
          ) : (
            formatNumber(line.unitCost)
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
      {isEditing ? (
        <tr className="border-b border-zinc-100 dark:border-zinc-900">
          <td className="pb-3 pr-3" colSpan={colSpan}>
            <LineStockInsight
              warehouses={warehouses}
              warehouseId={warehouseId}
              itemId={itemId}
              batchNumber={batchNumber}
              countedQuantity={countedQuantity}
            />
          </td>
        </tr>
      ) : null}
    </>
  );
}




