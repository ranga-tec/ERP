"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select, Textarea } from "@/components/ui";

type FundRef = { id: string; code: string; name: string };
type StaffRef = { userId: string; name: string; email?: string | null };
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
type Pending = "release" | "reject" | null;

function money(value: number): string {
  return value.toFixed(2);
}

export function PettyCashIouActions({
  id,
  status,
  funds,
  amount,
  serviceJobId,
  serviceJobNumber,
  staff,
  fundedCategories,
  permissions,
}: {
  id: string;
  status: number;
  funds: FundRef[];
  amount: number;
  serviceJobId: string | null;
  serviceJobNumber: string | null;
  staff: StaffRef[];
  fundedCategories: FundedCategoryRef[];
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const canApprove = permissionSet.has("Finance.PettyCashIou.Approve");
  const canReject = permissionSet.has("Finance.PettyCashIou.Reject");
  const canRelease = permissionSet.has("Finance.PettyCashIou.Release");
  const canApproveSettlement = status === 4 && permissionSet.has("Finance.PettyCashIou.Approve");
  const [requestLineId, setRequestLineId] = useState("");
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
        {status === 1 && (canApprove || canReject) ? (
          <>
            {canApprove ? (
              <Button type="button" disabled={busy !== null} onClick={() => run("approve")}>
                {busy === "approve" ? "Approving..." : "Approve"}
              </Button>
            ) : null}
            {canReject ? (
              <SecondaryButton type="button" disabled={busy !== null} onClick={() => setPending("reject")}>
                Reject
              </SecondaryButton>
            ) : null}
          </>
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

        {((status === 1 && !canApprove && !canReject) || (status === 2 && !canRelease) || status === 3 || (status === 4 && !canApproveSettlement)) ? (
          <span className="text-xs text-zinc-500">View only</span>
        ) : null}
      </div>

      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}

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
