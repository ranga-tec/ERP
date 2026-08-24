"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { ConfirmActionDialog } from "@/components/ConfirmActionDialog";
import { Button, Input, SecondaryButton, Select, Textarea } from "@/components/ui";

function money(value: number): string {
  return value.toFixed(2);
}

/**
 * A bill the holder brought back, entered on the advance itself. The expense voucher behind it is
 * created and maintained by the server, so the custodian never leaves this record - attaching in
 * one place and settling in another was the same work twice.
 */
export function PettyCashIouBillAddForm({
  iouId,
  disabled,
  expenseAccounts,
}: {
  iouId: string;
  disabled: boolean;
  expenseAccounts: { id: string; code: string; name: string }[];
}) {
  const router = useRouter();
  const [description, setDescription] = useState("");
  const [amount, setAmount] = useState("");
  const [receiptReference, setReceiptReference] = useState("");
  const [expenseAccountId, setExpenseAccountId] = useState("");
  const [billableToCustomer, setBillableToCustomer] = useState(false);
  const [missingReceipt, setMissingReceipt] = useState(false);
  const [missingReceiptReason, setMissingReceiptReason] = useState("");
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
        receiptReference: missingReceipt ? null : receiptReference.trim() || null,
        expenseAccountId,
        missingReceipt,
        missingReceiptReason: missingReceipt ? missingReceiptReason.trim() : null,
      });
      setDescription("");
      setAmount("");
      setReceiptReference("");
      setExpenseAccountId("");
      setBillableToCustomer(false);
      setMissingReceipt(false);
      setMissingReceiptReason("");
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
          <label className="mb-1 block text-sm font-medium">Receipt / bill number</label>
          <Input
            value={receiptReference}
            onChange={(event) => setReceiptReference(event.target.value)}
            required={!missingReceipt}
            disabled={disabled || missingReceipt}
          />
        </div>
      </div>

      <div className="space-y-2">
        <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={missingReceipt} onChange={(event) => setMissingReceipt(event.target.checked)} disabled={disabled} />Missing receipt — request higher-level approval</label>
        {missingReceipt ? <div><label className="mb-1 block text-sm font-medium">Missing receipt reason *</label><Input value={missingReceiptReason} onChange={(event) => setMissingReceiptReason(event.target.value)} required disabled={disabled} /></div> : null}
        <p className="text-xs text-zinc-500">After adding a normal bill, upload its receipt file in the line-evidence panel.</p>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Expense category/account *</label>
        <Select value={expenseAccountId} onChange={(event) => setExpenseAccountId(event.target.value)} required disabled={disabled}>
          <option value="">Select expense account...</option>
          {expenseAccounts.map((account) => <option key={account.id} value={account.id}>{account.code} - {account.name}</option>)}
        </Select>
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
        Goes back to the overall petty-cash fund. Part now and part later is fine.
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
  settlementExceptionAmount = 0,
  settlementExceptionReason,
  permissions,
}: {
  iouId: string;
  status: number;
  amount: number;
  returnedAmount: number;
  claimedAmount: number;
  remainingReleaseAmount?: number;
  settlementExceptionAmount?: number;
  settlementExceptionReason?: string | null;
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const canSettle = status === 3 && remainingReleaseAmount <= 0 && permissionSet.has("Finance.PettyCashIou.Settle");
  const canApprove = status === 4 && permissionSet.has("Finance.PettyCashIou.Approve");
  const [pending, setPending] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [exceptionReason, setExceptionReason] = useState("");

  const spent = amount - returnedAmount;
  const unaccounted = spent - claimedAmount;
  const hasApprovedException = settlementExceptionAmount > 0;
  const canApproveException = status === 3
    && unaccounted > 0
    && permissionSet.has("Finance.PettyCash.ReceiptExceptionApprove");

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

  async function approveException() {
    if (!exceptionReason.trim()) return;
    setError(null);
    setBusy("approve-settlement-exception");
    try {
      await apiPostNoContent(`finance/petty-cash-ious/${iouId}/approve-settlement-exception`, {
        reason: exceptionReason.trim(),
      });
      setExceptionReason("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  if (!canSettle && !canApprove && !canApproveException) {
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
          <Button type="button" disabled={busy !== null || unaccounted > 0} onClick={() => setPending(true)}>
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
          {money(unaccounted)} is not supported by receipts or returned cash. Settlement is blocked until the support is added,
          the cash is returned, or a higher-level exception is approved.
        </div>
      ) : null}

      {hasApprovedException ? (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-900 dark:border-emerald-900/40 dark:bg-emerald-950/40 dark:text-emerald-200">
          Exception approved for {money(settlementExceptionAmount)}. Reason: {settlementExceptionReason ?? "Documented separately"}.
        </div>
      ) : null}

      {canApproveException ? (
        <div className="space-y-2 rounded-md border border-amber-300 p-3 dark:border-amber-900">
          <label className="block text-sm font-medium">Higher-level settlement exception reason *</label>
          <Textarea value={exceptionReason} onChange={(event) => setExceptionReason(event.target.value)} placeholder="Explain the shortage or missing support and reference the attached evidence" />
          <SecondaryButton type="button" disabled={busy !== null || !exceptionReason.trim()} onClick={() => void approveException()}>
            {busy === "approve-settlement-exception" ? "Approving..." : `Approve ${money(unaccounted)} Exception`}
          </SecondaryButton>
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
                <span className="font-semibold">{money(unaccounted)}</span> is covered by the approved exception.
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
