"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost, apiPostNoContent } from "@/lib/api-client";
import { Button, Card, DecimalInput, Input, SecondaryButton, Table, Textarea } from "@/components/ui";

type CashCount = {
  id: string;
  number: string;
  countedAt: string;
  countedByUserId: string;
  countedByName: string;
  physicalCash: number;
  outstandingAdvances: number;
  supportedExpenseVouchers: number;
  authorizedFloatSnapshot: number;
  accountability: number;
  variance: number;
  notes?: string | null;
  status: number;
  rejectionReason?: string | null;
};

const statusLabel: Record<number, string> = { 1: "Submitted", 2: "Approved", 3: "Rejected" };

export function PettyCashCashCountPanel({
  fundId,
  counts,
  canCreate,
  canApprove,
}: {
  fundId: string;
  counts: CashCount[];
  canCreate: boolean;
  canApprove: boolean;
}) {
  const router = useRouter();
  const [physicalCash, setPhysicalCash] = useState("");
  const [supportedExpenseVouchers, setSupportedExpenseVouchers] = useState("");
  const [countedAt, setCountedAt] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function createCount(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy("create");
    try {
      await apiPost(`finance/petty-cash-funds/${fundId}/cash-counts`, {
        countedAt: countedAt ? new Date(countedAt).toISOString() : null,
        physicalCash: Number(physicalCash),
        supportedExpenseVouchers: Number(supportedExpenseVouchers || "0"),
        notes: notes.trim() || null,
      });
      setPhysicalCash("");
      setSupportedExpenseVouchers("");
      setCountedAt("");
      setNotes("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  async function approve(id: string) {
    setError(null);
    setBusy(`approve:${id}`);
    try {
      await apiPostNoContent(`finance/petty-cash-funds/cash-counts/${id}/approve`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  async function reject(id: string) {
    const reason = window.prompt("Reason for rejecting this cash count:");
    if (!reason?.trim()) return;
    setError(null);
    setBusy(`reject:${id}`);
    try {
      await apiPostNoContent(`finance/petty-cash-funds/cash-counts/${id}/reject`, { reason: reason.trim() });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  return (
    <Card>
      <div className="mb-3 text-sm font-semibold">Physical Cash Counts</div>
      {canCreate ? (
        <form onSubmit={createCount} className="mb-4 space-y-3 rounded-lg border border-[var(--card-border)] p-3">
          <div className="grid gap-3 sm:grid-cols-3">
            <div><label className="mb-1 block text-sm font-medium">Physical cash *</label><DecimalInput value={physicalCash} onChange={(event) => setPhysicalCash(event.target.value)} required /></div>
            <div><label className="mb-1 block text-sm font-medium">Supported vouchers</label><DecimalInput value={supportedExpenseVouchers} onChange={(event) => setSupportedExpenseVouchers(event.target.value)} placeholder="0.00" /></div>
            <div><label className="mb-1 block text-sm font-medium">Counted at</label><Input type="datetime-local" value={countedAt} onChange={(event) => setCountedAt(event.target.value)} /></div>
          </div>
          <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Count sheet reference, denomination notes, or variance explanation" />
          <Button type="submit" disabled={busy !== null}>{busy === "create" ? "Recording..." : "Record Cash Count"}</Button>
        </form>
      ) : null}

      <div className="overflow-auto">
        <Table>
          <thead><tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800"><th className="py-2 pr-3">Count</th><th className="py-2 pr-3">Cash</th><th className="py-2 pr-3">Advances</th><th className="py-2 pr-3">Vouchers</th><th className="py-2 pr-3">Accountability</th><th className="py-2 pr-3">Variance</th><th className="py-2 pr-3">Status</th><th className="py-2 pr-3">Actions</th></tr></thead>
          <tbody>
            {counts.map((count) => (
              <tr key={count.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                <td className="py-2 pr-3"><div className="font-mono text-xs">{count.number}</div><div className="text-xs text-zinc-500">{new Date(count.countedAt).toLocaleString()} · {count.countedByName}</div></td>
                <td className="py-2 pr-3">{count.physicalCash.toFixed(2)}</td><td className="py-2 pr-3">{count.outstandingAdvances.toFixed(2)}</td><td className="py-2 pr-3">{count.supportedExpenseVouchers.toFixed(2)}</td><td className="py-2 pr-3">{count.accountability.toFixed(2)}</td>
                <td className={`py-2 pr-3 font-medium ${count.variance === 0 ? "text-emerald-700 dark:text-emerald-300" : "text-amber-700 dark:text-amber-300"}`}>{count.variance.toFixed(2)}</td>
                <td className="py-2 pr-3">{statusLabel[count.status] ?? count.status}{count.rejectionReason ? <div className="text-xs text-red-700">{count.rejectionReason}</div> : null}</td>
                <td className="py-2 pr-3">{canApprove && count.status === 1 ? <div className="flex gap-2"><SecondaryButton type="button" disabled={busy !== null} onClick={() => void approve(count.id)}>Approve</SecondaryButton><SecondaryButton type="button" disabled={busy !== null} onClick={() => void reject(count.id)}>Reject</SecondaryButton></div> : "-"}</td>
              </tr>
            ))}
            {counts.length === 0 ? <tr><td colSpan={8} className="py-6 text-sm text-zinc-500">No cash counts recorded.</td></tr> : null}
          </tbody>
        </Table>
      </div>
      {error ? <div className="mt-3 text-sm text-red-700 dark:text-red-300">{error}</div> : null}
    </Card>
  );
}
