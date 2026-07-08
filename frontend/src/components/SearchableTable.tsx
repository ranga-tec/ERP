"use client";

import { Children, isValidElement, type ReactElement, type ReactNode, useMemo, useState } from "react";
import { Input, Table } from "@/components/ui";

type SearchableRowProps = {
  searchText: string;
  children: ReactNode;
};

export function SearchableRow({ children }: SearchableRowProps) {
  return <>{children}</>;
}

export function SearchableTable({
  placeholder,
  headers,
  children,
  emptyMessage,
  emptyColSpan,
}: {
  placeholder: string;
  headers: ReactNode;
  children: ReactNode;
  emptyMessage: string;
  emptyColSpan: number;
}) {
  const [query, setQuery] = useState("");
  const rows = useMemo(
    () => Children.toArray(children).filter(isValidElement) as ReactElement<SearchableRowProps>[],
    [children],
  );
  const normalizedQuery = query.trim().toLowerCase();
  const visibleRows = normalizedQuery
    ? rows.filter((row) => row.props.searchText.toLowerCase().includes(normalizedQuery))
    : rows;

  return (
    <div className="space-y-3">
      <div className="max-w-md">
        <Input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder={placeholder}
          aria-label={placeholder}
        />
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
