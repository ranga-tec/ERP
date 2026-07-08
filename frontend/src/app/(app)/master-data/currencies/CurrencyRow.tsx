"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { AuditTrailButton } from "@/components/AuditTrailButton";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type CurrencyDto = {
  id: string;
  code: string;
  name: string;
  symbol: string;
  minorUnits: number;
  isBase: boolean;
  isActive: boolean;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function CurrencyRow({ currency }: { currency: CurrencyDto }) {
  const router = useRouter();
  const [code, setCode] = useState(currency.code);
  const [name, setName] = useState(currency.name);
  const [symbol, setSymbol] = useState(currency.symbol);
  const [minorUnits, setMinorUnits] = useState(currency.minorUnits.toString());
  const [isBase, setIsBase] = useState(currency.isBase ? "true" : "false");
  const [isActive, setIsActive] = useState(currency.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCode(currency.code);
    setName(currency.name);
    setSymbol(currency.symbol);
    setMinorUnits(currency.minorUnits.toString());
    setIsBase(currency.isBase ? "true" : "false");
    setIsActive(currency.isActive ? "true" : "false");
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`currencies/${currency.id}`, {
        code,
        name,
        symbol,
        minorUnits: Number(minorUnits),
        isBase: isBase === "true",
        isActive: isActive === "true",
      });
      close();
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function deleteRow() {
    if (!window.confirm(`Delete currency ${currency.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`currencies/${currency.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">{currency.code}</td>
      <td className="py-2 pr-3">{currency.name}</td>
      <td className="py-2 pr-3">{currency.symbol}</td>
      <td className="py-2 pr-3">{currency.minorUnits}</td>
      <td className="py-2 pr-3">{currency.isBase ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">{currency.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit Currency ${currency.code}`} description="Update currency identity and active state." buttonLabel="Edit" variant="secondary" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">Code</label><Input value={code} onChange={(e) => setCode(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Name</label><Input value={name} onChange={(e) => setName(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Symbol</label><Input value={symbol} onChange={(e) => setSymbol(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Minor Units</label><Input value={minorUnits} onChange={(e) => setMinorUnits(e.target.value)} inputMode="numeric" required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Base</label><Select value={isBase} onChange={(e) => setIsBase(e.target.value)}><option value="false">No</option><option value="true">Yes</option></Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Active</label><Select value={isActive} onChange={(e) => setIsActive(e.target.value)}><option value="true">Yes</option><option value="false">No</option></Select></div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>{busy ? "Saving..." : "Save Currency"}</Button>
              </form>
            )}
          </AppFormModal>
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
          <AuditTrailButton tableName="Currencies" recordId={currency.id} />
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
