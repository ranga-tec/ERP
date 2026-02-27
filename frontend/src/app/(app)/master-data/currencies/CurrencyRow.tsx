"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
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
  const [isEditing, setIsEditing] = useState(false);
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
    setIsEditing(true);
  }

  async function saveEdit() {
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
      setIsEditing(false);
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
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? <Input value={code} onChange={(e) => setCode(e.target.value)} className="min-w-20" /> : currency.code}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={name} onChange={(e) => setName(e.target.value)} className="min-w-32" /> : currency.name}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={symbol} onChange={(e) => setSymbol(e.target.value)} className="min-w-20" /> : currency.symbol}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={minorUnits} onChange={(e) => setMinorUnits(e.target.value)} inputMode="numeric" className="min-w-16" />
        ) : (
          currency.minorUnits
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isBase} onChange={(e) => setIsBase(e.target.value)} className="min-w-20">
            <option value="false">No</option>
            <option value="true">Yes</option>
          </Select>
        ) : currency.isBase ? (
          "Yes"
        ) : (
          "No"
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : currency.isActive ? (
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
