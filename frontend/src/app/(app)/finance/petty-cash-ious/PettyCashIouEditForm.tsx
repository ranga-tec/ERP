"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPutNoContent } from "@/lib/api-client";
import { Button, DecimalInput, Input, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string };
type EditableIou = {
  id: string;
  serviceJobId: string | null;
  amount: number;
  purpose: string;
  expectedSettlementAt: string | null;
};

export function PettyCashIouEditForm({
  iou,
  serviceJobs,
  onSaved,
}: {
  iou: EditableIou;
  serviceJobs: ServiceJobRef[];
  onSaved?: () => void;
}) {
  const router = useRouter();
  const jobOptions = useMemo(
    () => serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number)),
    [serviceJobs],
  );
  const [serviceJobId, setServiceJobId] = useState(iou.serviceJobId ?? "");
  const [amount, setAmount] = useState(String(iou.amount));
  const [purpose, setPurpose] = useState(iou.purpose);
  const [expectedSettlementAt, setExpectedSettlementAt] = useState(
    iou.expectedSettlementAt?.slice(0, 10) ?? "",
  );
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPutNoContent(`finance/petty-cash-ious/${iou.id}`, {
        serviceJobId,
        amount: Number(amount),
        purpose: purpose.trim(),
        expectedSettlementAt: expectedSettlementAt || null,
      });
      router.refresh();
      onSaved?.();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Job number</label>
          <Select value={serviceJobId} onChange={(event) => setServiceJobId(event.target.value)} required>
            <option value="" disabled>Select...</option>
            {jobOptions.map((job) => (
              <option key={job.id} value={job.id}>{job.number}</option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Amount</label>
          <DecimalInput value={amount} onChange={(event) => setAmount(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Expected settlement</label>
          <Input type="date" value={expectedSettlementAt} onChange={(event) => setExpectedSettlementAt(event.target.value)} />
        </div>
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium">Purpose</label>
        <Textarea value={purpose} onChange={(event) => setPurpose(event.target.value)} required />
      </div>
      <p className="text-xs text-zinc-500">
        Saving keeps the IOU submitted for approval. The requester and audit history are unchanged.
      </p>
      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
      <Button type="submit" disabled={busy || !serviceJobId || !amount || !purpose.trim()}>
        {busy ? "Saving..." : "Save Changes"}
      </Button>
    </form>
  );
}
