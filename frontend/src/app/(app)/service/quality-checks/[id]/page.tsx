import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink } from "@/components/ui";

type QualityCheckDto = {
  id: string;
  serviceJobId: string;
  checkedAt: string;
  passed: boolean;
  notes?: string | null;
};

type ServiceJobDto = { id: string; number: string };

export default async function QualityCheckDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [qc, jobs] = await Promise.all([
    backendFetchJson<QualityCheckDto>(`/service/quality-checks/${id}`),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/service/quality-checks" className="hover:underline">
            Quality Checks
          </Link>{" "}
          / <span className="font-mono text-xs">{qc.id.slice(0, 8)}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Quality Check</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>
            Job: <span className="font-mono text-xs">{jobById.get(qc.serviceJobId)?.number ?? qc.serviceJobId}</span>
          </div>
          <div>Checked: {new Date(qc.checkedAt).toLocaleString()}</div>
          <div>Result: {qc.passed ? "Passed" : "Failed"}</div>
        </div>
      </div>

      <Card>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/service/quality-checks/${qc.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
      </Card>

      <Card>
        <div className="mb-2 text-sm font-semibold">Notes</div>
        <div className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-200">{qc.notes ?? "—"}</div>
      </Card>
    </div>
  );
}
