"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, DecimalInput, Input, Textarea } from "@/components/ui";

export function PettyCashRequestFundLineForm({ requestId, outstandingAmount }: { requestId: string; outstandingAmount: number }) {
  const router = useRouter();
  const [amount, setAmount] = useState(String(outstandingAmount));
  const [reference, setReference] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function submit(event: React.FormEvent) { event.preventDefault(); setBusy(true); setError(null); try { await apiPostNoContent(`finance/petty-cash-requests/${requestId}/fund`, { amount: Number(amount), paymentReference: reference.trim(), notes: notes.trim() || null }); router.refresh(); } catch (err) { setError(err instanceof Error ? err.message : String(err)); } finally { setBusy(false); } }
  return <form onSubmit={submit} className="space-y-3"><div className="grid gap-3 sm:grid-cols-2"><div><label className="mb-1 block text-sm font-medium">Amount received</label><DecimalInput value={amount} onChange={(event) => setAmount(event.target.value)} required /></div><div><label className="mb-1 block text-sm font-medium">Bank/remittance reference</label><Input value={reference} onChange={(event) => setReference(event.target.value)} required /></div></div><Textarea value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Optional receipt notes" />{error ? <div className="text-sm text-red-700">{error}</div> : null}<Button disabled={busy || !reference.trim()}>{busy ? "Recording..." : "Record Replenishment Received"}</Button></form>;
}
