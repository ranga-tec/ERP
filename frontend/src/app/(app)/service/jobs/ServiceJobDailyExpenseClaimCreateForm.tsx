"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type DailySheetRef = { id: string; number: string; status: number };
type ClaimDto = { id: string };

export function ServiceJobDailyExpenseClaimCreateForm({
  serviceJobId,
  dailySheets,
  defaultFundingSource = "1",
  lockFundingSource = false,
  submitLabel = "Create Expense Voucher",
  disabled,
}: {
  serviceJobId: string;
  dailySheets: DailySheetRef[];
  defaultFundingSource?: "1" | "2";
  lockFundingSource?: boolean;
  submitLabel?: string;
  disabled?: boolean;
}) {
  const router = useRouter();
  const [dailySheetId, setDailySheetId] = useState("");
  const [fundingSource, setFundingSource] = useState(defaultFundingSource);
  const [expenseDate, setExpenseDate] = useState("");
  const [merchantName, setMerchantName] = useState("");
  const [receiptReference, setReceiptReference] = useState("");
  const [handoverMethod, setHandoverMethod] = useState("cash-handover");
  const [handoverMethodOther, setHandoverMethodOther] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isPettyCash = fundingSource === "2";
  const handoverMethodLabel =
    handoverMethod === "bank-deposit"
      ? "Bank deposit"
      : handoverMethod === "cash-handover"
        ? "Cash handover"
        : handoverMethodOther.trim() || "Other";
  const resolvedNotes = [
    isPettyCash ? `Payment handover: ${handoverMethodLabel}` : null,
    notes.trim() || null,
  ]
    .filter(Boolean)
    .join("\n");

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const claim = await apiPost<ClaimDto>("service/expense-claims", {
        serviceJobId,
        serviceJobDailySheetId: dailySheetId || null,
        claimedByName: null,
        fundingSource: Number(fundingSource),
        expenseDate: expenseDate ? new Date(expenseDate).toISOString() : null,
        merchantName: merchantName.trim() || null,
        receiptReference: receiptReference.trim() || null,
        notes: resolvedNotes || null,
      });
      router.push(`/service/expense-claims/${claim.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="rounded-md border border-sky-200 bg-sky-50 p-3 text-sm text-sky-900 dark:border-sky-900/40 dark:bg-sky-950/30 dark:text-sky-100">
        {isPettyCash
          ? "Voucher receiver is recorded from the signed-in system user. Enter the accountant-issued bill number and how cash was handed over."
          : "Claimant is recorded from the signed-in system user. It cannot be typed manually."}
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
          <label className="mb-1 block text-sm font-medium">Funding</label>
          <Select
            value={fundingSource}
            onChange={(event) => setFundingSource(event.target.value === "2" ? "2" : "1")}
            disabled={disabled || busy || lockFundingSource}
          >
            <option value="1">Out of Pocket</option>
            <option value="2">Petty Cash</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">{isPettyCash ? "Voucher date" : "Expense date"}</label>
          <Input type="datetime-local" value={expenseDate} onChange={(event) => setExpenseDate(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      <div className="grid gap-3 lg:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Merchant / vendor</label>
          <Input value={merchantName} onChange={(event) => setMerchantName(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">{isPettyCash ? "Bill number" : "Receipt ref"}</label>
          <Input value={receiptReference} onChange={(event) => setReceiptReference(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      {isPettyCash ? (
        <div className="grid gap-3 lg:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium">Payment handover</label>
            <Select value={handoverMethod} onChange={(event) => setHandoverMethod(event.target.value)} disabled={disabled || busy}>
              <option value="cash-handover">Cash handover</option>
              <option value="bank-deposit">Bank deposit</option>
              <option value="other">Other</option>
            </Select>
          </div>
          {handoverMethod === "other" ? (
            <div>
              <label className="mb-1 block text-sm font-medium">Other handover method</label>
              <Input value={handoverMethodOther} onChange={(event) => setHandoverMethodOther(event.target.value)} disabled={disabled || busy} />
            </div>
          ) : null}
        </div>
      ) : null}
      <div>
        <label className="mb-1 block text-sm font-medium">Notes</label>
        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} disabled={disabled || busy} />
      </div>
      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
      <Button type="submit" disabled={disabled || busy}>{busy ? "Creating..." : submitLabel}</Button>
    </form>
  );
}
