"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Select } from "@/components/ui";

type SupplierRef = { id: string; code: string; name: string };
type PurchaseOrderRefDto = { id: string; number: string };

export function PurchaseRequisitionConvertToPoForm({
  purchaseRequisitionId,
  suppliers,
  disabled,
}: {
  purchaseRequisitionId: string;
  suppliers: SupplierRef[];
  disabled?: boolean;
}) {
  const router = useRouter();
  const supplierOptions = useMemo(
    () => suppliers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [suppliers],
  );
  const [supplierId, setSupplierId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const po = await apiPost<PurchaseOrderRefDto>(
        `procurement/purchase-requisitions/${purchaseRequisitionId}/convert-to-po`,
        { supplierId },
      );
      router.push(`/procurement/purchase-orders/${po.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Supplier for PO</label>
        <Select
          value={supplierId}
          onChange={(e) => setSupplierId(e.target.value)}
          required
          disabled={disabled || supplierOptions.length === 0}
        >
          <option value="" disabled>
            Select...
          </option>
          {supplierOptions.map((s) => (
            <option key={s.id} value={s.id}>
              {s.code} - {s.name}
            </option>
          ))}
        </Select>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy || disabled || !supplierId}>
        {busy ? "Converting..." : "Convert To Draft PO"}
      </Button>
    </form>
  );
}
