"use client";

import { useDeferredValue, useEffect, useMemo, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import {
  EditableDataTable,
  formatGridNumber,
  type EditableDataTableColumn,
} from "@/components/data-grid";
import { Button, Input, SecondaryButton } from "@/components/ui";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";

type PurchaseRequisitionLineDto = {
  id: string;
  itemId: string;
  quantity: number;
  notes?: string | null;
};

type EditablePurchaseRequisitionLine = {
  id: string;
  itemId: string;
  quantity: string;
  notes: string;
};

type PurchaseRequisitionLinesEditorProps = {
  purchaseRequisitionId: string;
  lines: PurchaseRequisitionLineDto[];
  itemLabelById: Map<string, ReactNode>;
  itemSearchLabelById: Map<string, string>;
  baseUomByItemId: Map<string, string>;
  canEdit: boolean;
  startInEditMode?: boolean;
};

function normalizeSearch(value: string): string {
  return value.trim().toLowerCase();
}

function toEditableLines(lines: PurchaseRequisitionLineDto[]): EditablePurchaseRequisitionLine[] {
  return lines.map((line) => ({
    id: line.id,
    itemId: line.itemId,
    quantity: line.quantity.toString(),
    notes: line.notes ?? "",
  }));
}

function parseDecimal(value: string): number {
  return Number(value || 0);
}

export function PurchaseRequisitionLinesEditor({
  purchaseRequisitionId,
  lines,
  itemLabelById,
  itemSearchLabelById,
  baseUomByItemId,
  canEdit,
  startInEditMode = false,
}: PurchaseRequisitionLinesEditorProps) {
  const router = useRouter();
  const [draftLines, setDraftLines] = useState<EditablePurchaseRequisitionLine[]>(() => toEditableLines(lines));
  const [editingLineId, setEditingLineId] = useState<string | null>(null);
  const [busyLineId, setBusyLineId] = useState<string | null>(null);
  const [errorByLineId, setErrorByLineId] = useState<Record<string, string | null>>({});
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search);
  const allRowsEditing = canEdit && startInEditMode;

  useEffect(() => {
    setDraftLines(toEditableLines(lines));
  }, [lines]);

  function updateRow(lineId: string, updater: (line: EditablePurchaseRequisitionLine) => EditablePurchaseRequisitionLine) {
    setDraftLines((current) => current.map((line) => (line.id === lineId ? updater(line) : line)));
  }

  function resetLine(lineId: string) {
    const originalLine = lines.find((candidate) => candidate.id === lineId);
    if (!originalLine) {
      return;
    }

    setErrorByLineId((current) => ({ ...current, [lineId]: null }));
    updateRow(lineId, () => toEditableLines([originalLine])[0]);

    if (!allRowsEditing) {
      setEditingLineId(null);
    }
  }

  async function saveLine(lineId: string) {
    const line = draftLines.find((candidate) => candidate.id === lineId);
    if (!line) {
      return;
    }

    setErrorByLineId((current) => ({ ...current, [lineId]: null }));
    setBusyLineId(lineId);

    try {
      const quantity = parseDecimal(line.quantity);
      if (Number.isNaN(quantity) || quantity <= 0) {
        throw new Error("Quantity must be positive.");
      }

      await apiPutNoContent(`procurement/purchase-requisitions/${purchaseRequisitionId}/lines/${lineId}`, {
        quantity,
        notes: line.notes.trim() || null,
      });

      if (!allRowsEditing) {
        setEditingLineId(null);
      }
      router.refresh();
    } catch (error) {
      setErrorByLineId((current) => ({
        ...current,
        [lineId]: error instanceof Error ? error.message : String(error),
      }));
    } finally {
      setBusyLineId((current) => (current === lineId ? null : current));
    }
  }

  async function deleteLine(lineId: string) {
    if (!window.confirm("Delete this purchase requisition line?")) {
      return;
    }

    setErrorByLineId((current) => ({ ...current, [lineId]: null }));
    setBusyLineId(lineId);

    try {
      await apiDeleteNoContent(`procurement/purchase-requisitions/${purchaseRequisitionId}/lines/${lineId}`);
      if (editingLineId === lineId) {
        setEditingLineId(null);
      }
      router.refresh();
    } catch (error) {
      setErrorByLineId((current) => ({
        ...current,
        [lineId]: error instanceof Error ? error.message : String(error),
      }));
      setBusyLineId(null);
    }
  }

  const filteredLines = useMemo(() => {
    const query = normalizeSearch(deferredSearch);
    if (!query) {
      return draftLines;
    }

    return draftLines.filter((line) => {
      const searchableText = [
        itemSearchLabelById.get(line.itemId) ?? line.itemId,
        baseUomByItemId.get(line.itemId) ?? "",
        line.notes,
      ]
        .join(" ")
        .toLowerCase();

      return searchableText.includes(query);
    });
  }, [baseUomByItemId, deferredSearch, draftLines, itemSearchLabelById]);

  const baseColumns = useMemo<EditableDataTableColumn<EditablePurchaseRequisitionLine>[]>(() => [
    {
      key: "item",
      header: "Item",
      kind: "display",
      render: (line) => itemLabelById.get(line.itemId) ?? line.itemId,
      footer: "Visible Qty",
    },
    {
      key: "quantity",
      header: "Qty (Base)",
      kind: "number",
      align: "right",
      getValue: (line) => line.quantity,
      setValue: (line, value) => ({ ...line, quantity: value }),
      inputClassName: "min-w-24",
      renderDisplay: (line) => formatGridNumber(parseDecimal(line.quantity)),
      footer: (visibleLines) =>
        formatGridNumber(
          visibleLines.reduce((sum, line) => {
            const quantity = parseDecimal(line.quantity);
            return Number.isFinite(quantity) ? sum + quantity : sum;
          }, 0),
        ),
    },
    {
      key: "baseUom",
      header: "Base UoM",
      kind: "display",
      render: (line) => baseUomByItemId.get(line.itemId) ?? "-",
    },
    {
      key: "notes",
      header: "Notes",
      kind: "text",
      getValue: (line) => line.notes,
      setValue: (line, value) => ({ ...line, notes: value }),
      inputClassName: "min-w-56",
      renderDisplay: (line) => line.notes || "-",
    },
  ], [baseUomByItemId, itemLabelById]);

  const columns: EditableDataTableColumn<EditablePurchaseRequisitionLine>[] = canEdit
    ? [
        ...baseColumns,
        {
          key: "actions",
          header: "Actions",
          kind: "display",
          render: (line) => {
            const isEditing = allRowsEditing || editingLineId === line.id;
            const busy = busyLineId === line.id;
            const error = errorByLineId[line.id];

            return (
              <>
                <div className="flex flex-wrap items-center gap-2">
                  {isEditing ? (
                    <>
                      <Button
                        type="button"
                        className="px-2 py-1 text-xs"
                        onClick={() => void saveLine(line.id)}
                        disabled={busy}
                        tabIndex={-1}
                      >
                        {busy ? "Saving..." : "Save"}
                      </Button>
                      <SecondaryButton
                        type="button"
                        className="px-2 py-1 text-xs"
                        onClick={() => resetLine(line.id)}
                        disabled={busy}
                        tabIndex={-1}
                      >
                        {allRowsEditing ? "Reset" : "Cancel"}
                      </SecondaryButton>
                    </>
                  ) : (
                    <SecondaryButton
                      type="button"
                      className="px-2 py-1 text-xs"
                      onClick={() => {
                        setErrorByLineId((current) => ({ ...current, [line.id]: null }));
                        setEditingLineId(line.id);
                      }}
                      disabled={busy}
                    >
                      Edit
                    </SecondaryButton>
                  )}
                  <SecondaryButton
                    type="button"
                    className="px-2 py-1 text-xs"
                    onClick={() => void deleteLine(line.id)}
                    disabled={busy}
                    tabIndex={isEditing ? -1 : undefined}
                  >
                    Delete
                  </SecondaryButton>
                </div>
                {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
              </>
            );
          },
        },
      ]
    : baseColumns;

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search item, UoM, or notes..."
          className="w-full max-w-md"
        />
        <div className="text-xs text-zinc-500">
          Showing {filteredLines.length} of {draftLines.length} line(s)
        </div>
      </div>

      <EditableDataTable
        caption="Purchase requisition lines"
        columns={columns}
        rows={filteredLines}
        rowKey={(line) => line.id}
        isRowEditing={(line) => canEdit && (allRowsEditing || editingLineId === line.id)}
        onRowChange={(lineId, updater) => updateRow(lineId, updater)}
        onSubmitRow={(lineId) => void saveLine(lineId)}
        emptyColSpan={columns.length}
        emptyState="No lines yet."
      />
    </div>
  );
}
