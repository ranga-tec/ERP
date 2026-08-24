import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, Table } from "@/components/ui";
import { PettyCashIouActions } from "../PettyCashIouActions";
import { MissingReceiptApprovalButton } from "@/app/(app)/service/expense-claims/MissingReceiptApprovalButton";
import {
  PettyCashIouBillAddForm,
  PettyCashIouReturnForm,
  PettyCashIouSettleActions,
} from "../PettyCashIouAccountingForms";

type PettyCashIouDto = {
  id: string;
  number: string;
  serviceJobId?: string | null;
  serviceJobNumber?: string | null;
  requestedByName: string;
  issuedToName?: string | null;
  amount: number;
  releasedAmount: number;
  remainingReleaseAmount: number;
  purpose: string;
  requestedAt: string;
  expectedSettlementAt?: string | null;
  status: number;
  reviewerName?: string | null;
  assignedApproverName?: string | null;
  assignedAt?: string | null;
  assignedApprovedAt?: string | null;
  headOfficeSubmittedAt?: string | null;
  isReviewer: boolean;
  isAssignedApprover: boolean;
  pettyCashFundId?: string | null;
  releasedAt?: string | null;
  settledAt?: string | null;
  settledAmount?: number | null;
  settlementApprovedAt?: string | null;
  settlementExceptionAmount: number;
  settlementExceptionReason?: string | null;
  settlementExceptionApprovedAt?: string | null;
  settlementExceptionExpenseClaimId?: string | null;
  issueBillNumber?: string | null;
  claimedAmount: number;
  claimCount: number;
  returnedAmount: number;
  outstandingAmount: number;
  isOpenForAccounting: boolean;
  approvalBatchId?: string | null;
};

type BillDto = {
  id: string;
  serviceExpenseClaimId: string;
  description: string;
  amount: number;
  billableToCustomer: boolean;
  receiptReference?: string | null;
  missingReceipt: boolean;
  missingReceiptReason?: string | null;
  missingReceiptApprovedAt?: string | null;
  missingReceiptApprovedByUserId?: string | null;
  voucherNumber: string;
  voucherStatus: number;
};

type CurrentPermissionsDto = { permissions: string[] };
type ServiceJobDto = { id: string; number: string; status: number };
type FundDto = { id: string; code: string; name: string; isActive: boolean };
type StaffDto = { userId: string; name: string; email?: string | null };
type ExpenseAccountDto = { id: string; code: string; name: string };

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

const voucherStatusLabel: Record<number, string> = {
  0: "Draft",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
  4: "Settled",
};

function money(value: number): string {
  return value.toFixed(2);
}

function Figure({ label, value, tone }: { label: string; value: string; tone?: "warn" | "ok" }) {
  const toneClass =
    tone === "warn"
      ? "text-amber-700 dark:text-amber-400"
      : tone === "ok"
        ? "text-emerald-700 dark:text-emerald-400"
        : "text-[var(--foreground)]";
  return (
    <div className="rounded-md border border-[var(--card-border)] bg-[var(--surface)] px-3 py-2">
      <div className="text-[11px] uppercase tracking-wide text-[var(--muted-foreground)]">{label}</div>
      <div className={`mt-0.5 font-mono text-lg tabular-nums ${toneClass}`}>{value}</div>
    </div>
  );
}

export default async function PettyCashIouDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [iou, bills, currentPermissions, jobs, funds, expenseAccounts] = await Promise.all([
    backendFetchJson<PettyCashIouDto>(`/finance/petty-cash-ious/${id}`),
    backendFetchJson<BillDto[]>(`/finance/petty-cash-ious/${id}/bills`),
    backendFetchJson<CurrentPermissionsDto>("/me/permissions"),
    backendFetchJson<ServiceJobDto[]>("/service/jobs?take=500"),
    backendFetchJson<FundDto[]>("/finance/petty-cash-funds"),
    backendFetchJson<ExpenseAccountDto[]>("/service/expense-claims/expense-accounts"),
  ]);

  const permissions = new Set(currentPermissions.permissions);
  const canAccount = iou.isOpenForAccounting && permissions.has("Finance.PettyCashIou.Settle");
  const canRelease = permissions.has("Finance.PettyCashIou.Release");
  const canReview = permissions.has("Finance.PettyCashIou.Review");
  const canApproveMissingReceipt = permissions.has("Finance.PettyCash.ReceiptExceptionApprove");
  const [staff, approvers] = await Promise.all([
    canRelease || canReview ? backendFetchJson<StaffDto[]>("/finance/petty-cash-ious/staff") : Promise.resolve([]),
    canReview ? backendFetchJson<StaffDto[]>("/finance/petty-cash-ious/approvers") : Promise.resolve([]),
  ]);
  const spent = iou.releasedAmount - iou.returnedAmount;
  const unaccounted = spent - iou.claimedAmount;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/finance/petty-cash-ious" className="hover:underline">
            Petty Cash Advances (IOU)
          </Link>{" "}
          / <span className="font-mono text-xs">{iou.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">I.O.U. {iou.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Requested by: {iou.requestedByName}</div>
          {iou.issuedToName ? <div>Collected by: {iou.issuedToName}</div> : null}
          {iou.issueBillNumber ? <div>Signed slip: {iou.issueBillNumber}</div> : null}
          <div>Status: {statusLabel[iou.status] ?? iou.status}</div>
          <div>
            Job:{" "}
            {iou.serviceJobId ? (
              <TransactionLink referenceType="SJ" referenceId={iou.serviceJobId} monospace>
                {iou.serviceJobNumber ?? "Job removed"}
              </TransactionLink>
            ) : (
              <span className="text-zinc-500">Not job related</span>
            )}
          </div>
          <div>Issued: {new Date(iou.requestedAt).toLocaleDateString()}</div>
          {iou.settlementApprovedAt ? (
            <div>Approved: {new Date(iou.settlementApprovedAt).toLocaleDateString()}</div>
          ) : null}
        </div>
        <p className="mt-2 max-w-3xl text-sm text-zinc-500">{iou.purpose}</p>
        <div className="mt-3">
          <PettyCashIouActions
            id={iou.id}
            status={iou.status}
            funds={funds.filter((fund) => fund.isActive)}
            amount={iou.amount}
            releasedAmount={iou.releasedAmount}
            pettyCashFundId={iou.pettyCashFundId ?? null}
            purpose={iou.purpose}
            expectedSettlementAt={iou.expectedSettlementAt ?? null}
            serviceJobId={iou.serviceJobId ?? null}
            serviceJobNumber={iou.serviceJobNumber ?? null}
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
        </div>
      </div>

      {(iou.reviewerName || iou.assignedApproverName) ? (
        <Card>
          <div className="text-sm font-semibold">Approval Route</div>
          <div className="mt-2 grid gap-2 text-sm sm:grid-cols-3">
            <div><span className="text-zinc-500">Receiver:</span> {iou.reviewerName ?? "-"}</div>
            <div><span className="text-zinc-500">Assigned approver:</span> {iou.assignedApproverName ?? "-"}</div>
            <div><span className="text-zinc-500">Head office:</span> {iou.headOfficeSubmittedAt ? "Submitted" : "Not submitted"}</div>
          </div>
        </Card>
      ) : null}

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Figure label="Approved" value={money(iou.amount)} />
        <Figure label="Released" value={money(iou.releasedAmount)} />
        <Figure label="Still to release" value={money(iou.remainingReleaseAmount)} tone={iou.remainingReleaseAmount > 0 ? "warn" : "ok"} />
        <Figure label="Cash returned" value={money(iou.returnedAmount)} />
        <Figure label="Bills against it" value={money(iou.claimedAmount)} />
        <Figure
          label="Unaccounted"
          value={money(unaccounted)}
          tone={unaccounted > 0 ? "warn" : "ok"}
        />
      </div>

      <Card>
        <div className="mb-1 text-sm font-semibold">Bills brought back</div>
        <div className="mb-3 text-xs text-zinc-500">
          Enter each bill here. The expense voucher is created behind this advance, so the spend reaches the job&apos;s
          cost without you leaving this page.
        </div>

        {canAccount ? (
          <div className="mb-4 rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)] p-3">
            <PettyCashIouBillAddForm iouId={iou.id} disabled={!canAccount} expenseAccounts={expenseAccounts} />
          </div>
        ) : null}

        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Description</th>
                <th className="py-2 pr-3 text-right">Amount</th>
                <th className="py-2 pr-3">Billable</th>
                <th className="py-2 pr-3">Evidence</th>
                <th className="py-2 pr-3">Voucher</th>
              </tr>
            </thead>
            <tbody>
              {bills.map((bill) => (
                <tr key={bill.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3">{bill.description}</td>
                  <td className="py-2 pr-3 text-right font-mono tabular-nums">{money(bill.amount)}</td>
                  <td className="py-2 pr-3 text-zinc-500">{bill.billableToCustomer ? "Yes" : "-"}</td>
                  <td className="py-2 pr-3 text-zinc-500">{bill.missingReceipt ? (bill.missingReceiptApprovedAt ? "Missing receipt approved" : "Missing receipt waiting") : bill.receiptReference ?? "Receipt required"}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
                    {bill.voucherNumber} · {voucherStatusLabel[bill.voucherStatus] ?? bill.voucherStatus}
                  </td>
                </tr>
              ))}
              {bills.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No bills entered against this advance yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
        <div className="mt-4 space-y-4">
          {bills.map((bill) => (
            <div key={`evidence:${bill.id}`} className="space-y-2 rounded-lg border border-[var(--card-border)] p-3">
              <div className="text-sm font-medium">Evidence — {bill.description}</div>
              {bill.missingReceipt ? <div className="text-xs text-zinc-500">Reason: {bill.missingReceiptReason ?? "Not recorded"}</div> : null}
              {bill.missingReceipt && !bill.missingReceiptApprovedAt && canApproveMissingReceipt ? <MissingReceiptApprovalButton claimId={bill.serviceExpenseClaimId} lineId={bill.id} /> : null}
              <DocumentCollaborationPanel referenceType="SEC-LINE" referenceId={bill.id} title="Receipt File & Line Evidence" />
            </div>
          ))}
        </div>
      </Card>

      <Card>
        <div className="mb-1 text-sm font-semibold">Cash returned</div>
        <div className="mb-3 text-xs text-zinc-500">
          {iou.outstandingAmount > 0
            ? `${money(iou.outstandingAmount)} of the advance is still out.`
            : "The whole advance has been returned."}
        </div>
        {canAccount && iou.outstandingAmount > 0 ? (
          <PettyCashIouReturnForm
            iouId={iou.id}
            outstandingAmount={iou.outstandingAmount}
            disabled={!canAccount}
          />
        ) : (
          <p className="text-sm text-zinc-500">
            {iou.isOpenForAccounting ? "Nothing left to return." : "This advance is closed."}
          </p>
        )}
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">Settlement</div>
        {iou.settlementExceptionExpenseClaimId ? (
          <div className="mb-3 text-sm text-zinc-600 dark:text-zinc-400">
            Approved shortage voucher:{" "}
            <TransactionLink referenceType="SEC" referenceId={iou.settlementExceptionExpenseClaimId} monospace>
              Open expense voucher
            </TransactionLink>
          </div>
        ) : null}
        <PettyCashIouSettleActions
          iouId={iou.id}
          status={iou.status}
          amount={iou.releasedAmount}
          returnedAmount={iou.returnedAmount}
          claimedAmount={iou.claimedAmount}
          remainingReleaseAmount={iou.remainingReleaseAmount}
          settlementExceptionAmount={iou.settlementExceptionAmount}
          settlementExceptionReason={iou.settlementExceptionReason}
          permissions={currentPermissions.permissions}
        />
      </Card>

      <DocumentCollaborationPanel
        referenceType="IOU"
        referenceId={id}
        title="Bill Copies, Comments & Attachments"
      />
    </div>
  );
}
