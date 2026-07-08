"use client";

import { useRef } from "react";
import { Input } from "@/components/ui";

export function TableSearchInput({ placeholder }: { placeholder: string }) {
  const rootRef = useRef<HTMLDivElement>(null);

  function onSearch(value: string) {
    const table = rootRef.current?.parentElement?.querySelector("table");
    if (!table) return;

    const query = value.trim().toLowerCase();
    const rows = Array.from(table.querySelectorAll("tbody tr"));
    for (const row of rows) {
      const text = row.textContent?.toLowerCase() ?? "";
      const visible = !query || text.includes(query);
      (row as HTMLElement).style.display = visible ? "" : "none";
    }
  }

  return (
    <div ref={rootRef} className="mb-3 max-w-md">
      <Input placeholder={placeholder} aria-label={placeholder} onChange={(event) => onSearch(event.target.value)} />
    </div>
  );
}
