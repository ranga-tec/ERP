"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type TaxDto = { id: string; code: string; name: string; isActive: boolean };
type TaxConversionDto = {
  id: string;
  sourceTaxCodeId: string;
  sourceTaxCode: string;
  sourceTaxName: string;
  targetTaxCodeId: string;
  targetTaxCode: string;
  targetTaxName: string;
  multiplier: number;
  notes?: string | null;
  isActive: boolean;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function TaxConversionRow({
  conversion,
  taxes,
}: {
  conversion: TaxConversionDto;
  taxes: TaxDto[];
}) {
  const router = useRouter();
  const options = useMemo(
    () => taxes.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [taxes],
  );

  const [sourceTaxCodeId, setSourceTaxCodeId] = useState(conversion.sourceTaxCodeId);
  const [targetTaxCodeId, setTargetTaxCodeId] = useState(conversion.targetTaxCodeId);
  const [multiplier, setMultiplier] = useState(conversion.multiplier.toString());
  const [notes, setNotes] = useState(conversion.notes ?? "");
  const [isActive, setIsActive] = useState(conversion.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setSourceTaxCodeId(conversion.sourceTaxCodeId);
    setTargetTaxCodeId(conversion.targetTaxCodeId);
    setMultiplier(conversion.multiplier.toString());
    setNotes(conversion.notes ?? "");
    setIsActive(conversion.isActive ? "true" : "false");
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`tax-conversions/${conversion.id}`, {
        sourceTaxCodeId,
        targetTaxCodeId,
        multiplier: Number(multiplier),
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
    if (!window.confirm(`Delete tax conversion ${conversion.sourceTaxCode} -> ${conversion.targetTaxCode}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`tax-conversions/${conversion.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">{conversion.sourceTaxCode}</td>
      <td className="py-2 pr-3 font-mono text-xs">{conversion.targetTaxCode}</td>
      <td className="py-2 pr-3">{conversion.multiplier}</td>
      <td className="py-2 pr-3 text-zinc-500">{conversion.notes ?? "-"}</td>
      <td className="py-2 pr-3">{conversion.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit Tax Conversion ${conversion.sourceTaxCode} -> ${conversion.targetTaxCode}`} description="Update tax mapping, multiplier, notes, or active state." buttonLabel="Edit" variant="secondary" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">Source Tax</label><Select value={sourceTaxCodeId} onChange={(e) => setSourceTaxCodeId(e.target.value)}>{options.map((tax) => <option key={tax.id} value={tax.id}>{tax.code}</option>)}</Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Target Tax</label><Select value={targetTaxCodeId} onChange={(e) => setTargetTaxCodeId(e.target.value)}>{options.map((tax) => <option key={tax.id} value={tax.id}>{tax.code}</option>)}</Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Multiplier</label><Input value={multiplier} onChange={(e) => setMultiplier(e.target.value)} inputMode="decimal" required /></div>
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
