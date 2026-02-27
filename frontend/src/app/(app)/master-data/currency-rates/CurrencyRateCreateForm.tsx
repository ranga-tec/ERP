"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type CurrencyRef = { id: string; code: string; name: string; isActive: boolean };
type CurrencyRateDto = {
  id: string;
  fromCurrencyId: string;
  toCurrencyId: string;
  rate: number;
  rateType: number;
  effectiveFrom: string;
  source?: string | null;
  isActive: boolean;
};

const rateTypeOptions = [
  { value: 1, label: "Spot" },
  { value: 2, label: "Corporate" },
  { value: 3, label: "Manual" },
];

export function CurrencyRateCreateForm({ currencies }: { currencies: CurrencyRef[] }) {
  const router = useRouter();
  const options = useMemo(
    () => currencies.filter((c) => c.isActive).slice().sort((a, b) => a.code.localeCompare(b.code)),
    [currencies],
  );

  const [fromCurrencyId, setFromCurrencyId] = useState("");
  const [toCurrencyId, setToCurrencyId] = useState("");
  const [rate, setRate] = useState("1");
  const [rateType, setRateType] = useState("1");
  const [effectiveFrom, setEffectiveFrom] = useState(new Date().toISOString().slice(0, 16));
  const [source, setSource] = useState("Manual entry");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPost<CurrencyRateDto>("currency-rates", {
        fromCurrencyId,
        toCurrencyId,
        rate: Number(rate),
        rateType: Number(rateType),
        effectiveFrom: new Date(effectiveFrom).toISOString(),
        source: source.trim() || null,
      });

      setRate("1");
      setSource("Manual entry");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">From</label>
          <Select value={fromCurrencyId} onChange={(e) => setFromCurrencyId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {options.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} - {c.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">To</label>
          <Select value={toCurrencyId} onChange={(e) => setToCurrencyId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {options.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} - {c.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Rate</label>
          <Input value={rate} onChange={(e) => setRate(e.target.value)} inputMode="decimal" required />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Rate Type</label>
          <Select value={rateType} onChange={(e) => setRateType(e.target.value)}>
            {rateTypeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Effective From</label>
          <Input
            type="datetime-local"
            value={effectiveFrom}
            onChange={(e) => setEffectiveFrom(e.target.value)}
            required
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Source</label>
          <Input value={source} onChange={(e) => setSource(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create FX Rate"}
      </Button>
    </form>
  );
}
