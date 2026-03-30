"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Input, SecondaryButton, Select } from "@/components/ui";

type EstimateRef = {
  id: string;
  number: string;
  revisionNumber: number;
  status: number;
  total: number;
};

type ConvertResponse = {
  serviceEstimateId: string;
  addedLineCount: number;
};

export function ServiceExpenseClaimConvertEstimateForm({
  claimId,
  estimates,
  disabled,
}: {
  claimId: string;
  estimates: EstimateRef[];
  disabled: boolean;
}) {
  const router = useRouter();
  const [serviceEstimateId, setServiceEstimateId] = useState("");
  const [taxPercent, setTaxPercent] = useState("0");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sortedEstimates = useMemo(
    () =>
      estimates
        .slice()
        .sort((a, b) => {
          if (a.status !== b.status) {
            return a.status - b.status;
          }
          return b.revisionNumber - a.revisionNumber;
        }),
    [estimates],
  );

  async function convert() {
    setError(null);
    setBusy(true);
    try {
      const tax = Number(taxPercent || 0);
      if (!Number.isFinite(tax) || tax < 0) {
        throw new Error("Tax percent must be 0 or greater.");
      }

      const result = await apiPost<ConvertResponse>(`service/expense-claims/${claimId}/convert-billable-lines-to-estimate`, {
        serviceEstimateId: serviceEstimateId || null,
        taxPercent: tax,
        validUntil: null,
        terms: null,
      });

      router.push(`/service/estimates/${result.serviceEstimateId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-3 rounded-lg border border-zinc-200 p-3 dark:border-zinc-800">
      <div className="text-sm font-medium">Convert Billable Lines to Estimate</div>
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Target Estimate</label>
          <Select value={serviceEstimateId} onChange={(event) => setServiceEstimateId(event.target.value)} disabled={disabled || busy}>
            <option value="">Use latest draft or auto-create revision</option>
            {sortedEstimates.map((estimate) => (
              <option key={estimate.id} value={estimate.id}>
                {estimate.number} {estimate.status === 0 ? "(Draft)" : estimate.status === 1 ? "(Approved)" : "(Rejected)"} - {estimate.total.toFixed(2)}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Tax % for new estimate lines</label>
          <Input value={taxPercent} onChange={(event) => setTaxPercent(event.target.value)} inputMode="decimal" disabled={disabled || busy} />
        </div>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <SecondaryButton type="button" disabled={disabled || busy} onClick={convert}>
          {busy ? "Converting..." : "Push to Estimate"}
        </SecondaryButton>
        <div className="text-xs text-zinc-500">
          Approved estimates are revised automatically so the original customer approval remains intact.
        </div>
      </div>
      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}
    </div>
  );
}
