import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { ItemCreateForm } from "../ItemCreateForm";
import type { BrandDto, CategoryDto, SubcategoryDto, UomDto } from "../item-definitions";

export default async function ItemCreatePage() {
  const [brands, uoms, categories, subcategories] = await Promise.all([
    backendFetchJson<BrandDto[]>("/brands"),
    backendFetchJson<UomDto[]>("/uoms"),
    backendFetchJson<CategoryDto[]>("/item-categories"),
    backendFetchJson<SubcategoryDto[]>("/item-subcategories"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/master-data/items" className="hover:underline">
            Items
          </Link>{" "}
          / Create
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Create Item</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Add a new item, equipment record, or service master.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Item Details</div>
        <ItemCreateForm
          brands={brands}
          uoms={uoms}
          categories={categories}
          subcategories={subcategories}
        />
      </Card>
    </div>
  );
}
