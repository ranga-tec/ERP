import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";

type DispatchJobDto = {
  id: string;
  number: string;
  customerCode: string;
  customerName: string;
  equipmentSerialNumber: string;
  status: number;
  expectedCompletionAt?: string | null;
  assignedStaff: string[];
  latestProgressAt?: string | null;
  hasDailySheetToday: boolean;
  pendingDailySheets: number;
  nextAction: string;
  nextActionHref: string;
};

type TechnicianAssignmentDto = {
  assignmentId: string;
  serviceJobId: string;
  jobNumber: string;
  customerCode: string;
  equipmentSerialNumber: string;
  employeeName: string;
  role: string;
  assignedTask: string;
  assignedDate: string;
  workStartAt?: string | null;
  workEndAt?: string | null;
  normalHours: number;
  overtimeHours: number;
  approvalStatus: number;
  dailySheetId?: string | null;
  dailySheetNumber?: string | null;
  dailySheetStatus?: number | null;
  jobHref: string;
  progressHref: string;
  materialHref: string;
  iouHref: string;
  expenseHref: string;
};

type TechnicianDailySheetDto = {
  id: string;
  serviceJobId: string;
  number: string;
  jobNumber: string;
  customerCode: string;
  equipmentSerialNumber: string;
  sheetDate: string;
  preparedByName: string;
  workPlanned: string;
  workCompleted?: string | null;
  workPending?: string | null;
  status: number;
  assignmentCount: number;
  progressCount: number;
  dailySheetHref: string;
  progressHref: string;
  materialHref: string;
  iouHref: string;
  expenseHref: string;
};

type WorkbenchDto = {
  generatedAt: string;
  todayAssignments: TechnicianAssignmentDto[];
  openDailySheets: TechnicianDailySheetDto[];
  activeJobs: DispatchJobDto[];
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

const assignmentStatusLabel: Record<number, string> = {
  0: "Pending",
  1: "Approved",
  2: "Rejected",
};

const dailySheetStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
};

function dateText(value?: string | null) {
  return value ? new Date(value).toLocaleString() : "-";
}

function shortDateText(value?: string | null) {
  return value ? new Date(value).toLocaleDateString() : "-";
}

function actionLinks(item: { progressHref: string; materialHref: string; iouHref: string; expenseHref: string }) {
  return (
    <div className="flex flex-wrap gap-3 text-xs">
      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={item.progressHref}>Progress</Link>
      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={item.materialHref}>Material</Link>
      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={item.iouHref}>IOU</Link>
      <Link className="font-semibold text-[var(--link)] underline underline-offset-2" href={item.expenseHref}>Expense</Link>
    </div>
  );
}

export default async function ServiceTechnicianWorkbenchPage() {
  const workbench = await backendFetchJson<WorkbenchDto>("/service/jobs/technician-workbench");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Technician Workbench</h1>
          <p className="mt-1 text-sm text-zinc-500">Today’s assignments, open daily sheets, and quick job actions for field/service staff.</p>
        </div>
        <div className="text-xs text-zinc-500">Updated {dateText(workbench.generatedAt)}</div>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Today&apos;s Assignments</div>
          <div className="mt-2 text-2xl font-semibold">{workbench.todayAssignments.length}</div>
          <div className="mt-1 text-xs text-zinc-500">Technician labour tasks due today</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Active Daily Sheets</div>
          <div className="mt-2 text-2xl font-semibold">{workbench.openDailySheets.length}</div>
          <div className="mt-1 text-xs text-zinc-500">Draft or submitted sheets needing action</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Active Jobs</div>
          <div className="mt-2 text-2xl font-semibold">{workbench.activeJobs.length}</div>
          <div className="mt-1 text-xs text-zinc-500">Open, assigned, in-progress, or reopened jobs</div>
        </Card>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Today&apos;s Assignments</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Technician</th>
                <th className="py-2 pr-3">Task</th>
                <th className="py-2 pr-3">Sheet</th>
                <th className="py-2 pr-3">Hours</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {workbench.todayAssignments.map((assignment) => (
                <tr key={assignment.assignmentId} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3">
                    <Link className="font-mono text-xs font-semibold text-[var(--link)] underline underline-offset-2" href={assignment.jobHref}>{assignment.jobNumber}</Link>
                    <div className="mt-1 text-xs text-zinc-500">{assignment.customerCode} / {assignment.equipmentSerialNumber}</div>
                  </td>
                  <td className="py-2 pr-3">
                    <div className="font-medium">{assignment.employeeName}</div>
                    <div className="text-xs text-zinc-500">{assignment.role}</div>
                  </td>
                  <td className="py-2 pr-3 max-w-xs whitespace-pre-wrap">{assignment.assignedTask}</td>
                  <td className="py-2 pr-3">
                    {assignment.dailySheetNumber ? <span className="font-mono text-xs">{assignment.dailySheetNumber}</span> : "-"}
                    <div className="mt-1 text-xs text-zinc-500">{assignment.dailySheetStatus === null || assignment.dailySheetStatus === undefined ? "-" : dailySheetStatusLabel[assignment.dailySheetStatus] ?? assignment.dailySheetStatus}</div>
                  </td>
                  <td className="py-2 pr-3 text-xs text-zinc-500">Normal {assignment.normalHours.toFixed(2)} / OT {assignment.overtimeHours.toFixed(2)}</td>
                  <td className="py-2 pr-3">{assignmentStatusLabel[assignment.approvalStatus] ?? assignment.approvalStatus}</td>
                  <td className="py-2 pr-3">{actionLinks(assignment)}</td>
                </tr>
              ))}
              {workbench.todayAssignments.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={7}>No technician assignments recorded for today.</td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <div className="grid gap-4 xl:grid-cols-2">
        <Card>
          <div className="mb-3 text-sm font-semibold">Active Daily Sheets</div>
          <div className="space-y-3">
            {workbench.openDailySheets.map((sheet) => (
              <div key={sheet.id} className="rounded-lg border border-[var(--card-border)] p-3">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <Link className="font-mono text-sm font-semibold text-[var(--link)] underline underline-offset-2" href={sheet.dailySheetHref}>{sheet.number}</Link>
                    <div className="mt-1 text-sm">{sheet.jobNumber} / {sheet.customerCode} / {sheet.equipmentSerialNumber}</div>
                    <div className="mt-1 text-xs text-zinc-500">{dateText(sheet.sheetDate)} / {dailySheetStatusLabel[sheet.status] ?? sheet.status}</div>
                  </div>
                  <div className="text-right text-xs text-zinc-500">
                    <div>Staff {sheet.assignmentCount}</div>
                    <div>Progress {sheet.progressCount}</div>
                  </div>
                </div>
                <div className="mt-3 line-clamp-2 text-sm">{sheet.workPlanned}</div>
                <div className="mt-3">{actionLinks(sheet)}</div>
              </div>
            ))}
            {workbench.openDailySheets.length === 0 ? <div className="text-sm text-zinc-500">No active daily sheets.</div> : null}
          </div>
        </Card>

        <Card>
          <div className="mb-3 text-sm font-semibold">Active Jobs</div>
          <div className="space-y-3">
            {workbench.activeJobs.map((job) => (
              <div key={job.id} className="rounded-lg border border-[var(--card-border)] p-3">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <Link className="font-mono text-sm font-semibold text-[var(--link)] underline underline-offset-2" href={`/service/jobs/${job.id}`}>{job.number}</Link>
                    <div className="mt-1 text-sm">{job.customerCode} / {job.equipmentSerialNumber}</div>
                  </div>
                  <div className="text-xs text-zinc-500">{statusLabel[job.status] ?? job.status}</div>
                </div>
                <div className="mt-3 grid gap-2 text-xs">
                  <div className="flex justify-between gap-2"><span className="text-zinc-500">Expected</span><span>{shortDateText(job.expectedCompletionAt)}</span></div>
                  <div className="flex justify-between gap-2"><span className="text-zinc-500">Last progress</span><span>{dateText(job.latestProgressAt)}</span></div>
                  <div className="flex justify-between gap-2"><span className="text-zinc-500">Staff</span><span>{job.assignedStaff.length > 0 ? job.assignedStaff.join(", ") : "-"}</span></div>
                </div>
                <div className="mt-3">
                  <Link className="text-xs font-semibold text-[var(--link)] underline underline-offset-2" href={job.nextActionHref}>{job.nextAction}</Link>
                </div>
              </div>
            ))}
            {workbench.activeJobs.length === 0 ? <div className="text-sm text-zinc-500">No active service jobs.</div> : null}
          </div>
        </Card>
      </div>
    </div>
  );
}
