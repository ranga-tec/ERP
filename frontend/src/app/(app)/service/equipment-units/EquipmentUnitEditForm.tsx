"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type EquipmentUnitDto = {
  id: string;
  customerId: string;
  purchasedAt?: string | null;
  warrantyUntil?: string | null;
  warrantyCoverage: number;
};

type CustomerRef = { id: string; code: string; name: string };

const warrantyCoverageOptions = [
  { value: "0", label: "No Warranty" },
  { value: "1", label: "Inspection Only" },
  { value: "2", label: "Labor Only" },
  { value: "3", label: "Parts Only" },
  { value: "4", label: "Labor and Parts" },
];

function toDateInput(value?: string | null): string {
  return value ? new Date(value).toISOString().slice(0, 10) : "";
}

export function EquipmentUnitEditForm({
  unit,
  customers,
}: {
  unit: EquipmentUnitDto;
  customers: CustomerRef[];
}) {
  const router = useRouter();
  const [customerId, setCustomerId] = useState(unit.customerId);
  const [purchasedAt, setPurchasedAt] = useState(toDateInput(unit.purchasedAt));
  const [warrantyUntil, setWarrantyUntil] = useState(toDateInput(unit.warrantyUntil));
  const [warrantyCoverage, setWarrantyCoverage] = useState(String(unit.warrantyCoverage));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPut(`service/equipment-units/${unit.id}`, {
        customerId,
        purchasedAt: purchasedAt ? new Date(purchasedAt).toISOString() : null,
        warrantyUntil: warrantyUntil ? new Date(warrantyUntil).toISOString() : null,
        warrantyCoverage: warrantyUntil ? Number(warrantyCoverage) : 0,
      });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select value={customerId} onChange={(event) => setCustomerId(event.target.value)} required>
            {customers.map((customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.code} - {customer.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Purchased at</label>
          <Input type="date" value={purchasedAt} onChange={(event) => setPurchasedAt(event.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Warranty until</label>
          <Input type="date" value={warrantyUntil} onChange={(event) => setWarrantyUntil(event.target.value)} />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Warranty coverage</label>
          <Select
            value={warrantyUntil ? warrantyCoverage : "0"}
            onChange={(event) => setWarrantyCoverage(event.target.value)}
            disabled={!warrantyUntil}
          >
            {warrantyCoverageOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving..." : "Save Equipment Unit"}
      </Button>
    </form>
  );
}
