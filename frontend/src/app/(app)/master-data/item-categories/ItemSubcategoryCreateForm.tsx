"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type CategoryRef = { id: string; code: string; name: string };
type SubcategoryDto = {
  id: string;
  categoryId: string;
  categoryCode?: string | null;
  categoryName?: string | null;
  code: string;
  name: string;
  isActive: boolean;
};

export function ItemSubcategoryCreateForm({ categories }: { categories: CategoryRef[] }) {
  const router = useRouter();
  const [categoryId, setCategoryId] = useState("");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPost<SubcategoryDto>("item-subcategories", { categoryId, code, name });
      setCode("");
      setName("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Category</label>
          <Select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} required>
            <option value="">Select category</option>
            {categories
              .slice()
              .sort((a, b) => a.code.localeCompare(b.code))
              .map((c) => (
                <option key={c.id} value={c.id}>
                  {c.code} - {c.name}
                </option>
              ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(e) => setCode(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy || categories.length === 0}>
        {busy ? "Creating..." : "Create Subcategory"}
      </Button>
      {categories.length === 0 ? (
        <div className="text-xs text-zinc-500">Create a category first.</div>
      ) : null}
    </form>
  );
}
