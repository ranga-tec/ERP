"use client";

import { useDeferredValue, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import {
  EditableDataTable,
  formatGridMoney,
  formatGridNumber,
  type EditableDataTableColumn,
} from "@/components/data-grid";
import { Button, Input } from "@/components/ui";

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

  const columns = useMemo<EditableDataTableColumn<EditableReceiptPlanLine>[]>(() => [
    {
      key: "item",
      header: "Item",
      kind: "display",
      render: (line) => {
        const item = itemById.get(line.itemId);
        const itemLabel = item ? `${item.sku} - ${item.name}` : line.itemId;
        const quantity = Number(line.quantity || 0);
        const lineState = formatLineState(line);
        const trackingLabel = trackingBadgeLabel(item, Number.isNaN(quantity) ? 0 : quantity);

        return (
          <>
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
          </>
        );
      },
      footer: "Receipt Value",
    },
    {
      key: "orderedQuantity",
      header: "Ordered",
      kind: "display",
      align: "right",
      render: (line) => formatGridNumber(line.orderedQuantity),
    },
    {
      key: "previouslyReceivedQuantity",
      header: "Posted",
      kind: "display",
      align: "right",
      render: (line) => formatGridNumber(line.previouslyReceivedQuantity),
    },
    {
      key: "reservedInOtherDraftsQuantity",
      header: "Other Drafts",
      kind: "display",
      align: "right",
      render: (line) => formatGridNumber(line.reservedInOtherDraftsQuantity),
    },
    {
      key: "availableQuantity",
      header: "Available",
      kind: "display",
      align: "right",
      cellClassName: "font-medium",
      render: (line) => formatGridNumber(line.availableQuantity),
    },
    {
      key: "quantity",
      header: "This GRN Qty",
      kind: "number",
      align: "right",
      getValue: (line) => line.quantity,
      setValue: (line, value) => ({ ...line, quantity: value }),
      placeholder: "0",
      inputClassName: "min-w-24",
      renderDisplay: (line) => formatGridNumber(Number(line.quantity || 0)),
    },
    {
      key: "unitCost",
      header: "Unit Cost",
      kind: "money",
      align: "right",
      getValue: (line) => line.unitCost,
      setValue: (line, value) => ({ ...line, unitCost: value }),
      inputClassName: "min-w-28",
      renderDisplay: (line) => formatGridMoney(Number(line.unitCost || 0)),
    },
    {
      key: "receiptValue",
      header: "Receipt Value",
      kind: "display",
      align: "right",
      render: (line) => {
        const quantity = Number(line.quantity || 0);
        const unitCost = Number(line.unitCost || 0);
        const total = Number.isFinite(quantity) && Number.isFinite(unitCost) ? quantity * unitCost : NaN;
        return Number.isFinite(total) ? formatGridMoney(total) : "-";
      },
      footer: (visibleLines) =>
        formatGridMoney(
          visibleLines.reduce((sum, line) => {
            const quantity = Number(line.quantity || 0);
            const unitCost = Number(line.unitCost || 0);
            return Number.isFinite(quantity) && Number.isFinite(unitCost) ? sum + quantity * unitCost : sum;
          }, 0),
        ),
    },
    {
      key: "batchNumber",
      header: "Batch",
      kind: "text",
      getValue: (line) => line.batchNumber,
      setValue: (line, value) => ({ ...line, batchNumber: value }),
      inputClassName: "min-w-28",
    },
    {
      key: "serials",
      header: "Serials",
      kind: "textarea",
      getValue: (line) => line.serials,
      setValue: (line, value) => ({ ...line, serials: value }),
      placeholder: "One per line or comma-separated",
      inputClassName: "min-w-56",
      rows: 4,
    },
  ], [itemById]);

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

      <EditableDataTable
        caption="GRN receipt plan lines"
        columns={columns}
        rows={filteredLines}
        rowKey={(line) => line.purchaseOrderLineId}
        isRowEditing={() => true}
        onRowChange={(purchaseOrderLineId, updater) => updateLine(purchaseOrderLineId, updater)}
        rowClassName={(line) => {
          const lineState = formatLineState(line);
          const quantity = Number(line.quantity || 0);
          return [
            lineState.rowClassName,
            quantity > 0 ? "shadow-[inset_4px_0_0_0_rgba(14,165,233,0.55)] dark:shadow-[inset_4px_0_0_0_rgba(56,189,248,0.45)]" : "",
          ].join(" ").trim();
        }}
        emptyState={draftLines.length === 0 ? "No PO lines available for receipt." : "No PO lines match the current search."}
      />
    </div>
  );
}
