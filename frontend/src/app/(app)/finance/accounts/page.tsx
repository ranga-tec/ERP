import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { LedgerAccountCreateForm } from "./LedgerAccountCreateForm";
import { LedgerAccountRow } from "./LedgerAccountRow";
import { type LedgerAccountDto, ledgerAccountTypeLabel } from "./types";

export default async function FinanceAccountsPage() {
  const accounts = await backendFetchJson<LedgerAccountDto[]>("/finance/accounts");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Chart of Accounts</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Maintain finance account codes for assets, liabilities, equity, revenue, and expenses.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create Account</div>
        <LedgerAccountCreateForm accounts={accounts} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Accounts</div>
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
                <th className="py-2 pr-3">Active</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {accounts.map((account) => (
                <LedgerAccountRow key={account.id} account={account} accounts={accounts} />
              ))}
              {accounts.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                    No accounts yet. Start by creating your chart of accounts.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>

        {accounts.length > 0 ? (
          <div className="mt-4 text-xs text-zinc-500">
            Posting accounts carry transactions. Group accounts help organize the chart.
            {" "}
            Current types: {Array.from(new Set(accounts.map((account) => ledgerAccountTypeLabel(account.accountType)))).join(", ")}.
          </div>
        ) : null}
      </Card>
    </div>
  );
}
