# Data Grid Module

This folder contains the reusable editable grid framework used by ISS transaction screens.

Design goals:

- no ERP document rules inside the grid
- typed column definitions for common ERP cell kinds
- explicit-save workflow support
- keyboard-friendly row editing
- portable enough to copy into another React/Tailwind project

Primary entrypoint:

- `@/components/data-grid`

Current column kinds:

- `display`
- `text`
- `number`
- `money`
- `percent`
- `date`
- `datetime`
- `textarea`
- `select`
- `lookup` (searchable dropdown)

The grid owns:

- row rendering
- cell editor rendering
- `Tab` cell navigation through natural DOM order
- `Enter` row submit support
- searchable lookup popup behavior
- optional footer row rendering

The document screen owns:

- DTO mapping
- validation rules
- dirty-state semantics
- API save/delete behavior
- search/filter toolbars outside the table

Minimal usage shape:

```tsx
import {
  EditableDataTable,
  formatGridMoney,
  formatGridNumber,
  type DataGridOption,
  type EditableDataTableColumn,
} from "@/components/data-grid";

const unitOptions: DataGridOption[] = [
  { value: "PCS", label: "PCS", description: "Pieces" },
  { value: "BOX", label: "BOX", description: "Box of 10", keywords: ["carton"] },
];

const columns: EditableDataTableColumn<Row>[] = [
  {
    key: "item",
    header: "Item",
    kind: "display",
    render: (row) => row.itemName,
  },
  {
    key: "uom",
    header: "UoM",
    kind: "lookup",
    options: unitOptions,
    getValue: (row) => row.uom,
    setValue: (row, value) => ({ ...row, uom: value }),
  },
  {
    key: "qty",
    header: "Qty",
    kind: "number",
    align: "right",
    getValue: (row) => row.quantity,
    setValue: (row, value) => ({ ...row, quantity: value }),
    renderDisplay: (row) => formatGridNumber(Number(row.quantity || 0)),
  },
  {
    key: "total",
    header: "Total",
    kind: "display",
    align: "right",
    render: (row) => formatGridMoney(row.total, { currency: "USD" }),
    footer: (rows) => formatGridMoney(rows.reduce((sum, row) => sum + row.total, 0), { currency: "USD" }),
  },
];
```
