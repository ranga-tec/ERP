"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type PaymentTypeDto = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function PaymentTypeRow({ paymentType }: { paymentType: PaymentTypeDto }) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [code, setCode] = useState(paymentType.code);
  const [name, setName] = useState(paymentType.name);
  const [description, setDescription] = useState(paymentType.description ?? "");
  const [isActive, setIsActive] = useState(paymentType.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCode(paymentType.code);
    setName(paymentType.name);
    setDescription(paymentType.description ?? "");
    setIsActive(paymentType.isActive ? "true" : "false");
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`payment-types/${paymentType.id}`, {
        code,
        name,
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
    if (!window.confirm(`Delete payment type ${paymentType.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`payment-types/${paymentType.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? <Input value={code} onChange={(e) => setCode(e.target.value)} className="min-w-20" /> : paymentType.code}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={name} onChange={(e) => setName(e.target.value)} className="min-w-32" /> : paymentType.name}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? (
          <Input value={description} onChange={(e) => setDescription(e.target.value)} className="min-w-40" />
        ) : (
          paymentType.description ?? "-"
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : paymentType.isActive ? (
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
