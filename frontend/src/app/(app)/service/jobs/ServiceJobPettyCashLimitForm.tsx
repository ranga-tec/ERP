"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPutNoContent } from "@/lib/api-client";
import { Input, SecondaryButton } from "@/components/ui";

export function ServiceJobPettyCashLimitForm({ jobId, currentLimit }: { jobId: string; currentLimit: number }) {
  const router = useRouter();
  const [amount, setAmount] = useState(String(currentLimit));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function save() {
    const value = Number(amount);
    if (!Number.isFinite(value) || value < 0) {
      setError("Limit must be 0 or greater.");
      return;
    }

    setError(null);
    setBusy(true);
    try {
      await apiPutNoContent(`service/jobs/${jobId}/petty-cash-spending-limit`, { amount: value });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-wrap items-end gap-2">
      <div>
        <label className="mb-1 block text-xs font-medium">Petty-cash authorization</label>
        <Input className="w-44" value={amount} onChange={(event) => setAmount(event.target.value)} inputMode="decimal" />
      </div>
      <SecondaryButton type="button" disabled={busy} onClick={() => void save()}>
        {busy ? "Saving..." : "Save Limit"}
      </SecondaryButton>
      <span className="text-xs text-zinc-500">0 = not configured. Approval checks actual plus open advances.</span>
      {error ? <div className="w-full text-xs text-red-700 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
