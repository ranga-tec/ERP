import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { ServiceHandoverCreateForm } from "./ServiceHandoverCreateForm";

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
      <div>
        <h1 className="text-2xl font-semibold">Service Handovers</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Record device/accessory return to the customer and close out delivery readiness.
        </p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <ServiceHandoverCreateForm serviceJobs={jobs} customers={customers} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Handover</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Customer</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Warranty</th>
                <th className="py-2 pr-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => {
                const job = jobById.get(r.serviceJobId);
                const customer = job ? customerById.get(job.customerId) : null;
                return (
                  <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-900">
                    <td className="py-2 pr-3 font-mono text-xs">
                      <Link className="hover:underline" href={`/service/handovers/${r.id}`}>
                        {r.number}
                      </Link>
                    </td>
                    <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                      {job?.number ?? r.serviceJobId}
                    </td>
                    <td className="py-2 pr-3">{customer ? customer.code : "-"}</td>
                    <td className="py-2 pr-3 text-zinc-500">{new Date(r.handoverDate).toLocaleString()}</td>
                    <td className="py-2 pr-3">
                      {typeof r.postServiceWarrantyMonths === "number" ? `${r.postServiceWarrantyMonths} mo` : "-"}
                    </td>
                    <td className="py-2 pr-3">{statusLabel[r.status] ?? r.status}</td>
                  </tr>
                );
              })}
              {rows.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No service handovers yet.
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
