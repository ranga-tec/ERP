export const itemListAnchorPrefix = "item-row-";

export function buildItemAnchorId(itemId: string): string {
  return `${itemListAnchorPrefix}${itemId}`;
}

export function buildItemHref(itemId: string): string {
  return `/master-data/items/${itemId}`;
}
