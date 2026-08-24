"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost, apiPostNoContent } from "@/lib/api-client";
import { Button, Card, Input, SecondaryButton, Select, Table, Textarea } from "@/components/ui";

type FundRef = { id: string; code: string; name: string };
type StaffRef = { userId: string; name: string; email?: string | null };
type IouRef = {
  id: string;
  number: string;
  serviceJobId?: string | null;
  serviceJobNumber?: string | null;
  requestedByName: string;
  amount: number;
  purpose: string;
  status: number;
};
type BatchLine = {
  id: string;
  pettyCashIouId: string;
  iouNumber: string;
  serviceJobNumber?: string | null;
  requestedByName: string;
  purpose: string;
  requestedAmount: number;
  approvedAmount: number;
};
export type PettyCashIouBatch = {
  id: string;
  number: string;
  pettyCashFundId: string;
  pettyCashFundCode?: string | null;
  reviewerName: string;
  assignedApproverName: string;
  status: number;
  requestedTotal: number;
  approvedTotal: number;
  isReviewer: boolean;
  isAssignedApprover: boolean;
  fundingReference?: string | null;
  rejectionReason?: string | null;
  lines: BatchLine[];
};

const statusLabel: Record<number, string> = {
  1: "With assigned approver",
  2: "Returned to accountant",
  3: "Awaiting head office",
  4: "Approved - ready for release",
  5: "Approved (legacy funding recorded)",
  6: "Rejected",
};

const money = (value: number) => value.toFixed(2);

export function PettyCashIouBatchPanel({
  batches,
  ious,
  funds,
  approvers,
  permissions,
}: {
  batches: PettyCashIouBatch[];
  ious: IouRef[];
  funds: FundRef[];
  approvers: StaffRef[];
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = useMemo(() => new Set(permissions), [permissions]);
  const selectableIous = ious.filter((iou) => iou.status === 1);
  const [selectedIouIds, setSelectedIouIds] = useState<string[]>([]);
  const [fundId, setFundId] = useState("");
  const [approverId, setApproverId] = useState("");
  const [approvedAmounts, setApprovedAmounts] = useState<Record<string, string>>({});
  const [rejectReasons, setRejectReasons] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(actionKey: string, action: () => Promise<void>) {
    setBusy(actionKey);
    setError(null);
    try {
      await action();
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  const selectedTotal = selectableIous
    .filter((iou) => selectedIouIds.includes(iou.id))
    .reduce((total, iou) => total + iou.amount, 0);

  return (
    <div className="space-y-4">
      {permissionSet.has("Finance.PettyCashIou.Review") ? (
        <Card>
          <h2 className="text-base font-semibold">Prepare approval batch</h2>
          <p className="mt-1 text-sm text-zinc-500">
            Select one fund and one or more received IOU requests. The selected approver receives one total with the complete job and IOU breakdown.
          </p>
          <div className="mt-3 grid gap-3 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium">Petty cash fund</label>
              <Select value={fundId} onChange={(event) => setFundId(event.target.value)}>
                <option value="" disabled>Select fund...</option>
                {funds.map((fund) => <option key={fund.id} value={fund.id}>{fund.code} - {fund.name}</option>)}
              </Select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Assigned approver</label>
              <Select value={approverId} onChange={(event) => setApproverId(event.target.value)}>
                <option value="" disabled>Select approver...</option>
                {approvers.map((person) => (
                  <option key={person.userId} value={person.userId}>{person.name}{person.email ? ` - ${person.email}` : ""}</option>
                ))}
              </Select>
            </div>
          </div>
          <div className="mt-3 overflow-auto rounded-md border border-[var(--card-border)]">
            <Table>
              <thead><tr><th className="p-2 text-left">Select</th><th className="p-2 text-left">IOU</th><th className="p-2 text-left">Job</th><th className="p-2 text-left">Requester / description</th><th className="p-2 text-right">Amount</th></tr></thead>
              <tbody>
                {selectableIous.map((iou) => (
                  <tr key={iou.id} className="border-t border-[var(--card-border)]">
                    <td className="p-2"><input type="checkbox" checked={selectedIouIds.includes(iou.id)} onChange={(event) => setSelectedIouIds((current) => event.target.checked ? [...current, iou.id] : current.filter((id) => id !== iou.id))} /></td>
                    <td className="p-2 font-mono"><Link className="text-[var(--link)] hover:underline" href={`/finance/petty-cash-ious/${iou.id}`}>{iou.number}</Link></td>
                    <td className="p-2">{iou.serviceJobNumber ?? "-"}</td>
                    <td className="p-2"><div>{iou.requestedByName}</div><div className="text-xs text-zinc-500">{iou.purpose}</div></td>
                    <td className="p-2 text-right">{money(iou.amount)}</td>
                  </tr>
                ))}
                {selectableIous.length === 0 ? <tr><td colSpan={5} className="p-4 text-center text-sm text-zinc-500">No submitted IOUs are waiting to be batched.</td></tr> : null}
              </tbody>
            </Table>
          </div>
          <div className="mt-3 flex items-center justify-between gap-3">
            <div className="text-sm">Selected total: <span className="font-semibold">{money(selectedTotal)}</span></div>
            <Button disabled={busy !== null || !fundId || !approverId || selectedIouIds.length === 0} onClick={() => void run("create", async () => {
              await apiPost<PettyCashIouBatch>("finance/petty-cash-iou-batches", { pettyCashFundId: fundId, assignedApproverUserId: approverId, pettyCashIouIds: selectedIouIds });
              setSelectedIouIds([]);
            })}>{busy === "create" ? "Sending..." : "Send Batch for Approval"}</Button>
          </div>
        </Card>
      ) : null}

      {error ? <div className="rounded-md border border-red-300 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}

      {batches.map((batch) => (
        <Card key={batch.id}>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <div className="font-semibold"><span className="font-mono">{batch.number}</span> · Fund {batch.pettyCashFundCode ?? batch.pettyCashFundId}</div>
              <div className="text-sm text-zinc-500">{statusLabel[batch.status] ?? batch.status} · Accountant {batch.reviewerName} · Approver {batch.assignedApproverName}</div>
            </div>
            <div className="text-right text-sm"><div>Requested <b>{money(batch.requestedTotal)}</b></div><div>Approved <b>{money(batch.approvedTotal)}</b></div></div>
          </div>
          <div className="mt-3 overflow-auto">
            <Table>
              <thead><tr><th className="py-2 pr-3 text-left">IOU</th><th className="py-2 pr-3 text-left">Job</th><th className="py-2 pr-3 text-left">Requester</th><th className="py-2 pr-3 text-left">Description</th><th className="py-2 pr-3 text-right">Requested</th><th className="py-2 text-right">Approved</th></tr></thead>
              <tbody>{batch.lines.map((line) => (
                <tr key={line.id} className="border-t border-[var(--card-border)]">
                  <td className="py-2 pr-3 font-mono"><Link className="text-[var(--link)] hover:underline" href={`/finance/petty-cash-ious/${line.pettyCashIouId}`}>{line.iouNumber}</Link></td>
                  <td className="py-2 pr-3">{line.serviceJobNumber ?? "-"}</td>
                  <td className="py-2 pr-3">{line.requestedByName}</td>
                  <td className="max-w-sm py-2 pr-3 text-zinc-500">{line.purpose}</td>
                  <td className="py-2 pr-3 text-right">{money(line.requestedAmount)}</td>
                  <td className="py-2 text-right">{batch.status === 1 && batch.isAssignedApprover ? <Input className="ml-auto w-32 text-right" type="number" min="0" max={line.requestedAmount} step="0.01" value={approvedAmounts[line.id] ?? String(line.approvedAmount)} onChange={(event) => setApprovedAmounts((current) => ({ ...current, [line.id]: event.target.value }))} /> : money(line.approvedAmount)}</td>
                </tr>
              ))}</tbody>
            </Table>
          </div>
          <div className="mt-3 flex flex-wrap items-end justify-end gap-2">
            {batch.status === 1 && batch.isAssignedApprover ? <Button disabled={busy !== null} onClick={() => void run(`${batch.id}-approve-assigned`, () => apiPostNoContent(`finance/petty-cash-iou-batches/${batch.id}/approve-assigned`, { lines: batch.lines.map((line) => ({ lineId: line.id, approvedAmount: Number(approvedAmounts[line.id] ?? line.approvedAmount) })) }))}>Approve Amounts & Return</Button> : null}
            {batch.status === 2 && batch.isReviewer ? <Button disabled={busy !== null} onClick={() => void run(`${batch.id}-head-office`, () => apiPostNoContent(`finance/petty-cash-iou-batches/${batch.id}/submit-head-office`, {}))}>Submit Batch to Head Office</Button> : null}
            {batch.status === 3 && permissionSet.has("Finance.PettyCashIou.Approve") ? <Button disabled={busy !== null} onClick={() => void run(`${batch.id}-approve`, () => apiPostNoContent(`finance/petty-cash-iou-batches/${batch.id}/approve`, {}))}>Head Office Approve Batch</Button> : null}
            {batch.status === 3 && permissionSet.has("Finance.PettyCashIou.Reject") ? <><Textarea className="min-h-9 w-64" placeholder="Rejection reason" value={rejectReasons[batch.id] ?? ""} onChange={(event) => setRejectReasons((current) => ({ ...current, [batch.id]: event.target.value }))} /><SecondaryButton disabled={busy !== null || !(rejectReasons[batch.id] ?? "").trim()} onClick={() => void run(`${batch.id}-reject`, () => apiPostNoContent(`finance/petty-cash-iou-batches/${batch.id}/reject`, { reason: rejectReasons[batch.id] }))}>Reject Batch</SecondaryButton></> : null}
            {batch.status === 4 ? <div className="text-sm text-emerald-700 dark:text-emerald-400">Approved IOUs may be released from the fund's available cash. Replenishment is managed separately.</div> : null}
          </div>
          {batch.fundingReference ? <div className="mt-2 text-xs text-zinc-500">Funding reference: {batch.fundingReference}</div> : null}
          {batch.rejectionReason ? <div className="mt-2 text-xs text-red-600">Rejected: {batch.rejectionReason}</div> : null}
        </Card>
      ))}
    </div>
  );
}
