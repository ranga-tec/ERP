"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type CurrencyDto = { id: string; code: string; name: string; isActive: boolean };
type CurrencyRateDto = {
  id: string;
  fromCurrencyId: string;
  fromCurrencyCode: string;
  toCurrencyId: string;
  toCurrencyCode: string;
  rate: number;
  rateType: number;
  effectiveFrom: string;
  source?: string | null;
  isActive: boolean;
};

const rateTypeLabel: Record<number, string> = { 1: "Spot", 2: "Corporate", 3: "Manual" };
const actionButtonClass = "px-2 py-1 text-xs";

function toLocalDateTimeInput(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  const adjusted = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return adjusted.toISOString().slice(0, 16);
}

export function CurrencyRateRow({
  rate,
  currencies,
}: {
  rate: CurrencyRateDto;
  currencies: CurrencyDto[];
}) {
  const router = useRouter();
  const options = useMemo(
    () => currencies.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [currencies],
  );

  const [fromCurrencyId, setFromCurrencyId] = useState(rate.fromCurrencyId);
  const [toCurrencyId, setToCurrencyId] = useState(rate.toCurrencyId);
  const [value, setValue] = useState(rate.rate.toString());
  const [rateType, setRateType] = useState(rate.rateType.toString());
  const [effectiveFrom, setEffectiveFrom] = useState(toLocalDateTimeInput(rate.effectiveFrom));
  const [source, setSource] = useState(rate.source ?? "");
  const [isActive, setIsActive] = useState(rate.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setFromCurrencyId(rate.fromCurrencyId);
    setToCurrencyId(rate.toCurrencyId);
    setValue(rate.rate.toString());
    setRateType(rate.rateType.toString());
    setEffectiveFrom(toLocalDateTimeInput(rate.effectiveFrom));
    setSource(rate.source ?? "");
    setIsActive(rate.isActive ? "true" : "false");
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      if (!effectiveFrom) {
        throw new Error("Effective from is required.");
      }

      await apiPut(`currency-rates/${rate.id}`, {
        fromCurrencyId,
        toCurrencyId,
        rate: Number(value),
        rateType: Number(rateType),
        effectiveFrom: new Date(effectiveFrom).toISOString(),
        source: source.trim() || null,
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
    if (!window.confirm(`Delete FX rate ${rate.fromCurrencyCode}/${rate.toCurrencyCode}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`currency-rates/${rate.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">{`${rate.fromCurrencyCode}/${rate.toCurrencyCode}`}</td>
      <td className="py-2 pr-3">{rate.rate}</td>
      <td className="py-2 pr-3">{rateTypeLabel[rate.rateType] ?? rate.rateType}</td>
      <td className="py-2 pr-3 text-zinc-500">{new Date(rate.effectiveFrom).toLocaleString()}</td>
      <td className="py-2 pr-3 text-zinc-500">{rate.source ?? "-"}</td>
      <td className="py-2 pr-3">{rate.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit Currency Rate ${rate.fromCurrencyCode}/${rate.toCurrencyCode}`} description="Update exchange rate details." buttonLabel="Edit" variant="secondary" size="lg" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">From Currency</label><Select value={fromCurrencyId} onChange={(e) => setFromCurrencyId(e.target.value)}>{options.map((currency) => <option key={currency.id} value={currency.id}>{currency.code}</option>)}</Select></div>
                  <div><label className="mb-1 block text-sm font-medium">To Currency</label><Select value={toCurrencyId} onChange={(e) => setToCurrencyId(e.target.value)}>{options.map((currency) => <option key={currency.id} value={currency.id}>{currency.code}</option>)}</Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Rate</label><Input value={value} onChange={(e) => setValue(e.target.value)} inputMode="decimal" required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Rate Type</label><Select value={rateType} onChange={(e) => setRateType(e.target.value)}><option value="1">Spot</option><option value="2">Corporate</option><option value="3">Manual</option></Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Effective From</label><Input type="datetime-local" value={effectiveFrom} onChange={(e) => setEffectiveFrom(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Source</label><Input value={source} onChange={(e) => setSource(e.target.value)} /></div>
                  <div><label className="mb-1 block text-sm font-medium">Active</label><Select value={isActive} onChange={(e) => setIsActive(e.target.value)}><option value="true">Yes</option><option value="false">No</option></Select></div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>{busy ? "Saving..." : "Save Currency Rate"}</Button>
              </form>
            )}
          </AppFormModal>
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
