"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type ReferenceFormDto = {
  id: string;
  code: string;
  name: string;
  module: string;
  routeTemplate?: string | null;
  isActive: boolean;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function ReferenceFormRow({ form }: { form: ReferenceFormDto }) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [code, setCode] = useState(form.code);
  const [name, setName] = useState(form.name);
  const [module, setModule] = useState(form.module);
  const [routeTemplate, setRouteTemplate] = useState(form.routeTemplate ?? "");
  const [isActive, setIsActive] = useState(form.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCode(form.code);
    setName(form.name);
    setModule(form.module);
    setRouteTemplate(form.routeTemplate ?? "");
    setIsActive(form.isActive ? "true" : "false");
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`reference-forms/${form.id}`, {
        code,
        name,
        module,
        routeTemplate: routeTemplate.trim() || null,
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
    if (!window.confirm(`Delete reference form ${form.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`reference-forms/${form.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? <Input value={code} onChange={(e) => setCode(e.target.value)} className="min-w-20" /> : form.code}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={name} onChange={(e) => setName(e.target.value)} className="min-w-32" /> : form.name}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={module} onChange={(e) => setModule(e.target.value)} className="min-w-24" /> : form.module}
      </td>
      <td className="py-2 pr-3 font-mono text-xs text-zinc-500">
        {isEditing ? (
          <Input value={routeTemplate} onChange={(e) => setRouteTemplate(e.target.value)} className="min-w-40" />
        ) : (
          form.routeTemplate ?? "-"
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : form.isActive ? (
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
