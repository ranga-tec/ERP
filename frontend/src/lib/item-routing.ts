export const itemListAnchorPrefix = "item-row-";

export function buildItemAnchorId(itemId: string): string {
  return `${itemListAnchorPrefix}${itemId}`;
}

export function buildItemHref(itemId: string): string {
  const searchParams = new URLSearchParams({ itemId });
  return `/master-data/items?${searchParams.toString()}#${buildItemAnchorId(itemId)}`;
}
