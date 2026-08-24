import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { Card, Table } from "@/components/ui";
import { PettyCashRequestCreateForm } from "./PettyCashRequestCreateForm";
import { money, statusLabel } from "./categories";

type Request = { id: string; number: string; pettyCashFundCode?: string | null; requestedByName: string; requestedAt: string; status: number; requestedAmount: number; approvedAmount: number; fundedAmount: number; outstandingAmount: number; isLegacyCategoryRequest: boolean };
type Fund = { id: string; code: string; name: string; isActive: boolean; balance: number; authorizedFloat: number };
type Permissions = { permissions: string[] };

export default async function Page() {
  const [rows, funds, access] = await Promise.all([
    backendFetchJson<Request[]>("/finance/petty-cash-requests"),
    backendFetchJson<Fund[]>("/finance/petty-cash-funds"),
    backendFetchJson<Permissions>("/me/permissions"),
  ]);
  const canCreate = access.permissions.includes("Finance.PettyCashRequest.Create");
  return <div className="space-y-6">
    <div className="flex flex-wrap items-start justify-between gap-3"><div><h1 className="text-2xl font-semibold">Petty Cash Replenishment</h1><p className="mt-1 max-w-3xl text-sm text-zinc-500">Restore a reconciled site float. Jobs and expense categories classify the actual expense; they are not separate cash balances.</p></div>{canCreate ? <AppFormModal title="New Replenishment Request" description="Record the reconciliation snapshot and amount required." buttonLabel="+ New Replenishment"><PettyCashRequestCreateForm funds={funds.filter((fund) => fund.isActive)} /></AppFormModal> : null}</div>
    <Card><div className="overflow-auto"><Table><thead><tr className="border-b text-left text-xs uppercase text-zinc-500"><th className="py-2 pr-3">Request</th><th className="py-2 pr-3">Fund</th><th className="py-2 pr-3">Requested by</th><th className="py-2 pr-3 text-right">Requested</th><th className="py-2 pr-3 text-right">Approved</th><th className="py-2 pr-3 text-right">Received</th><th className="py-2 pr-3">Status</th></tr></thead><tbody>{rows.map((row) => <tr key={row.id} className="border-b"><td className="py-2 pr-3"><Link className="font-mono text-xs underline" href={`/finance/petty-cash-requests/${row.id}`}>{row.number}</Link>{row.isLegacyCategoryRequest ? <div className="text-xs text-amber-600">Legacy category request</div> : null}</td><td className="py-2 pr-3">{row.pettyCashFundCode ?? "-"}</td><td className="py-2 pr-3">{row.requestedByName}</td><td className="py-2 pr-3 text-right">{money(row.requestedAmount)}</td><td className="py-2 pr-3 text-right">{money(row.approvedAmount)}</td><td className="py-2 pr-3 text-right">{money(row.fundedAmount)}</td><td className="py-2 pr-3">{statusLabel[row.status] ?? row.status}</td></tr>)}{rows.length === 0 ? <tr><td colSpan={7} className="py-8 text-center text-sm text-zinc-500">No replenishment requests.</td></tr> : null}</tbody></Table></div></Card>
  </div>;
}
