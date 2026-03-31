"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";
import { type LedgerAccountDto, ledgerAccountTypeLabel, ledgerAccountTypeOptions } from "./types";

const actionButtonClass = "px-2 py-1 text-xs";

export function LedgerAccountRow({
  account,
  accounts,
}: {
  account: LedgerAccountDto;
  accounts: LedgerAccountDto[];
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [code, setCode] = useState(account.code);
  const [name, setName] = useState(account.name);
  const [accountType, setAccountType] = useState<string>(String(account.accountType));
  const [parentAccountId, setParentAccountId] = useState(account.parentAccountId ?? "");
  const [allowsPosting, setAllowsPosting] = useState<string>(account.allowsPosting ? "true" : "false");
  const [description, setDescription] = useState(account.description ?? "");
  const [isActive, setIsActive] = useState<string>(account.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const parentOptions = accounts.filter((candidate) => candidate.id !== account.id);

  function beginEdit() {
    setError(null);
    setCode(account.code);
    setName(account.name);
    setAccountType(String(account.accountType));
    setParentAccountId(account.parentAccountId ?? "");
    setAllowsPosting(account.allowsPosting ? "true" : "false");
    setDescription(account.description ?? "");
    setIsActive(account.isActive ? "true" : "false");
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);

    try {
      await apiPut<LedgerAccountDto>(`finance/accounts/${account.id}`, {
        code,
        name,
        accountType: Number(accountType),
        parentAccountId: parentAccountId || null,
        allowsPosting: allowsPosting === "true",
        description: description.trim() || null,
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
    if (!window.confirm(`Delete account ${account.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`finance/accounts/${account.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? <Input value={code} onChange={(e) => setCode(e.target.value)} className="min-w-20" /> : account.code}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={name} onChange={(e) => setName(e.target.value)} className="min-w-36" /> : account.name}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={accountType} onChange={(e) => setAccountType(e.target.value)} className="min-w-28">
            {ledgerAccountTypeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        ) : (
          ledgerAccountTypeLabel(account.accountType)
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? (
          <Select value={parentAccountId} onChange={(e) => setParentAccountId(e.target.value)} className="min-w-48">
            <option value="">None</option>
            {parentOptions.map((candidate) => (
              <option key={candidate.id} value={candidate.id}>
                {candidate.code} - {candidate.name}
              </option>
            ))}
          </Select>
        ) : account.parentAccountCode ? (
          `${account.parentAccountCode} - ${account.parentAccountName}`
        ) : (
          "-"
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={allowsPosting} onChange={(e) => setAllowsPosting(e.target.value)} className="min-w-24">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : account.allowsPosting ? (
          "Yes"
        ) : (
          "No"
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? (
          <Input value={description} onChange={(e) => setDescription(e.target.value)} className="min-w-40" />
        ) : (
          account.description ?? "-"
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : account.isActive ? (
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
