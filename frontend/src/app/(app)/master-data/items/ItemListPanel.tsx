"use client";

import { useMemo, useState } from "react";
import { Input, SecondaryLink, Select, Table } from "@/components/ui";

type BrandRef = { id: string; code: string; name: string };
type CategoryRef = { id: string; code: string; name: string };
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

const itemTypeLabel: Record<number, string> = {
  1: "Equipment",
  2: "Spare Part",
  3: "Service",
};

const trackingLabel: Record<number, string> = {
  0: "None",
  1: "Serial",
  2: "Batch",
};

export function ItemListPanel({
  items,
  brands,
  categories,
}: {
  items: ItemDto[];
  brands: BrandRef[];
  categories: CategoryRef[];
}) {
  const [query, setQuery] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [trackingFilter, setTrackingFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [brandFilter, setBrandFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [uomFilter, setUomFilter] = useState("");

  const brandById = useMemo(() => new Map(brands.map((b) => [b.id, b])), [brands]);

  const uomOptions = useMemo(
    () =>
      Array.from(new Set(items.map((i) => i.unitOfMeasure).filter((v) => v?.length > 0)))
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
              .map((b) => (
                <option key={b.id} value={b.id}>
                  {b.code}
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
              .map((c) => (
                <option key={c.id} value={c.id}>
                  {c.code}
                </option>
              ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">UoM</label>
          <Select value={uomFilter} onChange={(e) => setUomFilter(e.target.value)}>
            <option value="">All</option>
            {uomOptions.map((u) => (
              <option key={u} value={u}>
                {u}
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
              <th className="py-2 pr-3">Active</th>
              <th className="py-2 pr-3">Label</th>
            </tr>
          </thead>
          <tbody>
            {filteredItems.map((i) => (
              <tr key={i.id} className="border-b border-zinc-100 dark:border-zinc-900">
                <td className="py-2 pr-3 font-mono text-xs">{i.sku}</td>
                <td className="py-2 pr-3">{i.name}</td>
                <td className="py-2 pr-3">{itemTypeLabel[i.type] ?? i.type}</td>
                <td className="py-2 pr-3">{trackingLabel[i.trackingType] ?? i.trackingType}</td>
                <td className="py-2 pr-3">{i.unitOfMeasure}</td>
                <td className="py-2 pr-3 text-zinc-500">
                  {i.categoryCode ? (
                    <>
                      <span className="font-mono text-xs">{i.categoryCode}</span> {i.categoryName ?? ""}
                    </>
                  ) : (
                    "-"
                  )}
                </td>
                <td className="py-2 pr-3 text-zinc-500">
                  {i.subcategoryCode ? (
                    <>
                      <span className="font-mono text-xs">{i.subcategoryCode}</span> {i.subcategoryName ?? ""}
                    </>
                  ) : (
                    "-"
                  )}
                </td>
                <td className="py-2 pr-3 text-zinc-500">
                  {i.brandId ? brandById.get(i.brandId)?.code ?? "-" : "-"}
                </td>
                <td className="py-2 pr-3 font-mono text-xs text-zinc-500">{i.barcode ?? "-"}</td>
                <td className="py-2 pr-3">{i.defaultUnitCost}</td>
                <td className="py-2 pr-3">{i.isActive ? "Yes" : "No"}</td>
                <td className="py-2 pr-3">
                  <SecondaryLink
                    href={`/api/backend/items/${i.id}/label/pdf`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="px-2 py-1 text-xs"
                  >
                    PDF
                  </SecondaryLink>
                </td>
              </tr>
            ))}
            {filteredItems.length === 0 ? (
              <tr>
                <td className="py-6 text-sm text-zinc-500" colSpan={12}>
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
