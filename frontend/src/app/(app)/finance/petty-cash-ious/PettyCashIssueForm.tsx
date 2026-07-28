"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type FundRef = { id: string; code: string; name: string };
type StaffRef = { userId: string; name: string; email?: string | null };
type CreatedDto = { id: string; number: string };

/**
 * Recording a pre-printed IOU slip that has just been written.
 *
 * Kept to what the slip itself asks for at the counter - its number, who took the cash, how much
 * and what for. Location/Dept, approvals and settlement details are all on the paper too, and are
 * captured later against the recorded advance rather than being retyped while someone waits.
 *
 * The slip number is the document number. The book issues it; the system records it. That is why
 * this form has no "create" behaviour for numbering and why entering the same slip twice is
 * refused.
 */
export function PettyCashIssueForm({
  funds,
  staff,
}: {
  funds: FundRef[];
  staff: StaffRef[];
}) {
  const router = useRouter();
  const [slipNumber, setSlipNumber] = useState("");
  const [issuedToUserId, setIssuedToUserId] = useState("");
  const [amount, setAmount] = useState("");
  const [purpose, setPurpose] = useState("");
  const [pettyCashFundId, setPettyCashFundId] = useState(funds[0]?.id ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPost<CreatedDto>("finance/petty-cash-ious/issue-directly", {
        slipNumber: slipNumber.trim(),
        amount: Number(amount),
        purpose: purpose.trim(),
        pettyCashFundId,
        issuedToUserId: issuedToUserId || null,
      });
      setSlipNumber("");
      setAmount("");
      setPurpose("");
      setIssuedToUserId("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">I.O.U. No.</label>
          <Input
            value={slipNumber}
            onChange={(event) => setSlipNumber(event.target.value)}
            placeholder="e.g. 4001"
            required
            autoFocus
          />
          <p className="mt-1 text-xs text-zinc-500">
            The number printed on the slip. This is the advance&apos;s number here too.
          </p>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Issued to (optional)</label>
          <Select value={issuedToUserId} onChange={(event) => setIssuedToUserId(event.target.value)}>
            <option value="">Not a named person</option>
            {staff.map((person) => (
              <option key={person.userId} value={person.userId}>{person.name}</option>
            ))}
          </Select>
          <p className="mt-1 text-xs text-zinc-500">
            Whoever took the cash. The advance is theirs to settle.
          </p>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Amount</label>
          <Input
            value={amount}
            inputMode="decimal"
            onChange={(event) => setAmount(event.target.value)}
            placeholder="0.00"
            required
          />
        </div>
        {funds.length > 1 ? (
          <div>
            <label className="mb-1 block text-sm font-medium">From fund</label>
            <Select value={pettyCashFundId} onChange={(event) => setPettyCashFundId(event.target.value)} required>
              <option value="" disabled>Select...</option>
              {funds.map((fund) => (
                <option key={fund.id} value={fund.id}>{fund.code} - {fund.name}</option>
              ))}
            </Select>
          </div>
        ) : null}
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Reason</label>
        <Input
          value={purpose}
          onChange={(event) => setPurpose(event.target.value)}
          placeholder="What the cash is for"
          required
        />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      {funds.length === 0 ? (
        <div className="text-xs text-amber-700 dark:text-amber-400">
          Activate a petty cash fund first - the cash has to come out of one.
        </div>
      ) : null}

      <Button type="submit" disabled={busy || !pettyCashFundId}>
        {busy ? "Recording..." : "Record IOU"}
      </Button>
    </form>
  );
}
