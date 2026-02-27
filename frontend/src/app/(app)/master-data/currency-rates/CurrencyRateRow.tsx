"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
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

  const [isEditing, setIsEditing] = useState(false);
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
    setIsEditing(true);
  }

  async function saveEdit() {
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
      setIsEditing(false);
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
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? (
          <div className="flex items-center gap-1">
            <Select value={fromCurrencyId} onChange={(e) => setFromCurrencyId(e.target.value)} className="min-w-24">
              {options.map((currency) => (
                <option key={currency.id} value={currency.id}>
                  {currency.code}
                </option>
              ))}
            </Select>
            <span>/</span>
            <Select value={toCurrencyId} onChange={(e) => setToCurrencyId(e.target.value)} className="min-w-24">
              {options.map((currency) => (
                <option key={currency.id} value={currency.id}>
                  {currency.code}
                </option>
              ))}
            </Select>
          </div>
        ) : (
          `${rate.fromCurrencyCode}/${rate.toCurrencyCode}`
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={value} onChange={(e) => setValue(e.target.value)} inputMode="decimal" className="min-w-20" /> : rate.rate}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={rateType} onChange={(e) => setRateType(e.target.value)} className="min-w-28">
            <option value="1">Spot</option>
            <option value="2">Corporate</option>
            <option value="3">Manual</option>
          </Select>
        ) : (
          rateTypeLabel[rate.rateType] ?? rate.rateType
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? (
          <Input type="datetime-local" value={effectiveFrom} onChange={(e) => setEffectiveFrom(e.target.value)} className="min-w-48" />
        ) : (
          new Date(rate.effectiveFrom).toLocaleString()
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? <Input value={source} onChange={(e) => setSource(e.target.value)} className="min-w-32" /> : rate.source ?? "-"}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : rate.isActive ? (
          "Yes"
        ) : (
          "No"
        )}
      </td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          {isEditing ? (
            <>
              <Button type="button" className={actionButtonClass} onClick={saveEdit} disabled={busy}>
                {busy ? "Saving..." : "Save"}
              </Button>
              <SecondaryButton
                type="button"
                className={actionButtonClass}
                onClick={() => {
                  setError(null);
                  setIsEditing(false);
                }}
                disabled={busy}
              >
                Cancel
              </SecondaryButton>
            </>
          ) : (
            <SecondaryButton type="button" className={actionButtonClass} onClick={beginEdit} disabled={busy}>
              Edit
            </SecondaryButton>
          )}
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
