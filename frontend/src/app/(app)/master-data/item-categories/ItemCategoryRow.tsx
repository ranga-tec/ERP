"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";
import { formatLedgerAccountOptionLabel, type CategoryDto, type LedgerAccountOptionDto } from "../items/item-definitions";
type SubcategoryDto = { id: string; code: string; name: string };

const actionButtonClass = "px-2 py-1 text-xs";

export function ItemCategoryRow({
  category,
  accountOptions,
  subcategories,
}: {
  category: CategoryDto;
  accountOptions: LedgerAccountOptionDto[];
  subcategories: SubcategoryDto[];
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [code, setCode] = useState(category.code);
  const [name, setName] = useState(category.name);
  const [revenueAccountId, setRevenueAccountId] = useState(category.revenueAccountId ?? "");
  const [expenseAccountId, setExpenseAccountId] = useState(category.expenseAccountId ?? "");
  const [isActive, setIsActive] = useState(category.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const revenueAccountOptions = accountOptions
    .filter((account) => account.accountType === 4)
    .slice()
    .sort((a, b) => a.code.localeCompare(b.code));
  const expenseAccountOptions = accountOptions
    .filter((account) => account.accountType === 5)
    .slice()
    .sort((a, b) => a.code.localeCompare(b.code));

  function beginEdit() {
    setError(null);
    setCode(category.code);
    setName(category.name);
    setRevenueAccountId(category.revenueAccountId ?? "");
    setExpenseAccountId(category.expenseAccountId ?? "");
    setIsActive(category.isActive ? "true" : "false");
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`item-categories/${category.id}`, {
        code,
        name,
        revenueAccountId: revenueAccountId || null,
        expenseAccountId: expenseAccountId || null,
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
    if (!window.confirm(`Delete category ${category.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`item-categories/${category.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? <Input value={code} onChange={(e) => setCode(e.target.value)} className="min-w-24" /> : category.code}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={name} onChange={(e) => setName(e.target.value)} className="min-w-32" /> : category.name}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={revenueAccountId} onChange={(e) => setRevenueAccountId(e.target.value)} className="min-w-52">
            <option value="">(None)</option>
            {revenueAccountOptions.map((account) => (
              <option key={account.id} value={account.id}>
                {formatLedgerAccountOptionLabel(account)}
              </option>
            ))}
          </Select>
        ) : category.revenueAccountCode ? (
          <span className="text-sm">{`${category.revenueAccountCode} - ${category.revenueAccountName ?? ""}`}</span>
        ) : (
          <span className="text-zinc-500">-</span>
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={expenseAccountId} onChange={(e) => setExpenseAccountId(e.target.value)} className="min-w-52">
            <option value="">(None)</option>
            {expenseAccountOptions.map((account) => (
              <option key={account.id} value={account.id}>
                {formatLedgerAccountOptionLabel(account)}
              </option>
            ))}
          </Select>
        ) : category.expenseAccountCode ? (
          <span className="text-sm">{`${category.expenseAccountCode} - ${category.expenseAccountName ?? ""}`}</span>
        ) : (
          <span className="text-zinc-500">-</span>
        )}
      </td>
      <td className="py-2 pr-3">
        {subcategories.length > 0 ? (
          <div className="space-y-1">
            {subcategories.map((sub) => (
              <div key={sub.id} className="text-sm">
                <span className="font-mono text-xs">{sub.code}</span> <span className="text-zinc-500">{sub.name}</span>
              </div>
            ))}
          </div>
        ) : (
          <span className="text-zinc-500">None</span>
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : category.isActive ? (
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
