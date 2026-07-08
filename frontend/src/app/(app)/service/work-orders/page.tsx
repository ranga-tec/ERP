import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AuditTrailButton } from "@/components/AuditTrailButton";
import { AppFormModal } from "@/components/AppFormModal";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
import { WorkOrderCreateForm } from "./WorkOrderCreateForm";

type WorkOrderDto = {
  id: string;
  serviceJobId: string;
  description: string;
  assignedToUserId?: string | null;
  status: number;
  timeEntryCount: number;
  approvedHours: number;
  approvedLaborCost: number;
};

type ServiceJobDto = { id: string; number: string };

const statusLabel: Record<number, string> = {
  0: "Open",
  1: "In Progress",
  2: "Done",
  3: "Cancelled",
};

export default async function WorkOrdersPage() {
  const [workOrders, jobs] = await Promise.all([
    backendFetchJson<WorkOrderDto[]>("/service/work-orders?take=100"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Job Sheets / Work Orders</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Create technician work orders, record job sheet time entries, and track work status under a job order.
          </p>
        </div>
        <AppFormModal title="Create Job Sheet / Work Order" description="Open a job sheet under an existing job order." buttonLabel="+ New Job Sheet">
          <WorkOrderCreateForm serviceJobs={jobs} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search job sheet, job, status, description..."
          emptyMessage="No work orders yet."
          emptyColSpan={7}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Job Sheet / Work Order</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Description</th>
                <th className="py-2 pr-3">Approved Hrs</th>
                <th className="py-2 pr-3">Approved Cost</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
              {workOrders.map((w) => {
                const job = jobById.get(w.serviceJobId);
                const status = statusLabel[w.status] ?? String(w.status);
                return (
                <SearchableRow
                  key={w.id}
                  searchText={[
                    "job sheet work order",
                    job?.number,
                    status,
                    w.description,
                    w.approvedHours.toFixed(2),
                    w.approvedLaborCost.toFixed(2),
                  ].filter(Boolean).join(" ")}
                >
                <tr key={w.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">
                    <Link className="hover:underline" href={`/service/work-orders/${w.id}`}>
                      Job Sheet
                    </Link>
                    <div className="mt-1 text-xs text-zinc-500">
                      {jobById.get(w.serviceJobId)?.number ?? "Job not found"}
                    </div>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    <TransactionLink referenceType="SJ" referenceId={w.serviceJobId} monospace>
                      {jobById.get(w.serviceJobId)?.number ?? w.serviceJobId}
                    </TransactionLink>
                  </td>
                  <td className="py-2 pr-3">{statusLabel[w.status] ?? w.status}</td>
                  <td className="py-2 pr-3 text-zinc-500">{w.description}</td>
                  <td className="py-2 pr-3">{w.approvedHours.toFixed(2)}</td>
                  <td className="py-2 pr-3">{w.approvedLaborCost.toFixed(2)}</td>
                  <td className="py-2 pr-3">
                    <AuditTrailButton tableName="WorkOrders" recordId={w.id} />
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
