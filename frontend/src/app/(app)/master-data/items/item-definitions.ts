export type BrandDto = { id: string; code: string; name: string; isActive: boolean };

export type UomDto = { id: string; code: string; name: string; isActive: boolean };

export type CategoryDto = { id: string; code: string; name: string; isActive: boolean };

export type SubcategoryDto = {
  id: string;
  categoryId: string;
  categoryCode?: string | null;
  categoryName?: string | null;
  code: string;
  name: string;
  isActive: boolean;
};

export type ItemDto = {
  id: string;
  sku: string;
  name: string;
  type: number;
  trackingType: number;
  unitOfMeasure: string;
  brandId?: string | null;
  categoryId?: string | null;
  categoryCode?: string | null;
  categoryName?: string | null;
  subcategoryId?: string | null;
  subcategoryCode?: string | null;
  subcategoryName?: string | null;
  barcode?: string | null;
  defaultUnitCost: number;
  revenueAccountId?: string | null;
  revenueAccountCode?: string | null;
  revenueAccountName?: string | null;
  expenseAccountId?: string | null;
  expenseAccountCode?: string | null;
  expenseAccountName?: string | null;
  isActive: boolean;
};

export type LedgerAccountOptionDto = {
  id: string;
  code: string;
  name: string;
  accountType: number;
  allowsPosting: boolean;
  isActive: boolean;
};

export type ItemRef = { id: string; sku: string; name: string };

export type ItemAttachmentDto = {
  id: string;
  itemId: string;
  fileName: string;
  url: string;
  isImage: boolean;
  contentType?: string | null;
  sizeBytes?: number | null;
  notes?: string | null;
  createdAt: string;
  createdBy?: string | null;
};

export type ItemPriceHistoryDto = {
  auditLogId: string;
  occurredAt: string;
  userId?: string | null;
  oldDefaultUnitCost?: number | null;
  newDefaultUnitCost: number;
};

export const itemTypes = [
  { value: 1, label: "Equipment" },
  { value: 2, label: "Spare Part" },
  { value: 3, label: "Service" },
];

export const trackingTypes = [
  { value: 0, label: "None" },
  { value: 1, label: "Serial" },
  { value: 2, label: "Batch" },
];

export const itemTypeLabel: Record<number, string> = {
  1: "Equipment",
  2: "Spare Part",
  3: "Service",
};

export const trackingLabel: Record<number, string> = {
  0: "None",
  1: "Serial",
  2: "Batch",
};

export function formatLedgerAccountOptionLabel(account: LedgerAccountOptionDto): string {
  const flags: string[] = [];

  if (!account.allowsPosting) {
    flags.push("group");
  }

  if (!account.isActive) {
    flags.push("inactive");
  }

  return flags.length > 0
    ? `${account.code} - ${account.name} (${flags.join(", ")})`
    : `${account.code} - ${account.name}`;
}
