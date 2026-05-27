import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";

type MetricDto = { key: string; label: string; count: number };
type QueueDto = {
  activeJobs: number;
  overdueJobs: number;
  jobsWithoutDailySheetToday: number;
  jobsWithoutProgressToday: number;
  pendingDailySheets: number;
  pendingIous: number;
  pendingExpenseClaims: number;
  pendingMaterialRequests: number;
  pendingMaterialDispositions: number;
  completedAwaitingServiceTaken: number;
  serviceTakenAwaitingInvoice: number;
};
type JobCardDto = {
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
  latestProgressAt?: string | null;
  staffToday: number;
  pendingDailySheets: number;
  pendingIous: number;
  pendingExpenseClaims: number;
  pendingMaterialRequests: number;
  pendingMaterialDispositions: number;
  hasCompletedServiceTaken: boolean;
  pendingBlockers: number;
  nextAction: string;
  nextActionHref: string;
};
type DashboardDto = {
  generatedAt: string;
  statusCounts: MetricDto[];
  stageCounts: MetricDto[];
  queues: QueueDto;
  activeJobs: JobCardDto[];
  financialQueue: JobCardDto[];
  billingQueue: JobCardDto[];
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Open",
  2: "Assigned",
  3: "In Progress",
  4: "Waiting for Parts",
  5: "Waiting for Customer Approval",
  6: "Waiting for Supplier",
  7: "Work Completed",
  8: "Pending Expense Settlement",
  9: "Pending Material Return",
  10: "Ready for Invoice",
  11: "Invoiced",
  12: "Closed",
  13: "Reopened",
  14: "Cancelled",
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

function MetricCard({ label, value, detail, href, tone = "neutral" }: { label: string; value: number | string; detail: string; href: string; tone?: "neutral" | "warn" | "good" }) {
  const toneClass = {
    neutral: "border-[var(--card-border)]",
    warn: "border-amber-300 bg-amber-50/60 dark:border-amber-900/50 dark:bg-amber-950/20",
    good: "border-emerald-300 bg-emerald-50/60 dark:border-emerald-900/50 dark:bg-emerald-950/20",
  }[tone];

  return (
    <Link href={href} className={`rounded-lg border p-3 transition hover:border-[var(--link)] ${toneClass}`}>
      <div className="text-xs font-semibold uppercase tracking-wide text-zinc-500">{label}</div>
      <div className="mt-2 text-2xl font-semibold">{value}</div>
      <div className="mt-1 text-xs text-zinc-500">{detail}</div>
    </Link>
  );
}

function StageBar({ metrics }: { metrics: MetricDto[] }) {
  const max = Math.max(1, ...metrics.map((metric) => metric.count));
  return (
    <div className="space-y-3">
      {metrics.map((metric) => (
        <div key={metric.key}>
          <div className="mb-1 flex items-center justify-between gap-3 text-sm">
            <span className="font-medium">{metric.label}</span>
            <span className="text-zinc-500">{metric.count}</span>
          </div>
          <div className="h-2 overflow-hidden rounded-full bg-zinc-100 dark:bg-zinc-900">
            <div className="h-full rounded-full bg-[var(--link)]" style={{ width: `${Math.max(4, Math.round((metric.count / max) * 100))}%` }} />
          </div>
        </div>
      ))}
    </div>
  );
}

function JobCard({ job }: { job: JobCardDto }) {
  const blockerCount = job.pendingDailySheets + job.pendingIous + job.pendingExpenseClaims + job.pendingMaterialRequests + job.pendingMaterialDispositions;
  return (
    <div className="rounded-lg border border-[var(--card-border)] p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link className="font-mono text-sm font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/jobs/${job.id}`}>
            {job.number}
          </Link>
          <div className="mt-1 text-sm font-medium">{job.customerCode} / {job.equipmentSerialNumber}</div>
          <div className="mt-1 text-xs text-zinc-500">{job.customerName}</div>
        </div>
        <div className="text-right text-xs text-zinc-500">
          <div>{statusLabel[job.status] ?? job.status}</div>
          <div>{kindLabel[job.kind] ?? job.kind}</div>
        </div>
      </div>

      <div className="mt-4 grid gap-2 sm:grid-cols-2">
        <div className="rounded-md bg-[var(--surface-soft)] p-2 text-xs">
          <div className="font-semibold text-zinc-500">Expected</div>
          <div className="mt-1">{shortDateText(job.expectedCompletionAt)}</div>
        </div>
        <div className="rounded-md bg-[var(--surface-soft)] p-2 text-xs">
          <div className="font-semibold text-zinc-500">Last Progress</div>
          <div className="mt-1">{dateText(job.latestProgressAt)}</div>
        </div>
      </div>

      <div className="mt-3 flex flex-wrap gap-2 text-[11px] text-zinc-500">
        <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">Staff today {job.staffToday}</span>
        <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">Daily {job.pendingDailySheets}</span>
        <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">Cash {job.pendingIous + job.pendingExpenseClaims}</span>
        <span className="rounded-full border border-[var(--card-border)] px-2 py-0.5">Materials {job.pendingMaterialRequests + job.pendingMaterialDispositions}</span>
        <span className={blockerCount > 0 ? "rounded-full border border-amber-300 px-2 py-0.5 text-amber-700 dark:border-amber-900/50 dark:text-amber-300" : "rounded-full border border-emerald-300 px-2 py-0.5 text-emerald-700 dark:border-emerald-900/50 dark:text-emerald-300"}>
          {blockerCount > 0 ? `${blockerCount} blockers` : "No blockers"}
        </span>
      </div>

      <div className="mt-4 flex items-center justify-between gap-3">
        <div className="text-xs text-zinc-500">Responsible: {job.responsibleOfficerName ?? "-"}</div>
        <Link className="text-xs font-semibold text-[var(--link)] underline underline-offset-2" href={job.nextActionHref}>
          {job.nextAction}
        </Link>
      </div>
    </div>
  );
}

function QueueTable({ jobs, emptyText }: { jobs: JobCardDto[]; emptyText: string }) {
  return (
    <div className="overflow-auto">
      <Table>
        <thead>
          <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
            <th className="py-2 pr-3">Job</th>
            <th className="py-2 pr-3">Customer</th>
            <th className="py-2 pr-3">Status</th>
            <th className="py-2 pr-3">Cash</th>
            <th className="py-2 pr-3">Materials</th>
            <th className="py-2 pr-3">Next</th>
          </tr>
        </thead>
        <tbody>
          {jobs.map((job) => (
            <tr key={job.id} className="border-b border-zinc-100 dark:border-zinc-900">
              <td className="py-2 pr-3 font-mono text-xs">
                <Link className="text-[var(--link)] underline underline-offset-2" href={`/service/jobs/${job.id}`}>
                  {job.number}
                </Link>
              </td>
              <td className="py-2 pr-3">{job.customerCode}</td>
              <td className="py-2 pr-3">{statusLabel[job.status] ?? job.status}</td>
              <td className="py-2 pr-3 text-zinc-500">{job.pendingIous + job.pendingExpenseClaims}</td>
              <td className="py-2 pr-3 text-zinc-500">{job.pendingMaterialRequests + job.pendingMaterialDispositions}</td>
              <td className="py-2 pr-3">
                <Link className="text-xs font-semibold text-[var(--link)] underline underline-offset-2" href={job.nextActionHref}>
                  {job.nextAction}
                </Link>
              </td>
            </tr>
          ))}
          {jobs.length === 0 ? (
            <tr>
              <td className="py-6 text-sm text-zinc-500" colSpan={6}>{emptyText}</td>
            </tr>
          ) : null}
        </tbody>
      </Table>
    </div>
  );
}

export default async function ServiceCommandCenterPage() {
  const dashboard = await backendFetchJson<DashboardDto>("/service/jobs/dashboard");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Service Command Center</h1>
          <p className="mt-1 text-sm text-zinc-500">Active service jobs, operational blockers, daily work gaps, finance queues, and billing readiness.</p>
        </div>
        <div className="text-xs text-zinc-500">Updated {dateText(dashboard.generatedAt)}</div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Active Jobs" value={dashboard.queues.activeJobs} detail="Open service workload" href="/service/jobs" />
        <MetricCard label="Overdue" value={dashboard.queues.overdueJobs} detail="Expected completion has passed" href="#active-jobs" tone={dashboard.queues.overdueJobs > 0 ? "warn" : "good"} />
        <MetricCard label="No Sheet Today" value={dashboard.queues.jobsWithoutDailySheetToday} detail="Active jobs without today's daily sheet" href="#active-jobs" tone={dashboard.queues.jobsWithoutDailySheetToday > 0 ? "warn" : "good"} />
        <MetricCard label="No Progress Today" value={dashboard.queues.jobsWithoutProgressToday} detail="Jobs without a progress update today" href="#active-jobs" tone={dashboard.queues.jobsWithoutProgressToday > 0 ? "warn" : "good"} />
        <MetricCard label="Daily Sheets" value={dashboard.queues.pendingDailySheets} detail="Draft or submitted daily sheets" href="#active-jobs" tone={dashboard.queues.pendingDailySheets > 0 ? "warn" : "good"} />
        <MetricCard label="IOUs" value={dashboard.queues.pendingIous} detail="Submitted, approved, or released advances" href="#finance-queue" tone={dashboard.queues.pendingIous > 0 ? "warn" : "good"} />
        <MetricCard label="Claims" value={dashboard.queues.pendingExpenseClaims} detail="Submitted or approved expenses" href="#finance-queue" tone={dashboard.queues.pendingExpenseClaims > 0 ? "warn" : "good"} />
        <MetricCard label="Billing Ready" value={dashboard.queues.serviceTakenAwaitingInvoice} detail="Service taken completed and awaiting invoice" href="#billing-queue" tone={dashboard.queues.serviceTakenAwaitingInvoice > 0 ? "warn" : "good"} />
      </div>

      <div className="grid gap-4 xl:grid-cols-2">
        <Card>
          <div className="mb-4 text-sm font-semibold">Jobs By Stage</div>
          <StageBar metrics={dashboard.stageCounts} />
        </Card>
        <Card>
          <div className="mb-4 text-sm font-semibold">Jobs By Status</div>
          <StageBar metrics={dashboard.statusCounts.filter((metric) => metric.count > 0)} />
        </Card>
      </div>

      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3" id="active-jobs">
          <div>
            <div className="text-sm font-semibold">Active Job Cards</div>
            <div className="mt-1 text-xs text-zinc-500">Sorted by expected completion and opening date, with the next practical action surfaced on each job.</div>
          </div>
          <Link className="text-sm font-semibold text-[var(--link)] underline underline-offset-2" href="/service/jobs">
            Open job list
          </Link>
        </div>
        <div className="grid gap-3 xl:grid-cols-2">
          {dashboard.activeJobs.map((job) => <JobCard key={job.id} job={job} />)}
          {dashboard.activeJobs.length === 0 ? <div className="text-sm text-zinc-500">No active service jobs.</div> : null}
        </div>
      </Card>

      <div className="grid gap-4 xl:grid-cols-2">
        <Card>
          <div className="mb-3 text-sm font-semibold" id="finance-queue">Finance Queue</div>
          <QueueTable jobs={dashboard.financialQueue} emptyText="No service jobs currently need IOU or expense follow-up." />
        </Card>
        <Card>
          <div className="mb-3 text-sm font-semibold" id="billing-queue">Billing / Closeout Queue</div>
          <QueueTable jobs={dashboard.billingQueue} emptyText="No service jobs are currently waiting for service taken or invoice follow-up." />
        </Card>
      </div>
    </div>
  );
}
