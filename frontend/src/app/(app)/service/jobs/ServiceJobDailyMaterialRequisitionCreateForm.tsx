"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type DailySheetRef = { id: string; number: string; status: number };
type WarehouseRef = { id: string; code: string; name: string };
type MaterialRequisitionDto = { id: string };

export function ServiceJobDailyMaterialRequisitionCreateForm({
  serviceJobId,
  dailySheets,
  warehouses,
  disabled,
}: {
  serviceJobId: string;
  dailySheets: DailySheetRef[];
  warehouses: WarehouseRef[];
  disabled?: boolean;
}) {
  const router = useRouter();
  const [dailySheetId, setDailySheetId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [purpose, setPurpose] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const mr = await apiPost<MaterialRequisitionDto>("service/material-requisitions", {
        serviceJobId,
        serviceJobDailySheetId: dailySheetId || null,
        warehouseId,
        purpose: purpose.trim() || null,
      });
      router.push(`/service/material-requisitions/${mr.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Daily sheet</label>
          <Select value={dailySheetId} onChange={(event) => setDailySheetId(event.target.value)} disabled={disabled || busy}>
            <option value="">Unlinked</option>
            {dailySheets.filter((sheet) => sheet.status !== 2).map((sheet) => <option key={sheet.id} value={sheet.id}>{sheet.number}</option>)}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Warehouse</label>
          <Select value={warehouseId} onChange={(event) => setWarehouseId(event.target.value)} disabled={disabled || busy} required>
            <option value="" disabled>Select...</option>
            {warehouses.map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} - {warehouse.name}</option>)}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Purpose</label>
          <Input value={purpose} onChange={(event) => setPurpose(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
      <Button type="submit" disabled={disabled || busy}>{busy ? "Creating..." : "Create MRN For Daily Sheet"}</Button>
    </form>
  );
}
