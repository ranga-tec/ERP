import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { Card } from "@/components/ui";
import { ItemCreateForm } from "./ItemCreateForm";
import { ItemListPanel } from "./ItemListPanel";
import type { BrandDto, CategoryDto, ItemDto, LedgerAccountOptionDto, SubcategoryDto, UomDto } from "./item-definitions";

export default async function ItemsPage({
  searchParams,
}: {
  searchParams?: Promise<{ itemId?: string }>;
}) {
  const sp = await searchParams;
  const [items, brands, uoms, categories, subcategories, accountOptions] = await Promise.all([
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<BrandDto[]>("/brands"),
    backendFetchJson<UomDto[]>("/uoms"),
    backendFetchJson<CategoryDto[]>("/item-categories"),
    backendFetchJson<SubcategoryDto[]>("/item-subcategories"),
    backendFetchJson<LedgerAccountOptionDto[]>("/items/account-options"),
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
        <AppFormModal title="Create Item" description="Add a new item, equipment record, or service master." buttonLabel="+ New Item" size="xl">
          <ItemCreateForm
            brands={brands}
            uoms={uoms}
            categories={categories}
            subcategories={subcategories}
            accountOptions={accountOptions}
          />
        </AppFormModal>
      </div>

      <Card id="item-list">
        <div className="mb-3 text-sm font-semibold">Item List</div>
        <ItemListPanel
          items={items}
          brands={brands}
          uoms={uoms}
          categories={categories}
          subcategories={subcategories}
          accountOptions={accountOptions}
          highlightItemId={initialSelectedItemId}
        />
      </Card>
    </div>
  );
}
