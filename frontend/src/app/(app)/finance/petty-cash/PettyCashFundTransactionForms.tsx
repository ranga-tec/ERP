"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

export function PettyCashFundTransactionForms({ fundId }: { fundId: string }) {
  const router = useRouter();
  const [topUpAmount, setTopUpAmount] = useState("");
  const [topUpReferenceNumber, setTopUpReferenceNumber] = useState("");
  const [topUpNotes, setTopUpNotes] = useState("");

  const [adjustAmount, setAdjustAmount] = useState("");
  const [adjustDirection, setAdjustDirection] = useState("1");
  const [adjustReferenceNumber, setAdjustReferenceNumber] = useState("");
  const [adjustNotes, setAdjustNotes] = useState("");

  const [busyAction, setBusyAction] = useState<"topup" | "adjust" | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function submitTopUp(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusyAction("topup");

    try {
      const amount = Number(topUpAmount);
      if (!Number.isFinite(amount) || amount <= 0) {
        throw new Error("Top-up amount must be positive.");
      }

      await apiPostNoContent(`finance/petty-cash-funds/${fundId}/top-ups`, {
        amount,
        occurredAt: null,
        referenceNumber: topUpReferenceNumber.trim() || null,
        notes: topUpNotes.trim() || null,
      });

      setTopUpAmount("");
      setTopUpReferenceNumber("");
      setTopUpNotes("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusyAction(null);
    }
  }

  async function submitAdjustment(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusyAction("adjust");

    try {
      const amount = Number(adjustAmount);
      if (!Number.isFinite(amount) || amount <= 0) {
        throw new Error("Adjustment amount must be positive.");
      }

      await apiPostNoContent(`finance/petty-cash-funds/${fundId}/adjustments`, {
        amount,
        direction: Number(adjustDirection),
        occurredAt: null,
        referenceNumber: adjustReferenceNumber.trim() || null,
        notes: adjustNotes.trim() || null,
      });

      setAdjustAmount("");
      setAdjustReferenceNumber("");
      setAdjustNotes("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusyAction(null);
    }
  }

  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <form onSubmit={submitTopUp} className="space-y-3 rounded-xl border border-[var(--card-border)] p-3">
        <div className="text-sm font-medium">Top Up Fund</div>
        <div className="grid gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium">Amount</label>
            <Input value={topUpAmount} onChange={(event) => setTopUpAmount(event.target.value)} inputMode="decimal" required />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Reference (optional)</label>
            <Input value={topUpReferenceNumber} onChange={(event) => setTopUpReferenceNumber(event.target.value)} />
          </div>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
          <Textarea value={topUpNotes} onChange={(event) => setTopUpNotes(event.target.value)} />
        </div>
        <Button type="submit" disabled={busyAction !== null}>
          {busyAction === "topup" ? "Posting..." : "Post Top Up"}
        </Button>
      </form>

      <form onSubmit={submitAdjustment} className="space-y-3 rounded-xl border border-[var(--card-border)] p-3">
        <div className="text-sm font-medium">Adjustment</div>
        <div className="grid gap-3 sm:grid-cols-3">
          <div>
            <label className="mb-1 block text-sm font-medium">Direction</label>
            <Select value={adjustDirection} onChange={(event) => setAdjustDirection(event.target.value)}>
              <option value="1">Increase</option>
              <option value="2">Decrease</option>
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Amount</label>
            <Input value={adjustAmount} onChange={(event) => setAdjustAmount(event.target.value)} inputMode="decimal" required />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Reference (optional)</label>
            <Input value={adjustReferenceNumber} onChange={(event) => setAdjustReferenceNumber(event.target.value)} />
          </div>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
          <Textarea value={adjustNotes} onChange={(event) => setAdjustNotes(event.target.value)} />
        </div>
        <Button type="submit" disabled={busyAction !== null}>
          {busyAction === "adjust" ? "Posting..." : "Post Adjustment"}
        </Button>
      </form>

      {error ? (
        <div className="lg:col-span-2 rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}
    </div>
  );
}
