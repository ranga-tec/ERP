"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiDeleteNoContent } from "@/lib/api-client";
import { buildItemAnchorId } from "@/lib/item-routing";
import { Input, SecondaryButton, SecondaryLink, Select, Table } from "@/components/ui";
import { itemTypeLabel, trackingLabel, type BrandDto, type CategoryDto, type ItemDto } from "./item-definitions";

const actionLinkClassName = "text-xs font-semibold text-[var(--link)] underline underline-offset-2 transition-colors hover:text-[var(--link-hover)]";
const actionButtonClass = "px-2 py-1 text-xs";

function ItemListRow({
  item,
  brandCode,
  highlight,
}: {
  item: ItemDto;
  brandCode: string;
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
      <td className="py-2 pr-3 font-mono text-xs">{item.sku}</td>
      <td className="py-2 pr-3">{item.name}</td>
      <td className="py-2 pr-3">{itemTypeLabel[item.type] ?? item.type}</td>
      <td className="py-2 pr-3">{trackingLabel[item.trackingType] ?? item.trackingType}</td>
      <td className="py-2 pr-3">{item.unitOfMeasure}</td>
      <td className="py-2 pr-3 text-zinc-500">
        {item.categoryCode ? (
          <>
            <span className="font-mono text-xs">{item.categoryCode}</span> {item.categoryName ?? ""}
          </>
        ) : (
          "-"
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {item.subcategoryCode ? (
          <>
            <span className="font-mono text-xs">{item.subcategoryCode}</span> {item.subcategoryName ?? ""}
          </>
        ) : (
          "-"
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">{brandCode || "-"}</td>
      <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{item.barcode ?? "-"}</td>
      <td className="py-2 pr-3">{item.defaultUnitCost}</td>
      <td className="py-2 pr-3 text-zinc-500">
        {item.revenueAccountCode ? (
          <>
            <span className="font-mono text-xs">{item.revenueAccountCode}</span> {item.revenueAccountName ?? ""}
          </>
        ) : (
          "-"
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {item.expenseAccountCode ? (
          <>
            <span className="font-mono text-xs">{item.expenseAccountCode}</span> {item.expenseAccountName ?? ""}
          </>
        ) : (
          "-"
        )}
      </td>
      <td className="py-2 pr-3">{item.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <Link href={`/master-data/items/${item.id}`} className={actionLinkClassName}>
            View
          </Link>
          <Link href={`/master-data/items/${item.id}/edit`} className={actionLinkClassName}>
            Edit
          </Link>
          <SecondaryButton type="button" className={actionButtonClass} onClick={() => void deleteItem()} disabled={busy}>
            {busy ? "Deleting..." : "Delete"}
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
      <td className="py-2 pr-3">
        <SecondaryLink
          href={`/api/backend/items/${item.id}/label/pdf`}
          target="_blank"
          rel="noopener noreferrer"
          className="px-2 py-1 text-xs"
        >
          PDF
        </SecondaryLink>
      </td>
    </tr>
  );
}

export function ItemListPanel({
  items,
  brands,
  categories,
  highlightItemId,
}: {
  items: ItemDto[];
  brands: BrandDto[];
  categories: CategoryDto[];
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
              <th className="py-2 pr-3">SKU</th>
              <th className="py-2 pr-3">Name</th>
              <th className="py-2 pr-3">Type</th>
              <th className="py-2 pr-3">Tracking</th>
              <th className="py-2 pr-3">UoM</th>
              <th className="py-2 pr-3">Category</th>
              <th className="py-2 pr-3">Subcategory</th>
              <th className="py-2 pr-3">Brand</th>
              <th className="py-2 pr-3">Barcode</th>
              <th className="py-2 pr-3">Default Cost</th>
              <th className="py-2 pr-3">Income Acct</th>
              <th className="py-2 pr-3">Expense Acct</th>
              <th className="py-2 pr-3">Active</th>
              <th className="py-2 pr-3">Actions</th>
              <th className="py-2 pr-3">Links</th>
            </tr>
          </thead>
          <tbody>
            {filteredItems.map((item) => (
              <ItemListRow
                key={item.id}
                item={item}
                brandCode={item.brandId ? brandById.get(item.brandId)?.code ?? "" : ""}
                highlight={highlightItemId === item.id}
              />
            ))}
            {filteredItems.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={15}>
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
