import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { ItemEditPanel } from "../../ItemEditPanel";
import type { BrandDto, CategoryDto, ItemDto, LedgerAccountOptionDto, SubcategoryDto, UomDto } from "../../item-definitions";

export default async function ItemEditPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [item, brands, uoms, categories, subcategories, accountOptions] = await Promise.all([
    backendFetchJson<ItemDto>(`/items/${id}`),
    backendFetchJson<BrandDto[]>("/brands"),
    backendFetchJson<UomDto[]>("/uoms"),
    backendFetchJson<CategoryDto[]>("/item-categories"),
    backendFetchJson<SubcategoryDto[]>("/item-subcategories"),
    backendFetchJson<LedgerAccountOptionDto[]>("/items/account-options"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/master-data/items" className="hover:underline">
            Items
          </Link>{" "}
          /{" "}
          <Link href={`/master-data/items/${item.id}`} className="hover:underline">
            <span className="font-mono text-xs">{item.sku}</span>
          </Link>{" "}
          / Edit
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Edit Item</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Update master data, classification, default cost, finance account mapping, and active status.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Item Details</div>
        <ItemEditPanel
          item={item}
          brands={brands}
          uoms={uoms}
          categories={categories}
          subcategories={subcategories}
          accountOptions={accountOptions}
        />
      </Card>
    </div>
  );
}
