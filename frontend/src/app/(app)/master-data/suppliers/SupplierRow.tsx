"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPut } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { AuditTrailButton } from "@/components/AuditTrailButton";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

type SupplierDto = {
  id: string;
  code: string;
  name: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  isActive: boolean;
  isAuthorized: boolean;
};

const actionButtonClass = "px-2 py-1 text-xs";

export function SupplierRow({ supplier }: { supplier: SupplierDto }) {
  const router = useRouter();
  const [code, setCode] = useState(supplier.code);
  const [name, setName] = useState(supplier.name);
  const [phone, setPhone] = useState(supplier.phone ?? "");
  const [email, setEmail] = useState(supplier.email ?? "");
  const [address, setAddress] = useState(supplier.address ?? "");
  const [isActive, setIsActive] = useState(supplier.isActive ? "true" : "false");
  const [isAuthorized, setIsAuthorized] = useState(supplier.isAuthorized ? "true" : "false");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setCode(supplier.code);
    setName(supplier.name);
    setPhone(supplier.phone ?? "");
    setEmail(supplier.email ?? "");
    setAddress(supplier.address ?? "");
    setIsActive(supplier.isActive ? "true" : "false");
    setIsAuthorized(supplier.isAuthorized ? "true" : "false");
  }

  async function saveEdit(close: () => void) {
    setError(null);
    setBusy(true);
    try {
      await apiPut(`suppliers/${supplier.id}`, {
        code,
        name,
        phone: phone.trim() || null,
        email: email.trim() || null,
        address: address.trim() || null,
        isActive: isActive === "true",
        isAuthorized: isAuthorized === "true",
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
    if (!window.confirm(`Delete supplier ${supplier.code}?`)) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`suppliers/${supplier.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3 font-mono text-xs">{supplier.code}</td>
      <td className="py-2 pr-3">{supplier.name}</td>
      <td className="py-2 pr-3 text-zinc-500">{supplier.phone ?? "-"}</td>
      <td className="py-2 pr-3 text-zinc-500">{supplier.email ?? "-"}</td>
      <td className="py-2 pr-3 text-zinc-500">{supplier.address ?? "-"}</td>
      <td className="py-2 pr-3">{supplier.isActive ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">{supplier.isAuthorized ? "Yes" : "No"}</td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap items-center gap-2">
          <AppFormModal title={`Edit Supplier ${supplier.code}`} description="Update supplier details, approval, and active state." buttonLabel="Edit" variant="secondary" onOpen={beginEdit}>
            {({ close }) => (
              <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveEdit(close); }}>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div><label className="mb-1 block text-sm font-medium">Code</label><Input value={code} onChange={(e) => setCode(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Name</label><Input value={name} onChange={(e) => setName(e.target.value)} required /></div>
                  <div><label className="mb-1 block text-sm font-medium">Phone</label><Input value={phone} onChange={(e) => setPhone(e.target.value)} /></div>
                  <div><label className="mb-1 block text-sm font-medium">Email</label><Input value={email} onChange={(e) => setEmail(e.target.value)} type="email" /></div>
                  <div className="sm:col-span-2"><label className="mb-1 block text-sm font-medium">Address</label><Input value={address} onChange={(e) => setAddress(e.target.value)} /></div>
                  <div><label className="mb-1 block text-sm font-medium">Active</label><Select value={isActive} onChange={(e) => setIsActive(e.target.value)}><option value="true">Yes</option><option value="false">No</option></Select></div>
                  <div><label className="mb-1 block text-sm font-medium">Authorized</label><Select value={isAuthorized} onChange={(e) => setIsAuthorized(e.target.value)}><option value="true">Yes</option><option value="false">No</option></Select></div>
                </div>
                {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
                <Button type="submit" disabled={busy}>{busy ? "Saving..." : "Save Supplier"}</Button>
              </form>
            )}
          </AppFormModal>
          <SecondaryButton type="button" className={actionButtonClass} onClick={deleteRow} disabled={busy}>
            Delete
          </SecondaryButton>
          <AuditTrailButton tableName="Suppliers" recordId={supplier.id} />
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
