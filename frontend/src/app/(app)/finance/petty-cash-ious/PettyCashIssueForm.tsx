"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string };
type FundRef = { id: string; code: string; name: string };
type StaffRef = { userId: string; name: string; email?: string | null };
type FundedCategoryRef = {
  id: string;
  requestNumber: string;
  category: number;
  serviceJobId?: string | null;
  customCategoryName?: string | null;
  purpose: string;
};

type CreatedDto = { id: string; number: string };

const CATEGORY_JOB_WISE = 1;

const categoryLabel: Record<number, string> = {
  1: "Job Wise",
  2: "Emergency Operation",
  3: "Transportation",
  4: "Custom",
};

function describeCategory(line: FundedCategoryRef): string {
  const name = line.category === 4 && line.customCategoryName
    ? line.customCategoryName
    : categoryLabel[line.category] ?? String(line.category);
  return `${line.requestNumber} - ${name} - ${line.purpose}`;
}

/**
 * Handing cash out of the float, with no prior request.
 *
 * Two different things happen depending on whether a person is named, and they are two different
 * accounting events rather than a cosmetic difference:
 *
 * - a staff member is chosen, so the cash is an advance they must settle, and it becomes their IOU;
 * - nobody is named, so it is a payment for something already bought. Nobody is left accountable,
 *   the bill is the entire support for it, and it is recorded as a settled expense voucher.
 *
 * That is why the receipt is mandatory on the payment branch and the bill number is only asked for
 * on the advance branch: on an advance the receipts arrive later, at settlement.
 */
export function PettyCashIssueForm({
  serviceJobs,
  funds,
  staff,
  fundedCategories = [],
}: {
  serviceJobs: ServiceJobRef[];
  funds: FundRef[];
  staff: StaffRef[];
  fundedCategories?: FundedCategoryRef[];
}) {
  const router = useRouter();
  const [issuedToUserId, setIssuedToUserId] = useState("");
  const [serviceJobId, setServiceJobId] = useState("");
  const [pettyCashFundId, setPettyCashFundId] = useState(funds[0]?.id ?? "");
  const [pettyCashRequestLineId, setPettyCashRequestLineId] = useState("");
  const [amount, setAmount] = useState("");
  const [purpose, setPurpose] = useState("");
  const [issueBillNumber, setIssueBillNumber] = useState("");
  const [receiptReference, setReceiptReference] = useState("");
  const [merchantName, setMerchantName] = useState("");
  const [billableToCustomer, setBillableToCustomer] = useState(false);
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isAdvance = issuedToUserId !== "";
  const sortedJobs = serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number));
  const availableCategories = fundedCategories.filter(
    (line) => line.category !== CATEGORY_JOB_WISE || line.serviceJobId === serviceJobId,
  );

  function selectJob(nextJobId: string) {
    setServiceJobId(nextJobId);
    setPettyCashRequestLineId("");
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      if (isAdvance) {
        const iou = await apiPost<CreatedDto>("finance/petty-cash-ious/issue-directly", {
          serviceJobId,
          amount: Number(amount),
          purpose: purpose.trim(),
          pettyCashFundId,
          issueBillNumber: issueBillNumber.trim(),
          pettyCashRequestLineId: pettyCashRequestLineId || null,
          issuedToUserId,
        });
        router.push(`/finance/petty-cash-ious?issued=${iou.number}`);
        router.refresh();
        return;
      }

      const voucher = await apiPost<CreatedDto>("service/expense-claims/pay-directly", {
        serviceJobId: serviceJobId || null,
        description: purpose.trim(),
        amount: Number(amount),
        billableToCustomer,
        merchantName: merchantName.trim() || null,
        receiptReference: receiptReference.trim(),
        notes: notes.trim() || null,
        pettyCashFundId,
        pettyCashRequestLineId: pettyCashRequestLineId || null,
      });
      // Straight to the voucher, which is where the bill itself gets attached.
      router.push(`/service/expense-claims/${voucher.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Issued to (optional)</label>
        <Select value={issuedToUserId} onChange={(event) => setIssuedToUserId(event.target.value)}>
          <option value="">Nobody - direct payment for a purchase</option>
          {staff.map((person) => (
            <option key={person.userId} value={person.userId}>{person.name}</option>
          ))}
        </Select>
        <p className="mt-1 text-xs text-zinc-500">
          {isAdvance
            ? "Recorded as this person's advance. They stay accountable until they settle it."
            : "No one is left accountable, so this is recorded as a paid expense voucher and the bill is required."}
        </p>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">
            Job order{isAdvance ? "" : " (optional)"}
          </label>
          <Select value={serviceJobId} onChange={(event) => selectJob(event.target.value)} required={isAdvance}>
            <option value="">{isAdvance ? "Select..." : "Not job related (overhead)"}</option>
            {sortedJobs.map((job) => (
              <option key={job.id} value={job.id}>{job.number}</option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">From fund</label>
          <Select value={pettyCashFundId} onChange={(event) => setPettyCashFundId(event.target.value)} required>
            <option value="" disabled>Select...</option>
            {funds.map((fund) => (
              <option key={fund.id} value={fund.id}>{fund.code} - {fund.name}</option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Amount</label>
          <Input value={amount} inputMode="decimal" onChange={(event) => setAmount(event.target.value)} placeholder="0.00" required />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Charge to funded category (optional)</label>
        <Select
          value={pettyCashRequestLineId}
          onChange={(event) => setPettyCashRequestLineId(event.target.value)}
          disabled={availableCategories.length === 0}
        >
          <option value="">Not from a funded category</option>
          {availableCategories.map((line) => (
            <option key={line.id} value={line.id}>{describeCategory(line)}</option>
          ))}
        </Select>
        <p className="mt-1 text-xs text-zinc-500">
          {availableCategories.length === 0
            ? "No category has money released against it yet."
            : "Draws the cash down against that category's sub-account."}
        </p>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">
          {isAdvance ? "Purpose" : "What was bought"}
        </label>
        <Input
          value={purpose}
          onChange={(event) => setPurpose(event.target.value)}
          placeholder={isAdvance ? "What the cash is for" : "Taxi to site, courier, etc."}
          required
        />
      </div>

      {isAdvance ? (
        <div>
          <label className="mb-1 block text-sm font-medium">Signed bill / voucher no.</label>
          <Input
            value={issueBillNumber}
            onChange={(event) => setIssueBillNumber(event.target.value)}
            placeholder="The slip they signed"
            required
          />
          <p className="mt-1 text-xs text-zinc-500">
            Required: with no written request behind it, the signed slip is the only record of the handover.
          </p>
        </div>
      ) : (
        <>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium">Bill / receipt no.</label>
              <Input
                value={receiptReference}
                onChange={(event) => setReceiptReference(event.target.value)}
                placeholder="From the receipt"
                required
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Merchant / vendor (optional)</label>
              <Input value={merchantName} onChange={(event) => setMerchantName(event.target.value)} />
            </div>
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={billableToCustomer}
              onChange={(event) => setBillableToCustomer(event.target.checked)}
            />
            Billable to the customer
          </label>
          <div>
            <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
            <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} />
          </div>
          <p className="text-xs text-zinc-500">
            You will land on the voucher after saving - attach the bill itself there.
          </p>
        </>
      )}

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy || !pettyCashFundId}>
        {busy ? "Recording..." : isAdvance ? "Issue Advance" : "Record Payment"}
      </Button>
    </form>
  );
}
