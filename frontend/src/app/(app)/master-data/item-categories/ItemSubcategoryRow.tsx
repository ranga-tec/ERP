"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

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

const actionButtonClass = "px-2 py-1 text-xs";

export function ItemSubcategoryRow({
  subcategory,
  categories,
}: {
  subcategory: SubcategoryDto;
  categories: CategoryDto[];
}) {
  const router = useRouter();
  const categoryOptions = useMemo(
    () => categories.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [categories],
  );

  const [categoryId, setCategoryId] = useState(subcategory.categoryId);
  const [code, setCode] = useState(subcategory.code);
  const [name, setName] = useState(subcategory.name);
  const [isActive, setIsActive] = useState(subcategory.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCategoryId(subcategory.categoryId);
    setCode(subcategory.code);
    setName(subcategory.name);
    setIsActive(subcategory.isActive ? "true" : "false");
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`item-subcategories/${subcategory.id}`, {
        categoryId,
        code,
        name,
        isActive: isActive === "true",
      });
      close();
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function deleteRow() {
    if (!window.confirm(`Delete subcategory ${subcategory.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`item-subcategories/${subcategory.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  const selectedCategoryCode = categoryOptions.find((c) => c.id === subcategory.categoryId)?.code ?? subcategory.categoryCode ?? "-";

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">{selectedCategoryCode}</td>
      <td className="py-2 pr-3 font-mono text-xs">{subcategory.code}</td>
      <td className="py-2 pr-3">{subcategory.name}</td>
      <td className="py-2 pr-3">{subcategory.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit Subcategory ${subcategory.code}`} description="Update subcategory details." buttonLabel="Edit" variant="secondary" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">Category</label><Select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>{categoryOptions.map((category) => <option key={category.id} value={category.id}>{category.code} - {category.name}</option>)}</Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Code</label><Input value={code} onChange={(e) => setCode(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Name</label><Input value={name} onChange={(e) => setName(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Active</label><Select value={isActive} onChange={(e) => setIsActive(e.target.value)}><option value="true">Yes</option><option value="false">No</option></Select></div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>{busy ? "Saving..." : "Save Subcategory"}</Button>
              </form>
            )}
          </AppFormModal>
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
