"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, SecondaryButton, Textarea } from "@/components/ui";
import { ConfirmActionDialog } from "@/components/ConfirmActionDialog";
import { REALLOCATION_DRAFT, REALLOCATION_SUBMITTED, money } from "./types";

export function PettyCashReallocationActions({
  reallocationId,
  reallocationNumber,
  status,
  amount,
  permissions,
}: {
  reallocationId: string;
  reallocationNumber: string;
  status: number;
  amount: number;
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmAction, setConfirmAction] = useState<"submit" | "approve" | "cancel" | null>(null);
  const [rejectionReason, setRejectionReason] = useState("");

  const canSubmit = status === REALLOCATION_DRAFT && permissionSet.has("Finance.PettyCashReallocation.Submit");
  const canCancel = status === REALLOCATION_DRAFT && permissionSet.has("Finance.PettyCashReallocation.Cancel");
  const canApprove = status === REALLOCATION_SUBMITTED && permissionSet.has("Finance.PettyCashReallocation.Approve");
  const canReject = status === REALLOCATION_SUBMITTED && permissionSet.has("Finance.PettyCashReallocation.Reject");

  async function run(path: string, body: unknown = {}) {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`finance/petty-cash-reallocations/${reallocationId}/${path}`, body);
      setConfirmAction(null);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  if (!canSubmit && !canCancel && !canApprove && !canReject) {
    return <div className="text-sm text-zinc-500">No actions are available for this reallocation.</div>;
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        {canSubmit ? <Button type="button" disabled={busy} onClick={() => setConfirmAction("submit")}>Submit to Head Office</Button> : null}
        {canCancel ? <SecondaryButton type="button" disabled={busy} onClick={() => setConfirmAction("cancel")}>Cancel Draft</SecondaryButton> : null}
        {canApprove ? <Button type="button" disabled={busy} onClick={() => setConfirmAction("approve")}>Approve and Post</Button> : null}
      </div>

      {canReject ? (
        <div className="rounded-md border border-red-200 p-3 dark:border-red-950">
          <div className="text-sm font-semibold">Reject reallocation</div>
          <label className="mt-2 mb-1 block text-xs font-medium">Reason *</label>
          <Textarea value={rejectionReason} onChange={(event) => setRejectionReason(event.target.value)} placeholder="Explain why the category authorization cannot be changed" />
          <SecondaryButton
            className="mt-2"
            type="button"
            disabled={busy || rejectionReason.trim().length === 0}
            onClick={() => void run("reject", { reason: rejectionReason.trim() })}
          >
            Reject
          </SecondaryButton>
        </div>
      ) : null}

      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}

      <ConfirmActionDialog
        open={confirmAction === "submit"}
        title="Submit category reallocation?"
        description={<>This reserves {money(amount)} from the source category while head-office approval is pending.</>}
        confirmWord="SUBMIT"
        confirmLabel="Submit Reallocation"
        busy={busy}
        onCancel={() => setConfirmAction(null)}
        onConfirm={() => void run("submit")}
      />
      <ConfirmActionDialog
        open={confirmAction === "approve"}
        title="Approve and post reallocation?"
        description={<>This posts equal transfer-out and transfer-in entries for {money(amount)}. The total petty cash fund and physical cash remain unchanged.</>}
        confirmWord="APPROVE"
        confirmLabel="Approve and Post"
        busy={busy}
        onCancel={() => setConfirmAction(null)}
        onConfirm={() => void run("approve")}
      />
      <ConfirmActionDialog
        open={confirmAction === "cancel"}
        title="Cancel draft reallocation?"
        description={<>This closes draft {reallocationNumber} without changing category or fund balances.</>}
        confirmWord="CANCEL"
        confirmLabel="Cancel Draft"
        busy={busy}
        onCancel={() => setConfirmAction(null)}
        onConfirm={() => void run("cancel")}
      />
    </div>
  );
}
