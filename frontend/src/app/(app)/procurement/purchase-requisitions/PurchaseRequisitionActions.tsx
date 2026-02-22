"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function PurchaseRequisitionActions({
  purchaseRequisitionId,
  canSubmit,
  canApprove,
  canReject,
  canCancel,
}: {
  purchaseRequisitionId: string;
  canSubmit: boolean;
  canApprove: boolean;
  canReject: boolean;
  canCancel: boolean;
}) {
  const router = useRouter();
  const [busyAction, setBusyAction] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(action: "submit" | "approve" | "reject" | "cancel") {
    setError(null);
    setBusyAction(action);
    try {
      await apiPostNoContent(`procurement/purchase-requisitions/${purchaseRequisitionId}/${action}`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusyAction(null);
    }
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        <SecondaryButton
          type="button"
          disabled={!canSubmit || busyAction !== null}
          onClick={() => void run("submit")}
        >
          {busyAction === "submit" ? "Submitting..." : "Submit"}
        </SecondaryButton>
        <SecondaryButton
          type="button"
          disabled={!canApprove || busyAction !== null}
          onClick={() => void run("approve")}
        >
          {busyAction === "approve" ? "Approving..." : "Approve"}
        </SecondaryButton>
        <SecondaryButton
          type="button"
          disabled={!canReject || busyAction !== null}
          onClick={() => void run("reject")}
        >
          {busyAction === "reject" ? "Rejecting..." : "Reject"}
        </SecondaryButton>
        <SecondaryButton
          type="button"
          disabled={!canCancel || busyAction !== null}
          onClick={() => void run("cancel")}
        >
          {busyAction === "cancel" ? "Cancelling..." : "Cancel"}
        </SecondaryButton>
      </div>
      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}
    </div>
  );
}
