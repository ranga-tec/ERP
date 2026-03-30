"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type CurrencyRef = { code: string; name: string; isBase: boolean; isActive: boolean };
type PettyCashFundDto = {
  id: string;
  code: string;
  name: string;
  currencyCode: string;
  custodianName?: string | null;
  notes?: string | null;
  isActive: boolean;
};

export function PettyCashFundEditForm({
  fund,
  currencies,
}: {
  fund: PettyCashFundDto;
  currencies: CurrencyRef[];
}) {
  const router = useRouter();
  const [code, setCode] = useState(fund.code);
  const [name, setName] = useState(fund.name);
  const [currencyCode, setCurrencyCode] = useState(fund.currencyCode);
  const [custodianName, setCustodianName] = useState(fund.custodianName ?? "");
  const [notes, setNotes] = useState(fund.notes ?? "");
  const [isActive, setIsActive] = useState(fund.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPut(`finance/petty-cash-funds/${fund.id}`, {
        code: code.trim(),
        name: name.trim(),
        currencyCode,
        custodianName: custodianName.trim() || null,
        notes: notes.trim() || null,
        isActive: isActive === "true",
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
