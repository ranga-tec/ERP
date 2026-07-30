"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiDeleteNoContent } from "@/lib/api-client";
import { buildItemAnchorId } from "@/lib/item-routing";
import { AppFormModal } from "@/components/AppFormModal";
import { Input, SecondaryButton, SecondaryLink, Select, Table } from "@/components/ui";
import { ItemEditPanel } from "./ItemEditPanel";
import {
  itemTypeLabel,
  trackingLabel,
  type BrandDto,
  type CategoryDto,
  type ItemDto,
  type LedgerAccountOptionDto,
  type SubcategoryDto,
  type UomDto,
} from "./item-definitions";

const actionLinkClassName = "text-xs font-semibold text-[var(--link)] underline underline-offset-2 transition-colors hover:text-[var(--link-hover)]";
const actionButtonClass = "px-2 py-1 text-xs";

function ItemListRow({
  item,
  brandCode,
  brands,
  uoms,
  categories,
  subcategories,
  accountOptions,
  highlight,
}: {
  item: ItemDto;
  brandCode: string;
  brands: BrandDto[];
  uoms: UomDto[];
  categories: CategoryDto[];
  subcategories: SubcategoryDto[];
  accountOptions: LedgerAccountOptionDto[];
  highlight: boolean;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function deleteItem() {
    if (!window.confirm(`Delete item ${item.sku}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`items/${item.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr
      id={buildItemAnchorId(item.id)}
      className={[
        "border-b border-zinc-100 align-top dark:border-zinc-900",
        highlight ? "bg-[var(--surface-soft)]" : "",
      ].join(" ")}
    >
      <td className="py-2 pr-3">
        <div className="font-mono text-xs text-zinc-500">{item.sku}</div>
        <div className="max-w-[24ch] truncate font-medium" title={item.name}>
          {item.name}
        </div>
        {item.barcode ? (
          <div className="font-mono text-[11px] text-zinc-400">{item.barcode}</div>
        ) : null}
      </td>
      <td className="py-2 pr-3">
        <div>{itemTypeLabel[item.type] ?? item.type}</div>
        <div className="text-xs text-zinc-500">
          {trackingLabel[item.trackingType] ?? item.trackingType} &middot; {item.unitOfMeasure}
        </div>
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {item.categoryCode ? (
          <div className="max-w-[22ch] truncate" title={`${item.categoryCode} ${item.categoryName ?? ""}`}>
            {item.categoryName ?? item.categoryCode}
          </div>
        ) : (
          <span className="text-zinc-400">-</span>
        )}
        {item.subcategoryCode ? (
          <div
            className="max-w-[22ch] truncate text-xs text-zinc-400"
            title={`${item.subcategoryCode} ${item.subcategoryName ?? ""}`}
          >
            {item.subcategoryName ?? item.subcategoryCode}
          </div>
        ) : null}
      </td>
      <td className="py-2 pr-3 text-zinc-500">{brandCode || <span className="text-zinc-400">-</span>}</td>
      <td className="py-2 pr-3 text-right font-mono tabular-nums">{item.defaultUnitCost.toFixed(2)}</td>
      <td className="py-2 pr-3 text-right font-mono tabular-nums">{item.defaultUnitPrice.toFixed(2)}</td>
      <td className="py-2 pr-3 text-xs text-zinc-500">
        {item.revenueAccountCode || item.expenseAccountCode ? (
          <>
            <div title={item.revenueAccountName ?? ""}>
              <span className="text-zinc-400">Inc</span> {item.revenueAccountCode ?? "-"}
            </div>
            <div title={item.expenseAccountName ?? ""}>
              <span className="text-zinc-400">Exp</span> {item.expenseAccountCode ?? "-"}
            </div>
          </>
        ) : (
          <span className="text-zinc-400">-</span>
        )}
      </td>
      <td className="py-2 pr-3">
        <span
          className={
            item.isActive
              ? "rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300"
              : "rounded-full bg-zinc-100 px-2 py-0.5 text-xs font-medium text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400"
          }
        >
          {item.isActive ? "Active" : "Inactive"}
        </span>
      </td>
      <td className="py-2 pr-3">
        {/* One line, no wrapping: the actions splitting over two rows is what made the grid look
            broken. Links folded in here too rather than owning a column for a single button. */}
        <div className="flex items-center gap-2 whitespace-nowrap">
          <Link href={`/master-data/items/${item.id}`} className={actionLinkClassName}>
            View
          </Link>
          <AppFormModal title={`Edit Item ${item.sku}`} description="Update item master data and account mapping." buttonLabel="Edit" variant="secondary" size="xl">
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
            className="px-2 py-1 text-xs"
          >
            PDF
          </SecondaryLink>
          <SecondaryButton type="button" className={actionButtonClass} onClick={() => void deleteItem()} disabled={busy}>
            {busy ? "Deleting..." : "Delete"}
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}

export function ItemListPanel({
  items,
  brands,
  uoms,
  categories,
  subcategories,
  accountOptions,
  highlightItemId,
}: {
  items: ItemDto[];
  brands: BrandDto[];
  uoms: UomDto[];
  categories: CategoryDto[];
  subcategories: SubcategoryDto[];
  accountOptions: LedgerAccountOptionDto[];
  highlightItemId?: string;
}) {
  const [query, setQuery] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [trackingFilter, setTrackingFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [brandFilter, setBrandFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [uomFilter, setUomFilter] = useState("");

  const brandById = useMemo(() => new Map(brands.map((brand) => [brand.id, brand])), [brands]);

  const uomOptions = useMemo(
    () =>
      Array.from(new Set(items.map((item) => item.unitOfMeasure).filter((value) => value?.length > 0)))
        .sort((a, b) => a.localeCompare(b)),
    [items],
  );

  const filteredItems = useMemo(() => {
    const q = query.trim().toLowerCase();
    return items.filter((item) => {
      if (typeFilter && String(item.type) !== typeFilter) return false;
      if (trackingFilter && String(item.trackingType) !== trackingFilter) return false;
      if (activeFilter) {
        const target = activeFilter === "active";
        if (item.isActive !== target) return false;
      }
      if (brandFilter && (item.brandId ?? "") !== brandFilter) return false;
      if (categoryFilter && (item.categoryId ?? "") !== categoryFilter) return false;
      if (uomFilter && item.unitOfMeasure !== uomFilter) return false;
      if (!q) return true;

      const brandCode = item.brandId ? brandById.get(item.brandId)?.code ?? "" : "";
      const haystack = [
        item.sku,
        item.name,
        item.barcode ?? "",
        item.unitOfMeasure,
        brandCode,
        item.categoryCode ?? "",
        item.categoryName ?? "",
        item.subcategoryCode ?? "",
        item.subcategoryName ?? "",
        item.revenueAccountCode ?? "",
        item.revenueAccountName ?? "",
        item.expenseAccountCode ?? "",
        item.expenseAccountName ?? "",
      ]
        .join(" ")
        .toLowerCase();

      return haystack.includes(q);
    });
  }, [
    activeFilter,
    brandById,
    brandFilter,
    categoryFilter,
    items,
    query,
    trackingFilter,
    typeFilter,
    uomFilter,
  ]);

  return (
    <div className="space-y-4">
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <div className="xl:col-span-2">
          <label className="mb-1 block text-sm font-medium">Search</label>
          <Input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="SKU, name, barcode, category..."
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Type</label>
          <Select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
            <option value="">All</option>
            <option value="1">Equipment</option>
            <option value="2">Spare Part</option>
            <option value="3">Service</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Tracking</label>
          <Select value={trackingFilter} onChange={(e) => setTrackingFilter(e.target.value)}>
            <option value="">All</option>
            <option value="0">None</option>
            <option value="1">Serial</option>
            <option value="2">Batch</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Active</label>
          <Select value={activeFilter} onChange={(e) => setActiveFilter(e.target.value)}>
            <option value="">All</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Brand</label>
          <Select value={brandFilter} onChange={(e) => setBrandFilter(e.target.value)}>
            <option value="">All</option>
            {brands
              .slice()
              .sort((a, b) => a.code.localeCompare(b.code))
              .map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.code}
                </option>
              ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Category</label>
          <Select value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)}>
            <option value="">All</option>
            {categories
              .slice()
              .sort((a, b) => a.code.localeCompare(b.code))
              .map((category) => (
                <option key={category.id} value={category.id}>
                  {category.code}
                </option>
              ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">UoM</label>
          <Select value={uomFilter} onChange={(e) => setUomFilter(e.target.value)}>
            <option value="">All</option>
            {uomOptions.map((uom) => (
              <option key={uom} value={uom}>
                {uom}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="text-xs text-zinc-500">
        Showing {filteredItems.length} of {items.length} items
      </div>

      <div className="overflow-auto">
        <Table>
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              {/* Sixteen columns forced every cell to wrap, which is what made this grid unreadable.
                  Related fields are stacked inside a cell instead: SKU/name/barcode, type/tracking/UoM,
                  category/subcategory, and the two account codes. */}
              <th className="py-2 pr-3">Item</th>
              <th className="py-2 pr-3">Type</th>
              <th className="py-2 pr-3">Category</th>
              <th className="py-2 pr-3">Brand</th>
              <th className="py-2 pr-3 text-right">Cost</th>
              <th className="py-2 pr-3 text-right">Price</th>
              <th className="py-2 pr-3">Accounts</th>
              <th className="py-2 pr-3">Status</th>
              <th className="py-2 pr-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredItems.map((item) => (
              <ItemListRow
                key={item.id}
                item={item}
                brandCode={item.brandId ? brandById.get(item.brandId)?.code ?? "" : ""}
                brands={brands}
                uoms={uoms}
                categories={categories}
                subcategories={subcategories}
                accountOptions={accountOptions}
                highlight={highlightItemId === item.id}
              />
            ))}
            {filteredItems.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={9}>
                  No items match the current filters.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </div>
    </div>
  );
}
