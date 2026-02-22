"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function ServiceJobActions({
  jobId,
  canStart,
  canComplete,
  canClose,
}: {
  jobId: string;
  canStart: boolean;
  canComplete: boolean;
  canClose: boolean;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function act(path: string) {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`service/jobs/${jobId}/${path}`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        <SecondaryButton type="button" disabled={!canStart || busy} onClick={() => act("start")}>
          Start
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canComplete || busy} onClick={() => act("complete")}>
          Complete
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canClose || busy} onClick={() => act("close")}>
          Close
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

