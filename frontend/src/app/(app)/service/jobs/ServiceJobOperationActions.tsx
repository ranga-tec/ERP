"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function ServiceJobOperationActions({
  serviceJobId,
  operationId,
  status,
  disabled,
}: {
  serviceJobId: string;
  operationId: string;
  status: number;
  disabled?: boolean;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(action: "start" | "complete" | "skip" | "delete") {
    setError(null);
    setBusy(action);
    try {
      if (action === "delete") {
        if (!window.confirm("Delete this planned job operation?")) {
          return;
        }
        await apiDeleteNoContent(`service/jobs/${serviceJobId}/operations/${operationId}`);
      } else {
        await apiPostNoContent(`service/jobs/${serviceJobId}/operations/${operationId}/${action}`, {});
      }
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {status === 0 ? (
        <SecondaryButton type="button" disabled={disabled || busy !== null} onClick={() => run("start")}>
          {busy === "start" ? "Starting..." : "Start"}
        </SecondaryButton>
      ) : null}
      {status === 0 || status === 1 ? (
        <SecondaryButton type="button" disabled={disabled || busy !== null} onClick={() => run("complete")}>
          {busy === "complete" ? "Completing..." : "Complete"}
        </SecondaryButton>
      ) : null}
      {status === 0 || status === 1 ? (
        <SecondaryButton type="button" disabled={disabled || busy !== null} onClick={() => run("skip")}>
          {busy === "skip" ? "Skipping..." : "Skip"}
        </SecondaryButton>
      ) : null}
      {status !== 2 ? (
        <SecondaryButton type="button" disabled={disabled || busy !== null} onClick={() => run("delete")}>
          {busy === "delete" ? "Deleting..." : "Delete"}
        </SecondaryButton>
      ) : null}
      {error ? <div className="basis-full text-xs text-red-600 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
