import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { QualityCheckCreateForm } from "./QualityCheckCreateForm";

type QualityCheckDto = {
  id: string;
  serviceJobId: string;
  checkedAt: string;
  passed: boolean;
  notes?: string | null;
};

type ServiceJobDto = { id: string; number: string };

export default async function QualityChecksPage() {
  const [qcs, jobs] = await Promise.all([
    backendFetchJson<QualityCheckDto[]>("/service/quality-checks?take=100"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
  ]);

  const jobById = new Map(jobs.map((j) => [j.id, j]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Quality Checks</h1>
        <p className="mt-1 text-sm text-zinc-500">Record inspection / QA results for a service job.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <QualityCheckCreateForm serviceJobs={jobs} />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Checked</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Result</th>
                <th className="py-2 pr-3">Notes</th>
              </tr>
            </thead>
            <tbody>
              {qcs.map((q) => (
                <tr key={q.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 text-zinc-500">
                    <Link className="hover:underline" href={`/service/quality-checks/${q.id}`}>
                      {new Date(q.checkedAt).toLocaleString()}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    {jobById.get(q.serviceJobId)?.number ?? q.serviceJobId}
                  </td>
                  <td className="py-2 pr-3">{q.passed ? "Passed" : "Failed"}</td>
                  <td className="py-2 pr-3 text-zinc-500">{q.notes ?? "—"}</td>
                </tr>
              ))}
              {qcs.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={4}>
                    No quality checks yet.
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

