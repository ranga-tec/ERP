"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

export function MissingReceiptApprovalButton({ claimId, lineId }: { claimId: string; lineId: string }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function approve() {
    setBusy(true);
    setError(null);
    try {
      await apiPostNoContent(`service/expense-claims/${claimId}/lines/${lineId}/approve-missing-receipt`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return <div><SecondaryButton type="button" disabled={busy} onClick={() => void approve()}>{busy ? "Approving..." : "Approve Missing Receipt"}</SecondaryButton>{error ? <div className="mt-2 text-xs text-red-700">{error}</div> : null}</div>;
}
