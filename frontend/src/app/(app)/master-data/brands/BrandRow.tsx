"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { AuditTrailButton } from "@/components/AuditTrailButton";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type BrandDto = { id: string; code: string; name: string; isActive: boolean };

const actionButtonClass = "px-2 py-1 text-xs";

export function BrandRow({ brand }: { brand: BrandDto }) {
  const router = useRouter();
  const [code, setCode] = useState(brand.code);
  const [name, setName] = useState(brand.name);
  const [isActive, setIsActive] = useState(brand.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCode(brand.code);
    setName(brand.name);
    setIsActive(brand.isActive ? "true" : "false");
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`brands/${brand.id}`, {
        code,
        name,
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
    if (!window.confirm(`Delete brand ${brand.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`brands/${brand.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">{brand.code}</td>
      <td className="py-2 pr-3">{brand.name}</td>
      <td className="py-2 pr-3">{brand.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit Brand ${brand.code}`} description="Update brand code, name, or active state." buttonLabel="Edit" variant="secondary" onOpen={beginEdit}>
            {({ close }) => (
              <form
                className="space-y-3"
                onSubmit={(event) => {
                  event.preventDefault();
                  void saveEdit(close);
                }}
              >
                <div className="grid gap-3 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium">Code</label>
                    <Input value={code} onChange={(e) => setCode(e.target.value)} required />
                  </div>
                  <div>
                    <label className="mb-1 block text-sm font-medium">Name</label>
                    <Input value={name} onChange={(e) => setName(e.target.value)} required />
                  </div>
                  <div>
                    <label className="mb-1 block text-sm font-medium">Active</label>
                    <Select value={isActive} onChange={(e) => setIsActive(e.target.value)}>
                      <option value="true">Yes</option>
                      <option value="false">No</option>
                    </Select>
                  </div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>
                  {busy ? "Saving..." : "Save Brand"}
                </Button>
              </form>
            )}
          </AppFormModal>
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
          <AuditTrailButton tableName="Brands" recordId={brand.id} />
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
