import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { WorkOrderCreateForm } from "./WorkOrderCreateForm";

type WorkOrderDto = {
  id: string;
  serviceJobId: string;
  description: string;
  assignedToUserId?: string | null;
  status: number;
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
      <div>
        <h1 className="text-2xl font-semibold">Work Orders</h1>
        <p className="mt-1 text-sm text-zinc-500">Tasks under a service job.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <WorkOrderCreateForm serviceJobs={jobs} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Work Order</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Description</th>
              </tr>
            </thead>
            <tbody>
              {workOrders.map((w) => (
                <tr key={w.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/service/work-orders/${w.id}`}>
                      {w.id.slice(0, 8)}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {jobById.get(w.serviceJobId)?.number ?? w.serviceJobId}
                  </td>
                  <td className="py-2 pr-3">{statusLabel[w.status] ?? w.status}</td>
                  <td className="py-2 pr-3 text-zinc-500">{w.description}</td>
                </tr>
              ))}
              {workOrders.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                    No work orders yet.
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

