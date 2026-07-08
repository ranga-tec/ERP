"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { AppFormModal } from "@/components/AppFormModal";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";
import { type LedgerAccountDto, ledgerAccountTypeOptions } from "./types";

export function LedgerAccountEditModal({ account, accounts }: { account: LedgerAccountDto; accounts: LedgerAccountDto[] }) {
  const router = useRouter();
  const [code, setCode] = useState(account.code);
  const [name, setName] = useState(account.name);
  const [accountType, setAccountType] = useState(String(account.accountType));
  const [parentAccountId, setParentAccountId] = useState(account.parentAccountId ?? "");
  const [allowsPosting, setAllowsPosting] = useState(account.allowsPosting ? "true" : "false");
  const [description, setDescription] = useState(account.description ?? "");
  const [isActive, setIsActive] = useState(account.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function resetDraft() {
    setError(null);
    setCode(account.code);
    setName(account.name);
    setAccountType(String(account.accountType));
    setParentAccountId(account.parentAccountId ?? "");
    setAllowsPosting(account.allowsPosting ? "true" : "false");
    setDescription(account.description ?? "");
    setIsActive(account.isActive ? "true" : "false");
  }

  async function save(close: () => void) {
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
      close();
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <AppFormModal
      title={`Edit Account ${account.code}`}
      description="Update chart of accounts details, posting behavior, parent, and active state."
      buttonLabel="Edit"
      variant="secondary"
      onOpen={resetDraft}
    >
      {({ close }) => (
        <div className="space-y-4">
          <div className="grid gap-3 md:grid-cols-2">
            <label className="space-y-1 text-sm font-medium">
              <span>Code</span>
              <Input value={code} onChange={(event) => setCode(event.target.value)} />
            </label>
            <label className="space-y-1 text-sm font-medium">
              <span>Name</span>
              <Input value={name} onChange={(event) => setName(event.target.value)} />
            </label>
            <label className="space-y-1 text-sm font-medium">
              <span>Type</span>
              <Select value={accountType} onChange={(event) => setAccountType(event.target.value)}>
                {ledgerAccountTypeOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>
            </label>
            <label className="space-y-1 text-sm font-medium">
              <span>Parent</span>
              <Select value={parentAccountId} onChange={(event) => setParentAccountId(event.target.value)}>
                <option value="">None</option>
                {accounts
                  .filter((candidate) => candidate.id !== account.id)
                  .map((candidate) => (
                    <option key={candidate.id} value={candidate.id}>
                      {candidate.code} - {candidate.name}
                    </option>
                  ))}
              </Select>
            </label>
            <label className="space-y-1 text-sm font-medium">
              <span>Posting</span>
              <Select value={allowsPosting} onChange={(event) => setAllowsPosting(event.target.value)}>
                <option value="true">Yes</option>
                <option value="false">No</option>
              </Select>
            </label>
            <label className="space-y-1 text-sm font-medium">
              <span>Status</span>
              <Select value={isActive} onChange={(event) => setIsActive(event.target.value)}>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </Select>
            </label>
            <label className="space-y-1 text-sm font-medium md:col-span-2">
              <span>Description</span>
              <Input value={description} onChange={(event) => setDescription(event.target.value)} />
            </label>
          </div>
          {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
          <div className="flex justify-end">
            <Button type="button" onClick={() => void save(close)} disabled={busy}>
              {busy ? "Saving..." : "Save Changes"}
            </Button>
          </div>
        </div>
      )}
    </AppFormModal>
  );
}
