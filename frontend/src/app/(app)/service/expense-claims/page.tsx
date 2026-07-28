import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AuditTrailButton } from "@/components/AuditTrailButton";
import { AppFormModal } from "@/components/AppFormModal";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
import { ServiceExpenseClaimCreateForm } from "./ServiceExpenseClaimCreateForm";

type ServiceExpenseClaimSummaryDto = {
  id: string;
  number: string;
  serviceJobId?: string | null;
  claimedByUserId?: string | null;
  claimedByName: string;
  fundingSource: number;
  expenseDate: string;
  merchantName?: string | null;
  status: number;
  total: number;
  lineCount: number;
  settledAt?: string | null;
  pettyCashIouId?: string | null;
  pettyCashIouNumber?: string | null;
};

type ServiceJobDto = { id: string; number: string; kind: number };
type PettyCashIouDto = { id: string; number: string; serviceJobId: string; status: number; amount: number };
type FundedCategoryDto = {
  id: string;
  requestNumber: string;
  category: number;
  serviceJobId?: string | null;
  serviceJobNumber?: string | null;
  customCategoryName?: string | null;
  purpose: string;
  fundedAmount: number;
};
type CurrentUserPermissionsDto = { userId: string; permissions: string[] };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Settled",
};

const fundingSourceLabel: Record<number, string> = {
  1: "Out of Pocket",
  2: "Petty Cash Fund",
};

export default async function ServiceExpenseClaimsPage() {
  const [claims, jobs, pettyCashIous, fundedCategories, currentUserPermissions] = await Promise.all([
    backendFetchJson<ServiceExpenseClaimSummaryDto[]>("/service/expense-claims?take=100"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<PettyCashIouDto[]>("/finance/petty-cash-ious?take=500"),
    backendFetchJson<FundedCategoryDto[]>("/finance/petty-cash-requests/funded-lines"),
    backendFetchJson<CurrentUserPermissionsDto>("/me/permissions"),
  ]);

  const jobById = new Map(jobs.map((job) => [job.id, job]));
  const permissions = new Set(currentUserPermissions.permissions);
  const canCreate = permissions.has("Service.ExpenseClaim.Create");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Expense Vouchers</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Money already spent on a job, documented with its bills, then routed to finance for approval and repayment. Repayment comes from the petty cash fund or by payment to the claimant. To issue cash <em>before</em> the spend, raise a petty cash advance (IOU) instead.
          </p>
        </div>
        {canCreate ? (
          <AppFormModal title="Create Expense Voucher" description="Record spend that has already happened on a job order, and how the claimant gets repaid." buttonLabel="+ New Voucher">
            <ServiceExpenseClaimCreateForm
              serviceJobs={jobs}
              pettyCashIous={pettyCashIous}
              fundedCategories={fundedCategories}
            />
          </AppFormModal>
        ) : null}
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search voucher, job, claimant, funding, status..."
          emptyMessage="No service expense claims yet."
          emptyColSpan={9}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Claim</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Claimed By</th>
                <th className="py-2 pr-3">Funding</th>
                <th className="py-2 pr-3">IOU</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
              {claims.map((claim) => {
                const job = claim.serviceJobId ? jobById.get(claim.serviceJobId) : undefined;
                const funding = fundingSourceLabel[claim.fundingSource] ?? String(claim.fundingSource);
                const status = statusLabel[claim.status] ?? String(claim.status);
                return (
                <SearchableRow
                  key={claim.id}
                  searchText={[
                    claim.number,
                    job?.number,
                    claim.claimedByName,
                    claim.merchantName,
                    claim.pettyCashIouNumber,
                    funding,
                    status,
                    claim.total.toFixed(2),
                  ].filter(Boolean).join(" ")}
                >
                <tr key={claim.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/expense-claims/${claim.id}`}>
                      {claim.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {claim.serviceJobId ? (
                      <TransactionLink referenceType="SJ" referenceId={claim.serviceJobId} monospace>
                        {jobById.get(claim.serviceJobId)?.number ?? "Job removed"}
                      </TransactionLink>
                    ) : (
                      <span className="text-zinc-400">Overhead</span>
                    )}
                  </td>
                  <td className="py-2 pr-3">{claim.claimedByName}</td>
                  <td className="py-2 pr-3">{fundingSourceLabel[claim.fundingSource] ?? claim.fundingSource}</td>
                  <td className="py-2 pr-3 font-mono text-xs">
                    {claim.pettyCashIouNumber ? (
                      <Link className="hover:underline" href="/finance/petty-cash-ious">
                        {claim.pettyCashIouNumber}
                      </Link>
                    ) : claim.pettyCashIouId ? (
                      <span className="text-zinc-400">Advance removed</span>
                    ) : (
                      <span className="text-zinc-400">-</span>
                    )}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(claim.expenseDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[claim.status] ?? claim.status}</td>
                  <td className="py-2 pr-3">{claim.total.toFixed(2)}</td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap items-center gap-3 text-xs">
                      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/expense-claims/${claim.id}`}>
                        View
                      </Link>
                      <AuditTrailButton tableName="ServiceExpenseClaims" recordId={claim.id} />
                    </div>
                  </td>
                </tr>
                </SearchableRow>
              );
              })}
        </SearchableTable>
      </Card>
    </div>
  );
}
