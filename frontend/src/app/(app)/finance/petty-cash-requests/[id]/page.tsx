import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { PettyCashRequestActions } from "../PettyCashRequestActions";
import { PettyCashRequestFundLineForm } from "../PettyCashRequestFundLineForm";
import { STATUS_APPROVED, STATUS_PARTIALLY_FUNDED, money, statusLabel } from "../categories";

type Request = { id: string; number: string; pettyCashFundId: string; pettyCashFundCode?: string | null; authorizedFloat?: number | null; requestedByName: string; requestedAt: string; neededByAt?: string | null; notes?: string | null; status: number; rejectionReason?: string | null; requestedAmount: number; approvedAmount: number; fundedAmount: number; outstandingAmount: number; cashOnHand: number; outstandingAdvances: number; reconciledExpenses: number; isLegacyCategoryRequest: boolean; legacyCategoryLineCount: number };
type Permissions = { permissions: string[] };

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [row, access] = await Promise.all([backendFetchJson<Request>(`/finance/petty-cash-requests/${id}`), backendFetchJson<Permissions>("/me/permissions")]);
  const canFund = !row.isLegacyCategoryRequest && [STATUS_APPROVED, STATUS_PARTIALLY_FUNDED].includes(row.status) && access.permissions.includes("Finance.PettyCashRequest.Fund");
  return <div className="space-y-6">
    <div><div className="text-sm text-zinc-500"><Link className="hover:underline" href="/finance/petty-cash-requests">Petty Cash Replenishment</Link> / <span className="font-mono">{row.number}</span></div><h1 className="mt-1 text-2xl font-semibold">Replenishment {row.number}</h1><div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-500"><span>Fund: <Link className="underline" href={`/finance/petty-cash/${row.pettyCashFundId}`}>{row.pettyCashFundCode ?? "View"}</Link></span><span>Requested by: {row.requestedByName}</span><span>Status: {statusLabel[row.status] ?? row.status}</span></div>{row.rejectionReason ? <p className="mt-2 text-sm text-red-700">Rejected: {row.rejectionReason}</p> : null}</div>
    <div className="grid gap-3 sm:grid-cols-4"><Card><div className="text-xs uppercase text-zinc-500">Requested</div><div className="text-xl font-semibold">{money(row.requestedAmount)}</div></Card><Card><div className="text-xs uppercase text-zinc-500">Approved</div><div className="text-xl font-semibold">{money(row.approvedAmount)}</div></Card><Card><div className="text-xs uppercase text-zinc-500">Received</div><div className="text-xl font-semibold">{money(row.fundedAmount)}</div></Card><Card><div className="text-xs uppercase text-zinc-500">Outstanding</div><div className="text-xl font-semibold">{money(row.outstandingAmount)}</div></Card></div>
    <Card><div className="mb-3 text-sm font-semibold">Reconciliation snapshot</div><div className="grid gap-3 sm:grid-cols-4 text-sm"><div>Cash on hand<br/><b>{money(row.cashOnHand)}</b></div><div>Outstanding advances<br/><b>{money(row.outstandingAdvances)}</b></div><div>Reconciled expenses<br/><b>{money(row.reconciledExpenses)}</b></div><div>Authorized float<br/><b>{row.authorizedFloat ? money(row.authorizedFloat) : "Not configured"}</b></div></div>{row.notes ? <p className="mt-3 text-sm text-zinc-500">{row.notes}</p> : null}{row.isLegacyCategoryRequest ? <p className="mt-3 text-sm text-amber-700">Historical read-only request with {row.legacyCategoryLineCount} category lines.</p> : null}</Card>
    <Card><div className="mb-3 text-sm font-semibold">Workflow</div><PettyCashRequestActions requestId={row.id} status={row.status} requestedAmount={row.requestedAmount} permissions={access.permissions} legacy={row.isLegacyCategoryRequest} /></Card>
    {canFund ? <Card><div className="mb-3 text-sm font-semibold">Record fund receipt</div><PettyCashRequestFundLineForm requestId={row.id} outstandingAmount={row.outstandingAmount} /></Card> : null}
    <DocumentCollaborationPanel referenceType="PCR" referenceId={id} title="Replenishment Evidence, Comments & Attachments" />
  </div>;
}
