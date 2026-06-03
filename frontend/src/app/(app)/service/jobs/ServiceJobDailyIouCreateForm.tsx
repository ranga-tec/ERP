"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost, apiPostNoContent } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type DailySheetRef = { id: string; number: string; status: number };
type PettyCashIouDto = { id: string; number: string; status: number };

export function ServiceJobDailyIouCreateForm({
  serviceJobId,
  dailySheets,
  disabled,
}: {
  serviceJobId: string;
  dailySheets: DailySheetRef[];
  disabled?: boolean;
}) {
  const router = useRouter();
  const [dailySheetId, setDailySheetId] = useState("");
  const [amount, setAmount] = useState("");
  const [expectedSettlementAt, setExpectedSettlementAt] = useState("");
  const [purpose, setPurpose] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);
    setBusy(true);
    try {
      const iou = await apiPost<PettyCashIouDto>("finance/petty-cash-ious", {
        serviceJobId,
        serviceJobDailySheetId: dailySheetId || null,
        requestedByName: null,
        amount: Number(amount),
        purpose: purpose.trim(),
        expectedSettlementAt: expectedSettlementAt || null,
      });
      await apiPostNoContent(`finance/petty-cash-ious/${iou.id}/submit`, {});
      setSuccess(`${iou.number} request sent. It is now visible in the Job IOU Register and waiting for finance approval.`);
      setDailySheetId("");
      setAmount("");
      setExpectedSettlementAt("");
      setPurpose("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="rounded-md border border-sky-200 bg-sky-50 p-3 text-sm text-sky-900 dark:border-sky-900/40 dark:bg-sky-950/30 dark:text-sky-100">
        Requester is recorded from the signed-in system user. It cannot be typed manually.
      </div>
      <div className="grid gap-3 lg:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Daily sheet</label>
          <Select value={dailySheetId} onChange={(event) => setDailySheetId(event.target.value)} disabled={disabled || busy}>
            <option value="">Unlinked</option>
            {dailySheets.filter((sheet) => sheet.status !== 2).map((sheet) => <option key={sheet.id} value={sheet.id}>{sheet.number}</option>)}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Amount</label>
          <Input value={amount} onChange={(event) => setAmount(event.target.value)} inputMode="decimal" disabled={disabled || busy} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Expected settlement</label>
          <Input type="date" value={expectedSettlementAt} onChange={(event) => setExpectedSettlementAt(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium">Purpose</label>
        <Textarea value={purpose} onChange={(event) => setPurpose(event.target.value)} disabled={disabled || busy} required />
      </div>
      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
      {success ? <div className="rounded-md border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-900 dark:border-emerald-900/40 dark:bg-emerald-950/30 dark:text-emerald-100">{success}</div> : null}
      <Button type="submit" disabled={disabled || busy}>{busy ? "Creating..." : "Create IOU Advance"}</Button>
    </form>
  );
}
