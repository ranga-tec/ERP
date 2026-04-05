"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";
import { type LedgerAccountDto, ledgerAccountTypeLabel, ledgerAccountTypeOptions } from "./types";

const actionButtonClass = "px-2.5 py-1.5 text-xs";
const gridInputClass =
  "min-w-[8rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]";
const gridSelectClass =
  "min-w-[8rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]";

export function LedgerAccountRow({
  account,
  accounts,
  variant = "priority",
}: {
  account: LedgerAccountDto;
  accounts: LedgerAccountDto[];
  variant?: "classic" | "priority";
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
  const isPriorityVariant = variant === "priority";
  const baseInputClass = isPriorityVariant
    ? "rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]"
    : "";

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
    <tr
      className={[
        isPriorityVariant
          ? "align-top transition-colors"
          : "cursor-pointer border-b border-zinc-100 align-top transition-colors dark:border-zinc-900",
        isEditing ? "grid-row-editing" : "",
      ].join(" ")}
      onClick={!isPriorityVariant && !isEditing ? beginEdit : undefined}
      onDoubleClick={isPriorityVariant && !isEditing ? beginEdit : undefined}
      title={!isEditing && !isPriorityVariant ? "Click row to edit" : undefined}
    >
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? (
          <Input
            value={code}
            onChange={(e) => setCode(e.target.value)}
            className={isPriorityVariant ? gridInputClass : `min-w-20 ${baseInputClass}`}
          />
        ) : (
          account.code
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input
            value={name}
            onChange={(e) => setName(e.target.value)}
            className={isPriorityVariant ? "min-w-[11rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]" : `min-w-36 ${baseInputClass}`}
          />
        ) : (
          <div className="font-medium text-[var(--foreground)]">{account.name}</div>
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select
            value={accountType}
            onChange={(e) => setAccountType(e.target.value)}
            className={isPriorityVariant ? gridSelectClass : `min-w-28 ${baseInputClass}`}
          >
            {ledgerAccountTypeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        ) : (
          <span className="inline-flex rounded-full border border-[var(--table-grid)] bg-[var(--surface)] px-2.5 py-1 text-[11px] font-semibold text-[var(--foreground)]">
            {ledgerAccountTypeLabel(account.accountType)}
          </span>
        )}
      </td>
      <td className="py-2 pr-3 text-[var(--muted-foreground)]">
        {isEditing ? (
          <Select
            value={parentAccountId}
            onChange={(e) => setParentAccountId(e.target.value)}
            className={isPriorityVariant ? "min-w-[12rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]" : `min-w-48 ${baseInputClass}`}
          >
            <option value="">None</option>
            {parentOptions.map((candidate) => (
              <option key={candidate.id} value={candidate.id}>
                {candidate.code} - {candidate.name}
              </option>
            ))}
          </Select>
        ) : account.parentAccountCode ? (
          <div className="max-w-[18rem] truncate" title={`${account.parentAccountCode} - ${account.parentAccountName}`}>
            {account.parentAccountCode} - {account.parentAccountName}
          </div>
        ) : (
          "-"
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select
            value={allowsPosting}
            onChange={(e) => setAllowsPosting(e.target.value)}
            className={isPriorityVariant ? "min-w-[7rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]" : `min-w-24 ${baseInputClass}`}
          >
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : account.allowsPosting ? (
          <span className="inline-flex rounded-full border border-emerald-300/70 bg-emerald-50 px-2.5 py-1 text-[11px] font-semibold text-emerald-700 dark:border-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-200">
            Posting
          </span>
        ) : (
          <span className="inline-flex rounded-full border border-slate-300/70 bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
            Group
          </span>
        )}
      </td>
      <td className="py-2 pr-3 text-[var(--muted-foreground)]">
        {isEditing ? (
          <Input
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className={isPriorityVariant ? "min-w-[12rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]" : `min-w-40 ${baseInputClass}`}
          />
        ) : (
          <div className="max-w-[18rem] truncate" title={account.description ?? undefined}>
            {account.description ?? "-"}
          </div>
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select
            value={isActive}
            onChange={(e) => setIsActive(e.target.value)}
            className={isPriorityVariant ? "min-w-[7rem] rounded-lg border-[var(--table-grid-strong)] bg-white/90 px-2.5 py-1.5 text-xs shadow-none dark:bg-[var(--surface)]" : `min-w-20 ${baseInputClass}`}
          >
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : account.isActive ? (
          <span className="inline-flex rounded-full border border-sky-300/70 bg-sky-50 px-2.5 py-1 text-[11px] font-semibold text-sky-700 dark:border-sky-700 dark:bg-sky-950/40 dark:text-sky-200">
            Active
          </span>
        ) : (
          <span className="inline-flex rounded-full border border-amber-300/70 bg-amber-50 px-2.5 py-1 text-[11px] font-semibold text-amber-700 dark:border-amber-700 dark:bg-amber-950/40 dark:text-amber-200">
            Inactive
          </span>
        )}
      </td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          {isEditing ? (
            <>
              <Button
                type="button"
                className={actionButtonClass}
                onClick={(event) => {
                  event.stopPropagation();
                  void saveEdit();
                }}
                disabled={busy}
              >
                {busy ? "Saving..." : "Save"}
              </Button>
              <SecondaryButton
                type="button"
                className={actionButtonClass}
                onClick={(event) => {
                  event.stopPropagation();
                  setError(null);
                  setIsEditing(false);
                }}
                disabled={busy}
              >
                Cancel
              </SecondaryButton>
            </>
          ) : (
            <SecondaryButton
              type="button"
              className={actionButtonClass}
              onClick={(event) => {
                event.stopPropagation();
                beginEdit();
              }}
              disabled={busy}
            >
              Edit
            </SecondaryButton>
          )}
          <SecondaryButton
            type="button"
            className={actionButtonClass}
            onClick={(event) => {
              event.stopPropagation();
              void deleteRow();
            }}
            disabled={busy}
          >
            Delete
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
