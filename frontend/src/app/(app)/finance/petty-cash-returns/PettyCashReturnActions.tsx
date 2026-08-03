"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Textarea } from "@/components/ui";
import { ConfirmActionDialog } from "@/components/ConfirmActionDialog";
import { RETURN_DRAFT, RETURN_SUBMITTED, money } from "./types";

export function PettyCashReturnActions({
  returnId,
  returnNumber,
  status,
  totalAmount,
  permissions,
}: {
  returnId: string;
  returnNumber: string;
  status: number;
  totalAmount: number;
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmSubmit, setConfirmSubmit] = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(false);
  const [receiptReference, setReceiptReference] = useState("");
  const [receivedAt, setReceivedAt] = useState("");
  const [rejectionReason, setRejectionReason] = useState("");

  const canSubmit = status === RETURN_DRAFT && permissionSet.has("Finance.PettyCashReturn.Submit");
  const canCancel = status === RETURN_DRAFT && permissionSet.has("Finance.PettyCashReturn.Cancel");
  const canReceive = status === RETURN_SUBMITTED && permissionSet.has("Finance.PettyCashReturn.Receive");
  const canReject = status === RETURN_SUBMITTED && permissionSet.has("Finance.PettyCashReturn.Reject");

  async function run(path: string, body: unknown = {}) {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`finance/petty-cash-returns/${returnId}/${path}`, body);
      setConfirmSubmit(false);
      setConfirmCancel(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  if (!canSubmit && !canCancel && !canReceive && !canReject) {
    return <div className="text-sm text-zinc-500">No actions are available for this return.</div>;
  }

  return (
    <div className="space-y-4">
      {(canSubmit || canCancel) ? (
        <div className="flex flex-wrap gap-2">
          {canSubmit ? <Button type="button" disabled={busy} onClick={() => setConfirmSubmit(true)}>Submit to Head Office</Button> : null}
          {canCancel ? <SecondaryButton type="button" disabled={busy} onClick={() => setConfirmCancel(true)}>Cancel Draft</SecondaryButton> : null}
        </div>
      ) : null}

      {canReceive ? (
        <div className="rounded-md border border-emerald-300 bg-emerald-50 p-3 dark:border-emerald-900 dark:bg-emerald-950/20">
          <div className="text-sm font-semibold">Head-office receipt</div>
          <p className="mt-1 text-xs text-zinc-600 dark:text-zinc-400">
            Count the cash against the category breakdown and supporting attachments. Confirmation posts {money(totalAmount)} out of the petty cash fund.
          </p>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-xs font-medium">Receipt / deposit reference *</label>
              <Input value={receiptReference} onChange={(event) => setReceiptReference(event.target.value)} placeholder="Required audit evidence" />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium">Received date and time</label>
              <Input type="datetime-local" value={receivedAt} onChange={(event) => setReceivedAt(event.target.value)} />
            </div>
          </div>
          <Button
            className="mt-3"
            type="button"
            disabled={busy || receiptReference.trim().length === 0}
            onClick={() => void run("receive", {
              receiptReference: receiptReference.trim(),
              receivedAt: receivedAt ? new Date(receivedAt).toISOString() : null,
            })}
          >
            {busy ? "Confirming..." : "Confirm Cash Received"}
          </Button>
        </div>
      ) : null}

      {canReject ? (
        <div className="rounded-md border border-red-200 p-3 dark:border-red-950">
          <div className="text-sm font-semibold">Reject return</div>
          <label className="mt-2 mb-1 block text-xs font-medium">Reason *</label>
          <Textarea value={rejectionReason} onChange={(event) => setRejectionReason(event.target.value)} placeholder="Explain the count, evidence, or reconciliation issue" />
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
        open={confirmSubmit}
        title="Submit reconciled cash?"
        description={<>This reserves {money(totalAmount)} for head-office return. The selected categories cannot spend that amount while receipt is pending.</>}
        confirmWord="SUBMIT"
        confirmLabel="Submit Return"
        busy={busy}
        onCancel={() => setConfirmSubmit(false)}
        onConfirm={() => void run("submit")}
      />
      <ConfirmActionDialog
        open={confirmCancel}
        title="Cancel draft return?"
        description={<>This closes draft {returnNumber} without changing any cash balance.</>}
        confirmWord="CANCEL"
        confirmLabel="Cancel Draft"
        busy={busy}
        onCancel={() => setConfirmCancel(false)}
        onConfirm={() => void run("cancel")}
      />
    </div>
  );
}
