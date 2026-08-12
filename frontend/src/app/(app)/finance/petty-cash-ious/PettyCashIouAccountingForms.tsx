"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { ConfirmActionDialog } from "@/components/ConfirmActionDialog";
import { Button, Input } from "@/components/ui";

function money(value: number): string {
  return value.toFixed(2);
}

/**
 * A bill the holder brought back, entered on the advance itself. The expense voucher behind it is
 * created and maintained by the server, so the custodian never leaves this record - attaching in
 * one place and settling in another was the same work twice.
 */
export function PettyCashIouBillAddForm({ iouId, disabled }: { iouId: string; disabled: boolean }) {
  const router = useRouter();
  const [description, setDescription] = useState("");
  const [amount, setAmount] = useState("");
  const [receiptReference, setReceiptReference] = useState("");
  const [billableToCustomer, setBillableToCustomer] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`finance/petty-cash-ious/${iouId}/bills`, {
        description: description.trim(),
        amount: Number(amount),
        billableToCustomer,
        receiptReference: receiptReference.trim() || null,
      });
      setDescription("");
      setAmount("");
      setReceiptReference("");
      setBillableToCustomer(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-4">
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium">What was bought</label>
          <Input
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="From the bill"
            required
            disabled={disabled}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Amount</label>
          <Input
            value={amount}
            inputMode="decimal"
            onChange={(event) => setAmount(event.target.value)}
            placeholder="0.00"
            required
            disabled={disabled}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Bill no. (optional)</label>
          <Input
            value={receiptReference}
            onChange={(event) => setReceiptReference(event.target.value)}
            disabled={disabled}
          />
        </div>
      </div>

      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={billableToCustomer}
          onChange={(event) => setBillableToCustomer(event.target.checked)}
          disabled={disabled}
        />
        Billable to the customer
      </label>

      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}

      <Button type="submit" disabled={busy || disabled}>
        {busy ? "Adding..." : "Add Bill"}
      </Button>
    </form>
  );
}

/** Cash handed back, in whatever instalments it arrives. */
export function PettyCashIouReturnForm({
  iouId,
  outstandingAmount,
  disabled,
}: {
  iouId: string;
  outstandingAmount: number;
  disabled: boolean;
}) {
  const router = useRouter();
  const [amount, setAmount] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const value = Number(amount);
  const invalid = !Number.isFinite(value) || value <= 0 || value > outstandingAmount;

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`finance/petty-cash-ious/${iouId}/return-balance`, { amount: value });
      setAmount("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-wrap items-end gap-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Cash returned now</label>
        <Input
          className="w-36"
          value={amount}
          inputMode="decimal"
          onChange={(event) => setAmount(event.target.value)}
          placeholder="0.00"
          disabled={disabled}
        />
      </div>
      <Button type="submit" disabled={busy || invalid || disabled}>
        {busy ? "Recording..." : "Record Return"}
      </Button>
      <p className="text-xs text-zinc-500">
        Goes back to the category this advance was drawn from. Part now and part later is fine.
      </p>
      {error ? <div className="w-full text-xs text-red-700 dark:text-red-300">{error}</div> : null}
    </form>
  );
}

/** Closing the advance, and head office signing it off. */
export function PettyCashIouSettleActions({
  iouId,
  status,
  amount,
  returnedAmount,
  claimedAmount,
  remainingReleaseAmount = 0,
  permissions,
}: {
  iouId: string;
  status: number;
  amount: number;
  returnedAmount: number;
  claimedAmount: number;
  remainingReleaseAmount?: number;
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const canSettle = status === 3 && remainingReleaseAmount <= 0 && permissionSet.has("Finance.PettyCashIou.Settle");
  const canApprove = status === 4 && permissionSet.has("Finance.PettyCashIou.Approve");
  const [pending, setPending] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const spent = amount - returnedAmount;
  const unaccounted = spent - claimedAmount;

  async function run(action: string) {
    setError(null);
    setBusy(action);
    try {
      await apiPostNoContent(`finance/petty-cash-ious/${iouId}/${action}`, {});
      setPending(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  if (!canSettle && !canApprove) {
    if (status === 3 && remainingReleaseAmount > 0) {
      return <p className="text-sm text-zinc-500">Release the remaining {money(remainingReleaseAmount)} before settling this advance.</p>;
    }

    return status === 7 ? (
      <p className="text-sm text-zinc-500">Settlement approved. This advance is closed.</p>
    ) : (
      <p className="text-sm text-zinc-500">Nothing to do here at this status.</p>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        {canSettle ? (
          <Button type="button" disabled={busy !== null} onClick={() => setPending(true)}>
            Settle / Account
          </Button>
        ) : null}
        {canApprove ? (
          <Button type="button" disabled={busy !== null} onClick={() => void run("approve-settlement")}>
            {busy === "approve-settlement" ? "Approving..." : "Approve Settlement"}
          </Button>
        ) : null}
        {canApprove ? (
          <span className="text-xs text-zinc-500">
            The bills are already in job cost. Approval confirms and closes the advance; nothing more can be added after.
          </span>
        ) : null}
      </div>

      {unaccounted > 0 ? (
        <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900 dark:border-amber-900/40 dark:bg-amber-950/40 dark:text-amber-200">
          {money(unaccounted)} of what was spent has no bill against it yet. Add the bills above before settling,
          or it will never reach job cost.
        </div>
      ) : null}

      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}

      <ConfirmActionDialog
        open={pending}
        title="Settle this advance"
        confirmWord="SETTLE"
        confirmLabel="Settle / Account"
        busy={busy === "settle"}
        onCancel={() => setPending(false)}
        onConfirm={() => void run("settle")}
        description={
          <>
            Records <span className="font-semibold">{money(spent)}</span> as spent — the advance of{" "}
            {money(amount)} less the {money(returnedAmount)} returned.
            {unaccounted > 0 ? (
              <span className="text-amber-700 dark:text-amber-300">
                {" "}
                Only {money(claimedAmount)} is covered by bills, so{" "}
                <span className="font-semibold">{money(unaccounted)}</span> stays unaccounted for.
              </span>
            ) : null}{" "}
            Entered bills move into job cost now. More bills can still be added until head office approves, and those bills
            will also enter job cost immediately.
          </>
        }
      />
    </div>
  );
}
