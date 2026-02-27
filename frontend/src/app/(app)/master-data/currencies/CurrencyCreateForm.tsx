"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type CurrencyDto = {
  id: string;
  code: string;
  name: string;
  symbol: string;
  minorUnits: number;
  isBase: boolean;
  isActive: boolean;
};

export function CurrencyCreateForm() {
  const router = useRouter();
  const [code, setCode] = useState("USD");
  const [name, setName] = useState("US Dollar");
  const [symbol, setSymbol] = useState("$");
  const [minorUnits, setMinorUnits] = useState("2");
  const [isBase, setIsBase] = useState("false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPost<CurrencyDto>("currencies", {
        code,
        name,
        symbol,
        minorUnits: Number(minorUnits),
        isBase: isBase === "true",
      });

      setCode("");
      setName("");
      setSymbol("");
      setMinorUnits("2");
      setIsBase("false");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(e) => setCode(e.target.value.toUpperCase())} maxLength={3} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Symbol</label>
          <Input value={symbol} onChange={(e) => setSymbol(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Minor Units</label>
          <Input value={minorUnits} onChange={(e) => setMinorUnits(e.target.value)} inputMode="numeric" required />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Base Currency</label>
          <Select value={isBase} onChange={(e) => setIsBase(e.target.value)}>
            <option value="false">No</option>
            <option value="true">Yes</option>
          </Select>
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Currency"}
      </Button>
    </form>
  );
}
