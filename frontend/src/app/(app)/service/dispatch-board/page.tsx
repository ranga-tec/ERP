import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";

type DispatchJobDto = {
  id: string;
  number: string;
  customerCode: string;
  customerName: string;
  equipmentSerialNumber: string;
  kind: number;
  status: number;
  openedAt: string;
  expectedCompletionAt?: string | null;
  responsibleOfficerName?: string | null;
  assignedStaff: string[];
  latestProgressAt?: string | null;
  hasDailySheetToday: boolean;
  pendingDailySheets: number;
  nextAction: string;
  nextActionHref: string;
};

type DispatchBoardDto = {
  generatedAt: string;
  unassignedJobs: DispatchJobDto[];
  assignedJobs: DispatchJobDto[];
  waitingJobs: DispatchJobDto[];
  completedJobs: DispatchJobDto[];
};

const statusLabel: Record<number, string> = {
  1: "Open",
  2: "Assigned",
  3: "In Progress",
  4: "Waiting for Parts",
  5: "Waiting for Customer",
  6: "Waiting for Supplier",
  7: "Work Completed",
  8: "Pending Expenses",
  9: "Pending Materials",
  10: "Ready for Invoice",
  11: "Invoiced",
  13: "Reopened",
};

const kindLabel: Record<number, string> = {
  0: "Service",
  1: "Repair",
  2: "PDI",
  3: "Warranty",
  4: "Inspection",
};

function dateText(value?: string | null) {
  return value ? new Date(value).toLocaleString() : "-";
}

function shortDateText(value?: string | null) {
  return value ? new Date(value).toLocaleDateString() : "-";
}

function DispatchJobCard({ job }: { job: DispatchJobDto }) {
  return (
    <div className="rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <Link className="font-mono text-sm font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/jobs/${job.id}`}>
            {job.number}
          </Link>
          <div className="mt-1 text-sm font-medium">{job.customerCode} / {job.equipmentSerialNumber}</div>
          <div className="mt-1 text-xs text-zinc-500">{job.customerName}</div>
        </div>
        <div className="text-right text-[11px] text-zinc-500">
          <div>{statusLabel[job.status] ?? job.status}</div>
          <div>{kindLabel[job.kind] ?? job.kind}</div>
        </div>
      </div>

      <div className="mt-3 grid gap-2 text-xs">
        <div className="flex justify-between gap-2">
          <span className="text-zinc-500">Expected</span>
          <span>{shortDateText(job.expectedCompletionAt)}</span>
        </div>
        <div className="flex justify-between gap-2">
          <span className="text-zinc-500">Last progress</span>
          <span>{dateText(job.latestProgressAt)}</span>
        </div>
        <div className="flex justify-between gap-2">
          <span className="text-zinc-500">Responsible</span>
          <span>{job.responsibleOfficerName ?? "-"}</span>
        </div>
      </div>

      <div className="mt-3 flex flex-wrap gap-2 text-[11px] text-zinc-500">
        <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">
          {job.assignedStaff.length > 0 ? job.assignedStaff.join(", ") : "Unassigned"}
        </span>
        <span className={job.hasDailySheetToday ? "rounded-full border border-emerald-300 px-2 py-0.5 text-emerald-700 dark:border-emerald-900/50 dark:text-emerald-300" : "rounded-full border border-amber-300 px-2 py-0.5 text-amber-700 dark:border-amber-900/50 dark:text-amber-300"}>
          {job.hasDailySheetToday ? "Sheet today" : "No sheet today"}
        </span>
        {job.pendingDailySheets > 0 ? (
          <span className="rounded-full border border-amber-300 px-2 py-0.5 text-amber-700 dark:border-amber-900/50 dark:text-amber-300">
            {job.pendingDailySheets} daily pending
          </span>
        ) : null}
      </div>

      <div className="mt-3">
        <Link className="text-xs font-semibold text-[var(--link)] underline underline-offset-2" href={job.nextActionHref}>
          {job.nextAction}
        </Link>
      </div>
    </div>
  );
}

function DispatchLane({ title, detail, jobs }: { title: string; detail: string; jobs: DispatchJobDto[] }) {
  return (
    <Card>
      <div className="mb-3 flex items-start justify-between gap-3">
        <div>
          <div className="text-sm font-semibold">{title}</div>
          <div className="mt-1 text-xs text-zinc-500">{detail}</div>
        </div>
        <div className="text-sm font-semibold">{jobs.length}</div>
      </div>
      <div className="space-y-3">
        {jobs.map((job) => <DispatchJobCard key={job.id} job={job} />)}
        {jobs.length === 0 ? <div className="rounded-md border border-[var(--card-border)] p-3 text-sm text-zinc-500">No jobs in this lane.</div> : null}
      </div>
    </Card>
  );
}

export default async function ServiceDispatchBoardPage() {
  const board = await backendFetchJson<DispatchBoardDto>("/service/jobs/dispatch-board");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Service Dispatch Board</h1>
          <p className="mt-1 text-sm text-zinc-500">Assign, schedule, and monitor active service work from simple operational lanes.</p>
        </div>
        <div className="text-xs text-zinc-500">Updated {dateText(board.generatedAt)}</div>
      </div>

      <div className="grid gap-4 xl:grid-cols-4">
        <DispatchLane title="Unassigned" detail="Open jobs that need staff or technician allocation." jobs={board.unassignedJobs} />
        <DispatchLane title="Assigned / Active" detail="Jobs with staff assigned or active daily work." jobs={board.assignedJobs} />
        <DispatchLane title="Waiting" detail="Jobs blocked by parts, customer, supplier, expenses, or material return." jobs={board.waitingJobs} />
        <DispatchLane title="Completed" detail="Work completed, ready for billing or closeout." jobs={board.completedJobs} />
      </div>
    </div>
  );
}
