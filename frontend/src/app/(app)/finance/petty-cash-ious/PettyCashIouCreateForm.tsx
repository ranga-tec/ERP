"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost, apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string };
type PettyCashIouDto = { id: string; number: string };

export function PettyCashIouCreateForm({ serviceJobs }: { serviceJobs: ServiceJobRef[] }) {
  const router = useRouter();
  const jobOptions = useMemo(
    () => serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number)),
    [serviceJobs],
  );
  const [serviceJobId, setServiceJobId] = useState("");
  const [amount, setAmount] = useState("");
  const [purpose, setPurpose] = useState("");
  const [expectedSettlementAt, setExpectedSettlementAt] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const iou = await apiPost<PettyCashIouDto>("finance/petty-cash-ious", {
        serviceJobId,
        amount: Number(amount),
        purpose: purpose.trim(),
        expectedSettlementAt: expectedSettlementAt || null,
      });
      await apiPostNoContent(`finance/petty-cash-ious/${iou.id}/submit`, {});
      setServiceJobId("");
      setAmount("");
      setPurpose("");
      setExpectedSettlementAt("");
      router.refresh();
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
          <Select value={serviceJobId} onChange={(e) => setServiceJobId(e.target.value)} required>
            <option value="" disabled>Select...</option>
            {jobOptions.map((job) => (
              <option key={job.id} value={job.id}>{job.number}</option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Amount</label>
          <Input inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Expected settlement</label>
          <Input type="date" value={expectedSettlementAt} onChange={(e) => setExpectedSettlementAt(e.target.value)} />
        </div>
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium">Purpose</label>
        <Textarea value={purpose} onChange={(e) => setPurpose(e.target.value)} required />
      </div>
      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
      <Button type="submit" disabled={busy}>{busy ? "Submitting..." : "Create IOU"}</Button>
    </form>
  );
}
