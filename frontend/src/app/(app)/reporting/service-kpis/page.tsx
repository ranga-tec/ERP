import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";

type ServiceKpiReport = {
  from: string;
  to: string;
  openedJobs: number;
  inProgressJobs: number;
  completedJobs: number;
  closedJobs: number;
  cancelledJobs: number;
  averageCompletionHours?: number | null;
  openJobsOlderThan7Days: number;
  openJobsOlderThan30Days: number;
  estimatesIssued: number;
  estimatesApproved: number;
  handoversCompleted: number;
  materialRequisitionsPosted: number;
  partsConsumedQuantity: number;
};

function number(value: number) {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value);
}

export default async function ServiceKpisPage() {
  const report = await backendFetchJson<ServiceKpiReport>("/reporting/service-kpis");

  const jobRows = [
    ["Open", report.openedJobs],
    ["In Progress", report.inProgressJobs],
    ["Completed", report.completedJobs],
    ["Closed", report.closedJobs],
    ["Cancelled", report.cancelledJobs],
  ] as const;

  const opsRows = [
    ["Avg completion hours", report.averageCompletionHours == null ? "n/a" : number(report.averageCompletionHours)],
    ["Open jobs > 7 days", number(report.openJobsOlderThan7Days)],
    ["Open jobs > 30 days", number(report.openJobsOlderThan30Days)],
    ["Estimates issued", number(report.estimatesIssued)],
    ["Estimates approved", number(report.estimatesApproved)],
    ["Handovers completed", number(report.handoversCompleted)],
    ["Material reqs posted", number(report.materialRequisitionsPosted)],
    ["Parts consumed qty", number(report.partsConsumedQuantity)],
  ] as const;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Service KPIs</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Service throughput and operational indicators from{" "}
          {new Date(report.from).toLocaleDateString()} to{" "}
          {new Date(report.to).toLocaleDateString()}.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card className="overflow-auto">
          <div className="mb-3 text-sm font-semibold">Job Status Distribution</div>
          <Table>
            <tbody>
              {jobRows.map(([label, value]) => (
                <tr key={label} className="border-b border-zinc-100 last:border-b-0 dark:border-zinc-900">
                  <td className="py-2 pr-3">{label}</td>
                  <td className="py-2 text-right font-semibold">{number(value)}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        </Card>

        <div className="grid gap-4 sm:grid-cols-2">
          <Card>
            <div className="text-xs uppercase tracking-wide text-zinc-500">
              Estimates Approval Rate
            </div>
            <div className="mt-2 text-2xl font-semibold">
              {report.estimatesIssued === 0
                ? "n/a"
                : `${Math.round((report.estimatesApproved / report.estimatesIssued) * 100)}%`}
            </div>
          </Card>
          <Card>
            <div className="text-xs uppercase tracking-wide text-zinc-500">
              Avg Completion Hours
            </div>
            <div className="mt-2 text-2xl font-semibold">
              {report.averageCompletionHours == null ? "n/a" : number(report.averageCompletionHours)}
            </div>
          </Card>
          <Card>
            <div className="text-xs uppercase tracking-wide text-zinc-500">
              Open &gt; 7 Days
            </div>
            <div className="mt-2 text-2xl font-semibold">{number(report.openJobsOlderThan7Days)}</div>
          </Card>
          <Card>
            <div className="text-xs uppercase tracking-wide text-zinc-500">
              Parts Consumed Qty
            </div>
            <div className="mt-2 text-2xl font-semibold">{number(report.partsConsumedQuantity)}</div>
          </Card>
        </div>
      </div>

      <Card className="overflow-auto">
        <div className="mb-3 text-sm font-semibold">Operational Metrics</div>
        <Table>
          <tbody>
            {opsRows.map(([label, value]) => (
              <tr key={label} className="border-b border-zinc-100 last:border-b-0 dark:border-zinc-900">
                <td className="py-2 pr-3">{label}</td>
                <td className="py-2 text-right font-semibold">{value}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      </Card>
    </div>
  );
}
