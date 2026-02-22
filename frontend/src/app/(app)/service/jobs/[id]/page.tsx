import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink } from "@/components/ui";
import { ServiceJobActions } from "../ServiceJobActions";

type ServiceJobDto = {
  id: string;
  number: string;
  equipmentUnitId: string;
  customerId: string;
  openedAt: string;
  problemDescription: string;
  status: number;
  completedAt?: string | null;
};

type EquipmentUnitDto = { id: string; serialNumber: string };
type CustomerDto = { id: string; code: string; name: string };

const statusLabel: Record<number, string> = {
  0: "Open",
  1: "In Progress",
  2: "Completed",
  3: "Closed",
  4: "Cancelled",
};

export default async function ServiceJobDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [job, units, customers] = await Promise.all([
    backendFetchJson<ServiceJobDto>(`/service/jobs/${id}`),
    backendFetchJson<EquipmentUnitDto[]>("/service/equipment-units?take=500"),
    backendFetchJson<CustomerDto[]>("/customers"),
  ]);

  const unitById = new Map(units.map((u) => [u.id, u]));
  const customerById = new Map(customers.map((c) => [c.id, c]));

  const canStart = job.status === 0;
  const canComplete = job.status === 0 || job.status === 1;
  const canClose = job.status === 2;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/jobs" className="hover:underline">
            Jobs
          </Link>{" "}
          / <span className="font-mono text-xs">{job.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Job {job.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Equipment:{" "}
            <span className="font-mono text-xs">
              {unitById.get(job.equipmentUnitId)?.serialNumber ?? job.equipmentUnitId}
            </span>
          </div>
          <div>Customer: {customerById.get(job.customerId)?.code ?? job.customerId}</div>
          <div>Status: {statusLabel[job.status] ?? job.status}</div>
          <div>Opened: {new Date(job.openedAt).toLocaleString()}</div>
          <div>Completed: {job.completedAt ? new Date(job.completedAt).toLocaleString() : "—"}</div>
        </div>
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/service/jobs/${job.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <ServiceJobActions jobId={job.id} canStart={canStart} canComplete={canComplete} canClose={canClose} />
      </Card>

      <Card>
        <div className="mb-2 text-sm font-semibold">Problem</div>
        <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{job.problemDescription}</div>
      </Card>

      <Card>
        <div className="text-sm text-zinc-500">
          Track work via <Link className="underline" href="/service/work-orders">Work Orders</Link> and{" "}
          <Link className="underline" href="/service/material-requisitions">Material Reqs</Link>.
        </div>
      </Card>
    </div>
  );
}
