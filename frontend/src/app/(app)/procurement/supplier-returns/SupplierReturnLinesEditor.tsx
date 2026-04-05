"use client";

import { useDeferredValue, useEffect, useMemo, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import {
  EditableDataTable,
  formatGridMoney,
  formatGridNumber,
  type EditableDataTableColumn,
} from "@/components/data-grid";
import { LineStockInsight } from "@/components/LineStockInsight";
import { Button, Input, SecondaryButton } from "@/components/ui";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";

type SupplierReturnLineDto = {
  id: string;
  itemId: string;
  quantity: number;
  unitCost: number;
  batchNumber?: string | null;
  serials: string[];
};

type EditableSupplierReturnLine = {
  id: string;
  itemId: string;
  quantity: string;
  unitCost: string;
  batchNumber: string;
  serials: string;
};

type WarehouseRef = { id: string; code: string; name: string };

type SupplierReturnLinesEditorProps = {
  supplierReturnId: string;
  warehouseId: string;
  warehouses: WarehouseRef[];
  lines: SupplierReturnLineDto[];
  itemLabelById: Map<string, ReactNode>;
  itemSearchLabelById: Map<string, string>;
  canEdit: boolean;
  startInEditMode?: boolean;
};

function normalizeSearch(value: string): string {
  return value.trim().toLowerCase();
}

function parseList(value: string): string[] {
  return value
    .split(/[\n,]/g)
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0);
}

function toEditableLines(lines: SupplierReturnLineDto[]): EditableSupplierReturnLine[] {
  return lines.map((line) => ({
    id: line.id,
    itemId: line.itemId,
    quantity: line.quantity.toString(),
    unitCost: line.unitCost.toString(),
    batchNumber: line.batchNumber ?? "",
    serials: line.serials.join("\n"),
  }));
}

function parseDecimal(value: string): number {
  return Number(value || 0);
}

function formatSerials(value: string): string {
  const serials = parseList(value);
  return serials.length ? serials.join(", ") : "-";
}

export function SupplierReturnLinesEditor({
  supplierReturnId,
  warehouseId,
  warehouses,
  lines,
  itemLabelById,
  itemSearchLabelById,
  canEdit,
  startInEditMode = false,
}: SupplierReturnLinesEditorProps) {
  const router = useRouter();
  const [draftLines, setDraftLines] = useState<EditableSupplierReturnLine[]>(() => toEditableLines(lines));
  const [editingLineId, setEditingLineId] = useState<string | null>(null);
  const [busyLineId, setBusyLineId] = useState<string | null>(null);
  const [errorByLineId, setErrorByLineId] = useState<Record<string, string | null>>({});
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search);
  const allRowsEditing = canEdit && startInEditMode;

  useEffect(() => {
    setDraftLines(toEditableLines(lines));
  }, [lines]);

  function updateRow(
    lineId: string,
    updater: (line: EditableSupplierReturnLine) => EditableSupplierReturnLine,
  ) {
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

      const unitCost = parseDecimal(line.unitCost);
      if (Number.isNaN(unitCost) || unitCost < 0) {
        throw new Error("Unit cost must be 0 or greater.");
      }

      const serials = parseList(line.serials);

      await apiPutNoContent(`procurement/supplier-returns/${supplierReturnId}/lines/${lineId}`, {
        quantity,
        unitCost,
        batchNumber: line.batchNumber.trim() || null,
        serials: serials.length ? serials : null,
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
    if (!window.confirm("Delete this supplier return line?")) {
      return;
    }

    setErrorByLineId((current) => ({ ...current, [lineId]: null }));
    setBusyLineId(lineId);

    try {
      await apiDeleteNoContent(`procurement/supplier-returns/${supplierReturnId}/lines/${lineId}`);
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

    return draftLines.filter((line) =>
      [
        itemSearchLabelById.get(line.itemId) ?? line.itemId,
        line.batchNumber,
        line.serials,
      ]
        .join(" ")
        .toLowerCase()
        .includes(query),
    );
  }, [deferredSearch, draftLines, itemSearchLabelById]);

  const editingLine = allRowsEditing
    ? null
    : editingLineId
    ? draftLines.find((line) => line.id === editingLineId) ?? null
    : null;

  const baseColumns = useMemo<EditableDataTableColumn<EditableSupplierReturnLine>[]>(() => [
    {
      key: "item",
      header: "Item",
      kind: "display",
      render: (line) => itemLabelById.get(line.itemId) ?? line.itemId,
      footer: "Visible Total",
    },
    {
      key: "quantity",
      header: "Qty",
      kind: "number",
      align: "right",
      getValue: (line) => line.quantity,
      setValue: (line, value) => ({ ...line, quantity: value }),
      inputClassName: "min-w-20",
      renderDisplay: (line) => formatGridNumber(parseDecimal(line.quantity)),
    },
    {
      key: "unitCost",
      header: "Unit Cost",
      kind: "money",
      align: "right",
      getValue: (line) => line.unitCost,
      setValue: (line, value) => ({ ...line, unitCost: value }),
      inputClassName: "min-w-24",
      renderDisplay: (line) => formatGridMoney(parseDecimal(line.unitCost)),
    },
    {
      key: "lineTotal",
      header: "Line Total",
      kind: "display",
      align: "right",
      render: (line) => {
        const quantity = parseDecimal(line.quantity);
        const unitCost = parseDecimal(line.unitCost);
        const lineTotal = Number.isFinite(quantity) && Number.isFinite(unitCost) ? quantity * unitCost : NaN;
        return Number.isFinite(lineTotal) ? formatGridMoney(lineTotal) : "-";
      },
      footer: (visibleLines) =>
        formatGridMoney(
          visibleLines.reduce((sum, line) => {
            const quantity = parseDecimal(line.quantity);
            const unitCost = parseDecimal(line.unitCost);
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
      inputClassName: "min-w-24",
      renderDisplay: (line) => (
        <span className="font-mono text-xs text-zinc-500">{line.batchNumber || "-"}</span>
      ),
    },
    {
      key: "serials",
      header: "Serials",
      kind: "textarea",
      getValue: (line) => line.serials,
      setValue: (line, value) => ({ ...line, serials: value }),
      placeholder: "One per line or comma-separated",
      rows: 3,
      inputClassName: "min-w-56",
      renderDisplay: (line) => (
        <span className="font-mono text-xs text-zinc-500">{formatSerials(line.serials)}</span>
      ),
    },
  ], [itemLabelById]);

  const columns: EditableDataTableColumn<EditableSupplierReturnLine>[] = canEdit
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
          placeholder="Search item, batch, or serial..."
          className="w-full max-w-md"
        />
        <div className="text-xs text-zinc-500">
          Showing {filteredLines.length} of {draftLines.length} line(s)
        </div>
      </div>

      <EditableDataTable
        caption="Supplier return lines"
        columns={columns}
        rows={filteredLines}
        rowKey={(line) => line.id}
        isRowEditing={(line) => canEdit && (allRowsEditing || editingLineId === line.id)}
        onRowChange={(lineId, updater) => updateRow(lineId, updater)}
        onSubmitRow={(lineId) => void saveLine(lineId)}
        emptyColSpan={columns.length}
        emptyState="No lines yet."
      />

      {canEdit && editingLine ? (
        <div className="rounded-md border border-[var(--table-grid-strong)] bg-[var(--surface-soft)] p-3">
          <LineStockInsight
            warehouses={warehouses}
            warehouseId={warehouseId}
            itemId={editingLine.itemId}
            batchNumber={editingLine.batchNumber}
          />
        </div>
      ) : null}
    </div>
  );
}
