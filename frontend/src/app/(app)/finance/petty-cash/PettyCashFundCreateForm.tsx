"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type CurrencyRef = { code: string; name: string; isBase: boolean; isActive: boolean };
type PettyCashFundDto = { id: string };

export function PettyCashFundCreateForm({ currencies }: { currencies: CurrencyRef[] }) {
  const router = useRouter();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [currencyCode, setCurrencyCode] = useState(currencies.find((currency) => currency.isBase)?.code ?? "USD");
  const [custodianName, setCustodianName] = useState("");
  const [openingBalance, setOpeningBalance] = useState("");
  const [openingReferenceNumber, setOpeningReferenceNumber] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const opening = openingBalance.trim() ? Number(openingBalance) : null;
      if (opening !== null && (!Number.isFinite(opening) || opening < 0)) {
        throw new Error("Opening balance must be 0 or greater.");
      }

      const fund = await apiPost<PettyCashFundDto>("finance/petty-cash-funds", {
        code: code.trim(),
        name: name.trim(),
        currencyCode,
        custodianName: custodianName.trim() || null,
        notes: notes.trim() || null,
        openingBalance: opening,
        openedAt: null,
        openingReferenceNumber: openingReferenceNumber.trim() || null,
      });

      router.push(`/finance/petty-cash/${fund.id}`);
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

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Custodian (optional)</label>
          <Input value={custodianName} onChange={(event) => setCustodianName(event.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Opening balance (optional)</label>
          <Input value={openingBalance} onChange={(event) => setOpeningBalance(event.target.value)} inputMode="decimal" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Opening ref (optional)</label>
          <Input
            value={openingReferenceNumber}
            onChange={(event) => setOpeningReferenceNumber(event.target.value)}
            placeholder="Voucher or float sheet"
          />
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
        {busy ? "Creating..." : "Create Petty Cash Fund"}
      </Button>
    </form>
  );
}
