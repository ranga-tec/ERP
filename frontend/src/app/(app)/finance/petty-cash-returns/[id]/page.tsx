import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { PettyCashReturnActions } from "../PettyCashReturnActions";
import { describeReturnCategory, money, returnStatusLabel } from "../types";

type ReturnLineDto = {
  id: string;
  pettyCashRequestLineId: string;
  pettyCashRequestId: string;
  requestNumber: string;
  category: number;
  serviceJobNumber?: string | null;
  customCategoryName?: string | null;
  purpose: string;
  amount: number;
};
type ReturnDto = {
  id: string;
  number: string;
  pettyCashFundId: string;
  pettyCashFundCode?: string | null;
  preparedByName: string;
  preparedAt: string;
  notes?: string | null;
  status: number;
  submittedAt?: string | null;
  receivedAt?: string | null;
  receiptReference?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
  cancelledAt?: string | null;
  totalAmount: number;
  lines: ReturnLineDto[];
};
type CurrentUserPermissionsDto = { permissions: string[] };

export default async function PettyCashReturnDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [pettyCashReturn, current] = await Promise.all([
    backendFetchJson<ReturnDto>(`/finance/petty-cash-returns/${id}`),
    backendFetchJson<CurrentUserPermissionsDto>("/me/permissions"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link className="hover:underline" href="/finance/petty-cash-returns">Return Money to Head Office</Link>
          {" / "}<span className="font-mono text-xs">{pettyCashReturn.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Petty Cash Return {pettyCashReturn.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Fund: <Link className="underline" href={`/finance/petty-cash/${pettyCashReturn.pettyCashFundId}`}>{pettyCashReturn.pettyCashFundCode ?? "View fund"}</Link></div>
          <div>Prepared by: {pettyCashReturn.preparedByName}</div>
          <div>Status: {returnStatusLabel[pettyCashReturn.status] ?? pettyCashReturn.status}</div>
          <div>Prepared: {new Date(pettyCashReturn.preparedAt).toLocaleString()}</div>
        </div>
        {pettyCashReturn.notes ? <p className="mt-2 text-sm text-zinc-500">{pettyCashReturn.notes}</p> : null}
        {pettyCashReturn.rejectionReason ? <p className="mt-2 text-sm text-red-700 dark:text-red-300">Rejected: {pettyCashReturn.rejectionReason}</p> : null}
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <Card><div className="text-xs uppercase tracking-wide text-zinc-500">Return total</div><div className="mt-1 text-xl font-semibold">{money(pettyCashReturn.totalAmount)}</div></Card>
        <Card><div className="text-xs uppercase tracking-wide text-zinc-500">Categories</div><div className="mt-1 text-xl font-semibold">{pettyCashReturn.lines.length}</div></Card>
        <Card><div className="text-xs uppercase tracking-wide text-zinc-500">Receipt reference</div><div className="mt-1 break-words font-mono text-sm font-semibold">{pettyCashReturn.receiptReference ?? "Pending"}</div>{pettyCashReturn.receivedAt ? <div className="mt-1 text-xs text-zinc-500">{new Date(pettyCashReturn.receivedAt).toLocaleString()}</div> : null}</Card>
      </div>

      <Card>
        <div className="mb-3">
          <div className="text-sm font-semibold">Category reconciliation</div>
          <div className="mt-1 text-xs text-zinc-500">Each amount returns against the same category line that head office originally funded.</div>
        </div>
        <div className="overflow-auto">
          <Table>
            <thead><tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800"><th className="py-2 pr-3">Funding request</th><th className="py-2 pr-3">Category</th><th className="py-2 pr-3">Purpose</th><th className="py-2 pr-3 text-right">Returned</th></tr></thead>
            <tbody>
              {pettyCashReturn.lines.map((line) => (
                <tr key={line.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3"><Link className="font-mono text-xs underline" href={`/finance/petty-cash-requests/${line.pettyCashRequestId}`}>{line.requestNumber}</Link></td>
                  <td className="py-2 pr-3">{describeReturnCategory(line.category, line.serviceJobNumber, line.customCategoryName)}</td>
                  <td className="py-2 pr-3 text-zinc-600 dark:text-zinc-400">{line.purpose}</td>
                  <td className="py-2 pr-3 text-right font-medium">{money(line.amount)}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Workflow actions</div>
        <PettyCashReturnActions
          returnId={pettyCashReturn.id}
          returnNumber={pettyCashReturn.number}
          status={pettyCashReturn.status}
          totalAmount={pettyCashReturn.totalAmount}
          permissions={current.permissions}
        />
      </Card>

      <DocumentCollaborationPanel referenceType="PCRTN" referenceId={id} title="Return Evidence, Comments & Attachments" />
    </div>
  );
}
