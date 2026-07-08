"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiDeleteNoContent } from "@/lib/api-client";
import { AppFormModal } from "@/components/AppFormModal";
import { SecondaryButton } from "@/components/ui";
import { ServiceTechnicianEditForm } from "./ServiceTechnicianEditForm";

type ServiceTechnicianDto = {
  id: string;
  code: string;
  name: string;
  defaultCostRate: number;
  defaultBillingRate: number;
  phone?: string | null;
  notes?: string | null;
  isActive: boolean;
};

export function ServiceTechnicianRow({ technician }: { technician: ServiceTechnicianDto }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function remove() {
    if (!window.confirm("Delete this technician?")) return;
    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`service/technicians/${technician.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">
        {technician.code}
      </td>
      <td className="py-2 pr-3">
        {technician.name}
      </td>
      <td className="py-2 pr-3">
        {technician.defaultCostRate.toFixed(2)}
      </td>
      <td className="py-2 pr-3">
        {technician.defaultBillingRate.toFixed(2)}
      </td>
      <td className="py-2 pr-3">
        {technician.phone ?? "-"}
      </td>
      <td className="py-2 pr-3">
        {technician.notes ?? "-"}
      </td>
      <td className="py-2 pr-3">
        {technician.isActive ? "Active" : "Inactive"}
      </td>
      <td className="py-2 pr-3">
        <div className="flex flex-wrap gap-2">
          <AppFormModal title={`Edit Technician ${technician.code}`} description="Update technician master data and default labour rates." buttonLabel="Edit" variant="secondary">
            <ServiceTechnicianEditForm technician={technician} />
          </AppFormModal>
          <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => void remove()} disabled={busy}>
            Delete
          </SecondaryButton>
        </div>
        {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
      </td>
    </tr>
  );
}
