"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type TaxDto = {
  id: string;
  code: string;
  name: string;
  ratePercent: number;
  isInclusive: boolean;
  scope: number;
  description?: string | null;
  isActive: boolean;
};

const scopeLabel: Record<number, string> = { 1: "Sales", 2: "Purchase", 3: "Both" };
const actionButtonClass = "px-2 py-1 text-xs";

export function TaxRow({ tax }: { tax: TaxDto }) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [code, setCode] = useState(tax.code);
  const [name, setName] = useState(tax.name);
  const [ratePercent, setRatePercent] = useState(tax.ratePercent.toString());
  const [scope, setScope] = useState(tax.scope.toString());
  const [isInclusive, setIsInclusive] = useState(tax.isInclusive ? "true" : "false");
  const [description, setDescription] = useState(tax.description ?? "");
  const [isActive, setIsActive] = useState(tax.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCode(tax.code);
    setName(tax.name);
    setRatePercent(tax.ratePercent.toString());
    setScope(tax.scope.toString());
    setIsInclusive(tax.isInclusive ? "true" : "false");
    setDescription(tax.description ?? "");
    setIsActive(tax.isActive ? "true" : "false");
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`taxes/${tax.id}`, {
        code,
        name,
        ratePercent: Number(ratePercent),
        scope: Number(scope),
        isInclusive: isInclusive === "true",
        description: description.trim() || null,
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
    if (!window.confirm(`Delete tax code ${tax.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`taxes/${tax.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? <Input value={code} onChange={(e) => setCode(e.target.value)} className="min-w-20" /> : tax.code}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={name} onChange={(e) => setName(e.target.value)} className="min-w-32" /> : tax.name}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={ratePercent} onChange={(e) => setRatePercent(e.target.value)} inputMode="decimal" className="min-w-20" />
        ) : (
          tax.ratePercent
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={scope} onChange={(e) => setScope(e.target.value)} className="min-w-24">
            <option value="1">Sales</option>
            <option value="2">Purchase</option>
            <option value="3">Both</option>
          </Select>
        ) : (
          scopeLabel[tax.scope] ?? tax.scope
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isInclusive} onChange={(e) => setIsInclusive(e.target.value)} className="min-w-20">
            <option value="false">No</option>
            <option value="true">Yes</option>
          </Select>
        ) : tax.isInclusive ? (
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
        ) : tax.isActive ? (
          "Yes"
        ) : (
          "No"
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={description} onChange={(e) => setDescription(e.target.value)} className="min-w-40" />
        ) : (
          <span className="text-zinc-500">{tax.description ?? "-"}</span>
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
