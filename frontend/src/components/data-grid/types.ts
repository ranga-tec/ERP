import type { ReactNode } from "react";

export type DataGridOption = {
  value: string;
  label: string;
  description?: string;
  searchText?: string;
  keywords?: string[];
};

export type DataGridAlignment = "left" | "center" | "right";

export type DataGridFooter<Row> = ReactNode | ((rows: Row[]) => ReactNode);

type CellClassName<Row> = string | ((row: Row) => string | null | undefined);

type ColumnBase<Row> = {
  key: string;
  header: ReactNode;
  align?: DataGridAlignment;
  headerClassName?: string;
  cellClassName?: CellClassName<Row>;
  footer?: DataGridFooter<Row>;
  footerClassName?: string;
};

type EditableColumnBase<Row> = ColumnBase<Row> & {
  placeholder?: string;
  inputClassName?: string;
  disabled?: (row: Row) => boolean;
  submitOnEnter?: boolean;
  getValue: (row: Row) => string;
  setValue: (row: Row, value: string) => Row;
  renderDisplay?: (row: Row) => ReactNode;
};

type OptionsSource<Row> = DataGridOption[] | ((row: Row) => DataGridOption[]);

export type DisplayColumn<Row> = ColumnBase<Row> & {
  kind: "display";
  render: (row: Row) => ReactNode;
};

export type TextLikeColumn<Row> = EditableColumnBase<Row> & {
  kind: "text" | "number" | "money" | "percent" | "date" | "datetime";
};

export type TextareaColumn<Row> = EditableColumnBase<Row> & {
  kind: "textarea";
  rows?: number;
};

export type SelectColumn<Row> = EditableColumnBase<Row> & {
  kind: "select";
  options: OptionsSource<Row>;
};

export type LookupColumn<Row> = EditableColumnBase<Row> & {
  kind: "lookup";
  options: OptionsSource<Row>;
  noOptionsLabel?: string;
  searchPlaceholder?: string;
  renderOption?: (
    option: DataGridOption,
    state: { active: boolean; selected: boolean },
  ) => ReactNode;
};

export type EditableDataTableColumn<Row> =
  | DisplayColumn<Row>
  | TextLikeColumn<Row>
  | TextareaColumn<Row>
  | SelectColumn<Row>
  | LookupColumn<Row>;

export type EditableDataTableProps<Row> = {
  caption?: ReactNode;
  columns: EditableDataTableColumn<Row>[];
  rows: Row[];
  rowKey: (row: Row) => string;
  emptyState: ReactNode;
  emptyColSpan?: number;
  isRowEditing?: (row: Row) => boolean;
  onRowChange?: (rowKey: string, updater: (row: Row) => Row) => void;
  onSubmitRow?: (rowKey: string) => void;
  rowClassName?: (row: Row) => string | null | undefined;
  tableClassName?: string;
};

export type { CellClassName };
