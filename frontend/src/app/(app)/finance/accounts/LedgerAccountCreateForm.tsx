"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";
import { type LedgerAccountDto, ledgerAccountTypeOptions } from "./types";

export function LedgerAccountCreateForm({ accounts }: { accounts: LedgerAccountDto[] }) {
  const router = useRouter();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [accountType, setAccountType] = useState<string>(ledgerAccountTypeOptions[0].value);
  const [parentAccountId, setParentAccountId] = useState("");
  const [allowsPosting, setAllowsPosting] = useState<string>("true");
  const [description, setDescription] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPost<LedgerAccountDto>("finance/accounts", {
        code,
        name,
        accountType: Number(accountType),
        parentAccountId: parentAccountId || null,
        allowsPosting: allowsPosting === "true",
        description: description.trim() || null,
      });

      setCode("");
      setName("");
      setAccountType(ledgerAccountTypeOptions[0].value);
      setParentAccountId("");
      setAllowsPosting("true");
      setDescription("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(e) => setCode(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Type</label>
          <Select value={accountType} onChange={(e) => setAccountType(e.target.value)}>
            {ledgerAccountTypeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-3 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Parent Account</label>
          <Select value={parentAccountId} onChange={(e) => setParentAccountId(e.target.value)}>
            <option value="">None</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.code} - {account.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Posting Account</label>
          <Select value={allowsPosting} onChange={(e) => setAllowsPosting(e.target.value)}>
            <option value="true">Yes</option>
            <option value="false">No, group only</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Description</label>
          <Input value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Account"}
      </Button>
    </form>
  );
}
