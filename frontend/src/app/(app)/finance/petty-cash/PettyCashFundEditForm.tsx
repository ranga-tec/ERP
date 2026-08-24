"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type CurrencyRef = { code: string; name: string; isBase: boolean; isActive: boolean };
type ExpenseAccountRef = { id: string; code: string; name: string; accountType: number; allowsPosting: boolean; isActive: boolean };
type PettyCashFundDto = {
  id: string;
  code: string;
  name: string;
  currencyCode: string;
  custodianName?: string | null;
  location?: string | null;
  notes?: string | null;
  authorizedFloat: number;
  transactionLimit: number;
  advanceLimit: number;
  requireReceipt: boolean;
  blockOverdueAdvances: boolean;
  settlementShortageExpenseAccountId?: string | null;
  settlementShortageCostCenterCode?: string | null;
  cashCountFrequency: number;
  nextCashCountDueAt?: string | null;
  isActive: boolean;
};

export function PettyCashFundEditForm({
  fund,
  currencies,
  expenseAccounts,
}: {
  fund: PettyCashFundDto;
  currencies: CurrencyRef[];
  expenseAccounts: ExpenseAccountRef[];
}) {
  const router = useRouter();
  const [code, setCode] = useState(fund.code);
  const [name, setName] = useState(fund.name);
  const [currencyCode, setCurrencyCode] = useState(fund.currencyCode);
  const [custodianName, setCustodianName] = useState(fund.custodianName ?? "");
  const [location, setLocation] = useState(fund.location ?? "");
  const [authorizedFloat, setAuthorizedFloat] = useState(String(fund.authorizedFloat));
  const [transactionLimit, setTransactionLimit] = useState(String(fund.transactionLimit));
  const [advanceLimit, setAdvanceLimit] = useState(String(fund.advanceLimit));
  const [requireReceipt, setRequireReceipt] = useState(fund.requireReceipt);
  const [blockOverdueAdvances, setBlockOverdueAdvances] = useState(fund.blockOverdueAdvances);
  const [settlementShortageExpenseAccountId, setSettlementShortageExpenseAccountId] = useState(fund.settlementShortageExpenseAccountId ?? "");
  const [settlementShortageCostCenterCode, setSettlementShortageCostCenterCode] = useState(fund.settlementShortageCostCenterCode ?? "");
  const [cashCountFrequency, setCashCountFrequency] = useState(String(fund.cashCountFrequency));
  const [nextCashCountDueAt, setNextCashCountDueAt] = useState(fund.nextCashCountDueAt ? new Date(fund.nextCashCountDueAt).toISOString().slice(0, 16) : "");
  const [notes, setNotes] = useState(fund.notes ?? "");
  const [isActive, setIsActive] = useState(fund.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const floatLimit = Number(authorizedFloat);
      const directLimit = Number(transactionLimit);
      const iouLimit = Number(advanceLimit);
      if (![floatLimit, directLimit, iouLimit].every((value) => Number.isFinite(value) && value >= 0)) {
        throw new Error("Fund limits must be 0 or greater.");
      }
      await apiPut(`finance/petty-cash-funds/${fund.id}`, {
        code: code.trim(),
        name: name.trim(),
        currencyCode,
        custodianName: custodianName.trim() || null,
        notes: notes.trim() || null,
        isActive: isActive === "true",
        location: location.trim() || null,
        authorizedFloat: floatLimit,
        transactionLimit: directLimit,
        advanceLimit: iouLimit,
        requireReceipt,
        blockOverdueAdvances,
        settlementShortageExpenseAccountId: settlementShortageExpenseAccountId || null,
        settlementShortageCostCenterCode: settlementShortageCostCenterCode.trim() || null,
        cashCountFrequency: Number(cashCountFrequency),
        nextCashCountDueAt: cashCountFrequency === "0" || !nextCashCountDueAt ? null : new Date(nextCashCountDueAt).toISOString(),
      });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  const activeCurrencies = currencies.filter((currency) => currency.isActive).sort((a, b) => a.code.localeCompare(b.code));

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(event) => setCode(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(event) => setName(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Currency</label>
          <Select value={currencyCode} onChange={(event) => setCurrencyCode(event.target.value)} required>
            {activeCurrencies.map((currency) => (
              <option key={currency.code} value={currency.code}>
                {currency.code} - {currency.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Location</label>
          <Input value={location} onChange={(event) => setLocation(event.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Authorized float</label>
          <Input value={authorizedFloat} onChange={(event) => setAuthorizedFloat(event.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Direct transaction limit</label>
          <Input value={transactionLimit} onChange={(event) => setTransactionLimit(event.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Advance limit</label>
          <Input value={advanceLimit} onChange={(event) => setAdvanceLimit(event.target.value)} inputMode="decimal" required />
        </div>
      </div>

      <div className="grid gap-2 text-sm sm:grid-cols-2">
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={requireReceipt} onChange={(event) => setRequireReceipt(event.target.checked)} />
          Require receipts for petty-cash spending
        </label>
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={blockOverdueAdvances} onChange={(event) => setBlockOverdueAdvances(event.target.checked)} />
          Block new advances for holders with overdue advances
        </label>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Settlement shortage expense account *</label>
          <Select value={settlementShortageExpenseAccountId} onChange={(event) => setSettlementShortageExpenseAccountId(event.target.value)} required>
            <option value="">Select posting expense account...</option>
            {expenseAccounts.filter((account) => account.accountType === 5 && account.allowsPosting && account.isActive).map((account) => (
              <option key={account.id} value={account.id}>{account.code} - {account.name}</option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Settlement shortage cost centre *</label>
          <Input value={settlementShortageCostCenterCode} onChange={(event) => setSettlementShortageCostCenterCode(event.target.value)} required />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Cash-count schedule</label>
          <Select value={cashCountFrequency} onChange={(event) => setCashCountFrequency(event.target.value)}>
            <option value="3">Every shift (8 hours)</option>
            <option value="1">Daily</option>
            <option value="2">Weekly</option>
            <option value="0">Disabled</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Next cash count due</label>
          <Input type="datetime-local" value={nextCashCountDueAt} onChange={(event) => setNextCashCountDueAt(event.target.value)} required={cashCountFrequency !== "0"} disabled={cashCountFrequency === "0"} />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Custodian (optional)</label>
          <Input value={custodianName} onChange={(event) => setCustodianName(event.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Status</label>
          <Select value={isActive} onChange={(event) => setIsActive(event.target.value)}>
            <option value="true">Active</option>
            <option value="false">Inactive</option>
          </Select>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving..." : "Save Fund"}
      </Button>
    </form>
  );
}
