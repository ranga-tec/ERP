import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { Card, Table } from "@/components/ui";
import { PettyCashReallocationCreateForm } from "./PettyCashReallocationCreateForm";
import { PettyCashCategoryBalanceRegister } from "./PettyCashCategoryBalanceRegister";
import {
  describeCategory,
  money,
  reallocationStatusLabel,
  type CategoryReferenceDto,
  type ReallocationCandidateDto,
} from "./types";

type ReallocationSummaryDto = {
  id: string;
  number: string;
  pettyCashFundCode?: string | null;
  requestedByName: string;
  requestedAt: string;
  status: number;
  amount: number;
  source: CategoryReferenceDto;
  destination: CategoryReferenceDto;
};
type CurrentUserPermissionsDto = { permissions: string[] };

function categoryName(category: CategoryReferenceDto) {
  return describeCategory(category.category, category.serviceJobNumber, category.customCategoryName);
}

export default async function PettyCashReallocationsPage() {
  const current = await backendFetchJson<CurrentUserPermissionsDto>("/me/permissions");
  const canCreate = current.permissions.includes("Finance.PettyCashReallocation.Create");
  const [rows, balances] = await Promise.all([
    backendFetchJson<ReallocationSummaryDto[]>("/finance/petty-cash-reallocations"),
    backendFetchJson<ReallocationCandidateDto[]>("/finance/petty-cash-reallocations/category-balances"),
  ]);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Petty Cash Category Reallocation</h1>
          <p className="mt-1 max-w-3xl text-sm text-zinc-500">
            Request approval to move an unused category balance to another approved category in the same fund. This changes authorization, not physical cash.
          </p>
        </div>
        {canCreate ? (
          <AppFormModal
            title="Prepare Category Reallocation"
            description="Select the source and destination, enter the amount, and document the business reason."
            buttonLabel="+ New Reallocation"
            variant="primary"
            size="lg"
          >
            <PettyCashReallocationCreateForm candidates={balances} />
          </AppFormModal>
        ) : null}
      </div>

      <PettyCashCategoryBalanceRegister rows={balances} />

      <Card>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Reallocation</th>
                <th className="py-2 pr-3">Fund</th>
                <th className="py-2 pr-3">From</th>
                <th className="py-2 pr-3">To</th>
                <th className="py-2 pr-3 text-right">Amount</th>
                <th className="py-2 pr-3">Requested by</th>
                <th className="py-2 pr-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3"><Link className="font-mono text-xs underline" href={`/finance/petty-cash-reallocations/${row.id}`}>{row.number}</Link></td>
                  <td className="py-2 pr-3">{row.pettyCashFundCode ?? "-"}</td>
                  <td className="py-2 pr-3"><div>{categoryName(row.source)}</div><div className="max-w-56 truncate text-xs text-zinc-500">{row.source.purpose}</div></td>
                  <td className="py-2 pr-3"><div>{categoryName(row.destination)}</div><div className="max-w-56 truncate text-xs text-zinc-500">{row.destination.purpose}</div></td>
                  <td className="py-2 pr-3 text-right font-medium">{money(row.amount)}</td>
                  <td className="py-2 pr-3"><div>{row.requestedByName}</div><div className="text-xs text-zinc-500">{new Date(row.requestedAt).toLocaleDateString()}</div></td>
                  <td className="py-2 pr-3">{reallocationStatusLabel[row.status] ?? row.status}</td>
                </tr>
              ))}
              {rows.length === 0 ? <tr><td colSpan={7} className="py-8 text-center text-sm text-zinc-500">No category reallocations have been prepared.</td></tr> : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
