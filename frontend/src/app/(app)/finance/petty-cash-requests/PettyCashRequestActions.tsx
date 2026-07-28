"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { ConfirmActionDialog } from "@/components/ConfirmActionDialog";
import { Button, Input, SecondaryButton, Textarea } from "@/components/ui";
import { STATUS_APPROVED, STATUS_DRAFT, STATUS_PARTIALLY_FUNDED, STATUS_SUBMITTED, money } from "./categories";

type LineRef = {
  id: string;
  purpose: string;
  requestedAmount: number;
  approvedAmount?: number | null;
};

type Pending = "submit" | "approve" | "reject" | null;

export function PettyCashRequestActions({
  requestId,
  status,
  lines,
  permissions,
}: {
  requestId: string;
  status: number;
  lines: LineRef[];
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const canSubmit = status === STATUS_DRAFT && permissionSet.has("Finance.PettyCashRequest.Submit");
  const canApprove = status === STATUS_SUBMITTED && permissionSet.has("Finance.PettyCashRequest.Approve");
  const canReject = status === STATUS_SUBMITTED && permissionSet.has("Finance.PettyCashRequest.Reject");

  // Head office decides each line separately, so the approval form starts at the requested amount
  // and is edited down where they are giving less.
  const [approvedAmounts, setApprovedAmounts] = useState<Record<string, string>>(() =>
    Object.fromEntries(lines.map((line) => [line.id, String(line.approvedAmount ?? line.requestedAmount)])),
  );
  const [rejectReason, setRejectReason] = useState("");
  const [pending, setPending] = useState<Pending>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(action: string, body: object = {}) {
    setError(null);
    setBusy(action);
    try {
      await apiPostNoContent(`finance/petty-cash-requests/${requestId}/${action}`, body);
      setPending(null);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  const approvedTotal = lines.reduce((sum, line) => {
    const value = Number(approvedAmounts[line.id]);
    return sum + (Number.isFinite(value) ? value : 0);
  }, 0);
  const requestedTotal = lines.reduce((sum, line) => sum + line.requestedAmount, 0);
  const anyOverRequested = lines.some((line) => Number(approvedAmounts[line.id]) > line.requestedAmount);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        {canSubmit ? (
          <Button type="button" disabled={busy !== null || lines.length === 0} onClick={() => setPending("submit")}>
            Submit to Head Office
          </Button>
        ) : null}
        {canApprove ? (
          <Button type="button" disabled={busy !== null} onClick={() => setPending("approve")}>
            Approve
          </Button>
        ) : null}
        {canReject ? (
          <SecondaryButton type="button" disabled={busy !== null} onClick={() => setPending("reject")}>
            Reject
          </SecondaryButton>
        ) : null}
        {status === STATUS_DRAFT && lines.length === 0 ? (
          <span className="text-xs text-zinc-500">Add at least one category line before submitting.</span>
        ) : null}
        {(status === STATUS_APPROVED || status === STATUS_PARTIALLY_FUNDED) ? (
          <span className="text-xs text-zinc-500">
            Approved. Release money per line from the table below.
          </span>
        ) : null}
      </div>

      {canApprove ? (
        <div className="rounded-md border border-[var(--card-border)] p-3">
          <div className="text-sm font-semibold">Approved amount per line</div>
          <div className="mt-1 text-xs text-zinc-500">
            Approve each category on its own. Enter 0 to approve nothing for a line.
          </div>
          <div className="mt-3 space-y-2">
            {lines.map((line) => (
              <div key={line.id} className="flex flex-wrap items-center gap-3">
                <div className="min-w-0 flex-1 text-sm">
                  <div className="truncate">{line.purpose}</div>
                  <div className="text-xs text-zinc-500">requested {money(line.requestedAmount)}</div>
                </div>
                <Input
                  className="w-32"
                  inputMode="decimal"
                  value={approvedAmounts[line.id] ?? ""}
                  onChange={(e) =>
                    setApprovedAmounts((prev) => ({ ...prev, [line.id]: e.target.value }))
                  }
                />
              </div>
            ))}
          </div>
          <div className="mt-3 text-sm">
            Approving <span className="font-semibold">{money(approvedTotal)}</span> of {money(requestedTotal)} requested
          </div>
          {anyOverRequested ? (
            <div className="mt-1 text-xs text-red-700 dark:text-red-300">
              A line cannot be approved for more than was requested.
            </div>
          ) : null}
        </div>
      ) : null}

      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}

      <ConfirmActionDialog
        open={pending === "submit"}
        title="Submit petty cash request"
        confirmWord="SUBMIT"
        confirmLabel="Submit"
        busy={busy === "submit"}
        onCancel={() => setPending(null)}
        onConfirm={() => run("submit")}
        description={
          <>
            This sends {money(requestedTotal)} across {lines.length} categor{lines.length === 1 ? "y" : "ies"} to head
            office. The request can no longer be edited once submitted.
          </>
        }
      />

      <ConfirmActionDialog
        open={pending === "approve"}
        title="Approve petty cash request"
        confirmWord="APPROVE"
        confirmLabel="Approve"
        busy={busy === "approve"}
        onCancel={() => setPending(null)}
        onConfirm={() =>
          run("approve", {
            lines: lines.map((line) => ({
              lineId: line.id,
              approvedAmount: Number(approvedAmounts[line.id]) || 0,
            })),
          })
        }
        description={
          <>
            Approves <span className="font-semibold">{money(approvedTotal)}</span> of the {money(requestedTotal)}{" "}
            requested. Approval does not move any money - each line is funded separately afterwards.
          </>
        }
      />

      {pending === "reject" ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4">
          <div role="dialog" aria-modal="true" className="w-full max-w-md rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl">
            <div className="text-base font-semibold">Reject petty cash request</div>
            <div className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
              Give the requester a reason. This is recorded against the request.
            </div>
            <label className="mt-4 block text-sm font-medium">Reason</label>
            <Textarea
              className="mt-1"
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
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
