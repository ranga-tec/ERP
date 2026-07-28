"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { ConfirmActionDialog } from "@/components/ConfirmActionDialog";
import { Button, Input, SecondaryButton, Select, Textarea } from "@/components/ui";

type FundRef = { id: string; code: string; name: string };
type Pending = "release" | "settle" | "reject" | null;

function money(value: number): string {
  return value.toFixed(2);
}

export function PettyCashIouActions({
  id,
  status,
  funds,
  amount,
  claimedAmount = 0,
  claimCount = 0,
  permissions,
}: {
  id: string;
  status: number;
  funds: FundRef[];
  amount: number;
  claimedAmount?: number;
  claimCount?: number;
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const canApprove = permissionSet.has("Finance.PettyCashIou.Approve");
  const canReject = permissionSet.has("Finance.PettyCashIou.Reject");
  const canRelease = permissionSet.has("Finance.PettyCashIou.Release");
  const canSettle = permissionSet.has("Finance.PettyCashIou.Settle");
  const canApproveSettlement = status === 4 && permissionSet.has("Finance.PettyCashIou.Approve");
  const [fundId, setFundId] = useState(funds[0]?.id ?? "");
  const [issueBillNumber, setIssueBillNumber] = useState("");
  // Settling no longer takes an amount spent - what was spent is the advance less what came back.
  // So this is the cash physically handed back, and it may be zero.
  const [returnedNow, setReturnedNow] = useState("0");
  const [rejectReason, setRejectReason] = useState("");
  const [pending, setPending] = useState<Pending>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(action: string, body: object = {}) {
    setError(null);
    setBusy(action);
    try {
      await apiPostNoContent(`finance/petty-cash-ious/${id}/${action}`, body);
      setPending(null);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  async function settleWithReturn() {
    setError(null);
    setBusy("settle");
    try {
      if (returnValue > 0) {
        await apiPostNoContent(`finance/petty-cash-ious/${id}/return-balance`, { amount: returnValue });
      }
      await apiPostNoContent(`finance/petty-cash-ious/${id}/settle`, {});
      setPending(null);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  const returnValue = Number(returnedNow);
  const returnInvalid = !Number.isFinite(returnValue) || returnValue < 0 || returnValue > amount;
  const fundLabel = funds.find((fund) => fund.id === fundId);

  // Whatever did not come back has to be covered by bills; the rest is cash nobody can account for.
  // Surfaced, not blocked - finance decides.
  const unaccounted = returnInvalid ? 0 : amount - returnValue - claimedAmount;

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-end gap-2">
        {status === 1 && (canApprove || canReject) ? (
          <>
            {canApprove ? (
              <Button type="button" disabled={busy !== null} onClick={() => run("approve")}>
                {busy === "approve" ? "Approving..." : "Approve"}
              </Button>
            ) : null}
            {canReject ? (
              <SecondaryButton type="button" disabled={busy !== null} onClick={() => setPending("reject")}>
                Reject
              </SecondaryButton>
            ) : null}
          </>
        ) : null}

        {status === 2 && canRelease ? (
          <>
            <div>
              <label className="mb-1 block text-xs font-medium text-zinc-500">Petty cash fund</label>
              <Select value={fundId} onChange={(event) => setFundId(event.target.value)} className="w-56">
                <option value="" disabled>Select fund...</option>
                {funds.map((fund) => (
                  <option key={fund.id} value={fund.id}>{fund.code} - {fund.name}</option>
                ))}
              </Select>
              {funds.length === 0 ? (
                <div className="mt-1 text-xs text-amber-700 dark:text-amber-300">
                  Create or activate a petty cash fund before releasing cash.
                </div>
              ) : null}
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-zinc-500">Signed bill no.</label>
              <Input
                className="w-32"
                value={issueBillNumber}
                onChange={(event) => setIssueBillNumber(event.target.value)}
                placeholder="Optional"
              />
            </div>
            <Button type="button" disabled={!fundId || busy !== null} onClick={() => setPending("release")}>
              Release Cash
            </Button>
          </>
        ) : null}

        {canApproveSettlement ? (
          <Button type="button" disabled={busy !== null} onClick={() => void run("approve-settlement")}>
            {busy === "approve-settlement" ? "Approving..." : "Approve Settlement"}
          </Button>
        ) : null}

        {status === 3 && canSettle ? (
          <>
            <div>
              <label className="mb-1 block text-xs font-medium text-zinc-500">Cash returned</label>
              <Input
                className="w-32"
                inputMode="decimal"
                value={returnedNow}
                onChange={(event) => setReturnedNow(event.target.value)}
              />
              <div className="mt-1 text-xs text-zinc-500">
                {claimCount === 0
                  ? "No expense vouchers linked to this advance."
                  : `${money(claimedAmount)} on ${claimCount} voucher${claimCount === 1 ? "" : "s"}`}
              </div>
              {!returnInvalid && unaccounted > 0 ? (
                <div className="mt-1 text-xs text-amber-700 dark:text-amber-400">
                  {money(unaccounted)} has no voucher behind it and will not reach job cost.
                </div>
              ) : null}
            </div>
            <Button type="button" disabled={busy !== null || returnInvalid} onClick={() => setPending("settle")}>
              Settle / Account
            </Button>
          </>
        ) : null}

        {((status === 1 && !canApprove && !canReject) || (status === 2 && !canRelease) || (status === 3 && !canSettle) || (status === 4 && !canApproveSettlement)) ? (
          <span className="text-xs text-zinc-500">View only</span>
        ) : null}
      </div>

      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}

      <ConfirmActionDialog
        open={pending === "release"}
        title="Release petty cash"
        confirmWord="RELEASE"
        confirmLabel="Release Cash"
        busy={busy === "release"}
        onCancel={() => setPending(null)}
        onConfirm={() => run("release", { pettyCashFundId: fundId, issueBillNumber: issueBillNumber.trim() || null })}
        description={
          <>
            This pays out <span className="font-semibold">{money(amount)}</span> from{" "}
            <span className="font-semibold">{fundLabel ? `${fundLabel.code} - ${fundLabel.name}` : "the selected fund"}</span> and
            reduces that fund&apos;s balance. It is not reversible from here.
          </>
        }
      />

      <ConfirmActionDialog
        open={pending === "settle"}
        title="Settle petty cash IOU"
        confirmWord="SETTLE"
        confirmLabel="Settle / Account"
        busy={busy === "settle"}
        onCancel={() => setPending(null)}
        onConfirm={() => void settleWithReturn()}
        description={
          <>
            This accounts the advance of <span className="font-semibold">{money(amount)}</span> as spent at{" "}
            <span className="font-semibold">{money(amount - returnValue)}</span>, returning{" "}
            <span className="font-semibold">{money(returnValue)}</span> to the fund.
            {unaccounted > 0 ? (
              <span className="text-amber-700 dark:text-amber-300">
                {" "}
                Only {money(claimedAmount)} is documented on expense vouchers, so{" "}
                <span className="font-semibold">{money(unaccounted)}</span> will be recorded as spent with no bill
                behind it and will never reach this job&apos;s cost.
              </span>
            ) : null}{" "}
            Settling closes the IOU.
          </>
        }
      />

      {pending === "reject" ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4">
          <div role="dialog" aria-modal="true" className="w-full max-w-md rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl">
            <div className="text-base font-semibold">Reject IOU request</div>
            <div className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
              Give the requester a reason. This is recorded against the IOU.
            </div>
            <label className="mt-4 block text-sm font-medium">Reason</label>
            <Textarea
              className="mt-1"
              value={rejectReason}
              onChange={(event) => setRejectReason(event.target.value)}
              placeholder="Why is this being rejected?"
              autoFocus
              disabled={busy === "reject"}
            />
            <div className="mt-4 flex flex-wrap justify-end gap-2">
              <SecondaryButton type="button" disabled={busy === "reject"} onClick={() => setPending(null)}>
                Cancel
              </SecondaryButton>
              <Button
                type="button"
                disabled={busy === "reject" || rejectReason.trim().length === 0}
                onClick={() => run("reject", { reason: rejectReason.trim() })}
              >
                {busy === "reject" ? "Rejecting..." : "Reject"}
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
