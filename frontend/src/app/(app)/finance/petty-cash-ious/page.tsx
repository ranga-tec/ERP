import { backendFetchJson } from "@/lib/backend.server";
import Link from "next/link";
import { AppFormModal } from "@/components/AppFormModal";
import { TableSearchInput } from "@/components/TableSearchInput";
import { Card, Table } from "@/components/ui";
import { PettyCashIouActions } from "./PettyCashIouActions";
import { PettyCashIouCreateForm } from "./PettyCashIouCreateForm";
import { PettyCashIouBatchPanel, type PettyCashIouBatch } from "./PettyCashIouBatchPanel";

type ServiceJobDto = { id: string; number: string; status: number };
type FundDto = { id: string; code: string; name: string; isActive: boolean };
type CurrentPermissionsDto = { permissions: string[] };
type StaffDto = { userId: string; name: string; email?: string | null };
type PettyCashIouDto = {
  id: string;
  number: string;
  serviceJobId?: string | null;
  serviceJobNumber?: string | null;
  requestedByName: string;
  issuedToName?: string | null;
  amount: number;
  releasedAmount: number;
  purpose: string;
  requestedAt: string;
  expectedSettlementAt?: string | null;
  status: number;
  reviewerName?: string | null;
  assignedApproverName?: string | null;
  isReviewer: boolean;
  isAssignedApprover: boolean;
  pettyCashFundId?: string | null;
  settledAmount?: number | null;
  claimedAmount: number;
  claimCount: number;
  returnedAmount?: number | null;
  unaccountedAmount?: number | null;
  issueBillNumber?: string | null;
  approvalBatchId?: string | null;
};

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Cash Released",
  4: "Settled / Accounted",
  5: "Rejected",
  6: "Cancelled",
  7: "Settlement Approved",
  8: "With Assigned Approver",
  9: "Returned to Receiver",
  10: "Awaiting Head Office",
};

export default async function PettyCashIousPage() {
  const [jobs, funds, ious, batches, currentPermissions] = await Promise.all([
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<FundDto[]>("/finance/petty-cash-funds"),
    backendFetchJson<PettyCashIouDto[]>("/finance/petty-cash-ious?take=200"),
    backendFetchJson<PettyCashIouBatch[]>("/finance/petty-cash-iou-batches?take=100"),
    backendFetchJson<CurrentPermissionsDto>("/me/permissions"),
  ]);
  const activeFunds = funds.filter((fund) => fund.isActive);
  const permissions = new Set(currentPermissions.permissions);
  const canCreate = permissions.has("Finance.PettyCashIou.Create");
  const canRelease = permissions.has("Finance.PettyCashIou.Release");
  const canReview = permissions.has("Finance.PettyCashIou.Review");
  const [staff, approvers] = await Promise.all([
    canRelease || canReview
      ? backendFetchJson<StaffDto[]>("/finance/petty-cash-ious/staff")
      : Promise.resolve([]),
    canReview
      ? backendFetchJson<StaffDto[]>("/finance/petty-cash-ious/approvers")
      : Promise.resolve([]),
  ]);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Petty Cash Advances (IOU)</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Cash issued from a fund <em>before</em> the spend, against a job order. The holder later settles it: unspent cash returns to the fund, and what was spent should be documented on expense vouchers linked to this advance.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {canCreate ? (
            <AppFormModal title="Create Petty Cash Advance" description="Request cash up front against a job order, to be settled and accounted for later." buttonLabel="+ New IOU">
              <PettyCashIouCreateForm serviceJobs={jobs.filter((job) => job.status !== 3 && job.status !== 4)} />
            </AppFormModal>
          ) : null}
        </div>
      </div>

      <PettyCashIouBatchPanel
        batches={batches}
        ious={ious.map((iou) => ({
          id: iou.id,
          number: iou.number,
          serviceJobId: iou.serviceJobId,
          serviceJobNumber: iou.serviceJobNumber
            ?? (iou.serviceJobId ? jobs.find((job) => job.id === iou.serviceJobId)?.number : null),
          requestedByName: iou.requestedByName,
          amount: iou.amount,
          purpose: iou.purpose,
          status: iou.status,
        }))}
        funds={activeFunds}
        approvers={approvers}
        permissions={currentPermissions.permissions}
      />

      <Card>
        <div className="mb-3 text-sm font-semibold">IOUs</div>
        <TableSearchInput placeholder="Search petty cash IOUs..." />
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Number</th>
                <th className="py-2 pr-3">Job</th>
                <th className="py-2 pr-3">Requester</th>
                <th className="py-2 pr-3">Advanced</th>
                <th className="py-2 pr-3">Settled</th>
                <th className="py-2 pr-3">Claimed</th>
                <th className="py-2 pr-3">Unaccounted</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Purpose</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {ious.map((iou) => (
                <tr key={iou.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link href={`/finance/petty-cash-ious/${iou.id}`} className="text-blue-700 hover:underline dark:text-blue-300">
                      {iou.number}
                    </Link>
                    {iou.issueBillNumber ? (
                      <div className="mt-0.5 text-[11px] text-zinc-500">Slip {iou.issueBillNumber}</div>
                    ) : null}
                  </td>
                  <td className="py-2 pr-3">
                    {iou.serviceJobId
                      ? jobs.find((job) => job.id === iou.serviceJobId)?.number ?? "Job removed"
                      : <span className="text-zinc-400">-</span>}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {iou.requestedByName}
                    {iou.issuedToName ? (
                      <div className="text-xs text-zinc-500">Collected by {iou.issuedToName}</div>
                    ) : null}
                  </td>
                  <td className="py-2 pr-3">{iou.amount.toFixed(2)}</td>
                  <td className="py-2 pr-3">
                    {iou.settledAmount == null ? (
                      <span className="text-zinc-400">-</span>
                    ) : (
                      <>
                        {iou.settledAmount.toFixed(2)}
                        {iou.returnedAmount ? (
                          <div className="text-xs text-zinc-500">{iou.returnedAmount.toFixed(2)} returned</div>
                        ) : null}
                      </>
                    )}
                  </td>
                  <td className="py-2 pr-3">
                    {iou.claimedAmount.toFixed(2)}
                    <div className="text-xs text-zinc-500">
                      {iou.claimCount === 0 ? "no vouchers" : `${iou.claimCount} voucher${iou.claimCount === 1 ? "" : "s"}`}
                    </div>
                  </td>
                  <td className="py-2 pr-3">
                    {iou.unaccountedAmount == null ? (
                      <span className="text-xs text-zinc-400">not settled</span>
                    ) : iou.unaccountedAmount === 0 ? (
                      <span className="text-xs text-zinc-500">reconciled</span>
                    ) : (
                      <span
                        className="font-semibold text-amber-700 dark:text-amber-400"
                        title="Declared as spent at settlement but never documented on a petty cash voucher, so it never reached job cost."
                      >
                        {iou.unaccountedAmount.toFixed(2)}
                      </span>
                    )}
                  </td>
                  <td className="py-2 pr-3">{statusLabel[iou.status] ?? iou.status}</td>
                  <td className="max-w-sm py-2 pr-3 text-zinc-500">{iou.purpose}</td>
                  <td className="py-2 pr-3">
                    <PettyCashIouActions
                      id={iou.id}
                      status={iou.status}
                      funds={activeFunds}
                      amount={iou.amount}
                      releasedAmount={iou.releasedAmount}
                      pettyCashFundId={iou.pettyCashFundId ?? null}
                      purpose={iou.purpose}
                      expectedSettlementAt={iou.expectedSettlementAt ?? null}
                      serviceJobId={iou.serviceJobId ?? null}
                      serviceJobNumber={
                        iou.serviceJobId
                          ? jobs.find((job) => job.id === iou.serviceJobId)?.number ?? null
                          : null
                      }
                      serviceJobs={jobs.filter((job) => job.status !== 3 && job.status !== 4)}
                      staff={staff}
                      approvers={approvers}
                      reviewerName={iou.reviewerName ?? null}
                      assignedApproverName={iou.assignedApproverName ?? null}
                      isReviewer={iou.isReviewer}
                      isAssignedApprover={iou.isAssignedApprover}
                      permissions={currentPermissions.permissions}
                      approvalBatchId={iou.approvalBatchId ?? null}
                    />
                  </td>
                </tr>
              ))}
              {ious.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={10}>No petty cash IOUs yet.</td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
