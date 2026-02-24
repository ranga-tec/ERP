"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button } from "@/components/ui";

type CreateReorderPurchaseRequisitionResponse = {
  purchaseRequisitionId: string;
  purchaseRequisitionNumber: string;
  lineCount: number;
  totalSuggestedQuantity: number;
};

export function ReorderAlertsCreatePrButton({
  warehouseId,
  alertCount,
}: {
  warehouseId: string;
  alertCount: number;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onClick() {
    setError(null);
    setBusy(true);
    try {
      const result = await apiPost<CreateReorderPurchaseRequisitionResponse>(
        "inventory/reorder-alerts/create-purchase-requisition",
        { warehouseId, notes: null, submit: false },
      );
      router.push(`/procurement/purchase-requisitions/${result.purchaseRequisitionId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-2">
      <Button type="button" onClick={onClick} disabled={busy || alertCount === 0}>
        {busy ? "Creating PR..." : `Create PR Draft from ${alertCount} Alert${alertCount === 1 ? "" : "s"}`}
      </Button>
      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}
    </div>
  );
}
