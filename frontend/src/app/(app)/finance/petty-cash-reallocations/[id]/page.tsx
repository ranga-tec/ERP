import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { PettyCashReallocationActions } from "../PettyCashReallocationActions";
import { describeCategory, money, reallocationStatusLabel, type CategoryReferenceDto, type ReallocationDto } from "../types";

type CurrentUserPermissionsDto = { permissions: string[] };

function CategoryCard({ title, category, balance }: { title: string; category: CategoryReferenceDto; balance: number }) {
  return (
    <Card>
      <div className="text-xs uppercase tracking-wide text-zinc-500">{title}</div>
      <div className="mt-1 font-semibold">{describeCategory(category.category, category.serviceJobNumber, category.customCategoryName)}</div>
      <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">{category.purpose}</div>
      <div className="mt-2 text-lg font-semibold">{money(balance)}</div>
      <Link className="mt-1 inline-block font-mono text-xs underline" href={`/finance/petty-cash-requests/${category.pettyCashRequestId}`}>{category.requestNumber}</Link>
    </Card>
  );
}

export default async function PettyCashReallocationDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [reallocation, current] = await Promise.all([
    backendFetchJson<ReallocationDto>(`/finance/petty-cash-reallocations/${id}`),
    backendFetchJson<CurrentUserPermissionsDto>("/me/permissions"),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link className="hover:underline" href="/finance/petty-cash-reallocations">Petty Cash Category Reallocation</Link>
          {" / "}<span className="font-mono text-xs">{reallocation.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Category Reallocation {reallocation.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Fund: <Link className="underline" href={`/finance/petty-cash/${reallocation.pettyCashFundId}`}>{reallocation.pettyCashFundCode ?? "View fund"}</Link></div>
          <div>Requested by: {reallocation.requestedByName}</div>
          <div>Status: {reallocationStatusLabel[reallocation.status] ?? reallocation.status}</div>
          <div>Requested: {new Date(reallocation.requestedAt).toLocaleString()}</div>
        </div>
        {reallocation.rejectionReason ? <p className="mt-2 text-sm text-red-700 dark:text-red-300">Rejected: {reallocation.rejectionReason}</p> : null}
      </div>

      <div className="grid items-stretch gap-3 md:grid-cols-[1fr_auto_1fr]">
        <CategoryCard title="Source category · current balance" category={reallocation.source} balance={reallocation.sourceBalance} />
        <div className="flex items-center justify-center px-2 text-2xl text-zinc-400">→</div>
        <CategoryCard title="Destination category · current balance" category={reallocation.destination} balance={reallocation.destinationBalance} />
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <Card><div className="text-xs uppercase tracking-wide text-zinc-500">Amount reallocated</div><div className="mt-1 text-xl font-semibold">{money(reallocation.amount)}</div><div className="mt-1 text-xs text-zinc-500">Fund total change: 0.00</div></Card>
        <Card><div className="text-xs uppercase tracking-wide text-zinc-500">Business reason</div><div className="mt-1 text-sm">{reallocation.reason}</div></Card>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Workflow actions</div>
        <PettyCashReallocationActions
          reallocationId={reallocation.id}
          reallocationNumber={reallocation.number}
          status={reallocation.status}
          amount={reallocation.amount}
          permissions={current.permissions}
        />
      </Card>

      <DocumentCollaborationPanel referenceType="PCRAL" referenceId={id} title="Reallocation Evidence, Comments & Attachments" />
    </div>
  );
}
