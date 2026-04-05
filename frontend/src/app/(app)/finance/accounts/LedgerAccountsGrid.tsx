"use client";

import { useState } from "react";
import { Button, Card, Table } from "@/components/ui";
import { LedgerAccountCreateRow } from "./LedgerAccountCreateRow";
import { LedgerAccountRow } from "./LedgerAccountRow";
import { type LedgerAccountDto } from "./types";

export function LedgerAccountsGrid({
  accounts,
  filteredAccounts,
}: {
  accounts: LedgerAccountDto[];
  filteredAccounts: LedgerAccountDto[];
}) {
  const [showCreateRow, setShowCreateRow] = useState(false);

  return (
    <Card className="overflow-hidden p-0">
      <div className="border-b border-[var(--card-border)] bg-[linear-gradient(180deg,rgba(255,255,255,0.62),rgba(255,255,255,0.24))] p-4 dark:bg-[linear-gradient(180deg,rgba(148,163,184,0.06),rgba(148,163,184,0.02))]">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <div className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
              Priority grid
            </div>
            <h2 className="mt-2 text-xl font-semibold text-[var(--foreground)]">Chart of Accounts</h2>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-[var(--muted-foreground)]">
              Dense row editing stays inside the table. Active accounts are shown first by default, while the workspace filters above let users include inactive or all records whenever needed.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <Button type="button" onClick={() => setShowCreateRow((value) => !value)}>
              {showCreateRow ? "Hide New Row" : "New Account"}
            </Button>
            <div className="rounded-full border border-[var(--input-border)] bg-[var(--surface)] px-3 py-1 text-xs font-medium text-[var(--muted-foreground)]">
              {filteredAccounts.length} visible
            </div>
          </div>
        </div>
      </div>

      <div className="erp-grid-shell max-h-[70vh] overflow-auto">
        <Table className="erp-grid erp-grid-compact">
          <thead>
            <tr className="text-left text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
              <th className="py-3 pr-3">Code</th>
              <th className="py-3 pr-3">Name</th>
              <th className="py-3 pr-3">Type</th>
              <th className="py-3 pr-3">Parent</th>
              <th className="py-3 pr-3">Posting</th>
              <th className="py-3 pr-3">Description</th>
              <th className="py-3 pr-3">Status</th>
              <th className="py-3 pr-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {showCreateRow ? <LedgerAccountCreateRow accounts={accounts} onCancel={() => setShowCreateRow(false)} /> : null}

            {filteredAccounts.map((account) => (
              <LedgerAccountRow key={account.id} account={account} accounts={accounts} variant="priority" />
            ))}

            {filteredAccounts.length === 0 ? (
              <tr>
                <td className="py-8 text-sm text-[var(--muted-foreground)]" colSpan={8}>
                  No accounts match the current search and filter combination.
                </td>
              </tr>
            ) : null}
          </tbody>
        </Table>
      </div>

      <div className="border-t border-[var(--card-border)] bg-[var(--surface-soft)] px-4 py-3 text-xs text-[var(--muted-foreground)]">
        Double-click a row or use <span className="font-semibold text-[var(--foreground)]">Edit</span> to change it in place. Posting accounts carry transactions; group accounts organize the chart.
      </div>
    </Card>
  );
}
