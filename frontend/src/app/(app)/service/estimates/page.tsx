import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, Table } from "@/components/ui";
import { ServiceEstimateCreateForm } from "./ServiceEstimateCreateForm";

type ServiceEstimateSummaryDto = {
  id: string;
  number: string;
  serviceJobId: string;
  issuedAt: string;
  validUntil?: string | null;
  revisedFromEstimateId?: string | null;
  revisionNumber: number;
  status: number;
  customerApprovalStatus: number;
  sentToCustomerAt?: string | null;
  customerDecisionAt?: string | null;
  subtotal: number;
  taxTotal: number;
  total: number;
  lineCount: number;
};

type ServiceJobDto = { id: string; number: string; customerId: string; status: number };
type CustomerDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Approved",
  2: "Rejected",
};

const approvalLabel: Record<number, string> = {
  0: "Not Sent",
  1: "Pending",
  2: "Approved",
  3: "Rejected",
};

export default async function ServiceEstimatesPage() {
  const [estimates, jobs, customers] = await Promise.all([
    backendFetchJson<ServiceEstimateSummaryDto[]>("/service/estimates?take=100"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const customerById = new Map(customers.map((c) => [c.id, c]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Service Estimates</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Quote parts and labor for service jobs, then create a revision if extra findings appear after approval.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <ServiceEstimateCreateForm serviceJobs={jobs} customers={customers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Estimate</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Issued</th>
                <th className="py-2 pr-3">Valid Till</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Customer Approval</th>
                <th className="py-2 pr-3">Total</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {estimates.map((e) => {
                const job = jobById.get(e.serviceJobId);
                const customer = job ? customerById.get(job.customerId) : null;
                return (
                  <tr key={e.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3 font-mono text-xs">
                      <Link className="hover:underline" href={`/service/estimates/${e.id}`}>
                        {e.number}
                        {e.revisionNumber > 0 ? ` / R${e.revisionNumber}` : ""}
                      </Link>
                    </td>
                    <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                      <TransactionLink referenceType="SJ" referenceId={e.serviceJobId} monospace>
                        {job?.number ?? e.serviceJobId}
                      </TransactionLink>
                    </td>
                    <td className="py-2 pr-3">{customer ? customer.code : "-"}</td>
                    <td className="py-2 pr-3 text-zinc-500">{new Date(e.issuedAt).toLocaleString()}</td>
                    <td className="py-2 pr-3 text-zinc-500">
                      {e.validUntil ? new Date(e.validUntil).toLocaleString() : "-"}
                    </td>
                    <td className="py-2 pr-3">{statusLabel[e.status] ?? e.status}</td>
                    <td className="py-2 pr-3">
                      <div>{approvalLabel[e.customerApprovalStatus] ?? e.customerApprovalStatus}</div>
                      <div className="text-xs text-zinc-500">
                        {e.sentToCustomerAt ? `Sent ${new Date(e.sentToCustomerAt).toLocaleDateString()}` : "-"}
                      </div>
                    </td>
                    <td className="py-2 pr-3">{e.total.toFixed(2)}</td>
                    <td className="py-2 pr-3">
                      <div className="flex flex-wrap gap-3 text-xs">
                        <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/estimates/${e.id}`}>
                          View
                        </Link>
                        {e.status === 0 ? (
                          <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/estimates/${e.id}`}>
                            Edit
                          </Link>
                        ) : (
                          <span className="text-zinc-400">Edit</span>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
              {estimates.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={9}>
                    No service estimates yet.
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
