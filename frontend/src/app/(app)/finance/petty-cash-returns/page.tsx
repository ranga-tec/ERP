import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { Card, Table } from "@/components/ui";
import { PettyCashReturnCreateForm } from "./PettyCashReturnCreateForm";
import { money, returnStatusLabel, type ReturnCandidateDto } from "./types";

type ReturnSummaryDto = {
  id: string;
  number: string;
  pettyCashFundCode?: string | null;
  preparedByName: string;
  preparedAt: string;
  status: number;
  lineCount: number;
  totalAmount: number;
  receiptReference?: string | null;
};
type CurrentUserPermissionsDto = { permissions: string[] };

export default async function PettyCashReturnsPage() {
  const current = await backendFetchJson<CurrentUserPermissionsDto>("/me/permissions");
  const canCreate = current.permissions.includes("Finance.PettyCashReturn.Create");
  const [returns, candidates] = await Promise.all([
    backendFetchJson<ReturnSummaryDto[]>("/finance/petty-cash-returns"),
    canCreate
      ? backendFetchJson<ReturnCandidateDto[]>("/finance/petty-cash-returns/candidates")
      : Promise.resolve([]),
  ]);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Return Money to Head Office</h1>
          <p className="mt-1 max-w-3xl text-sm text-zinc-500">
            Reconcile unused petty cash by its original funding category. Head office confirms the physical receipt before the fund and category balances are reduced.
          </p>
        </div>
        {canCreate ? (
          <AppFormModal
            title="Prepare Petty Cash Return"
            description="Select reconciled category balances. Open IOUs must be completed first."
            buttonLabel="+ Prepare Return"
            variant="primary"
            size="lg"
          >
            <PettyCashReturnCreateForm candidates={candidates} />
          </AppFormModal>
        ) : null}
      </div>

      <Card>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Return</th>
                <th className="py-2 pr-3">Fund</th>
                <th className="py-2 pr-3">Prepared by</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3 text-right">Categories</th>
                <th className="py-2 pr-3 text-right">Amount</th>
                <th className="py-2 pr-3">Receipt ref</th>
              </tr>
            </thead>
            <tbody>
              {returns.map((row) => (
                <tr key={row.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3"><Link className="font-mono text-xs underline" href={`/finance/petty-cash-returns/${row.id}`}>{row.number}</Link></td>
                  <td className="py-2 pr-3">{row.pettyCashFundCode ?? "-"}</td>
                  <td className="py-2 pr-3">{row.preparedByName}</td>
                  <td className="py-2 pr-3">{new Date(row.preparedAt).toLocaleDateString()}</td>
                  <td className="py-2 pr-3">{returnStatusLabel[row.status] ?? row.status}</td>
                  <td className="py-2 pr-3 text-right">{row.lineCount}</td>
                  <td className="py-2 pr-3 text-right font-medium">{money(row.totalAmount)}</td>
                  <td className="py-2 pr-3">{row.receiptReference ?? "-"}</td>
                </tr>
              ))}
              {returns.length === 0 ? <tr><td colSpan={8} className="py-8 text-center text-sm text-zinc-500">No petty cash returns have been prepared.</td></tr> : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
