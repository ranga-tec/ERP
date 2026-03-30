import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink, Table } from "@/components/ui";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { TransactionLink } from "@/components/TransactionLink";
import { WorkOrderTimeEntryAddForm } from "../WorkOrderTimeEntryAddForm";
import { WorkOrderTimeEntryRow } from "../WorkOrderTimeEntryRow";

type WorkOrderDto = {
  id: string;
  serviceJobId: string;
  description: string;
  assignedToUserId?: string | null;
  status: number;
  approvedHours: number;
  approvedLaborCost: number;
  pendingLaborCost: number;
  billableApprovedAmount: number;
  timeEntries: {
    id: string;
    technicianName: string;
    workDate: string;
    workDescription: string;
    hoursWorked: number;
    costRate: number;
    laborCost: number;
    billableToCustomer: boolean;
    billableHours: number;
    billingRate: number;
    taxPercent: number;
    billableTotal: number;
    effectiveBillableTotal: number;
    notes?: string | null;
    status: number;
    rejectionReason?: string | null;
    salesInvoiceId?: string | null;
  }[];
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
  const canAddLabor = wo.status !== 3;

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
            Job:{" "}
            <TransactionLink referenceType="SJ" referenceId={wo.serviceJobId} monospace>
              {jobById.get(wo.serviceJobId)?.number ?? wo.serviceJobId}
            </TransactionLink>
          </div>
          <div>Status: {statusLabel[wo.status] ?? wo.status}</div>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Approved Hours</div>
          <div className="mt-2 text-2xl font-semibold">{wo.approvedHours.toFixed(2)}</div>
          <div className="mt-1 text-xs text-zinc-500">Approved or invoiced labor only</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Approved Labor Cost</div>
          <div className="mt-2 text-2xl font-semibold">{wo.approvedLaborCost.toFixed(2)}</div>
          <div className="mt-1 text-xs text-zinc-500">Actual labor cost posted into the job</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Pending Approval Cost</div>
          <div className="mt-2 text-2xl font-semibold">{wo.pendingLaborCost.toFixed(2)}</div>
          <div className="mt-1 text-xs text-zinc-500">Submitted labor awaiting approval</div>
        </Card>
        <Card>
          <div className="text-xs uppercase tracking-wide text-zinc-500">Approved Billable Labor</div>
          <div className="mt-2 text-2xl font-semibold">{wo.billableApprovedAmount.toFixed(2)}</div>
          <div className="mt-1 text-xs text-zinc-500">Coverage-adjusted amount used by handover invoice conversion when billing actual labor</div>
        </Card>
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

      <Card>
        <div className="mb-3 text-sm font-semibold">Add Labor Entry</div>
        <WorkOrderTimeEntryAddForm workOrderId={wo.id} disabled={!canAddLabor} />
        <div className="mt-3 text-xs text-zinc-500">
          Warranty or contract coverage can reduce approved labor billing to zero even when the technician entry is marked billable.
        </div>
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Labor Entries</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Technician</th>
                <th className="py-2 pr-3">Work</th>
                <th className="py-2 pr-3">Hours</th>
                <th className="py-2 pr-3">Cost Rate</th>
                <th className="py-2 pr-3">Labor Cost</th>
                <th className="py-2 pr-3">Billable</th>
                <th className="py-2 pr-3">Billable Total</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Invoice</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {wo.timeEntries.map((entry) => (
                <WorkOrderTimeEntryRow key={entry.id} workOrderId={wo.id} entry={entry} />
              ))}
              {wo.timeEntries.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={11}>
                    No labor entries yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>

      <DocumentCollaborationPanel referenceType="WO" referenceId={id} />
    </div>
  );
}
