"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, DecimalInput, Input, Select, Textarea } from "@/components/ui";

type FundRef = { id: string; code: string; name: string; balance: number; authorizedFloat: number };
type RequestDto = { id: string };

export function PettyCashRequestCreateForm({ funds }: { funds: FundRef[] }) {
  const router = useRouter();
  const [fundId, setFundId] = useState(funds[0]?.id ?? "");
  const selectedFund = funds.find((fund) => fund.id === fundId);
  const [requestedAmount, setRequestedAmount] = useState("");
  const [cashOnHand, setCashOnHand] = useState(String(selectedFund?.balance ?? 0));
  const [outstandingAdvances, setOutstandingAdvances] = useState("0");
  const [reconciledExpenses, setReconciledExpenses] = useState("0");
  const [neededByAt, setNeededByAt] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const row = await apiPost<RequestDto>("finance/petty-cash-requests", {
        pettyCashFundId: fundId,
        requestedAmount: Number(requestedAmount),
        cashOnHand: Number(cashOnHand),
        outstandingAdvances: Number(outstandingAdvances),
        reconciledExpenses: Number(reconciledExpenses),
        neededByAt: neededByAt ? new Date(neededByAt).toISOString() : null,
        notes: notes.trim() || null,
      });
      router.push(`/finance/petty-cash-requests/${row.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return <form onSubmit={submit} className="space-y-3">
    <div><label className="mb-1 block text-sm font-medium">Fund</label><Select value={fundId} onChange={(event) => { const id = event.target.value; setFundId(id); setCashOnHand(String(funds.find((fund) => fund.id === id)?.balance ?? 0)); }} required><option value="" disabled>Select fund...</option>{funds.map((fund) => <option key={fund.id} value={fund.id}>{fund.code} - {fund.name}</option>)}</Select></div>
    <div className="grid gap-3 sm:grid-cols-2">
      <div><label className="mb-1 block text-sm font-medium">Requested replenishment</label><DecimalInput value={requestedAmount} onChange={(event) => setRequestedAmount(event.target.value)} min="0.01" required /></div>
      <div><label className="mb-1 block text-sm font-medium">Cash physically on hand</label><DecimalInput value={cashOnHand} onChange={(event) => setCashOnHand(event.target.value)} min="0" required /></div>
      <div><label className="mb-1 block text-sm font-medium">Outstanding employee advances</label><DecimalInput value={outstandingAdvances} onChange={(event) => setOutstandingAdvances(event.target.value)} min="0" required /></div>
      <div><label className="mb-1 block text-sm font-medium">Reconciled expenses</label><DecimalInput value={reconciledExpenses} onChange={(event) => setReconciledExpenses(event.target.value)} min="0" required /></div>
    </div>
    {selectedFund?.authorizedFloat ? <p className="text-xs text-zinc-500">Authorized float: {selectedFund.authorizedFloat.toFixed(2)}</p> : null}
    <div><label className="mb-1 block text-sm font-medium">Needed by</label><Input type="date" value={neededByAt} onChange={(event) => setNeededByAt(event.target.value)} /></div>
    <div><label className="mb-1 block text-sm font-medium">Reconciliation notes</label><Textarea value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Explain the reconciled expenses and required replenishment." /></div>
    {error ? <div className="text-sm text-red-700">{error}</div> : null}
    <Button disabled={busy || !fundId}>{busy ? "Creating..." : "Create Replenishment Request"}</Button>
  </form>;
}
