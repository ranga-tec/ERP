"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost, apiPostNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

type ServiceEstimateRef = { id: string };

export function ServiceEstimateActions({
  estimateId,
  canApprove,
  canReject,
  canSend,
  canRevise,
}: {
  estimateId: string;
  canApprove: boolean;
  canReject: boolean;
  canSend: boolean;
  canRevise: boolean;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function act(action: "approve" | "reject" | "send" | "revise") {
    setError(null);
    setBusy(true);
    try {
      if (action === "send") {
        await apiPostNoContent(`service/estimates/${estimateId}/send`, {
          appBaseUrl: typeof window !== "undefined" ? window.location.origin : null,
        });
      } else if (action === "revise") {
        const revised = await apiPost<ServiceEstimateRef>(`service/estimates/${estimateId}/revise`, {});
        router.push(`/service/estimates/${revised.id}`);
        return;
      } else {
        await apiPostNoContent(`service/estimates/${estimateId}/${action}`, {});
      }

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
        <SecondaryButton type="button" disabled={!canApprove || busy} onClick={() => act("approve")}>
          {busy ? "Working..." : "Approve"}
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canReject || busy} onClick={() => act("reject")}>
          Reject
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canSend || busy} onClick={() => act("send")}>
          Send to Customer
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canRevise || busy} onClick={() => act("revise")}>
          Create Revision
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
