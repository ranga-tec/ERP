"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type CustomerDto = {
  id: string;
  code: string;
  name: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  isActive: boolean;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function CustomerRow({ customer }: { customer: CustomerDto }) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [code, setCode] = useState(customer.code);
  const [name, setName] = useState(customer.name);
  const [phone, setPhone] = useState(customer.phone ?? "");
  const [email, setEmail] = useState(customer.email ?? "");
  const [address, setAddress] = useState(customer.address ?? "");
  const [isActive, setIsActive] = useState(customer.isActive ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCode(customer.code);
    setName(customer.name);
    setPhone(customer.phone ?? "");
    setEmail(customer.email ?? "");
    setAddress(customer.address ?? "");
    setIsActive(customer.isActive ? "true" : "false");
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`customers/${customer.id}`, {
        code,
        name,
        phone: phone.trim() || null,
        email: email.trim() || null,
        address: address.trim() || null,
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
    if (!window.confirm(`Delete customer ${customer.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`customers/${customer.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">
        {isEditing ? <Input value={code} onChange={(e) => setCode(e.target.value)} className="min-w-20" /> : customer.code}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? <Input value={name} onChange={(e) => setName(e.target.value)} className="min-w-32" /> : customer.name}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? <Input value={phone} onChange={(e) => setPhone(e.target.value)} className="min-w-28" /> : customer.phone ?? "-"}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? <Input value={email} onChange={(e) => setEmail(e.target.value)} className="min-w-36" /> : customer.email ?? "-"}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? <Input value={address} onChange={(e) => setAddress(e.target.value)} className="min-w-40" /> : customer.address ?? "-"}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="min-w-20">
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        ) : customer.isActive ? (
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
