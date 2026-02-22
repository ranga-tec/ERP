"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function NotificationRetryButton({ id }: { id: string }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function retry() {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`admin/notifications/${id}/retry`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-1">
      <SecondaryButton type="button" disabled={busy} onClick={retry}>
        {busy ? "Retrying..." : "Retry"}
      </SecondaryButton>
      {error ? <div className="text-xs text-red-600 dark:text-red-400">{error}</div> : null}
    </div>
  );
}

