"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type TaxRef = { id: string; code: string; name: string; isActive: boolean };
type TaxConversionDto = {
  id: string;
  sourceTaxCodeId: string;
  targetTaxCodeId: string;
  multiplier: number;
  notes?: string | null;
  isActive: boolean;
};

export function TaxConversionCreateForm({ taxes }: { taxes: TaxRef[] }) {
  const router = useRouter();
  const options = useMemo(
    () => taxes.filter((t) => t.isActive).slice().sort((a, b) => a.code.localeCompare(b.code)),
    [taxes],
  );

  const [sourceTaxCodeId, setSourceTaxCodeId] = useState("");
  const [targetTaxCodeId, setTargetTaxCodeId] = useState("");
  const [multiplier, setMultiplier] = useState("1");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPost<TaxConversionDto>("tax-conversions", {
        sourceTaxCodeId,
        targetTaxCodeId,
        multiplier: Number(multiplier),
        notes: notes.trim() || null,
      });

      setSourceTaxCodeId("");
      setTargetTaxCodeId("");
      setMultiplier("1");
      setNotes("");
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
          <label className="mb-1 block text-sm font-medium">Source Tax</label>
          <Select value={sourceTaxCodeId} onChange={(e) => setSourceTaxCodeId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {options.map((tax) => (
              <option key={tax.id} value={tax.id}>
                {tax.code} - {tax.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Target Tax</label>
          <Select value={targetTaxCodeId} onChange={(e) => setTargetTaxCodeId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {options.map((tax) => (
              <option key={tax.id} value={tax.id}>
                {tax.code} - {tax.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Multiplier</label>
          <Input value={multiplier} onChange={(e) => setMultiplier(e.target.value)} inputMode="decimal" required />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes</label>
        <Input value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Tax Conversion"}
      </Button>
    </form>
  );
}
