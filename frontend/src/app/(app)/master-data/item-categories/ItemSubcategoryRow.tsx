"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
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

  const [isEditing, setIsEditing] = useState(false);
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
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`item-subcategories/${subcategory.id}`, {
        categoryId,
        code,
        name,
        isActive: isActive === "true",
      });
      setIsEditing(false);
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
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? (
          <Select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className="min-w-36">
            {categoryOptions.map((category) => (
              <option key={category.id} value={category.id}>
                {category.code} - {category.name}
              </option>
            ))}
          </Select>
        ) : (
          selectedCategoryCode
        )}
      </td>
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? <Input value={code} onChange={(e) => setCode(e.target.value)} className="min-w-24" /> : subcategory.code}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={name} onChange={(e) => setName(e.target.value)} className="min-w-32" /> : subcategory.name}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : subcategory.isActive ? (
          "Yes"
        ) : (
          "No"
        )}
      </td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          {isEditing ? (
            <>
              <Button type="button" className={actionButtonClass} onClick={saveEdit} disabled={busy}>
                {busy ? "Saving..." : "Save"}
              </Button>
              <SecondaryButton
                type="button"
                className={actionButtonClass}
                onClick={() => {
                  setError(null);
                  setIsEditing(false);
                }}
                disabled={busy}
              >
                Cancel
              </SecondaryButton>
            </>
          ) : (
            <SecondaryButton type="button" className={actionButtonClass} onClick={beginEdit} disabled={busy}>
              Edit
            </SecondaryButton>
          )}
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
