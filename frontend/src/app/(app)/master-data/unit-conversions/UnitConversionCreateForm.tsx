"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type UomRef = { id: string; code: string; name: string; isActive: boolean };
type UnitConversionDto = {
  id: string;
  fromUnitOfMeasureId: string;
  toUnitOfMeasureId: string;
  factor: number;
  notes?: string | null;
  isActive: boolean;
};

export function UnitConversionCreateForm({ uoms }: { uoms: UomRef[] }) {
  const router = useRouter();
  const options = useMemo(
    () => uoms.filter((u) => u.isActive).slice().sort((a, b) => a.code.localeCompare(b.code)),
    [uoms],
  );

  const [fromUnitOfMeasureId, setFromUnitOfMeasureId] = useState("");
  const [toUnitOfMeasureId, setToUnitOfMeasureId] = useState("");
  const [factor, setFactor] = useState("1");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPost<UnitConversionDto>("uom-conversions", {
        fromUnitOfMeasureId,
        toUnitOfMeasureId,
        factor: Number(factor),
        notes: notes.trim() || null,
      });

      setFromUnitOfMeasureId("");
      setToUnitOfMeasureId("");
      setFactor("1");
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
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">From UoM</label>
          <Select value={fromUnitOfMeasureId} onChange={(e) => setFromUnitOfMeasureId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {options.map((uom) => (
              <option key={uom.id} value={uom.id}>
                {uom.code} - {uom.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">To UoM</label>
          <Select value={toUnitOfMeasureId} onChange={(e) => setToUnitOfMeasureId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {options.map((uom) => (
              <option key={uom.id} value={uom.id}>
                {uom.code} - {uom.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Factor</label>
          <Input value={factor} onChange={(e) => setFactor(e.target.value)} inputMode="decimal" required />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Notes</label>
          <Input value={notes} onChange={(e) => setNotes(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create UoM Conversion"}
      </Button>
    </form>
  );
}
