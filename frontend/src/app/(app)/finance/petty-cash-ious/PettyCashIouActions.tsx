"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type FundRef = { id: string; code: string; name: string };

export function PettyCashIouActions({ id, status, funds, amount }: { id: string; status: number; funds: FundRef[]; amount: number }) {
  const router = useRouter();
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
        {status === 1 ? (
          <>
            <Button type="button" disabled={busy !== null} onClick={() => run("approve")}>
              {busy === "approve" ? "Approving..." : "Approve"}
            </Button>
            <SecondaryButton type="button" disabled={busy !== null} onClick={() => run("reject", { reason: "Rejected from IOU list" })}>
              Reject
            </SecondaryButton>
          </>
        ) : null}
        {status === 2 ? (
          <>
            <Select value={fundId} onChange={(event) => setFundId(event.target.value)} className="w-56">
              {funds.map((fund) => (
                <option key={fund.id} value={fund.id}>{fund.code} - {fund.name}</option>
              ))}
            </Select>
            <Button type="button" disabled={!fundId || busy !== null} onClick={() => run("release", { pettyCashFundId: fundId })}>
              {busy === "release" ? "Releasing..." : "Release"}
            </Button>
          </>
        ) : null}
        {status === 3 ? (
          <>
            <Input className="w-32" inputMode="decimal" value={settledAmount} onChange={(event) => setSettledAmount(event.target.value)} />
            <Button type="button" disabled={busy !== null} onClick={() => run("settle", { settledAmount: Number(settledAmount) })}>
              {busy === "settle" ? "Settling..." : "Settle"}
            </Button>
          </>
        ) : null}
      </div>
      {error ? <div className="text-xs text-red-700 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
