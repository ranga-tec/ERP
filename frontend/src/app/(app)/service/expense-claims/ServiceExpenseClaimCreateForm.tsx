"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string; kind: number };
type ServiceExpenseClaimDto = { id: string; number: string };

const kindLabel: Record<number, string> = {
  0: "Service",
  1: "Repair",
};

export function ServiceExpenseClaimCreateForm({
  serviceJobs,
}: {
  serviceJobs: ServiceJobRef[];
}) {
  const router = useRouter();
  const [serviceJobId, setServiceJobId] = useState("");
  const [claimedByName, setClaimedByName] = useState("");
  const [fundingSource, setFundingSource] = useState("1");
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
        serviceJobId,
        claimedByName: claimedByName.trim() || null,
        fundingSource: Number(fundingSource),
        expenseDate: expenseDate ? new Date(expenseDate).toISOString() : null,
        merchantName: merchantName.trim() || null,
        receiptReference: receiptReference.trim() || null,
        notes: notes.trim() || null,
      });

      router.push(`/service/expense-claims/${claim.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  const sortedJobs = serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number));

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Service job</label>
          <Select value={serviceJobId} onChange={(event) => setServiceJobId(event.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {sortedJobs.map((job) => (
              <option key={job.id} value={job.id}>
                {job.number} - {kindLabel[job.kind] ?? job.kind}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Funding source</label>
          <Select value={fundingSource} onChange={(event) => setFundingSource(event.target.value)}>
            <option value="1">Out of Pocket</option>
            <option value="2">Petty Cash</option>
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
          <label className="mb-1 block text-sm font-medium">Receipt ref (optional)</label>
          <Input value={receiptReference} onChange={(event) => setReceiptReference(event.target.value)} />
        </div>
      </div>

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
        {busy ? "Creating..." : "Create Expense Claim"}
      </Button>
    </form>
  );
}
