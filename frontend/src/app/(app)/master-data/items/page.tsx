import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { ItemCreateForm } from "./ItemCreateForm";
import { ItemEditPanel } from "./ItemEditPanel";
import { ItemListPanel } from "./ItemListPanel";
import { ItemManagementPanel } from "./ItemManagementPanel";

type BrandDto = { id: string; code: string; name: string; isActive: boolean };
type UomDto = { id: string; code: string; name: string; isActive: boolean };
type CategoryDto = { id: string; code: string; name: string; isActive: boolean };
type SubcategoryDto = {
  id: string;
  categoryId: string;
  categoryCode?: string | null;
  categoryName?: string | null;
  code: string;
  name: string;
  isActive: boolean;
};

type ItemDto = {
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
  isActive: boolean;
};

export default async function ItemsPage() {
  const [items, brands, uoms, categories, subcategories] = await Promise.all([
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<BrandDto[]>("/brands"),
    backendFetchJson<UomDto[]>("/uoms"),
    backendFetchJson<CategoryDto[]>("/item-categories"),
    backendFetchJson<SubcategoryDto[]>("/item-subcategories"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Items</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Items, equipment, and services with tracking, classification, and metadata.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <ItemCreateForm
          brands={brands.map((b) => ({ id: b.id, code: b.code, name: b.name }))}
          uoms={uoms}
          categories={categories}
          subcategories={subcategories}
        />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List / Search</div>
        <ItemListPanel items={items} brands={brands} categories={categories} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Edit Item</div>
        <ItemEditPanel
          items={items}
          brands={brands.map((b) => ({ id: b.id, code: b.code, name: b.name }))}
          uoms={uoms}
          categories={categories}
          subcategories={subcategories.map((s) => ({
            id: s.id,
            categoryId: s.categoryId,
            code: s.code,
            name: s.name,
            isActive: s.isActive,
          }))}
        />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Attachments + Price History</div>
        <ItemManagementPanel items={items.map((i) => ({ id: i.id, sku: i.sku, name: i.name }))} />
      </Card>
    </div>
  );
}
