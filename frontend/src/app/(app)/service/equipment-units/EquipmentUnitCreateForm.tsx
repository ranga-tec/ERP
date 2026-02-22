"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string };
type CustomerRef = { id: string; code: string; name: string };

type EquipmentUnitDto = { id: string; itemId: string; serialNumber: string; customerId: string };

export function EquipmentUnitCreateForm({
  equipmentItems,
  customers,
}: {
  equipmentItems: ItemRef[];
  customers: CustomerRef[];
}) {
  const router = useRouter();
  const itemOptions = useMemo(
    () => equipmentItems.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [equipmentItems],
  );
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );

  const [itemId, setItemId] = useState("");
  const [serialNumber, setSerialNumber] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [purchasedAt, setPurchasedAt] = useState("");
  const [warrantyUntil, setWarrantyUntil] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const unit = await apiPost<EquipmentUnitDto>("service/equipment-units", {
        itemId,
        serialNumber: serialNumber.trim(),
        customerId,
        purchasedAt: purchasedAt ? new Date(purchasedAt).toISOString() : null,
        warrantyUntil: warrantyUntil ? new Date(warrantyUntil).toISOString() : null,
      });
      router.push(`/service/equipment-units/${unit.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Equipment item</label>
          <Select value={itemId} onChange={(e) => setItemId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {itemOptions.map((i) => (
              <option key={i.id} value={i.id}>
                {i.sku} — {i.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select value={customerId} onChange={(e) => setCustomerId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {customerOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} — {c.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div className="sm:col-span-1">
          <label className="mb-1 block text-sm font-medium">Serial number</label>
          <Input value={serialNumber} onChange={(e) => setSerialNumber(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Purchased at (optional)</label>
          <Input type="date" value={purchasedAt} onChange={(e) => setPurchasedAt(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Warranty until (optional)</label>
          <Input type="date" value={warrantyUntil} onChange={(e) => setWarrantyUntil(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Equipment Unit"}
      </Button>
    </form>
  );
}

