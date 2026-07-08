import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AuditTrailButton } from "@/components/AuditTrailButton";
import { AppFormModal } from "@/components/AppFormModal";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
import { ServiceHandoverCreateForm } from "./ServiceHandoverCreateForm";
import { ServiceHandoverEditForm } from "./ServiceHandoverEditForm";

type ServiceHandoverDto = {
  id: string;
  number: string;
  serviceJobId: string;
  handoverDate: string;
  itemsReturned: string;
  postServiceWarrantyMonths?: number | null;
  customerAcknowledgement?: string | null;
  notes?: string | null;
  status: number;
};

type ServiceJobDto = { id: string; number: string; customerId: string; status: number };
type CustomerDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Completed",
  2: "Cancelled",
};

export default async function ServiceHandoversPage() {
  const [rows, jobs, customers] = await Promise.all([
    backendFetchJson<ServiceHandoverDto[]>("/service/handovers?take=100"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));
  const customerById = new Map(customers.map((c) => [c.id, c]));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Service Taken / Delivery Confirmation</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Record device/accessory return to the customer and close out delivery readiness.
          </p>
        </div>
        <AppFormModal title="Create Service Taken" description="Record customer delivery confirmation for a job order." buttonLabel="+ New Confirmation">
          <ServiceHandoverCreateForm serviceJobs={jobs} customers={customers} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search confirmation, job, customer, status..."
          emptyMessage="No service handovers yet."
          emptyColSpan={7}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Confirmation</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Warranty</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
              {rows.map((r) => {
                const job = jobById.get(r.serviceJobId);
                const customer = job ? customerById.get(job.customerId) : null;
                const status = statusLabel[r.status] ?? String(r.status);
                return (
                  <SearchableRow
                    key={r.id}
                    searchText={[
                      r.number,
                      job?.number,
                      customer?.code,
                      customer?.name,
                      status,
                      r.itemsReturned,
                      r.customerAcknowledgement,
                      r.notes,
                    ].filter(Boolean).join(" ")}
                  >
                  <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3 font-mono text-xs">
                      <Link className="hover:underline" href={`/service/handovers/${r.id}`}>
                        {r.number}
                      </Link>
                    </td>
                    <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                      <TransactionLink referenceType="SJ" referenceId={r.serviceJobId} monospace>
                        {job?.number ?? r.serviceJobId}
                      </TransactionLink>
                    </td>
                    <td className="py-2 pr-3">{customer ? customer.code : "-"}</td>
                    <td className="py-2 pr-3 text-zinc-500">{new Date(r.handoverDate).toLocaleString()}</td>
                    <td className="py-2 pr-3">
                      {typeof r.postServiceWarrantyMonths === "number" ? `${r.postServiceWarrantyMonths} mo` : "-"}
                    </td>
                    <td className="py-2 pr-3">{statusLabel[r.status] ?? r.status}</td>
                    <td className="py-2 pr-3">
                      <div className="flex flex-wrap items-center gap-3 text-xs">
                        <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/handovers/${r.id}`}>
                          View
                        </Link>
                        {r.status === 0 ? (
                          <AppFormModal title={`Edit Service Taken ${r.number}`} description="Update delivery confirmation details." buttonLabel="Edit" variant="secondary">
                            <ServiceHandoverEditForm handover={r} />
                          </AppFormModal>
                        ) : (
                          <span className="text-zinc-400">Edit</span>
                        )}
                        <AuditTrailButton tableName="ServiceHandovers" recordId={r.id} />
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
