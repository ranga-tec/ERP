"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";
import {
  formatLedgerAccountOptionLabel,
  itemTypes,
  trackingTypes,
  type BrandDto,
  type CategoryDto,
  type ItemDto,
  type LedgerAccountOptionDto,
  type SubcategoryDto,
  type UomDto,
} from "./item-definitions";

export function ItemEditPanel({
  item,
  brands,
  uoms,
  categories,
  subcategories,
  accountOptions,
}: {
  item: ItemDto;
  brands: BrandDto[];
  uoms: UomDto[];
  categories: CategoryDto[];
  subcategories: SubcategoryDto[];
  accountOptions: LedgerAccountOptionDto[];
}) {
  const router = useRouter();
  const [sku, setSku] = useState(item.sku);
  const [name, setName] = useState(item.name);
  const [type, setType] = useState<number>(item.type);
  const [trackingType, setTrackingType] = useState<number>(item.trackingType);
  const [unitOfMeasure, setUnitOfMeasure] = useState(item.unitOfMeasure);
  const [brandId, setBrandId] = useState(item.brandId ?? "");
  const [categoryId, setCategoryId] = useState(item.categoryId ?? "");
  const [subcategoryId, setSubcategoryId] = useState(item.subcategoryId ?? "");
  const [barcode, setBarcode] = useState(item.barcode ?? "");
  const [defaultUnitCost, setDefaultUnitCost] = useState(String(item.defaultUnitCost));
  const [revenueAccountId, setRevenueAccountId] = useState(item.revenueAccountId ?? "");
  const [expenseAccountId, setExpenseAccountId] = useState(item.expenseAccountId ?? "");
  const [isActive, setIsActive] = useState(item.isActive);
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
  const subcategoryOptions = useMemo(() => {
    const filtered = categoryId ? subcategories.filter((s) => s.categoryId === categoryId) : subcategories;
    return filtered
      .filter((s) => s.isActive)
      .slice()
      .sort((a, b) => a.code.localeCompare(b.code));
  }, [categoryId, subcategories]);
  const revenueAccountOptions = useMemo(
    () =>
      accountOptions
        .filter((account) => account.accountType === 4)
        .slice()
        .sort((a, b) => a.code.localeCompare(b.code)),
    [accountOptions],
  );
  const expenseAccountOptions = useMemo(
    () =>
      accountOptions
        .filter((account) => account.accountType === 5)
        .slice()
        .sort((a, b) => a.code.localeCompare(b.code)),
    [accountOptions],
  );

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();

    setError(null);
    setBusy(true);
    try {
      const cost = Number(defaultUnitCost);
      if (Number.isNaN(cost) || cost < 0) {
        throw new Error("Default unit cost must be a non-negative number.");
      }

      await apiPut<ItemDto>(`items/${item.id}`, {
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
        revenueAccountId: revenueAccountId || null,
        expenseAccountId: expenseAccountId || null,
        isActive,
      });

      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function deleteItem() {
    if (!window.confirm("Delete this item?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`items/${item.id}`);
      router.push("/master-data/items");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
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
          <Select value={String(trackingType)} onChange={(e) => setTrackingType(Number(e.target.value))}>
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
            disabled={subcategoryOptions.length === 0}
          >
            <option value="">(None)</option>
            {subcategoryOptions.map((s) => (
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
        <div>
          <label className="mb-1 block text-sm font-medium">Active</label>
          <Select value={isActive ? "true" : "false"} onChange={(e) => setIsActive(e.target.value === "true")}>
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        </div>
      </div>

      <div className="space-y-2 rounded-lg border border-[var(--card-border)] bg-[var(--surface-soft)] p-3">
        <div>
          <div className="text-sm font-semibold">Finance Account Mapping</div>
          <div className="mt-1 text-xs text-[var(--muted-foreground)]">
            Maintain the default income and expense accounts attached to this item master.
          </div>
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium">Income / Revenue Account</label>
            <Select value={revenueAccountId} onChange={(e) => setRevenueAccountId(e.target.value)}>
              <option value="">(None)</option>
              {revenueAccountOptions.map((account) => (
                <option key={account.id} value={account.id}>
                  {formatLedgerAccountOptionLabel(account)}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Expense Account</label>
            <Select value={expenseAccountId} onChange={(e) => setExpenseAccountId(e.target.value)}>
              <option value="">(None)</option>
              {expenseAccountOptions.map((account) => (
                <option key={account.id} value={account.id}>
                  {formatLedgerAccountOptionLabel(account)}
                </option>
              ))}
            </Select>
          </div>
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <div className="flex flex-wrap items-center gap-2">
        <Button type="submit" disabled={busy}>
          {busy ? "Saving..." : "Save Item Changes"}
        </Button>
        <SecondaryButton type="button" disabled={busy} onClick={deleteItem}>
          Delete Item
        </SecondaryButton>
      </div>
    </form>
  );
}
