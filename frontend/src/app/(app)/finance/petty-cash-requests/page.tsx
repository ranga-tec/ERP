import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { PettyCashRequestCreateForm } from "./PettyCashRequestCreateForm";
import { money, statusLabel } from "./categories";

type PettyCashRequestSummaryDto = {
  id: string;
  number: string;
  pettyCashFundId: string;
  pettyCashFundCode?: string | null;
  requestedByName: string;
  requestedAt: string;
  neededByAt?: string | null;
  status: number;
  lineCount: number;
  requestedTotal: number;
  approvedTotal: number;
  fundedTotal: number;
  outstandingTotal: number;
};

type FundDto = { id: string; code: string; name: string; isActive: boolean };
type CurrentUserPermissionsDto = { permissions: string[] };

export default async function PettyCashRequestsPage() {
  const [requests, funds, currentPermissions] = await Promise.all([
    backendFetchJson<PettyCashRequestSummaryDto[]>("/finance/petty-cash-requests?take=200"),
    backendFetchJson<FundDto[]>("/finance/petty-cash-funds"),
    backendFetchJson<CurrentUserPermissionsDto>("/me/permissions"),
  ]);

  const activeFunds = funds.filter((fund) => fund.isActive);
  const permissions = new Set(currentPermissions.permissions);
  const canCreate = permissions.has("Finance.PettyCashRequest.Create");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Petty Cash Requests</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Day-to-day requests to head office for petty cash, split by category - job wise, emergency operations,
            transportation or your own. Head office approves each category on its own and releases the money
            separately, even when one bank transfer covers several.
          </p>
        </div>
        {canCreate ? (
          <AppFormModal
            title="Create Petty Cash Request"
            description="Raise a request to head office. Category lines are added next."
            buttonLabel="+ New Request"
          >
            <PettyCashRequestCreateForm funds={activeFunds} />
          </AppFormModal>
        ) : null}
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Requests</div>
        <SearchableTable
          placeholder="Search request, fund, requester, status..."
          emptyMessage="No petty cash requests yet."
          emptyColSpan={9}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Request</th>
                <th className="py-2 pr-3">Fund</th>
                <th className="py-2 pr-3">Requested By</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3 text-right">Lines</th>
                <th className="py-2 pr-3 text-right">Requested</th>
                <th className="py-2 pr-3 text-right">Approved</th>
                <th className="py-2 pr-3 text-right">Funded</th>
                <th className="py-2 pr-3">Status</th>
              </tr>
            </thead>
          }
        >
          {requests.map((request) => {
            const status = statusLabel[request.status] ?? String(request.status);
            return (
              <SearchableRow
                key={request.id}
                searchText={[
                  request.number,
                  request.pettyCashFundCode,
                  request.requestedByName,
                  status,
                ].filter(Boolean).join(" ")}
              >
                <tr className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/finance/petty-cash-requests/${request.id}`}>
                      {request.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {request.pettyCashFundCode ?? <span className="text-zinc-400">Fund removed</span>}
                  </td>
                  <td className="py-2 pr-3">{request.requestedByName}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(request.requestedAt).toLocaleDateString()}</td>
                  <td className="py-2 pr-3 text-right">{request.lineCount}</td>
                  <td className="py-2 pr-3 text-right">{money(request.requestedTotal)}</td>
                  <td className="py-2 pr-3 text-right">{money(request.approvedTotal)}</td>
                  <td className="py-2 pr-3 text-right">
                    {money(request.fundedTotal)}
                    {request.outstandingTotal > 0 ? (
                      <div className="text-xs text-amber-700 dark:text-amber-400">
                        {money(request.outstandingTotal)} awaiting release
                      </div>
                    ) : null}
                  </td>
                  <td className="py-2 pr-3">{status}</td>
                </tr>
              </SearchableRow>
            );
          })}
        </SearchableTable>
      </Card>
    </div>
  );
}
