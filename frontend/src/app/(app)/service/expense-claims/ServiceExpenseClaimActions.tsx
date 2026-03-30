"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { Input, SecondaryButton, Select } from "@/components/ui";

type PaymentTypeRef = { id: string; code: string; name: string };
type PettyCashFundRef = { id: string; code: string; name: string; balance: number; isActive: boolean };

export function ServiceExpenseClaimActions({
  claimId,
  fundingSource,
  paymentTypes,
  pettyCashFunds,
  canSubmit,
  canApprove,
  canReject,
  canSettle,
}: {
  claimId: string;
  fundingSource: number;
  paymentTypes: PaymentTypeRef[];
  pettyCashFunds: PettyCashFundRef[];
  canSubmit: boolean;
  canApprove: boolean;
  canReject: boolean;
  canSettle: boolean;
}) {
  const router = useRouter();
  const [rejectionReason, setRejectionReason] = useState("");
  const [settlementPaymentTypeId, setSettlementPaymentTypeId] = useState("");
  const [settlementPettyCashFundId, setSettlementPettyCashFundId] = useState("");
  const [settlementReference, setSettlementReference] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function act(action: "submit" | "approve" | "reject" | "settle") {
    setError(null);
    setBusy(true);
    try {
      if (action === "reject") {
        await apiPostNoContent(`service/expense-claims/${claimId}/reject`, {
          rejectionReason: rejectionReason.trim() || null,
        });
      } else if (action === "settle") {
        if (requiresPettyCashFund && !settlementPettyCashFundId) {
          throw new Error("Select the petty cash fund used for this claim.");
        }

        await apiPostNoContent(`service/expense-claims/${claimId}/settle`, {
          settlementPaymentTypeId: settlementPaymentTypeId || null,
          settlementPettyCashFundId: settlementPettyCashFundId || null,
          settlementReference: settlementReference.trim() || null,
        });
      } else {
        await apiPostNoContent(`service/expense-claims/${claimId}/${action}`, {});
      }

      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  const sortedPaymentTypes = paymentTypes.slice().sort((a, b) => a.code.localeCompare(b.code));
  const sortedFunds = pettyCashFunds
    .filter((fund) => fund.isActive)
    .slice()
    .sort((a, b) => a.code.localeCompare(b.code));
  const requiresPettyCashFund = fundingSource === 2;

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2">
        <SecondaryButton type="button" disabled={!canSubmit || busy} onClick={() => void act("submit")}>
          {busy ? "Working..." : "Submit Claim"}
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canApprove || busy} onClick={() => void act("approve")}>
          Approve
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canReject || busy} onClick={() => void act("reject")}>
          Reject
        </SecondaryButton>
        <SecondaryButton type="button" disabled={!canSettle || busy} onClick={() => void act("settle")}>
          Settle
        </SecondaryButton>
      </div>

      {canReject ? (
        <div>
          <label className="mb-1 block text-sm font-medium">Rejection reason (optional)</label>
          <Input
            value={rejectionReason}
            onChange={(event) => setRejectionReason(event.target.value)}
            placeholder="Why finance is sending this back"
          />
        </div>
      ) : null}

      {canSettle ? (
        <div className="grid gap-3 sm:grid-cols-3">
          <div>
            <label className="mb-1 block text-sm font-medium">
              Petty cash fund {requiresPettyCashFund ? "(required)" : "(optional)"}
            </label>
            <Select value={settlementPettyCashFundId} onChange={(event) => setSettlementPettyCashFundId(event.target.value)}>
              <option value="">{requiresPettyCashFund ? "Select fund..." : "Not used"}</option>
              {sortedFunds.map((fund) => (
                <option key={fund.id} value={fund.id}>
                  {fund.code} - {fund.name} ({fund.balance.toFixed(2)})
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Settlement method (optional)</label>
            <Select value={settlementPaymentTypeId} onChange={(event) => setSettlementPaymentTypeId(event.target.value)}>
              <option value="">Not recorded</option>
              {sortedPaymentTypes.map((paymentType) => (
                <option key={paymentType.id} value={paymentType.id}>
                  {paymentType.code} - {paymentType.name}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Settlement reference (optional)</label>
            <Input
              value={settlementReference}
              onChange={(event) => setSettlementReference(event.target.value)}
              placeholder="Voucher, petty cash sheet, bank ref, etc."
            />
          </div>
        </div>
      ) : null}

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}
    </div>
  );
}
