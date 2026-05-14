"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function ServiceJobDailySheetActions({
  serviceJobId,
  dailySheetId,
  status,
}: {
  serviceJobId: string;
  dailySheetId: string;
  status: number;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(action: "submit" | "approve" | "reject") {
    setError(null);
    setBusy(action);
    try {
      const payload = action === "reject" ? { reason: window.prompt("Reason for rejection?") ?? null } : {};
      await apiPostNoContent(`service/jobs/${serviceJobId}/daily-sheets/${dailySheetId}/${action}`, payload);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {status === 0 ? <SecondaryButton type="button" disabled={busy !== null} onClick={() => run("submit")}>{busy === "submit" ? "Submitting..." : "Submit"}</SecondaryButton> : null}
      {status === 1 ? (
        <>
          <SecondaryButton type="button" disabled={busy !== null} onClick={() => run("approve")}>{busy === "approve" ? "Approving..." : "Approve"}</SecondaryButton>
          <SecondaryButton type="button" disabled={busy !== null} onClick={() => run("reject")}>{busy === "reject" ? "Rejecting..." : "Reject"}</SecondaryButton>
        </>
      ) : null}
      {error ? <div className="text-xs text-red-600 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
