"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function ServiceJobAssignmentActions({
  serviceJobId,
  assignmentId,
  status,
}: {
  serviceJobId: string;
  assignmentId: string;
  status: number;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run(action: "approve" | "reject" | "delete") {
    setError(null);
    setBusy(true);
    try {
      if (action === "delete") {
        await apiDeleteNoContent(`service/jobs/${serviceJobId}/assignments/${assignmentId}`);
      } else if (action === "reject") {
        const reason = window.prompt("Reason for rejecting this assignment?");
        if (reason === null) return;
        await apiPostNoContent(`service/jobs/${serviceJobId}/assignments/${assignmentId}/reject`, { reason: reason.trim() || null });
      } else {
        await apiPostNoContent(`service/jobs/${serviceJobId}/assignments/${assignmentId}/approve`, {});
      }

      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-1">
      <div className="flex flex-wrap gap-2">
        <SecondaryButton type="button" disabled={busy || status !== 0} onClick={() => run("approve")}>
          Approve
        </SecondaryButton>
        <SecondaryButton type="button" disabled={busy || status !== 0} onClick={() => run("reject")}>
          Reject
        </SecondaryButton>
        <SecondaryButton type="button" disabled={busy || status === 1} onClick={() => run("delete")}>
          Delete
        </SecondaryButton>
      </div>
      {error ? <div className="text-xs text-red-600 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
