"use client";

import { useMemo } from "react";
import { LookupCell } from "@/components/data-grid/LookupCell";
import type { DataGridOption } from "@/components/data-grid/types";

type ItemLookupRef = {
  id: string;
  sku: string;
  name: string;
};

type ItemLookupFieldProps = {
  items: ItemLookupRef[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  searchPlaceholder?: string;
  noOptionsLabel?: string;
  emptyLabel?: string;
  className?: string;
  disabled?: boolean;
};

export function ItemLookupField({
  items,
  value,
  onChange,
  placeholder,
  searchPlaceholder = "Search item by SKU or name...",
  noOptionsLabel = "No matching items found.",
  emptyLabel,
  className,
  disabled,
}: ItemLookupFieldProps) {
  const options = useMemo<DataGridOption[]>(() => {
    const itemOptions = items
      .slice()
      .sort((left, right) => left.sku.localeCompare(right.sku) || left.name.localeCompare(right.name))
      .map((item) => ({
        value: item.id,
        label: `${item.sku} - ${item.name}`,
        searchText: `${item.sku} ${item.name}`,
        keywords: [item.sku, item.name],
      }));

    if (!emptyLabel) {
      return itemOptions;
    }

    return [
      {
        value: "",
        label: emptyLabel,
        searchText: emptyLabel,
      },
      ...itemOptions,
    ];
  }, [emptyLabel, items]);

  return (
    <LookupCell
      value={value}
      onChange={onChange}
      options={options}
      placeholder={placeholder ?? (emptyLabel ? undefined : "Select item...")}
      searchPlaceholder={searchPlaceholder}
      noOptionsLabel={noOptionsLabel}
      className={className}
      disabled={disabled}
      submitOnEnter={false}
    />
  );
}
