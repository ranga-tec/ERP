"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type FundRef = { id: string; code: string; name: string };

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
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(action: string, body: object = {}) {
    setError(null);
    setBusy(action);
    try {
      await apiPostNoContent(`finance/petty-cash-ious/${id}/${action}`, body);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

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
              <SecondaryButton type="button" disabled={busy !== null} onClick={() => run("reject", { reason: "Rejected from IOU list" })}>
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
            <Button type="button" disabled={!fundId || busy !== null} onClick={() => run("release", { pettyCashFundId: fundId })}>
              {busy === "release" ? "Releasing..." : "Release Cash"}
            </Button>
          </>
        ) : null}
        {status === 3 && canSettle ? (
          <>
            <Input className="w-32" inputMode="decimal" value={settledAmount} onChange={(event) => setSettledAmount(event.target.value)} />
            <Button type="button" disabled={busy !== null} onClick={() => run("settle", { settledAmount: Number(settledAmount) })}>
              {busy === "settle" ? "Settling..." : "Settle / Account"}
            </Button>
          </>
        ) : null}
        {((status === 1 && !canApprove && !canReject) || (status === 2 && !canRelease) || (status === 3 && !canSettle)) ? (
          <span className="text-xs text-zinc-500">View only</span>
        ) : null}
      </div>
      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
