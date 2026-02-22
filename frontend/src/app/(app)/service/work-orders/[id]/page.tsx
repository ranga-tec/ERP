import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink } from "@/components/ui";

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

export default async function WorkOrderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [wo, jobs] = await Promise.all([
    backendFetchJson<WorkOrderDto>(`/service/work-orders/${id}`),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/work-orders" className="hover:underline">
            Work Orders
          </Link>{" "}
          / <span className="font-mono text-xs">{wo.id.slice(0, 8)}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Work Order {wo.id.slice(0, 8)}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Job: <span className="font-mono text-xs">{jobById.get(wo.serviceJobId)?.number ?? wo.serviceJobId}</span>
          </div>
          <div>Status: {statusLabel[wo.status] ?? wo.status}</div>
        </div>
      </div>

      <Card>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/service/work-orders/${wo.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
      </Card>

      <Card>
        <div className="mb-2 text-sm font-semibold">Description</div>
        <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{wo.description}</div>
      </Card>
    </div>
  );
}
