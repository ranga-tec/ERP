"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, DecimalInput, SecondaryButton, Textarea } from "@/components/ui";
import { STATUS_DRAFT, STATUS_SUBMITTED } from "./categories";

export function PettyCashRequestActions({ requestId, status, requestedAmount, permissions, legacy }: { requestId: string; status: number; requestedAmount: number; permissions: string[]; legacy: boolean }) {
  const router = useRouter();
  const access = new Set(permissions);
  const [approvedAmount, setApprovedAmount] = useState(String(requestedAmount));
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function run(path: string, body: object = {}) { setBusy(true); setError(null); try { await apiPostNoContent(`finance/petty-cash-requests/${requestId}/${path}`, body); router.refresh(); } catch (err) { setError(err instanceof Error ? err.message : String(err)); } finally { setBusy(false); } }
  if (legacy) return <div className="text-sm text-amber-700">Historical category request: retained read-only for audit.</div>;
  return <div className="space-y-3">
    <div className="flex flex-wrap items-end gap-2">
      {status === STATUS_DRAFT && access.has("Finance.PettyCashRequest.Submit") ? <Button disabled={busy} onClick={() => void run("submit")}>Submit to Head Office</Button> : null}
      {status === STATUS_DRAFT && access.has("Finance.PettyCashRequest.Cancel") ? <SecondaryButton disabled={busy} onClick={() => void run("cancel")}>Cancel</SecondaryButton> : null}
      {status === STATUS_SUBMITTED && access.has("Finance.PettyCashRequest.Approve") ? <><div><label className="mb-1 block text-xs font-medium">Approved amount</label><DecimalInput className="w-40" value={approvedAmount} onChange={(event) => setApprovedAmount(event.target.value)} /></div><Button disabled={busy} onClick={() => void run("approve", { approvedAmount: Number(approvedAmount) })}>Approve</Button></> : null}
      {status === STATUS_SUBMITTED && access.has("Finance.PettyCashRequest.Reject") ? <><Textarea className="min-h-9 w-64" value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Rejection reason" /><SecondaryButton disabled={busy || !reason.trim()} onClick={() => void run("reject", { reason })}>Reject</SecondaryButton></> : null}
    </div>
    {error ? <div className="text-sm text-red-700">{error}</div> : null}
  </div>;
}
