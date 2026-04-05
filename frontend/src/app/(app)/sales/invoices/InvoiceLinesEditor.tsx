"use client";

import { useDeferredValue, useEffect, useMemo, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import {
  EditableDataTable,
  formatGridMoney,
  formatGridNumber,
  formatGridPercent,
  type EditableDataTableColumn,
} from "@/components/data-grid";
import { Button, Input, SecondaryButton } from "@/components/ui";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";

type InvoiceLineDto = {
  id: string;
  itemId: string;
  revenueAccountId?: string | null;
  revenueAccountCode?: string | null;
  revenueAccountName?: string | null;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  taxPercent: number;
  lineTotal: number;
};

type EditableInvoiceLine = {
  id: string;
  itemId: string;
  revenueAccountId?: string | null;
  revenueAccountCode?: string | null;
  revenueAccountName?: string | null;
  quantity: string;
  unitPrice: string;
  discountPercent: string;
  taxPercent: string;
};

type InvoiceLinesEditorProps = {
  invoiceId: string;
  lines: InvoiceLineDto[];
  itemLabelById: Map<string, ReactNode>;
  itemSearchLabelById: Map<string, string>;
  canEdit: boolean;
};

function normalizeSearch(value: string): string {
  return value.trim().toLowerCase();
}

function toEditableLines(lines: InvoiceLineDto[]): EditableInvoiceLine[] {
  return lines.map((line) => ({
    id: line.id,
    itemId: line.itemId,
    revenueAccountId: line.revenueAccountId ?? null,
    revenueAccountCode: line.revenueAccountCode ?? null,
    revenueAccountName: line.revenueAccountName ?? null,
    quantity: line.quantity.toString(),
    unitPrice: line.unitPrice.toString(),
    discountPercent: line.discountPercent.toString(),
    taxPercent: line.taxPercent.toString(),
  }));
}

function parseDecimal(value: string): number {
  return Number(value || 0);
}

export function InvoiceLinesEditor({
  invoiceId,
  lines,
  itemLabelById,
  itemSearchLabelById,
  canEdit,
}: InvoiceLinesEditorProps) {
  const router = useRouter();
  const [draftLines, setDraftLines] = useState<EditableInvoiceLine[]>(() => toEditableLines(lines));
  const [editingLineId, setEditingLineId] = useState<string | null>(null);
  const [busyLineId, setBusyLineId] = useState<string | null>(null);
  const [errorByLineId, setErrorByLineId] = useState<Record<string, string | null>>({});
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search);

  useEffect(() => {
    setDraftLines(toEditableLines(lines));
  }, [lines]);

  function updateRow(lineId: string, updater: (line: EditableInvoiceLine) => EditableInvoiceLine) {
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

      const discountPercent = parseDecimal(line.discountPercent);
      if (Number.isNaN(discountPercent) || discountPercent < 0) {
        throw new Error("Discount percent must be 0 or greater.");
      }

      const taxPercent = parseDecimal(line.taxPercent);
      if (Number.isNaN(taxPercent) || taxPercent < 0) {
        throw new Error("Tax percent must be 0 or greater.");
      }

      await apiPutNoContent(`sales/invoices/${invoiceId}/lines/${lineId}`, {
        quantity,
        unitPrice,
        discountPercent,
        taxPercent,
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
    if (!window.confirm("Delete this invoice line?")) {
      return;
    }

    setErrorByLineId((current) => ({ ...current, [lineId]: null }));
    setBusyLineId(lineId);

    try {
      await apiDeleteNoContent(`sales/invoices/${invoiceId}/lines/${lineId}`);
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

    return draftLines.filter((line) => (itemSearchLabelById.get(line.itemId) ?? line.itemId).includes(query));
  }, [deferredSearch, draftLines, itemSearchLabelById]);

  const baseColumns = useMemo<EditableDataTableColumn<EditableInvoiceLine>[]>(() => [
    {
      key: "item",
      header: "Item",
      kind: "display",
      render: (line) => itemLabelById.get(line.itemId) ?? line.itemId,
      footer: "Visible Total",
    },
    {
      key: "revenueAccount",
      header: "Income Acct",
      kind: "display",
      render: (line) =>
        line.revenueAccountCode
          ? `${line.revenueAccountCode}${line.revenueAccountName ? ` - ${line.revenueAccountName}` : ""}`
          : <span className="text-amber-700 dark:text-amber-300">Unassigned</span>,
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
      key: "discountPercent",
      header: "Disc %",
      kind: "percent",
      align: "right",
      getValue: (line) => line.discountPercent,
      setValue: (line, value) => ({ ...line, discountPercent: value }),
      inputClassName: "min-w-20",
      renderDisplay: (line) => formatGridPercent(parseDecimal(line.discountPercent)),
    },
    {
      key: "taxPercent",
      header: "Tax %",
      kind: "percent",
      align: "right",
      getValue: (line) => line.taxPercent,
      setValue: (line, value) => ({ ...line, taxPercent: value }),
      inputClassName: "min-w-20",
      renderDisplay: (line) => formatGridPercent(parseDecimal(line.taxPercent)),
    },
    {
      key: "lineTotal",
      header: "Line Total",
      kind: "display",
      align: "right",
      render: (line) => {
        const quantity = parseDecimal(line.quantity);
        const unitPrice = parseDecimal(line.unitPrice);
        const discountPercent = parseDecimal(line.discountPercent);
        const taxPercent = parseDecimal(line.taxPercent);
        const subtotal = Number.isFinite(quantity) && Number.isFinite(unitPrice) && Number.isFinite(discountPercent)
          ? quantity * unitPrice * (1 - discountPercent / 100)
          : NaN;
        const lineTotal = Number.isFinite(subtotal) && Number.isFinite(taxPercent) ? subtotal * (1 + taxPercent / 100) : NaN;
        return Number.isFinite(lineTotal) ? formatGridMoney(lineTotal) : "-";
      },
      footer: (visibleLines) =>
        formatGridMoney(
          visibleLines.reduce((sum, line) => {
            const quantity = parseDecimal(line.quantity);
            const unitPrice = parseDecimal(line.unitPrice);
            const discountPercent = parseDecimal(line.discountPercent);
            const taxPercent = parseDecimal(line.taxPercent);
            const subtotal = Number.isFinite(quantity) && Number.isFinite(unitPrice) && Number.isFinite(discountPercent)
              ? quantity * unitPrice * (1 - discountPercent / 100)
              : NaN;
            const lineTotal = Number.isFinite(subtotal) && Number.isFinite(taxPercent) ? subtotal * (1 + taxPercent / 100) : NaN;
            return Number.isFinite(lineTotal) ? sum + lineTotal : sum;
          }, 0),
        ),
    },
  ], [itemLabelById]);

  const columns: EditableDataTableColumn<EditableInvoiceLine>[] = canEdit
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
          placeholder="Search item..."
          className="w-full max-w-md"
        />
        <div className="text-xs text-zinc-500">
          Showing {filteredLines.length} of {draftLines.length} line(s)
        </div>
      </div>

      <EditableDataTable
        caption="Invoice lines"
        columns={columns}
        rows={filteredLines}
        rowKey={(line) => line.id}
        isRowEditing={(line) => canEdit && editingLineId === line.id}
        onRowChange={(lineId, updater) => updateRow(lineId, updater)}
        onSubmitRow={(lineId) => void saveLine(lineId)}
        emptyColSpan={columns.length}
        emptyState="No lines yet."
      />
    </div>
  );
}
