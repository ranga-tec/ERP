"use client";

import { useDeferredValue, useMemo, useState } from "react";
import { ItemInlineLink } from "@/components/InlineLink";
import { Input, Table } from "@/components/ui";
import { GoodsReceiptLineRow } from "./GoodsReceiptLineRow";

type GoodsReceiptLineDto = {
  id: string;
  purchaseOrderLineId?: string | null;
  itemId: string;
  quantity: number;
  unitCost: number;
  batchNumber?: string | null;
  serials: string[];
};

type ItemRef = {
  id: string;
  sku: string;
  name: string;
  trackingType: number;
};

function normalizeSearch(value: string): string {
  return value.trim().toLowerCase();
}

export function GoodsReceiptDraftLinesTable({
  goodsReceiptId,
  lines,
  items,
  canEdit,
}: {
  goodsReceiptId: string;
  lines: GoodsReceiptLineDto[];
  items: ItemRef[];
  canEdit: boolean;
}) {
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search);
  const itemById = useMemo(() => new Map(items.map((item) => [item.id, item])), [items]);

  const filteredLines = useMemo(() => {
    const query = normalizeSearch(deferredSearch);
    if (!query) {
      return lines;
    }

    return lines.filter((line) => {
      const item = itemById.get(line.itemId);
      const searchableText = [
        item?.sku ?? "",
        item?.name ?? "",
        line.purchaseOrderLineId ?? "",
        line.batchNumber ?? "",
        line.serials.join(" "),
      ]
        .join(" ")
        .toLowerCase();

      return searchableText.includes(query);
    });
  }, [deferredSearch, itemById, lines]);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search item, PO line, batch, serial..."
          className="w-full max-w-md"
        />
        <div className="text-xs text-zinc-500">
          Showing {filteredLines.length} of {lines.length} line(s)
        </div>
      </div>

      <div className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="py-2 pr-3">Item</th>
              <th className="py-2 pr-3">Qty</th>
              <th className="py-2 pr-3">Unit Cost</th>
              <th className="py-2 pr-3">Batch</th>
              <th className="py-2 pr-3">Serials</th>
              {canEdit ? <th className="py-2 pr-3">Actions</th> : null}
            </tr>
          </thead>
          <tbody>
            {filteredLines.map((line) => {
              const item = itemById.get(line.itemId);
              const itemLabel = (
                <ItemInlineLink itemId={line.itemId}>
                  {item ? `${item.sku} - ${item.name}` : line.itemId}
                </ItemInlineLink>
              );

              return (
                <GoodsReceiptLineRow
                  key={line.id}
                  goodsReceiptId={goodsReceiptId}
                  line={line}
                  itemLabel={itemLabel}
                  canEdit={canEdit}
                />
              );
            })}
            {filteredLines.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={canEdit ? 6 : 5}>
                  {lines.length === 0
                    ? "No draft lines yet. Enter received quantities in the PO receipt grid above."
                    : "No draft lines match the current search."}
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </div>
    </div>
  );
}
