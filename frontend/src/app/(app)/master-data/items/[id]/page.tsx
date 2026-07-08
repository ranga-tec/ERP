import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { Card, SecondaryLink } from "@/components/ui";
import { ItemEditPanel } from "../ItemEditPanel";
import { ItemManagementPanel } from "../ItemManagementPanel";
import {
  itemTypeLabel,
  trackingLabel,
  type BrandDto,
  type CategoryDto,
  type ItemDto,
  type LedgerAccountOptionDto,
  type SubcategoryDto,
  type UomDto,
} from "../item-definitions";

export default async function ItemDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [item, brands, uoms, categories, subcategories, accountOptions] = await Promise.all([
    backendFetchJson<ItemDto>(`/items/${id}`),
    backendFetchJson<BrandDto[]>("/brands"),
    backendFetchJson<UomDto[]>("/uoms"),
    backendFetchJson<CategoryDto[]>("/item-categories"),
    backendFetchJson<SubcategoryDto[]>("/item-subcategories"),
    backendFetchJson<LedgerAccountOptionDto[]>("/items/account-options"),
  ]);
  const brand = item.brandId ? brands.find((entry) => entry.id === item.brandId) : null;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/master-data/items" className="hover:underline">
            Items
          </Link>{" "}
          / <span className="font-mono text-xs">{item.sku}</span>
        </div>
        <div className="mt-2 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h1 className="text-2xl font-semibold">{item.name}</h1>
            <p className="mt-1 text-sm text-zinc-500">
              {item.sku} | {itemTypeLabel[item.type] ?? item.type} | {trackingLabel[item.trackingType] ?? item.trackingType}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <AppFormModal title="Edit Item" description="Update item master data and account mapping." buttonLabel="Edit Item" size="xl">
              <ItemEditPanel
                item={item}
                brands={brands}
                uoms={uoms}
                categories={categories}
                subcategories={subcategories}
                accountOptions={accountOptions}
              />
            </AppFormModal>
            <SecondaryLink
              href={`/api/backend/items/${item.id}/label/pdf`}
              target="_blank"
              rel="noopener noreferrer"
            >
              Item Label
            </SecondaryLink>
          </div>
        </div>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Summary</div>
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">SKU</div>
            <div className="mt-1 font-mono text-sm">{item.sku}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Name</div>
            <div className="mt-1 text-sm">{item.name}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Type</div>
            <div className="mt-1 text-sm">{itemTypeLabel[item.type] ?? item.type}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Tracking</div>
            <div className="mt-1 text-sm">{trackingLabel[item.trackingType] ?? item.trackingType}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">UoM</div>
            <div className="mt-1 text-sm">{item.unitOfMeasure}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Brand</div>
            <div className="mt-1 text-sm">
              {brand ? `${brand.code} - ${brand.name}` : "-"}
            </div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Category</div>
            <div className="mt-1 text-sm">
              {item.categoryCode ? `${item.categoryCode} - ${item.categoryName ?? ""}` : "-"}
            </div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Subcategory</div>
            <div className="mt-1 text-sm">
              {item.subcategoryCode ? `${item.subcategoryCode} - ${item.subcategoryName ?? ""}` : "-"}
            </div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Barcode</div>
            <div className="mt-1 font-mono text-sm">{item.barcode ?? "-"}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Default Cost</div>
            <div className="mt-1 text-sm">{item.defaultUnitCost}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Income / Revenue Account</div>
            <div className="mt-1 text-sm">
              {item.revenueAccountCode ? `${item.revenueAccountCode} - ${item.revenueAccountName ?? ""}` : "-"}
            </div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Expense Account</div>
            <div className="mt-1 text-sm">
              {item.expenseAccountCode ? `${item.expenseAccountCode} - ${item.expenseAccountName ?? ""}` : "-"}
            </div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-zinc-500">Active</div>
            <div className="mt-1 text-sm">{item.isActive ? "Yes" : "No"}</div>
          </div>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Attachments + Price History</div>
        <ItemManagementPanel item={{ id: item.id, sku: item.sku, name: item.name }} />
      </Card>
    </div>
  );
}
