import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { ItemCategoryCreateForm } from "./ItemCategoryCreateForm";
import { ItemSubcategoryCreateForm } from "./ItemSubcategoryCreateForm";
import { ItemCategoryRow } from "./ItemCategoryRow";
import { ItemSubcategoryRow } from "./ItemSubcategoryRow";
import type { CategoryDto, LedgerAccountOptionDto } from "../items/item-definitions";

type SubcategoryDto = {
  id: string;
  categoryId: string;
  categoryCode?: string | null;
  categoryName?: string | null;
  code: string;
  name: string;
  isActive: boolean;
};

export default async function ItemCategoriesPage() {
  const [categories, subcategories, accountOptions] = await Promise.all([
    backendFetchJson<CategoryDto[]>("/item-categories"),
    backendFetchJson<SubcategoryDto[]>("/item-subcategories"),
    backendFetchJson<LedgerAccountOptionDto[]>("/items/account-options"),
  ]);

  const subcategoriesByCategory = new Map<string, SubcategoryDto[]>();
  for (const sub of subcategories) {
    const list = subcategoriesByCategory.get(sub.categoryId) ?? [];
    list.push(sub);
    subcategoriesByCategory.set(sub.categoryId, list);
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Item Categories</h1>
        <p className="mt-1 text-sm text-zinc-500">Category and subcategory masters used to classify items.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create Category</div>
        <ItemCategoryCreateForm accountOptions={accountOptions} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create Subcategory</div>
        <ItemSubcategoryCreateForm
          categories={categories.map((category) => ({ id: category.id, code: category.code, name: category.name }))}
        />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Categories</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Category</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Income Acct</th>
                <th className="py-2 pr-3">Expense Acct</th>
                <th className="py-2 pr-3">Subcategories</th>
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {categories.map((category) => {
                const subs = (subcategoriesByCategory.get(category.id) ?? [])
                  .slice()
                  .sort((a, b) => a.code.localeCompare(b.code));

                return (
                  <ItemCategoryRow
                    key={category.id}
                    category={category}
                    accountOptions={accountOptions}
                    subcategories={subs.map((sub) => ({ id: sub.id, code: sub.code, name: sub.name }))}
                  />
                );
              })}
              {categories.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No categories yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Subcategory List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Category</th>
                <th className="py-2 pr-3">Subcategory</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {subcategories
                .slice()
                .sort((a, b) =>
                  (a.categoryCode ?? "").localeCompare(b.categoryCode ?? "") ||
                  a.code.localeCompare(b.code),
                )
                .map((sub) => (
                  <ItemSubcategoryRow key={sub.id} subcategory={sub} categories={categories} />
                ))}
              {subcategories.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No subcategories yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
