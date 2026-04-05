"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";
import { formatLedgerAccountOptionLabel, type CategoryDto, type LedgerAccountOptionDto } from "../items/item-definitions";

export function ItemCategoryCreateForm({ accountOptions }: { accountOptions: LedgerAccountOptionDto[] }) {
  const router = useRouter();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [revenueAccountId, setRevenueAccountId] = useState("");
  const [expenseAccountId, setExpenseAccountId] = useState("");
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

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPost<CategoryDto>("item-categories", {
        code,
        name,
        revenueAccountId: revenueAccountId || null,
        expenseAccountId: expenseAccountId || null,
      });
      setCode("");
      setName("");
      setRevenueAccountId("");
      setExpenseAccountId("");
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
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(e) => setCode(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Default Income / Revenue Account</label>
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
          <label className="mb-1 block text-sm font-medium">Default Expense Account</label>
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

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Category"}
      </Button>
    </form>
  );
}
