"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input } from "@/components/ui";
import { money } from "./categories";

/**
 * Releasing money against one approved line. Deliberately per line rather than per request: head
 * office funds each category separately, and a single bank transfer covering several is expressed
 * by giving each line the same payment reference.
 */
export function PettyCashRequestFundLineForm({
  requestId,
  lineId,
  outstandingAmount,
  defaultPaymentReference,
}: {
  requestId: string;
  lineId: string;
  outstandingAmount: number;
  defaultPaymentReference?: string;
}) {
  const router = useRouter();
  const [amount, setAmount] = useState(String(outstandingAmount));
  const [paymentReference, setPaymentReference] = useState(defaultPaymentReference ?? "");
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const value = Number(amount);
  const invalid = !Number.isFinite(value) || value <= 0 || value > outstandingAmount;

  async function onFund() {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`finance/petty-cash-requests/${requestId}/lines/${lineId}/fund`, {
        amount: value,
        paymentReference: paymentReference.trim() || null,
      });
      setOpen(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  if (outstandingAmount <= 0) {
    return <span className="text-xs text-zinc-500">fully funded</span>;
  }

  if (!open) {
    return (
      <Button type="button" onClick={() => setOpen(true)}>
        Release
      </Button>
    );
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-end gap-2">
        <div>
          <label className="mb-1 block text-xs font-medium text-zinc-500">Amount</label>
          <Input className="w-28" inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-zinc-500">Transfer / slip ref</label>
          <Input
            className="w-40"
            value={paymentReference}
            onChange={(e) => setPaymentReference(e.target.value)}
            placeholder="Shared across lines"
          />
        </div>
        <Button type="button" disabled={busy || invalid} onClick={() => void onFund()}>
          {busy ? "Releasing..." : "Confirm"}
        </Button>
        <Button type="button" disabled={busy} onClick={() => setOpen(false)}>
          Cancel
        </Button>
      </div>
      {invalid ? (
        <div className="text-[11px] text-red-700 dark:text-red-300">
          Enter an amount between 0 and the {money(outstandingAmount)} outstanding.
        </div>
      ) : null}
      {error ? <div className="text-[11px] text-red-700 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
