"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function ServiceJobFinalInvoiceActions({ jobId, disabled }: { jobId: string; disabled?: boolean }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function markNotRequired() {
    const reason = window.prompt("Reason final invoice is not required?");
    if (reason === null) return;

    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`service/jobs/${jobId}/final-invoice-not-required`, { reason });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-2">
      <SecondaryButton type="button" disabled={disabled || busy} onClick={markNotRequired}>
        Mark Not Billable
      </SecondaryButton>
      {error ? <div className="text-xs text-red-600 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
