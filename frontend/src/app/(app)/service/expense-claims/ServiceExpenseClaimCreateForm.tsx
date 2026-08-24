"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string; kind: number };
type ServiceExpenseClaimDto = { id: string; number: string };
type PettyCashIouRef = {
  id: string;
  number: string;
  serviceJobId: string;
  status: number;
  amount: number;
};

// Released and Settled are the only states where cash has actually left the fund, so they are the
// only advances an expense can have been paid from. The API enforces the same rule.
const fundedIouStatuses = new Set([3, 4, 7]);

const kindLabel: Record<number, string> = {
  0: "Service",
  1: "Repair",
  2: "PDI",
  3: "Warranty",
  4: "Inspection",
};

export function ServiceExpenseClaimCreateForm({
  serviceJobs,
  pettyCashIous = [],
}: {
  serviceJobs: ServiceJobRef[];
  pettyCashIous?: PettyCashIouRef[];
}) {
  const router = useRouter();
  const [serviceJobId, setServiceJobId] = useState("");
  const [claimedByName, setClaimedByName] = useState("");
  const [fundingSource, setFundingSource] = useState("1");
  const [pettyCashIouId, setPettyCashIouId] = useState("");
  const [costCenterCode, setCostCenterCode] = useState("");
  const [expenseDate, setExpenseDate] = useState("");
  const [merchantName, setMerchantName] = useState("");
  const [receiptReference, setReceiptReference] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const claim = await apiPost<ServiceExpenseClaimDto>("service/expense-claims", {
        serviceJobId: serviceJobId || null,
        claimedByName: claimedByName.trim() || null,
        fundingSource: Number(fundingSource),
        expenseDate: expenseDate ? new Date(expenseDate).toISOString() : null,
        merchantName: merchantName.trim() || null,
        receiptReference: receiptReference.trim() || null,
        notes: notes.trim() || null,
        pettyCashIouId: pettyCashIouId || null,
        pettyCashRequestLineId: null,
        costCenterCode: costCenterCode.trim() || null,
      });

      router.push(`/service/expense-claims/${claim.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  const sortedJobs = serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number));

  const isPettyCash = fundingSource === "2";
  const availableIous = pettyCashIous
    .filter((iou) => iou.serviceJobId === serviceJobId && fundedIouStatuses.has(iou.status))
    .sort((a, b) => b.number.localeCompare(a.number));

  function selectJob(nextJobId: string) {
    setServiceJobId(nextJobId);
    setPettyCashIouId("");
  }

  function selectFundingSource(nextFundingSource: string) {
    setFundingSource(nextFundingSource);
    setPettyCashIouId("");
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Job Order</label>
          <Select value={serviceJobId} onChange={(event) => selectJob(event.target.value)}>
            <option value="">Not job related (overhead)</option>
            {sortedJobs.map((job) => (
              <option key={job.id} value={job.id}>
                {job.number} - {kindLabel[job.kind] ?? job.kind}
              </option>
            ))}
          </Select>
          {!serviceJobId ? (
            <p className="mt-1 text-xs text-zinc-500">
              Transport, emergency callouts and the like. Overhead is not charged to any job&apos;s cost.
            </p>
          ) : null}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Funding source</label>
          <Select value={fundingSource} onChange={(event) => selectFundingSource(event.target.value)}>
            <option value="1">Out of Pocket</option>
            <option value="2">Petty Cash Fund</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Expense date/time (optional)</label>
          <Input type="datetime-local" value={expenseDate} onChange={(event) => setExpenseDate(event.target.value)} />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Claimed by (optional)</label>
          <Input
            value={claimedByName}
            onChange={(event) => setClaimedByName(event.target.value)}
            placeholder="Defaults to signed-in user"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Merchant / vendor (optional)</label>
          <Input value={merchantName} onChange={(event) => setMerchantName(event.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Voucher receipt ref (legacy/optional)</label>
          <Input value={receiptReference} onChange={(event) => setReceiptReference(event.target.value)} />
          <p className="mt-1 text-xs text-zinc-500">Each expense line records and attaches its own receipt.</p>
        </div>
      </div>

      {isPettyCash && !serviceJobId ? (
        <div>
          <label className="mb-1 block text-sm font-medium">Cost centre code *</label>
          <Input value={costCenterCode} onChange={(event) => setCostCenterCode(event.target.value)} required placeholder="Required for non-job petty-cash spending" />
          <p className="mt-1 text-xs text-zinc-500">This classifies overhead without creating a cash sub-ledger.</p>
        </div>
      ) : null}

      {isPettyCash ? (
        <div>
          <label className="mb-1 block text-sm font-medium">
            Funded by IOU advance (optional)
          </label>
          <Select
            value={pettyCashIouId}
            onChange={(event) => setPettyCashIouId(event.target.value)}
            disabled={!serviceJobId || availableIous.length === 0}
          >
            <option value="">Not from an advance</option>
            {availableIous.map((iou) => (
              <option key={iou.id} value={iou.id}>
                {iou.number} - {iou.amount.toFixed(2)} advanced
              </option>
            ))}
          </Select>
          <p className="mt-1 text-xs text-zinc-500">
            {!serviceJobId
              ? "Pick a job order to see its released advances."
              : availableIous.length === 0
                ? "This job has no released advances to claim against."
                : "Linking the advance lets finance see what the cash was actually spent on."}
          </p>
        </div>
      ) : null}

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Textarea
          value={notes}
          onChange={(event) => setNotes(event.target.value)}
          placeholder="What was bought, why stock was not used, and whether it should be billed back to the customer."
        />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Expense Voucher"}
      </Button>
    </form>
  );
}
