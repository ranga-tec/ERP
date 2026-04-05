import { backendFetchJson } from "@/lib/backend.server";
import { LedgerAccountsWorkspace } from "./LedgerAccountsWorkspace";
import { type LedgerAccountDto } from "./types";

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

      <LedgerAccountsWorkspace accounts={accounts} />
    </div>
  );
}
