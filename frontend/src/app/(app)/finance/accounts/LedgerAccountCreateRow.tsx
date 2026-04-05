"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";
import { type LedgerAccountDto, ledgerAccountTypeOptions } from "./types";

const gridInputClass =
  "min-w-[8rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]";
const gridSelectClass =
  "min-w-[8rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]";

export function LedgerAccountCreateRow({
  accounts,
  onCancel,
}: {
  accounts: LedgerAccountDto[];
  onCancel: () => void;
}) {
  const router = useRouter();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [accountType, setAccountType] = useState<string>(ledgerAccountTypeOptions[0].value);
  const [parentAccountId, setParentAccountId] = useState("");
  const [allowsPosting, setAllowsPosting] = useState<string>("true");
  const [description, setDescription] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function createRow() {
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
      onCancel();
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="grid-row-creating align-top">
      <td className="py-2 pr-3 font-mono text-xs">
        <Input value={code} onChange={(e) => setCode(e.target.value)} className={gridInputClass} required />
      </td>
      <td className="py-2 pr-3">
        <Input value={name} onChange={(e) => setName(e.target.value)} className={gridInputClass} required />
      </td>
      <td className="py-2 pr-3">
        <Select value={accountType} onChange={(e) => setAccountType(e.target.value)} className={gridSelectClass}>
          {ledgerAccountTypeOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      </td>
      <td className="py-2 pr-3 text-[var(--muted-foreground)]">
        <Select value={parentAccountId} onChange={(e) => setParentAccountId(e.target.value)} className="min-w-[12rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]">
          <option value="">None</option>
          {accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.code} - {account.name}
            </option>
          ))}
        </Select>
      </td>
      <td className="py-2 pr-3">
        <Select value={allowsPosting} onChange={(e) => setAllowsPosting(e.target.value)} className="min-w-[7rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]">
          <option value="true">Yes</option>
          <option value="false">No</option>
        </Select>
      </td>
      <td className="py-2 pr-3">
        <Input value={description} onChange={(e) => setDescription(e.target.value)} className="min-w-[12rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]" />
      </td>
      <td className="py-2 pr-3 text-xs text-[var(--muted-foreground)]">New</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" className="px-2.5 py-1.5 text-xs" onClick={createRow} disabled={busy}>
            {busy ? "Adding..." : "Add"}
          </Button>
          <SecondaryButton type="button" className="px-2.5 py-1.5 text-xs" onClick={onCancel} disabled={busy}>
            Cancel
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
