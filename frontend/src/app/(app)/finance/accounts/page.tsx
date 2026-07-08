import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { LedgerAccountCreateForm } from "./LedgerAccountCreateForm";
import { LedgerAccountsWorkspace } from "./LedgerAccountsWorkspace";
import { type LedgerAccountDto } from "./types";

export default async function FinanceAccountsPage() {
  const accounts = await backendFetchJson<LedgerAccountDto[]>("/finance/accounts");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Chart of Accounts</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Maintain finance account codes for assets, liabilities, equity, revenue, and expenses.
          </p>
        </div>
        <AppFormModal title="Create Account" description="Add a new ledger account to the chart of accounts." buttonLabel="+ New Account" size="xl">
          <LedgerAccountCreateForm accounts={accounts} />
        </AppFormModal>
      </div>

      <LedgerAccountsWorkspace accounts={accounts} />
    </div>
  );
}
