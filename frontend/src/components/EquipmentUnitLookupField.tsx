"use client";

import { useMemo } from "react";
import { LookupCell } from "@/components/data-grid/LookupCell";
import type { DataGridOption } from "@/components/data-grid/types";

type EquipmentUnitLookupRef = {
  id: string;
  serialNumber: string;
  itemSku?: string | null;
  itemName?: string | null;
  customerCode?: string | null;
};

type EquipmentUnitLookupFieldProps = {
  equipmentUnits: EquipmentUnitLookupRef[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  searchPlaceholder?: string;
  noOptionsLabel?: string;
  className?: string;
  disabled?: boolean;
};

export function EquipmentUnitLookupField({
  equipmentUnits,
  value,
  onChange,
  placeholder = "Select equipment unit...",
  searchPlaceholder = "Search equipment by serial number...",
  noOptionsLabel = "No matching equipment units found.",
  className,
  disabled,
}: EquipmentUnitLookupFieldProps) {
  const options = useMemo<DataGridOption[]>(
    () =>
      equipmentUnits
        .slice()
        .sort(
          (left, right) =>
            (left.itemSku ?? "").localeCompare(right.itemSku ?? "") ||
            left.serialNumber.localeCompare(right.serialNumber),
        )
        .map((unit) => {
          const itemLabel = [unit.itemSku, unit.itemName].filter(Boolean).join(" - ");
          const label = itemLabel ? `${itemLabel} / SN: ${unit.serialNumber}` : unit.serialNumber;
          const searchText = [unit.serialNumber, unit.itemSku, unit.itemName, unit.customerCode].filter(Boolean).join(" ");

          return {
            value: unit.id,
            label,
            searchText,
            keywords: [unit.serialNumber, unit.itemSku, unit.itemName, unit.customerCode].filter(Boolean) as string[],
          };
        }),
    [equipmentUnits],
  );

  return (
    <LookupCell
      value={value}
      onChange={onChange}
      options={options}
      placeholder={placeholder}
      searchPlaceholder={searchPlaceholder}
      noOptionsLabel={noOptionsLabel}
      className={className}
      disabled={disabled}
      submitOnEnter={false}
    />
  );
}
