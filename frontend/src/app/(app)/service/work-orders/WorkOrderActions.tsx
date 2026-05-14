"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function WorkOrderActions({
  workOrderId,
  canStart,
  canMarkDone,
  canCancel,
}: {
  workOrderId: string;
  canStart: boolean;
  canMarkDone: boolean;
  canCancel: boolean;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(action: "start" | "done" | "cancel") {
    setError(null);
    setBusy(action);
    try {
      await apiPostNoContent(`service/work-orders/${workOrderId}/${action}`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        <SecondaryButton type="button" disabled={!canStart || busy !== null} onClick={() => run("start")}>
          Start
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canMarkDone || busy !== null} onClick={() => run("done")}>
          Mark Done
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canCancel || busy !== null} onClick={() => run("cancel")}>
          Cancel
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
