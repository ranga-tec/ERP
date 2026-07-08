import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { TransactionLink } from "@/components/TransactionLink";
import { Card } from "@/components/ui";
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
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Inspection / QC</h1>
          <p className="mt-1 text-sm text-zinc-500">Record inspection and quality-check results for a job order.</p>
        </div>
        <AppFormModal title="Create Inspection / QC" description="Record inspection and quality-check results for a job order." buttonLabel="+ New QC">
          <QualityCheckCreateForm serviceJobs={jobs} />
        </AppFormModal>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search QC, job, result, notes..."
          emptyMessage="No quality checks yet."
          emptyColSpan={4}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Checked</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Result</th>
                <th className="py-2 pr-3">Notes</th>
              </tr>
            </thead>
          }
        >
          {qcs.map((q) => {
            const job = jobById.get(q.serviceJobId);
            const result = q.passed ? "Passed" : "Failed";
            return (
              <SearchableRow
                key={q.id}
                searchText={[job?.number, result, q.notes, new Date(q.checkedAt).toLocaleString()].filter(Boolean).join(" ")}
              >
                <tr key={q.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 text-zinc-500">
                    <Link className="hover:underline" href={`/service/quality-checks/${q.id}`}>
                      {new Date(q.checkedAt).toLocaleString()}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">
                    <TransactionLink referenceType="SJ" referenceId={q.serviceJobId} monospace>
                      {job?.number ?? q.serviceJobId}
                    </TransactionLink>
                  </td>
                  <td className="py-2 pr-3">{result}</td>
                  <td className="py-2 pr-3 text-zinc-500">{q.notes ?? "-"}</td>
                </tr>
              </SearchableRow>
            );
          })}
        </SearchableTable>
      </Card>
    </div>
  );
}
