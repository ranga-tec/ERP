import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { ItemListPanel } from "./ItemListPanel";
import type { BrandDto, CategoryDto, ItemDto } from "./item-definitions";

const primaryLinkClassName = "inline-flex items-center justify-center rounded-xl bg-[var(--accent)] px-3.5 py-2 text-sm font-semibold text-[var(--accent-contrast)] shadow-[var(--shadow-button)] transition-all duration-200 hover:-translate-y-px hover:bg-[var(--accent-hover)] hover:shadow-[var(--shadow-button)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]";

export default async function ItemsPage({
  searchParams,
}: {
  searchParams?: Promise<{ itemId?: string }>;
}) {
  const sp = await searchParams;
  const [items, brands, categories] = await Promise.all([
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<BrandDto[]>("/brands"),
    backendFetchJson<CategoryDto[]>("/item-categories"),
  ]);
  const requestedItemId = sp?.itemId?.trim() ?? "";
  const initialSelectedItemId = items.some((item) => item.id === requestedItemId)
    ? requestedItemId
    : "";

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Items</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Search the full item master and open separate view and edit screens from the grid.
          </p>
        </div>
        <Link href="/master-data/items/create" className={primaryLinkClassName}>
          Create Item
        </Link>
      </div>

      <Card id="item-list">
        <div className="mb-3 text-sm font-semibold">Item List</div>
        <ItemListPanel
          items={items}
          brands={brands}
          categories={categories}
          highlightItemId={initialSelectedItemId}
        />
      </Card>
    </div>
  );
}
