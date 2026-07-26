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
  permissions,
}: {
  id: string;
  status: number;
  funds: FundRef[];
  amount: number;
  permissions: string[];
}) {
  const router = useRouter();
  const permissionSet = new Set(permissions);
  const canApprove = permissionSet.has("Finance.PettyCashIou.Approve");
  const canReject = permissionSet.has("Finance.PettyCashIou.Reject");
  const canRelease = permissionSet.has("Finance.PettyCashIou.Release");
  const canSettle = permissionSet.has("Finance.PettyCashIou.Settle");
  const [fundId, setFundId] = useState(funds[0]?.id ?? "");
  const [settledAmount, setSettledAmount] = useState(String(amount));
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

  const settleValue = Number(settledAmount);
  const settleInvalid = !Number.isFinite(settleValue) || settleValue < 0;
  const fundLabel = funds.find((fund) => fund.id === fundId);

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
            <Button type="button" disabled={!fundId || busy !== null} onClick={() => setPending("release")}>
              Release Cash
            </Button>
          </>
        ) : null}

        {status === 3 && canSettle ? (
          <>
            <div>
              <label className="mb-1 block text-xs font-medium text-zinc-500">Settled amount</label>
              <Input
                className="w-32"
                inputMode="decimal"
                value={settledAmount}
                onChange={(event) => setSettledAmount(event.target.value)}
              />
            </div>
            <Button type="button" disabled={busy !== null || settleInvalid} onClick={() => setPending("settle")}>
              Settle / Account
            </Button>
          </>
        ) : null}

        {((status === 1 && !canApprove && !canReject) || (status === 2 && !canRelease) || (status === 3 && !canSettle)) ? (
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
        onConfirm={() => run("release", { pettyCashFundId: fundId })}
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
        onConfirm={() => run("settle", { settledAmount: settleValue })}
        description={
          <>
            This accounts the advance of <span className="font-semibold">{money(amount)}</span> as settled at{" "}
            <span className="font-semibold">{money(settleValue)}</span>
            {settleValue !== amount ? (
              <span className="text-amber-700 dark:text-amber-300">
                {" "}
                — a difference of {money(Math.abs(amount - settleValue))} against the amount advanced
              </span>
            ) : null}
            . Settling closes the IOU.
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
