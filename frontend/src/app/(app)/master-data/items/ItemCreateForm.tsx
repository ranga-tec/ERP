"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type BrandRef = { id: string; code: string; name: string };
type UomRef = { id: string; code: string; name: string; isActive: boolean };
type CategoryRef = { id: string; code: string; name: string; isActive: boolean };
type SubcategoryRef = {
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

const itemTypes = [
  { value: 1, label: "Equipment" },
  { value: 2, label: "Spare Part" },
  { value: 3, label: "Service" },
];

const trackingTypes = [
  { value: 0, label: "None" },
  { value: 1, label: "Serial" },
  { value: 2, label: "Batch" },
];

export function ItemCreateForm({
  brands,
  uoms,
  categories,
  subcategories,
}: {
  brands: BrandRef[];
  uoms: UomRef[];
  categories: CategoryRef[];
  subcategories: SubcategoryRef[];
}) {
  const router = useRouter();

  const [sku, setSku] = useState("");
  const [name, setName] = useState("");
  const [type, setType] = useState<number>(2);
  const [trackingType, setTrackingType] = useState<number>(0);
  const [unitOfMeasure, setUnitOfMeasure] = useState(uoms.find((u) => u.isActive)?.code ?? "PCS");
  const [brandId, setBrandId] = useState<string>("");
  const [categoryId, setCategoryId] = useState<string>("");
  const [subcategoryId, setSubcategoryId] = useState<string>("");
  const [barcode, setBarcode] = useState("");
  const [defaultUnitCost, setDefaultUnitCost] = useState("0");

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const brandOptions = useMemo(
    () => brands.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [brands],
  );
  const uomOptions = useMemo(
    () => uoms.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [uoms],
  );
  const categoryOptions = useMemo(
    () => categories.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [categories],
  );
  const filteredSubcategoryOptions = useMemo(() => {
    const base = categoryId ? subcategories.filter((s) => s.categoryId === categoryId) : subcategories;
    return base
      .filter((s) => s.isActive)
      .slice()
      .sort((a, b) => a.code.localeCompare(b.code));
  }, [categoryId, subcategories]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const cost = Number(defaultUnitCost);
      if (Number.isNaN(cost) || cost < 0) {
        throw new Error("Default unit cost must be a non-negative number.");
      }

      await apiPost<ItemDto>("items", {
        sku,
        name,
        type,
        trackingType,
        unitOfMeasure,
        brandId: brandId || null,
        categoryId: categoryId || null,
        subcategoryId: subcategoryId || null,
        barcode: barcode || null,
        defaultUnitCost: cost,
      });

      setSku("");
      setName("");
      setSubcategoryId("");
      setBarcode("");
      setDefaultUnitCost("0");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">SKU</label>
          <Input value={sku} onChange={(e) => setSku(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Type</label>
          <Select value={String(type)} onChange={(e) => setType(Number(e.target.value))}>
            {itemTypes.map((t) => (
              <option key={t.value} value={t.value}>
                {t.label}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Tracking</label>
          <Select
            value={String(trackingType)}
            onChange={(e) => setTrackingType(Number(e.target.value))}
          >
            {trackingTypes.map((t) => (
              <option key={t.value} value={t.value}>
                {t.label}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">UoM</label>
          {uomOptions.length > 0 ? (
            <Select value={unitOfMeasure} onChange={(e) => setUnitOfMeasure(e.target.value)} required>
              {uomOptions.map((u) => (
                <option key={u.id} value={u.code}>
                  {u.code} - {u.name}
                </option>
              ))}
            </Select>
          ) : (
            <Input
              value={unitOfMeasure}
              onChange={(e) => setUnitOfMeasure(e.target.value)}
              required
            />
          )}
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Brand</label>
          <Select value={brandId} onChange={(e) => setBrandId(e.target.value)}>
            <option value="">(None)</option>
            {brandOptions.map((b) => (
              <option key={b.id} value={b.id}>
                {b.code} - {b.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Category</label>
          <Select
            value={categoryId}
            onChange={(e) => {
              const nextCategoryId = e.target.value;
              setCategoryId(nextCategoryId);
              if (!nextCategoryId) {
                setSubcategoryId("");
                return;
              }

              const selectedSub = subcategories.find((s) => s.id === subcategoryId);
              if (selectedSub && selectedSub.categoryId !== nextCategoryId) {
                setSubcategoryId("");
              }
            }}
          >
            <option value="">(None)</option>
            {categoryOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} - {c.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Subcategory</label>
          <Select
            value={subcategoryId}
            onChange={(e) => setSubcategoryId(e.target.value)}
            disabled={filteredSubcategoryOptions.length === 0}
          >
            <option value="">(None)</option>
            {filteredSubcategoryOptions.map((s) => (
              <option key={s.id} value={s.id}>
                {s.code} - {s.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Barcode</label>
          <Input value={barcode} onChange={(e) => setBarcode(e.target.value)} />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Default Unit Cost</label>
          <Input
            value={defaultUnitCost}
            onChange={(e) => setDefaultUnitCost(e.target.value)}
            inputMode="decimal"
          />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Item"}
      </Button>
    </form>
  );
}
