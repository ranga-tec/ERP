"use client";

import { useDeferredValue, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Table } from "@/components/ui";
import { AutoGrowTextarea } from "./AutoGrowTextarea";

type ItemRef = {
  id: string;
  sku: string;
  name: string;
  trackingType: number;
};

type ReceiptPlanLine = {
  purchaseOrderLineId: string;
  itemId: string;
  orderedQuantity: number;
  previouslyReceivedQuantity: number;
  reservedInOtherDraftsQuantity: number;
  availableQuantity: number;
  goodsReceiptLineId?: string | null;
  currentQuantity: number;
  unitCost: number;
  batchNumber?: string | null;
  serials: string[];
};

type ReceiptPlanResponse = {
  lines: ReceiptPlanLine[];
};

type EditableReceiptPlanLine = {
  purchaseOrderLineId: string;
  itemId: string;
  orderedQuantity: number;
  previouslyReceivedQuantity: number;
  reservedInOtherDraftsQuantity: number;
  availableQuantity: number;
  quantity: string;
  unitCost: string;
  batchNumber: string;
  serials: string;
};

const TRACKING_SERIAL = 1;
const TRACKING_BATCH = 2;

function parseList(text: string): string[] {
  return text
    .split(/[\n,]/g)
    .map((value) => value.trim())
    .filter((value) => value.length > 0);
}

function normalizeSearch(value: string): string {
  return value.trim().toLowerCase();
}

function formatLineState(line: EditableReceiptPlanLine): {
  label: string;
  detail: string;
  rowClassName: string;
} {
  const quantity = Number(line.quantity || 0);
  if (Number.isNaN(quantity) || quantity <= 0) {
    return {
      label: "Open",
      detail: "Not received on this GRN yet.",
      rowClassName: "bg-zinc-50/60 dark:bg-zinc-950/30",
    };
  }

  const totalAfterThisGrn = line.previouslyReceivedQuantity + quantity;
  if (totalAfterThisGrn < line.orderedQuantity) {
    return {
      label: "Partial",
      detail: "Still open after this GRN.",
      rowClassName: "bg-amber-50/70 dark:bg-amber-500/10",
    };
  }

  return {
    label: "Complete",
    detail: "This GRN closes the PO line quantity.",
    rowClassName: "bg-emerald-50/70 dark:bg-emerald-500/10",
  };
}

function trackingBadgeLabel(item: ItemRef | undefined, quantity: number): string | null {
  if (!item) {
    return null;
  }

  if (item.trackingType === TRACKING_SERIAL) {
    if (quantity > 0) {
      return `Serial tracked: enter ${quantity} serial${quantity === 1 ? "" : "s"}`;
    }

    return "Serial tracked";
  }

  if (item.trackingType === TRACKING_BATCH) {
    return "Batch tracked";
  }

  return null;
}

function toEditableLines(lines: ReceiptPlanLine[]): EditableReceiptPlanLine[] {
  return lines.map((line) => ({
    purchaseOrderLineId: line.purchaseOrderLineId,
    itemId: line.itemId,
    orderedQuantity: line.orderedQuantity,
    previouslyReceivedQuantity: line.previouslyReceivedQuantity,
    reservedInOtherDraftsQuantity: line.reservedInOtherDraftsQuantity,
    availableQuantity: line.availableQuantity,
    quantity: line.currentQuantity > 0 ? line.currentQuantity.toString() : "",
    unitCost: line.unitCost.toString(),
    batchNumber: line.batchNumber ?? "",
    serials: line.serials.join("\n"),
  }));
}

export function GoodsReceiptReceiptPlanForm({
  goodsReceiptId,
  lines,
  items,
}: {
  goodsReceiptId: string;
  lines: ReceiptPlanLine[];
  items: ItemRef[];
}) {
  const router = useRouter();
  const itemById = useMemo(() => new Map(items.map((item) => [item.id, item])), [items]);
  const [draftLines, setDraftLines] = useState<EditableReceiptPlanLine[]>(() => toEditableLines(lines));
  const [search, setSearch] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const deferredSearch = useDeferredValue(search);

  useEffect(() => {
    setDraftLines(toEditableLines(lines));
  }, [lines]);

  function updateLine(purchaseOrderLineId: string, updater: (line: EditableReceiptPlanLine) => EditableReceiptPlanLine) {
    setDraftLines((current) =>
      current.map((line) => (line.purchaseOrderLineId === purchaseOrderLineId ? updater(line) : line)),
    );
  }

  async function savePlan() {
    setError(null);
    setBusy(true);

    try {
      const payload = draftLines.map((line) => {
        const quantity = Number(line.quantity || 0);
        if (Number.isNaN(quantity) || quantity < 0) {
          throw new Error("Receipt quantity must be 0 or greater.");
        }

        if (quantity > line.availableQuantity) {
          throw new Error("Receipt quantity cannot exceed the remaining PO quantity.");
        }

        const unitCost = Number(line.unitCost || 0);
        if (Number.isNaN(unitCost) || unitCost < 0) {
          throw new Error("Unit cost must be 0 or greater.");
        }

        const item = itemById.get(line.itemId);
        const itemLabel = item ? `${item.sku} - ${item.name}` : line.itemId;
        const serials = quantity > 0 ? parseList(line.serials) : [];

        if (quantity > 0 && item?.trackingType === TRACKING_SERIAL) {
          if (!Number.isInteger(quantity)) {
            throw new Error(`${itemLabel}: quantity must be a whole number for serial-tracked items.`);
          }

          if (serials.length === 0) {
            throw new Error(`${itemLabel}: serial numbers are required.`);
          }

          if (serials.length !== quantity) {
            throw new Error(`${itemLabel}: quantity must match the serial count.`);
          }
        }

        if (quantity > 0 && item?.trackingType === TRACKING_BATCH && !line.batchNumber.trim()) {
          throw new Error(`${itemLabel}: batch number is required.`);
        }

        return {
          purchaseOrderLineId: line.purchaseOrderLineId,
          quantity,
          unitCost,
          batchNumber: quantity > 0 ? line.batchNumber.trim() || null : null,
          serials,
        };
      });

      await apiPut<ReceiptPlanResponse>(`procurement/goods-receipts/${goodsReceiptId}/receipt-plan`, {
        lines: payload,
      });

      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  const filteredLines = useMemo(() => {
    const query = normalizeSearch(deferredSearch);
    if (!query) {
      return draftLines;
    }

    return draftLines.filter((line) => {
      const item = itemById.get(line.itemId);
      const searchableText = [
        item?.sku ?? "",
        item?.name ?? "",
        line.purchaseOrderLineId,
        line.batchNumber,
        line.serials,
      ]
        .join(" ")
        .toLowerCase();

      return searchableText.includes(query);
    });
  }, [deferredSearch, draftLines, itemById]);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="max-w-3xl text-sm text-zinc-600 dark:text-zinc-400">
          Enter the actual received quantity against each PO line. Leave `0` or blank for anything not yet received. If some
          lines stay open, this PO remains partially received and another GRN can be created later for the balance.
        </div>
        <Button type="button" onClick={savePlan} disabled={busy}>
          {busy ? "Saving..." : "Save receipt plan"}
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2 text-xs">
        <span className="rounded-full bg-zinc-100 px-2.5 py-1 text-zinc-700 dark:bg-zinc-900 dark:text-zinc-200">
          Grey: not received on this GRN
        </span>
        <span className="rounded-full bg-amber-100 px-2.5 py-1 text-amber-900 dark:bg-amber-500/15 dark:text-amber-200">
          Amber: partial receipt
        </span>
        <span className="rounded-full bg-emerald-100 px-2.5 py-1 text-emerald-900 dark:bg-emerald-500/15 dark:text-emerald-200">
          Green: fully received on this line
        </span>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <Input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search item, PO line, batch, serial..."
          className="w-full max-w-md"
        />
        <div className="text-xs text-zinc-500">
          Showing {filteredLines.length} of {draftLines.length} PO line(s)
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <div className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">Item</th>
              <th className="py-2 pr-3">Ordered</th>
              <th className="py-2 pr-3">Posted</th>
              <th className="py-2 pr-3">Other Drafts</th>
              <th className="py-2 pr-3">Available</th>
              <th className="py-2 pr-3">This GRN Qty</th>
              <th className="py-2 pr-3">Unit Cost</th>
              <th className="py-2 pr-3">Batch</th>
              <th className="py-2 pr-3">Serials</th>
            </tr>
          </thead>
          <tbody>
            {filteredLines.map((line) => {
              const item = itemById.get(line.itemId);
              const itemLabel = item ? `${item.sku} - ${item.name}` : line.itemId;
              const quantity = Number(line.quantity || 0);
              const lineState = formatLineState(line);
              const trackingLabel = trackingBadgeLabel(item, Number.isNaN(quantity) ? 0 : quantity);

              return (
                <tr
                  key={line.purchaseOrderLineId}
                  className={[
                    "border-b border-zinc-100 align-top dark:border-zinc-900",
                    lineState.rowClassName,
                    quantity > 0 ? "shadow-[inset_4px_0_0_0_rgba(14,165,233,0.55)] dark:shadow-[inset_4px_0_0_0_rgba(56,189,248,0.45)]" : "",
                  ].join(" ")}
                >
                  <td className="py-2 pr-3">
                    <div className="font-medium">{itemLabel}</div>
                    <div className="mt-2 flex flex-wrap items-center gap-2 text-xs">
                      <span className="rounded-full border border-current/10 bg-white/70 px-2 py-0.5 font-medium text-zinc-700 dark:bg-zinc-950/70 dark:text-zinc-200">
                        {lineState.label}
                      </span>
                      {trackingLabel ? (
                        <span className="rounded-full border border-current/10 bg-white/70 px-2 py-0.5 text-zinc-600 dark:bg-zinc-950/70 dark:text-zinc-300">
                          {trackingLabel}
                        </span>
                      ) : null}
                    </div>
                    <div className="mt-1 text-xs text-zinc-500">{lineState.detail}</div>
                  </td>
                  <td className="py-2 pr-3">{line.orderedQuantity}</td>
                  <td className="py-2 pr-3">{line.previouslyReceivedQuantity}</td>
                  <td className="py-2 pr-3">{line.reservedInOtherDraftsQuantity}</td>
                  <td className="py-2 pr-3 font-medium">{line.availableQuantity}</td>
                  <td className="py-2 pr-3">
                    <Input
                      value={line.quantity}
                      onChange={(event) =>
                        updateLine(line.purchaseOrderLineId, (current) => ({ ...current, quantity: event.target.value }))
                      }
                      inputMode="decimal"
                      placeholder="0"
                      className="min-w-24"
                    />
                  </td>
                  <td className="py-2 pr-3">
                    <Input
                      value={line.unitCost}
                      onChange={(event) =>
                        updateLine(line.purchaseOrderLineId, (current) => ({ ...current, unitCost: event.target.value }))
                      }
                      inputMode="decimal"
                      className="min-w-28"
                    />
                  </td>
                  <td className="py-2 pr-3">
                    <Input
                      value={line.batchNumber}
                      onChange={(event) =>
                        updateLine(line.purchaseOrderLineId, (current) => ({ ...current, batchNumber: event.target.value }))
                      }
                      className="min-w-28"
                    />
                  </td>
                  <td className="py-2 pr-3">
                    <AutoGrowTextarea
                      value={line.serials}
                      onChange={(event) =>
                        updateLine(line.purchaseOrderLineId, (current) => ({ ...current, serials: event.target.value }))
                      }
                      placeholder="One per line or comma-separated"
                      className="min-w-56"
                    />
                  </td>
                </tr>
              );
            })}
            {filteredLines.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={9}>
                  {draftLines.length === 0 ? "No PO lines available for receipt." : "No PO lines match the current search."}
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </div>
    </div>
  );
}
