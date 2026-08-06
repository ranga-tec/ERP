"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { Button, Input, SecondaryButton, Select, Textarea } from "@/components/ui";
import { PettyCashIouEditForm } from "./PettyCashIouEditForm";

type FundRef = { id: string; code: string; name: string };
type StaffRef = { userId: string; name: string; email?: string | null };
type ServiceJobRef = { id: string; number: string };
type FundedCategoryRef = {
  id: string;
  requestNumber: string;
  pettyCashFundId: string;
  category: number;
  serviceJobId?: string | null;
  serviceJobNumber?: string | null;
  purpose: string;
  availableBalance: number;
};
type Pending = "assign" | "release" | "reject" | null;

function money(value: number): string {
  return value.toFixed(2);
}

export function PettyCashIouActions({
  id,
  status,
  funds,
  amount,
  purpose,
  expectedSettlementAt,
  serviceJobId,
  serviceJobNumber,
  serviceJobs,
  staff,
  approvers,
  reviewerName,
  assignedApproverName,
  isReviewer,
  isAssignedApprover,
  fundedCategories,
  permissions,
}: {
  id: string;
  status: number;
  funds: FundRef[];
  amount: number;
  purpose: string;
  expectedSettlementAt: string | null;
  serviceJobId: string | null;
  serviceJobNumber: string | null;
  serviceJobs: ServiceJobRef[];
  staff: StaffRef[];
  approvers: StaffRef[];
  reviewerName: string | null;
  assignedApproverName: string | null;
  isReviewer: boolean;
  isAssignedApprover: boolean;
  fundedCategories: FundedCategoryRef[];
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const canReview = permissionSet.has("Finance.PettyCashIou.Review");
  const canAssignedApprove = permissionSet.has("Finance.PettyCashIou.AssignedApprove");
  const canEdit = (status === 0 || status === 1 || (status === 8 && isAssignedApprover))
    && permissionSet.has("Finance.PettyCashIou.Edit");
  const canApprove = permissionSet.has("Finance.PettyCashIou.Approve");
  const canReject = permissionSet.has("Finance.PettyCashIou.Reject");
  const canRelease = permissionSet.has("Finance.PettyCashIou.Release");
  const canApproveSettlement = status === 4 && permissionSet.has("Finance.PettyCashIou.Approve");
  const [requestLineId, setRequestLineId] = useState("");
  const [assignedApproverUserId, setAssignedApproverUserId] = useState("");
  const [issuedToUserId, setIssuedToUserId] = useState("");
  const [issueBillNumber, setIssueBillNumber] = useState("");
  const [rejectReason, setRejectReason] = useState("");
  const [pending, setPending] = useState<Pending>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(action: string, body: object = {}) {
    setError(null);
    setBusy(action);
    try {
      await apiPostNoContent(`finance/petty-cash-ious/${id}/${action}`, body);
      setPending(null);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  const availableCategories = fundedCategories.filter(
    (line) =>
      line.category === 1
      && line.serviceJobId === serviceJobId
      && funds.some((fund) => fund.id === line.pettyCashFundId),
  );
  const selectedCategory = availableCategories.find((line) => line.id === requestLineId);
  const fundLabel = funds.find((fund) => fund.id === selectedCategory?.pettyCashFundId);
  const releaseInvalid =
    !selectedCategory
    || selectedCategory.availableBalance < amount
    || issuedToUserId.length === 0
    || issueBillNumber.trim().length === 0;

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-end gap-2">
        {canEdit ? (
          <AppFormModal
            title="Edit Petty Cash Advance"
            description="Change this IOU before it is approved."
            buttonLabel="Edit"
            variant="secondary"
          >
            {({ close }) => (
              <PettyCashIouEditForm
                iou={{ id, serviceJobId, amount, purpose, expectedSettlementAt }}
                serviceJobs={serviceJobs}
                onSaved={close}
              />
            )}
          </AppFormModal>
        ) : null}

        {status === 1 && canReview ? (
          <Button type="button" disabled={busy !== null} onClick={() => setPending("assign")}>Send for Approval</Button>
        ) : null}

        {status === 8 && isAssignedApprover && canAssignedApprove ? (
          <Button type="button" disabled={busy !== null} onClick={() => void run("approve-assigned")}>
            {busy === "approve-assigned" ? "Approving..." : "Approve & Return"}
          </Button>
        ) : null}

        {status === 9 && isReviewer && canReview ? (
          <Button type="button" disabled={busy !== null} onClick={() => void run("submit-head-office")}>
            {busy === "submit-head-office" ? "Submitting..." : "Submit to Head Office"}
          </Button>
        ) : null}

        {status === 10 && canApprove ? (
          <Button type="button" disabled={busy !== null} onClick={() => void run("approve")}>
            {busy === "approve" ? "Approving..." : "Head Office Approve"}
          </Button>
        ) : null}

        {status === 10 && canReject ? (
          <SecondaryButton type="button" disabled={busy !== null} onClick={() => setPending("reject")}>Reject</SecondaryButton>
        ) : null}

        {status === 2 && canRelease ? (
          <Button type="button" disabled={busy !== null} onClick={() => setPending("release")}>
            Release Cash
          </Button>
        ) : null}

        {canApproveSettlement ? (
          <Button type="button" disabled={busy !== null} onClick={() => void run("approve-settlement")}>
            {busy === "approve-settlement" ? "Approving..." : "Approve Settlement"}
          </Button>
        ) : null}

        {((status === 1 && !canReview)
          || (status === 8 && (!isAssignedApprover || !canAssignedApprove))
          || (status === 9 && (!isReviewer || !canReview))
          || (status === 10 && !canApprove && !canReject)
          || (status === 2 && !canRelease)
          || status === 3
          || (status === 4 && !canApproveSettlement)) ? (
          <span className="text-xs text-zinc-500">View only</span>
        ) : null}
      </div>

      {status === 8 && assignedApproverName ? <div className="text-xs text-zinc-500">Assigned to {assignedApproverName}</div> : null}
      {status === 9 && reviewerName ? <div className="text-xs text-zinc-500">Returned to {reviewerName}</div> : null}

      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}

      {pending === "assign" ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4">
          <div role="dialog" aria-modal="true" className="w-full max-w-md rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl">
            <div className="text-base font-semibold">Send IOU for operational approval</div>
            <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
              The selected person can review and edit this request. After approval it returns to you for head-office submission.
            </p>
            <label className="mt-4 block text-sm font-medium">Assigned approver</label>
            <Select className="mt-1" value={assignedApproverUserId} onChange={(event) => setAssignedApproverUserId(event.target.value)}>
              <option value="" disabled>Select an authorized approver...</option>
              {approvers.map((person) => (
                <option key={person.userId} value={person.userId}>{person.name}{person.email ? ` - ${person.email}` : ""}</option>
              ))}
            </Select>
            {approvers.length === 0 ? <p className="mt-2 text-xs text-amber-700 dark:text-amber-300">No other active user has assigned-IOU approval permission.</p> : null}
            <div className="mt-4 flex justify-end gap-2">
              <SecondaryButton type="button" disabled={busy === "assign"} onClick={() => setPending(null)}>Cancel</SecondaryButton>
              <Button
                type="button"
                disabled={busy === "assign" || assignedApproverUserId.length === 0}
                onClick={() => void run("assign", { assignedApproverUserId })}
              >
                {busy === "assign" ? "Sending..." : "Send for Approval"}
              </Button>
            </div>
          </div>
        </div>
      ) : null}

      {pending === "release" ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4">
          <div role="dialog" aria-modal="true" className="w-full max-w-lg rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl">
            <div className="text-base font-semibold">Release petty cash advance</div>
            <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
              Record the physical handover for job <span className="font-semibold">{serviceJobNumber ?? "Unknown"}</span>.
            </div>

            <div className="mt-4 space-y-3">
              <div>
                <label className="mb-1 block text-sm font-medium">Funded job category</label>
                <Select value={requestLineId} onChange={(event) => setRequestLineId(event.target.value)} required>
                  <option value="" disabled>Select the approved job funding...</option>
                  {availableCategories.map((line) => {
                    const enough = line.availableBalance >= amount;
                    return (
                      <option key={line.id} value={line.id} disabled={!enough}>
                        {line.requestNumber} - {line.purpose} - {money(line.availableBalance)} available
                        {enough ? "" : " (insufficient)"}
                      </option>
                    );
                  })}
                </Select>
                {availableCategories.length === 0 ? (
                  <p className="mt-1 text-xs text-amber-700 dark:text-amber-300">
                    This job has no funded Job Wise category. Head office must fund one before cash can be released.
                  </p>
                ) : null}
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">Collected by</label>
                <Select value={issuedToUserId} onChange={(event) => setIssuedToUserId(event.target.value)} required>
                  <option value="" disabled>Select staff member...</option>
                  {staff.map((person) => (
                    <option key={person.userId} value={person.userId}>
                      {person.name}{person.email ? ` - ${person.email}` : ""}
                    </option>
                  ))}
                </Select>
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">IOU slip number</label>
                <Input
                  value={issueBillNumber}
                  onChange={(event) => setIssueBillNumber(event.target.value)}
                  placeholder="Number on the signed physical slip"
                  required
                />
              </div>

              <div className="rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)] p-3 text-sm">
                Release <span className="font-semibold">{money(amount)}</span> from{" "}
                <span className="font-semibold">
                  {fundLabel ? `${fundLabel.code} - ${fundLabel.name}` : "the selected funded category"}
                </span>.
              </div>
            </div>

            <div className="mt-4 flex flex-wrap justify-end gap-2">
              <SecondaryButton type="button" disabled={busy === "release"} onClick={() => setPending(null)}>
                Cancel
              </SecondaryButton>
              <Button
                type="button"
                disabled={busy === "release" || releaseInvalid}
                onClick={() => void run("release", {
                  pettyCashFundId: selectedCategory?.pettyCashFundId,
                  issueBillNumber: issueBillNumber.trim(),
                  issuedToUserId,
                  pettyCashRequestLineId: selectedCategory?.id,
                })}
              >
                {busy === "release" ? "Releasing..." : "Release Cash"}
              </Button>
            </div>
          </div>
        </div>
      ) : null}

      {pending === "reject" ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4">
          <div role="dialog" aria-modal="true" className="w-full max-w-md rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl">
            <div className="text-base font-semibold">Reject IOU request</div>
            <div className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
              Give the requester a reason. This is recorded against the IOU.
            </div>
            <label className="mt-4 block text-sm font-medium">Reason</label>
            <Textarea
              className="mt-1"
              value={rejectReason}
              onChange={(event) => setRejectReason(event.target.value)}
              placeholder="Why is this being rejected?"
              autoFocus
              disabled={busy === "reject"}
            />
            <div className="mt-4 flex flex-wrap justify-end gap-2">
              <SecondaryButton type="button" disabled={busy === "reject"} onClick={() => setPending(null)}>
                Cancel
              </SecondaryButton>
              <Button
                type="button"
                disabled={busy === "reject" || rejectReason.trim().length === 0}
                onClick={() => run("reject", { reason: rejectReason.trim() })}
              >
                {busy === "reject" ? "Rejecting..." : "Reject"}
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
