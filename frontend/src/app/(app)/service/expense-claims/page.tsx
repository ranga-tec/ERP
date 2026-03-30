import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, Table } from "@/components/ui";
import { ServiceExpenseClaimCreateForm } from "./ServiceExpenseClaimCreateForm";

type ServiceExpenseClaimSummaryDto = {
  id: string;
  number: string;
  serviceJobId: string;
  claimedByUserId?: string | null;
  claimedByName: string;
  fundingSource: number;
  expenseDate: string;
  merchantName?: string | null;
  status: number;
  total: number;
  lineCount: number;
  settledAt?: string | null;
};

type ServiceJobDto = { id: string; number: string; kind: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Settled",
};

const fundingSourceLabel: Record<number, string> = {
  1: "Out of Pocket",
  2: "Petty Cash",
};

export default async function ServiceExpenseClaimsPage() {
  const [claims, jobs] = await Promise.all([
    backendFetchJson<ServiceExpenseClaimSummaryDto[]>("/service/expense-claims?take=100"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
  ]);

  const jobById = new Map(jobs.map((job) => [job.id, job]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Service Expense Claims</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Record out-of-pocket technician spending and petty-cash repair purchases against service jobs, then route them to finance for approval and settlement.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <ServiceExpenseClaimCreateForm serviceJobs={jobs} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Claim</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Claimed By</th>
                <th className="py-2 pr-3">Funding</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Total</th>
              </tr>
            </thead>
            <tbody>
              {claims.map((claim) => (
                <tr key={claim.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/expense-claims/${claim.id}`}>
                      {claim.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    <TransactionLink referenceType="SJ" referenceId={claim.serviceJobId} monospace>
                      {jobById.get(claim.serviceJobId)?.number ?? claim.serviceJobId}
                    </TransactionLink>
                  </td>
                  <td className="py-2 pr-3">{claim.claimedByName}</td>
                  <td className="py-2 pr-3">{fundingSourceLabel[claim.fundingSource] ?? claim.fundingSource}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(claim.expenseDate).toLocaleString()}</td>
                  <td className="py-2 pr-3">{statusLabel[claim.status] ?? claim.status}</td>
                  <td className="py-2 pr-3">{claim.total.toFixed(2)}</td>
                </tr>
              ))}
              {claims.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>
                    No service expense claims yet.
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
