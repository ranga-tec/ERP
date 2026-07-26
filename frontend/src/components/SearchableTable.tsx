"use client";

import { Children, isValidElement, type ReactElement, type ReactNode, useMemo, useState } from "react";
import { Input, Select, Table } from "@/components/ui";

type SearchableRowProps = {
  searchText: string;
  /** Optional bucket this row belongs to, matched against the active `filter` option. */
  filterKey?: string;
  children: ReactNode;
};

export function SearchableRow({ children }: SearchableRowProps) {
  return <>{children}</>;
}

/** Dropdown filter shown beside the search box. The `all` value is never matched against rows. */
export type SearchableTableFilter = {
  label: string;
  defaultValue: string;
  options: { value: string; label: string }[];
};

export function SearchableTable({
  placeholder,
  headers,
  children,
  emptyMessage,
  emptyColSpan,
  filter,
}: {
  placeholder: string;
  headers: ReactNode;
  children: ReactNode;
  emptyMessage: string;
  emptyColSpan: number;
  filter?: SearchableTableFilter;
}) {
  const [query, setQuery] = useState("");
  const [filterValue, setFilterValue] = useState(filter?.defaultValue ?? "all");
  const rows = useMemo(
    () => Children.toArray(children).filter(isValidElement) as ReactElement<SearchableRowProps>[],
    [children],
  );
  const normalizedQuery = query.trim().toLowerCase();
  const visibleRows = rows.filter((row) => {
    if (normalizedQuery && !row.props.searchText.toLowerCase().includes(normalizedQuery)) {
      return false;
    }
    if (filter && filterValue !== "all" && row.props.filterKey !== filterValue) {
      return false;
    }
    return true;
  });

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-3">
        <div className="min-w-0 flex-1 sm:max-w-md">
          <Input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={placeholder}
            aria-label={placeholder}
          />
        </div>
        {filter ? (
          <div className="w-44">
            <Select
              value={filterValue}
              onChange={(event) => setFilterValue(event.target.value)}
              aria-label={filter.label}
            >
              {filter.options.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </Select>
          </div>
        ) : null}
      </div>
      <div className="overflow-auto">
        <Table>
          {headers}
          <tbody>
            {visibleRows.map((row) => row.props.children)}
            {visibleRows.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={emptyColSpan}>
                  {rows.length === 0 ? emptyMessage : "No matching records."}
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </div>
    </div>
  );
}
