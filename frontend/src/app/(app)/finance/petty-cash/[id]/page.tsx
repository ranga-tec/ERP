import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { TransactionLink } from "@/components/TransactionLink";
import { PettyCashFundEditForm } from "../PettyCashFundEditForm";
import { PettyCashFundTransactionForms } from "../PettyCashFundTransactionForms";

type CurrencyDto = { code: string; name: string; isBase: boolean; isActive: boolean };
type PettyCashFundDto = {
  id: string;
  code: string;
  name: string;
  currencyCode: string;
  custodianName?: string | null;
  notes?: string | null;
  isActive: boolean;
  balance: number;
  transactions: {
    id: string;
    occurredAt: string;
    type: number;
    direction: number;
    amount: number;
    signedAmount: number;
    referenceType?: string | null;
    referenceId?: string | null;
    referenceNumber?: string | null;
    notes?: string | null;
  }[];
};

const transactionTypeLabel: Record<number, string> = {
  1: "Opening Balance",
  2: "Top Up",
  3: "Expense Settlement",
  4: "Adjustment",
};

const directionLabel: Record<number, string> = {
  1: "In",
  2: "Out",
};

export default async function PettyCashFundDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [fund, currencies] = await Promise.all([
    backendFetchJson<PettyCashFundDto>(`/finance/petty-cash-funds/${id}`),
    backendFetchJson<CurrencyDto[]>("/currencies"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/finance/petty-cash" className="hover:underline">
            Petty Cash
          </Link>{" "}
          / <span className="font-mono text-xs">{fund.code}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">{fund.name}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Code: <span className="font-mono text-xs">{fund.code}</span></div>
          <div>Currency: {fund.currencyCode}</div>
          <div>Balance: {fund.balance.toFixed(2)}</div>
          <div>Custodian: {fund.custodianName ?? "-"}</div>
          <div>Status: {fund.isActive ? "Active" : "Inactive"}</div>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Current Balance</div>
          <div className="mt-2 text-2xl font-semibold">{fund.balance.toFixed(2)}</div>
          <div className="mt-1 text-xs text-zinc-500">{fund.currencyCode}</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Transactions</div>
          <div className="mt-2 text-2xl font-semibold">{fund.transactions.length}</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Latest Activity</div>
          <div className="mt-2 text-sm font-medium">
            {fund.transactions[0] ? new Date(fund.transactions[0].occurredAt).toLocaleString() : "No activity"}
          </div>
        </Card>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Fund Details</div>
        <PettyCashFundEditForm fund={fund} currencies={currencies} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Transactions</div>
        <PettyCashFundTransactionForms fundId={fund.id} />
      </Card>

      {fund.notes ? (
        <Card>
          <div className="mb-2 text-sm font-semibold">Notes</div>
          <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{fund.notes}</div>
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Ledger</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Type</th>
                <th className="py-2 pr-3">Direction</th>
                <th className="py-2 pr-3">Amount</th>
                <th className="py-2 pr-3">Reference</th>
                <th className="py-2 pr-3">Notes</th>
              </tr>
            </thead>
            <tbody>
              {fund.transactions.map((transaction) => (
                <tr key={transaction.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 text-zinc-500">{new Date(transaction.occurredAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{transactionTypeLabel[transaction.type] ?? transaction.type}</td>
                  <td className="py-2 pr-3">{directionLabel[transaction.direction] ?? transaction.direction}</td>
                  <td className="py-2 pr-3 font-medium">{transaction.signedAmount.toFixed(2)}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {transaction.referenceType && transaction.referenceId ? (
                      <TransactionLink referenceType={transaction.referenceType} referenceId={transaction.referenceId}>
                        {transaction.referenceNumber ?? transaction.referenceId}
                      </TransactionLink>
                    ) : (
                      transaction.referenceNumber ?? "-"
                    )}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{transaction.notes ?? "-"}</td>
                </tr>
              ))}
              {fund.transactions.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No ledger entries yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
