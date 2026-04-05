"use client";

import { Card, Table } from "@/components/ui";
import { LedgerAccountCreateForm } from "./LedgerAccountCreateForm";
import { LedgerAccountRow } from "./LedgerAccountRow";
import { type LedgerAccountDto, ledgerAccountTypeLabel } from "./types";

export function LedgerAccountsClassicView({
  accounts,
  filteredAccounts,
}: {
  accounts: LedgerAccountDto[];
  filteredAccounts: LedgerAccountDto[];
}) {
  return (
    <div className="space-y-6">
      <Card>
        <div className="mb-3 flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
          <div>
            <div className="text-sm font-semibold text-[var(--foreground)]">Create Account</div>
            <p className="mt-1 max-w-2xl text-sm text-[var(--muted-foreground)]">
              Traditional ERP workflow: create accounts in a dedicated form, then click a row below or use Edit to update it.
            </p>
          </div>
          <div className="rounded-full border border-[var(--input-border)] bg-[var(--surface)] px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
            Classic workspace
          </div>
        </div>
        <LedgerAccountCreateForm accounts={accounts} />
      </Card>

      <Card>
        <div className="mb-3 flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
          <div>
            <div className="text-sm font-semibold text-[var(--foreground)]">Accounts</div>
            <p className="mt-1 text-sm text-[var(--muted-foreground)]">
              Active accounts are shown by default. Use the filters above to include inactive or all records.
            </p>
          </div>
          <div className="rounded-full border border-[var(--input-border)] bg-[var(--surface-soft)] px-3 py-1 text-xs font-medium text-[var(--muted-foreground)]">
            {filteredAccounts.length} visible
          </div>
        </div>

        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Type</th>
                <th className="py-2 pr-3">Parent</th>
                <th className="py-2 pr-3">Posting</th>
                <th className="py-2 pr-3">Description</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredAccounts.map((account) => (
                <LedgerAccountRow key={account.id} account={account} accounts={accounts} variant="classic" />
              ))}
              {filteredAccounts.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                    No accounts match the current search and filter combination.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>

        {accounts.length > 0 ? (
          <div className="mt-4 text-xs text-zinc-500">
            Posting accounts carry transactions. Group accounts organize the ledger structure.
            {" "}
            Current types: {Array.from(new Set(accounts.map((account) => ledgerAccountTypeLabel(account.accountType)))).join(", ")}.
          </div>
        ) : null}
      </Card>
    </div>
  );
}
