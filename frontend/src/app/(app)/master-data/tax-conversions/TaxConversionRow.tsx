"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
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

  const [isEditing, setIsEditing] = useState(false);
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
    setIsEditing(true);
  }

  async function saveEdit() {
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
      setIsEditing(false);
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
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? (
          <Select value={sourceTaxCodeId} onChange={(e) => setSourceTaxCodeId(e.target.value)} className="min-w-32">
            {options.map((tax) => (
              <option key={tax.id} value={tax.id}>
                {tax.code}
              </option>
            ))}
          </Select>
        ) : (
          conversion.sourceTaxCode
        )}
      </td>
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? (
          <Select value={targetTaxCodeId} onChange={(e) => setTargetTaxCodeId(e.target.value)} className="min-w-32">
            {options.map((tax) => (
              <option key={tax.id} value={tax.id}>
                {tax.code}
              </option>
            ))}
          </Select>
        ) : (
          conversion.targetTaxCode
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={multiplier} onChange={(e) => setMultiplier(e.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          conversion.multiplier
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? <Input value={notes} onChange={(e) => setNotes(e.target.value)} className="min-w-36" /> : conversion.notes ?? "-"}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : conversion.isActive ? (
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
