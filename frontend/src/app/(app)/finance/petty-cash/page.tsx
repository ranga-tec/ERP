import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { PettyCashFundCreateForm } from "./PettyCashFundCreateForm";

type CurrencyDto = { code: string; name: string; isBase: boolean; isActive: boolean };
type PettyCashFundSummaryDto = {
  id: string;
  code: string;
  name: string;
  currencyCode: string;
  custodianName?: string | null;
  isActive: boolean;
  balance: number;
  transactionCount: number;
  lastActivityAt?: string | null;
};

export default async function PettyCashFundsPage() {
  const [currencies, funds] = await Promise.all([
    backendFetchJson<CurrencyDto[]>("/currencies"),
    backendFetchJson<PettyCashFundSummaryDto[]>("/finance/petty-cash-funds"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Petty Cash</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Manage petty cash floats, replenishments, adjustments, and service-claim settlements against controlled funds.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create Fund</div>
        <PettyCashFundCreateForm currencies={currencies} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Funds</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Code</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Currency</th>
                <th className="py-2 pr-3">Custodian</th>
                <th className="py-2 pr-3">Balance</th>
                <th className="py-2 pr-3">Transactions</th>
                <th className="py-2 pr-3">Last Activity</th>
                <th className="py-2 pr-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {funds.map((fund) => (
                <tr key={fund.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/finance/petty-cash/${fund.id}`}>
                      {fund.code}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{fund.name}</td>
                  <td className="py-2 pr-3 text-zinc-500">{fund.currencyCode}</td>
                  <td className="py-2 pr-3 text-zinc-500">{fund.custodianName ?? "-"}</td>
                  <td className="py-2 pr-3">{fund.balance.toFixed(2)}</td>
                  <td className="py-2 pr-3 text-zinc-500">{fund.transactionCount}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {fund.lastActivityAt ? new Date(fund.lastActivityAt).toLocaleString() : "-"}
                  </td>
                  <td className="py-2 pr-3">{fund.isActive ? "Active" : "Inactive"}</td>
                </tr>
              ))}
              {funds.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                    No petty cash funds yet.
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
