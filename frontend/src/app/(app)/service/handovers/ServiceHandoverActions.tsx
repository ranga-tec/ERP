"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function ServiceHandoverActions({
  handoverId,
  canComplete,
  canCancel,
}: {
  handoverId: string;
  canComplete: boolean;
  canCancel: boolean;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function act(action: "complete" | "cancel") {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`service/handovers/${handoverId}/${action}`, {});
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
        <SecondaryButton type="button" disabled={!canComplete || busy} onClick={() => act("complete")}>
          {busy ? "Working..." : "Complete"}
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canCancel || busy} onClick={() => act("cancel")}>
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
