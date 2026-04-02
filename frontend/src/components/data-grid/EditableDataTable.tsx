"use client";

import type { ComponentProps } from "react";
import { LookupCell } from "./LookupCell";
import type {
  CellClassName,
  DataGridFooter,
  EditableDataTableColumn,
  EditableDataTableProps,
  LookupColumn,
  SelectColumn,
  TextLikeColumn,
} from "./types";

const inputClassName =
  "w-full rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80";

const textareaClassName =
  "w-full min-h-24 rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80";

const selectClassName =
  "w-full rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]";

function cx(...parts: Array<string | null | undefined | false>) {
  return parts.filter(Boolean).join(" ");
}

function resolveClassName<Row>(value: CellClassName<Row> | undefined, row: Row): string {
  if (!value) {
    return "";
  }

  return typeof value === "function" ? value(row) ?? "" : value;
}

function alignmentClassName(align: EditableDataTableColumn<unknown>["align"] | undefined): string {
  if (align === "center") {
    return "text-center";
  }

  if (align === "right") {
    return "text-right";
  }

  return "text-left";
}

function editorInputMode(kind: TextLikeColumn<unknown>["kind"]): ComponentProps<"input">["inputMode"] | undefined {
  if (kind === "number" || kind === "money" || kind === "percent") {
    return "decimal";
  }

  return undefined;
}

function editorType(kind: TextLikeColumn<unknown>["kind"]): ComponentProps<"input">["type"] | undefined {
  if (kind === "date") {
    return "date";
  }

  if (kind === "datetime") {
    return "datetime-local";
  }

  return undefined;
}

function resolveOptions<Row>(column: SelectColumn<Row> | LookupColumn<Row>, row: Row) {
  return typeof column.options === "function" ? column.options(row) : column.options;
}

function defaultDisplayValue<Row>(column: EditableDataTableColumn<Row>, row: Row): string {
  if (column.kind !== "select" && column.kind !== "lookup") {
    return column.kind === "display" ? "" : column.getValue(row);
  }

  const selectedValue = column.getValue(row);
  const option = resolveOptions(column, row).find((candidate) => candidate.value === selectedValue);
  return option?.label ?? selectedValue;
}

function resolveFooter<Row>(footer: DataGridFooter<Row> | undefined, rows: Row[]) {
  if (!footer) {
    return null;
  }

  return typeof footer === "function" ? footer(rows) : footer;
}

function renderColumnContent<Row>(
  column: EditableDataTableColumn<Row>,
  row: Row,
  editing: boolean,
  onRowChange: EditableDataTableProps<Row>["onRowChange"],
  onSubmitRow: EditableDataTableProps<Row>["onSubmitRow"],
  rowIdentifier: string,
) {
  if (column.kind === "display") {
    return column.render(row);
  }

  if (!editing || !onRowChange) {
    return column.renderDisplay ? column.renderDisplay(row) : defaultDisplayValue(column, row);
  }

  const disabled = column.disabled?.(row) ?? false;
  const updateValue = (value: string) => {
    onRowChange(rowIdentifier, (current) => column.setValue(current, value));
  };

  const handleKeyDown = (event: {
    key: string;
    preventDefault: () => void;
    ctrlKey: boolean;
    metaKey: boolean;
  }) => {
    if (!onSubmitRow || event.key !== "Enter") {
      return;
    }

    if (column.kind === "textarea") {
      if (!event.ctrlKey && !event.metaKey && column.submitOnEnter !== true) {
        return;
      }
    } else if (column.submitOnEnter === false) {
      return;
    }

    event.preventDefault();
    onSubmitRow(rowIdentifier);
  };

  if (column.kind === "textarea") {
    return (
      <textarea
        value={column.getValue(row)}
        onChange={(event) => updateValue(event.target.value)}
        placeholder={column.placeholder}
        rows={column.rows}
        className={cx(textareaClassName, column.inputClassName)}
        disabled={disabled}
        onKeyDown={handleKeyDown}
      />
    );
  }

  if (column.kind === "select") {
    const options = resolveOptions(column, row);
    return (
      <select
        value={column.getValue(row)}
        onChange={(event) => updateValue(event.target.value)}
        className={cx(selectClassName, column.inputClassName)}
        disabled={disabled}
        onKeyDown={handleKeyDown}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    );
  }

  if (column.kind === "lookup") {
    return (
      <LookupCell
        value={column.getValue(row)}
        onChange={updateValue}
        options={resolveOptions(column, row)}
        placeholder={column.placeholder}
        searchPlaceholder={column.searchPlaceholder}
        noOptionsLabel={column.noOptionsLabel}
        className={column.inputClassName}
        disabled={disabled}
        submitOnEnter={column.submitOnEnter}
        onSubmit={onSubmitRow ? () => onSubmitRow(rowIdentifier) : undefined}
        renderOption={column.renderOption}
      />
    );
  }

  return (
    <input
      value={column.getValue(row)}
      onChange={(event) => updateValue(event.target.value)}
      placeholder={column.placeholder}
      inputMode={editorInputMode(column.kind)}
      type={editorType(column.kind)}
      className={cx(inputClassName, column.inputClassName)}
      disabled={disabled}
      onKeyDown={handleKeyDown}
    />
  );
}

export function EditableDataTable<Row>({
  caption,
  columns,
  rows,
  rowKey,
  emptyState,
  emptyColSpan,
  isRowEditing,
  onRowChange,
  onSubmitRow,
  rowClassName,
  tableClassName,
}: EditableDataTableProps<Row>) {
  const hasFooters = columns.some((column) => column.footer !== undefined);

  return (
    <div className="overflow-auto">
      <table className={cx("app-table w-full border-separate border-spacing-0 text-sm", tableClassName)}>
        {caption ? <caption className="sr-only">{caption}</caption> : null}
        <thead>
          <tr className="border-b border-zinc-200 text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
            {columns.map((column) => (
              <th
                key={column.key}
                className={cx(
                  "py-2 pr-3",
                  alignmentClassName(column.align),
                  column.headerClassName,
                )}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const rowIdentifier = rowKey(row);
            const editing = isRowEditing?.(row) ?? false;

            return (
              <tr
                key={rowIdentifier}
                className={cx(
                  "border-b border-zinc-100 align-top dark:border-zinc-900",
                  rowClassName?.(row),
                )}
              >
                {columns.map((column) => (
                  <td
                    key={column.key}
                    className={cx(
                      "py-2 pr-3",
                      alignmentClassName(column.align),
                      resolveClassName(column.cellClassName, row),
                    )}
                  >
                    {renderColumnContent(column, row, editing, onRowChange, onSubmitRow, rowIdentifier)}
                  </td>
                ))}
              </tr>
            );
          })}

          {rows.length === 0 ? (
            <tr>
              <td className="py-6 text-sm text-zinc-500" colSpan={emptyColSpan ?? columns.length}>
                {emptyState}
              </td>
            </tr>
          ) : null}
        </tbody>
        {hasFooters ? (
          <tfoot>
            <tr className="border-t border-zinc-200 text-sm font-semibold text-zinc-700 dark:border-zinc-800 dark:text-zinc-200">
              {columns.map((column) => (
                <td
                  key={column.key}
                  className={cx(
                    "py-3 pr-3",
                    alignmentClassName(column.align),
                    column.footerClassName,
                  )}
                >
                  {resolveFooter(column.footer, rows)}
                </td>
              ))}
            </tr>
          </tfoot>
        ) : null}
      </table>
    </div>
  );
}

export const EditableDataGrid = EditableDataTable;
