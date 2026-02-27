"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
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

  const [isEditing, setIsEditing] = useState(false);
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
    setIsEditing(true);
  }

  async function saveEdit() {
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
      setIsEditing(false);
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
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? (
          <Select value={fromUnitOfMeasureId} onChange={(e) => setFromUnitOfMeasureId(e.target.value)} className="min-w-28">
            {options.map((uom) => (
              <option key={uom.id} value={uom.id}>
                {uom.code}
              </option>
            ))}
          </Select>
        ) : (
          conversion.fromUnitOfMeasureCode
        )}
      </td>
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? (
          <Select value={toUnitOfMeasureId} onChange={(e) => setToUnitOfMeasureId(e.target.value)} className="min-w-28">
            {options.map((uom) => (
              <option key={uom.id} value={uom.id}>
                {uom.code}
              </option>
            ))}
          </Select>
        ) : (
          conversion.toUnitOfMeasureCode
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={factor} onChange={(e) => setFactor(e.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          conversion.factor
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
