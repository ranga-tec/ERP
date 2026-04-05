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

type CustomerReturnLineDto = {
  id: string;
  itemId: string;
  quantity: number;
  unitPrice: number;
  batchNumber?: string | null;
  serials: string[];
};

type EditableCustomerReturnLine = {
  id: string;
  itemId: string;
  quantity: string;
  unitPrice: string;
  batchNumber: string;
  serials: string;
};

type WarehouseRef = { id: string; code: string; name: string };

type CustomerReturnLinesEditorProps = {
  customerReturnId: string;
  warehouseId: string;
  warehouses: WarehouseRef[];
  lines: CustomerReturnLineDto[];
  itemLabelById: Map<string, ReactNode>;
  itemSearchLabelById: Map<string, string>;
  canEdit: boolean;
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

function toEditableLines(lines: CustomerReturnLineDto[]): EditableCustomerReturnLine[] {
  return lines.map((line) => ({
    id: line.id,
    itemId: line.itemId,
    quantity: line.quantity.toString(),
    unitPrice: line.unitPrice.toString(),
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

export function CustomerReturnLinesEditor({
  customerReturnId,
  warehouseId,
  warehouses,
  lines,
  itemLabelById,
  itemSearchLabelById,
  canEdit,
}: CustomerReturnLinesEditorProps) {
  const router = useRouter();
  const [draftLines, setDraftLines] = useState<EditableCustomerReturnLine[]>(() => toEditableLines(lines));
  const [editingLineId, setEditingLineId] = useState<string | null>(null);
  const [busyLineId, setBusyLineId] = useState<string | null>(null);
  const [errorByLineId, setErrorByLineId] = useState<Record<string, string | null>>({});
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search);

  useEffect(() => {
    setDraftLines(toEditableLines(lines));
  }, [lines]);

  function updateRow(
    lineId: string,
    updater: (line: EditableCustomerReturnLine) => EditableCustomerReturnLine,
  ) {
    setDraftLines((current) => current.map((line) => (line.id === lineId ? updater(line) : line)));
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

      const unitPrice = parseDecimal(line.unitPrice);
      if (Number.isNaN(unitPrice) || unitPrice < 0) {
        throw new Error("Unit price must be 0 or greater.");
      }

      const serials = parseList(line.serials);

      await apiPutNoContent(`sales/customer-returns/${customerReturnId}/lines/${lineId}`, {
        quantity,
        unitPrice,
        batchNumber: line.batchNumber.trim() || null,
        serials: serials.length ? serials : null,
      });

      setEditingLineId(null);
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
    if (!window.confirm("Delete this customer return line?")) {
      return;
    }

    setErrorByLineId((current) => ({ ...current, [lineId]: null }));
    setBusyLineId(lineId);

    try {
      await apiDeleteNoContent(`sales/customer-returns/${customerReturnId}/lines/${lineId}`);
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

  const editingLine = editingLineId
    ? draftLines.find((line) => line.id === editingLineId) ?? null
    : null;

  const baseColumns = useMemo<EditableDataTableColumn<EditableCustomerReturnLine>[]>(() => [
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
      key: "unitPrice",
      header: "Unit Price",
      kind: "money",
      align: "right",
      getValue: (line) => line.unitPrice,
      setValue: (line, value) => ({ ...line, unitPrice: value }),
      inputClassName: "min-w-24",
      renderDisplay: (line) => formatGridMoney(parseDecimal(line.unitPrice)),
    },
    {
      key: "lineTotal",
      header: "Line Total",
      kind: "display",
      align: "right",
      render: (line) => {
        const quantity = parseDecimal(line.quantity);
        const unitPrice = parseDecimal(line.unitPrice);
        const lineTotal = Number.isFinite(quantity) && Number.isFinite(unitPrice) ? quantity * unitPrice : NaN;
        return Number.isFinite(lineTotal) ? formatGridMoney(lineTotal) : "-";
      },
      footer: (visibleLines) =>
        formatGridMoney(
          visibleLines.reduce((sum, line) => {
            const quantity = parseDecimal(line.quantity);
            const unitPrice = parseDecimal(line.unitPrice);
            return Number.isFinite(quantity) && Number.isFinite(unitPrice) ? sum + quantity * unitPrice : sum;
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

  const columns: EditableDataTableColumn<EditableCustomerReturnLine>[] = canEdit
    ? [
        ...baseColumns,
        {
          key: "actions",
          header: "Actions",
          kind: "display",
          render: (line) => {
            const isEditing = editingLineId === line.id;
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
                        onClick={() => {
                          setErrorByLineId((current) => ({ ...current, [line.id]: null }));
                          setEditingLineId(null);
                          setDraftLines(toEditableLines(lines));
                        }}
                        disabled={busy}
                        tabIndex={-1}
                      >
                        Cancel
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
        caption="Customer return lines"
        columns={columns}
        rows={filteredLines}
        rowKey={(line) => line.id}
        isRowEditing={(line) => canEdit && editingLineId === line.id}
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
