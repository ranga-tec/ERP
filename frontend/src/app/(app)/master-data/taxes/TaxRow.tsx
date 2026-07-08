"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
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
  }

  async function saveEdit(close: () => void) {
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
      close();
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
      <td className="py-2 pr-3 font-mono text-xs">{tax.code}</td>
      <td className="py-2 pr-3">{tax.name}</td>
      <td className="py-2 pr-3">{tax.ratePercent}</td>
      <td className="py-2 pr-3">{scopeLabel[tax.scope] ?? tax.scope}</td>
      <td className="py-2 pr-3">{tax.isInclusive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">{tax.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3"><span className="text-zinc-500">{tax.description ?? "-"}</span></td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit Tax Code ${tax.code}`} description="Update tax rate, scope, inclusivity, and active state." buttonLabel="Edit" variant="secondary" size="lg" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">Code</label><Input value={code} onChange={(e) => setCode(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Name</label><Input value={name} onChange={(e) => setName(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Rate %</label><Input value={ratePercent} onChange={(e) => setRatePercent(e.target.value)} inputMode="decimal" required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Scope</label><Select value={scope} onChange={(e) => setScope(e.target.value)}><option value="1">Sales</option><option value="2">Purchase</option><option value="3">Both</option></Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Inclusive</label><Select value={isInclusive} onChange={(e) => setIsInclusive(e.target.value)}><option value="false">No</option><option value="true">Yes</option></Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Active</label><Select value={isActive} onChange={(e) => setIsActive(e.target.value)}><option value="true">Yes</option><option value="false">No</option></Select></div>
                  <div className="sm:col-span-2"><label className="mb-1 block text-sm font-medium">Description</label><Input value={description} onChange={(e) => setDescription(e.target.value)} /></div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>{busy ? "Saving..." : "Save Tax Code"}</Button>
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
