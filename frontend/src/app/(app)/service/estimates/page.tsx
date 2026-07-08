import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
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
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Quotations</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Prepare customer quotations for job orders, then create a revision if extra findings appear after approval.
          </p>
        </div>
        <AppFormModal title="Create Quotation" description="Create a draft quotation for a job order." buttonLabel="+ New Quotation">
          <ServiceEstimateCreateForm serviceJobs={jobs} customers={customers} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search quotation, job, customer, status, approval..."
          emptyMessage="No service estimates yet."
          emptyColSpan={9}
          headers={
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
          }
        >
              {estimates.map((e) => {
                const job = jobById.get(e.serviceJobId);
                const customer = job ? customerById.get(job.customerId) : null;
                const status = statusLabel[e.status] ?? String(e.status);
                const approval = approvalLabel[e.customerApprovalStatus] ?? String(e.customerApprovalStatus);
                return (
                  <SearchableRow
                    key={e.id}
                    searchText={[
                      e.number,
                      e.revisionNumber > 0 ? `R${e.revisionNumber}` : "",
                      job?.number,
                      customer?.code,
                      customer?.name,
                      status,
                      approval,
                      e.total.toFixed(2),
                    ].filter(Boolean).join(" ")}
                  >
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
                      <ListViewEditActions
                        viewHref={`/service/estimates/${e.id}`}
                        canEdit={e.status === 0}
                        auditTableName="ServiceEstimates"
                        auditRecordId={e.id}
                      />
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
