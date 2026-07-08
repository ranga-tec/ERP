"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type UomDto = { id: string; code: string; name: string; isActive: boolean };
type UnitConversionDto = {
  id: string;
  fromUnitOfMeasureId: string;
  fromUnitOfMeasureCode: string;
  fromUnitOfMeasureName: string;
  toUnitOfMeasureId: string;
  toUnitOfMeasureCode: string;
  toUnitOfMeasureName: string;
  factor: number;
  notes?: string | null;
  isActive: boolean;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function UnitConversionRow({
  conversion,
  uoms,
}: {
  conversion: UnitConversionDto;
  uoms: UomDto[];
}) {
  const router = useRouter();
  const options = useMemo(
    () => uoms.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [uoms],
  );

  const [fromUnitOfMeasureId, setFromUnitOfMeasureId] = useState(conversion.fromUnitOfMeasureId);
  const [toUnitOfMeasureId, setToUnitOfMeasureId] = useState(conversion.toUnitOfMeasureId);
  const [factor, setFactor] = useState(conversion.factor.toString());
  const [notes, setNotes] = useState(conversion.notes ?? "");
  const [isActive, setIsActive] = useState(conversion.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setFromUnitOfMeasureId(conversion.fromUnitOfMeasureId);
    setToUnitOfMeasureId(conversion.toUnitOfMeasureId);
    setFactor(conversion.factor.toString());
    setNotes(conversion.notes ?? "");
    setIsActive(conversion.isActive ? "true" : "false");
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`uom-conversions/${conversion.id}`, {
        fromUnitOfMeasureId,
        toUnitOfMeasureId,
        factor: Number(factor),
        notes: notes.trim() || null,
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
    if (!window.confirm(`Delete UoM conversion ${conversion.fromUnitOfMeasureCode} -> ${conversion.toUnitOfMeasureCode}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`uom-conversions/${conversion.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">{conversion.fromUnitOfMeasureCode}</td>
      <td className="py-2 pr-3 font-mono text-xs">{conversion.toUnitOfMeasureCode}</td>
      <td className="py-2 pr-3">{conversion.factor}</td>
      <td className="py-2 pr-3 text-zinc-500">{conversion.notes ?? "-"}</td>
      <td className="py-2 pr-3">{conversion.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit UoM Conversion ${conversion.fromUnitOfMeasureCode} -> ${conversion.toUnitOfMeasureCode}`} description="Update conversion units, factor, notes, or active state." buttonLabel="Edit" variant="secondary" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">From</label><Select value={fromUnitOfMeasureId} onChange={(e) => setFromUnitOfMeasureId(e.target.value)}>{options.map((uom) => <option key={uom.id} value={uom.id}>{uom.code}</option>)}</Select></div>
                  <div><label className="mb-1 block text-sm font-medium">To</label><Select value={toUnitOfMeasureId} onChange={(e) => setToUnitOfMeasureId(e.target.value)}>{options.map((uom) => <option key={uom.id} value={uom.id}>{uom.code}</option>)}</Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Factor</label><Input value={factor} onChange={(e) => setFactor(e.target.value)} inputMode="decimal" required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Notes</label><Input value={notes} onChange={(e) => setNotes(e.target.value)} /></div>
                  <div><label className="mb-1 block text-sm font-medium">Active</label><Select value={isActive} onChange={(e) => setIsActive(e.target.value)}><option value="true">Yes</option><option value="false">No</option></Select></div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>{busy ? "Saving..." : "Save Conversion"}</Button>
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
