"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { ItemLookupField } from "@/components/ItemLookupField";
import { Button, Input, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string };
type CustomerRef = { id: string; code: string; name: string };

type EquipmentUnitDto = { id: string; itemId: string; serialNumber: string; customerId: string };

const warrantyCoverageOptions = [
  { value: "0", label: "No Warranty" },
  { value: "1", label: "Inspection Only" },
  { value: "2", label: "Labor Only" },
  { value: "3", label: "Parts Only" },
  { value: "4", label: "Labor and Parts" },
];

export function EquipmentUnitCreateForm({
  equipmentItems,
  customers,
}: {
  equipmentItems: ItemRef[];
  customers: CustomerRef[];
}) {
  const router = useRouter();
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );

  const [entryMode, setEntryMode] = useState<"existing" | "external">("existing");
  const [itemId, setItemId] = useState("");
  const [externalItemSku, setExternalItemSku] = useState("");
  const [externalItemName, setExternalItemName] = useState("");
  const [externalUnitOfMeasure, setExternalUnitOfMeasure] = useState("PCS");
  const [serialNumber, setSerialNumber] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [purchasedAt, setPurchasedAt] = useState("");
  const [warrantyUntil, setWarrantyUntil] = useState("");
  const [warrantyCoverage, setWarrantyCoverage] = useState("4");
  const [serviceIntervalDays, setServiceIntervalDays] = useState("");
  const [nextServiceDueAt, setNextServiceDueAt] = useState("");
  const [nextRepairDueAt, setNextRepairDueAt] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      if (entryMode === "existing" && !itemId) {
        throw new Error("Equipment item is required.");
      }

      if (entryMode === "external" && (!externalItemSku.trim() || !externalItemName.trim())) {
        throw new Error("External equipment SKU and name are required.");
      }

      const basePayload = {
        serialNumber: serialNumber.trim(),
        customerId,
        purchasedAt: purchasedAt ? new Date(purchasedAt).toISOString() : null,
        warrantyUntil: warrantyUntil ? new Date(warrantyUntil).toISOString() : null,
        warrantyCoverage: warrantyUntil ? Number(warrantyCoverage) : 0,
        serviceIntervalDays: serviceIntervalDays ? Number(serviceIntervalDays) : null,
        nextServiceDueAt: nextServiceDueAt ? new Date(nextServiceDueAt).toISOString() : null,
        nextRepairDueAt: nextRepairDueAt ? new Date(nextRepairDueAt).toISOString() : null,
      };

      const unit =
        entryMode === "external"
          ? await apiPost<EquipmentUnitDto>("service/equipment-units/external", {
              ...basePayload,
              itemSku: externalItemSku.trim(),
              itemName: externalItemName.trim(),
              unitOfMeasure: externalUnitOfMeasure.trim() || "PCS",
            })
          : await apiPost<EquipmentUnitDto>("service/equipment-units", {
              ...basePayload,
              itemId,
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
      <div className="inline-flex rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)] p-1 text-sm">
        <button
          type="button"
          className={`rounded px-3 py-1.5 ${entryMode === "existing" ? "bg-[var(--surface)] font-medium shadow-sm" : "text-zinc-500"}`}
          onClick={() => setEntryMode("existing")}
        >
          Existing item
        </button>
        <button
          type="button"
          className={`rounded px-3 py-1.5 ${entryMode === "external" ? "bg-[var(--surface)] font-medium shadow-sm" : "text-zinc-500"}`}
          onClick={() => setEntryMode("external")}
        >
          Outside equipment
        </button>
      </div>

      <div className={`grid gap-3 ${entryMode === "external" ? "lg:grid-cols-[2fr_1fr]" : "sm:grid-cols-2"}`}>
        {entryMode === "existing" ? (
          <div>
            <label className="mb-1 block text-sm font-medium">Equipment item</label>
            <ItemLookupField items={equipmentItems} value={itemId} onChange={setItemId} />
          </div>
        ) : (
          <div className="grid gap-3 sm:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium">External equipment SKU</label>
              <Input value={externalItemSku} onChange={(e) => setExternalItemSku(e.target.value)} required={entryMode === "external"} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">External equipment name</label>
              <Input value={externalItemName} onChange={(e) => setExternalItemName(e.target.value)} required={entryMode === "external"} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">UoM</label>
              <Input value={externalUnitOfMeasure} onChange={(e) => setExternalUnitOfMeasure(e.target.value)} required={entryMode === "external"} />
            </div>
          </div>
        )}
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select value={customerId} onChange={(e) => setCustomerId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {customerOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} - {c.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-4">
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
        <div>
          <label className="mb-1 block text-sm font-medium">Warranty coverage</label>
          <Select
            value={warrantyUntil ? warrantyCoverage : "0"}
            onChange={(e) => setWarrantyCoverage(e.target.value)}
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

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Service interval days</label>
          <Input min="1" type="number" value={serviceIntervalDays} onChange={(e) => setServiceIntervalDays(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Next service date</label>
          <Input type="date" value={nextServiceDueAt} onChange={(e) => setNextServiceDueAt(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Next repair date</label>
          <Input type="date" value={nextRepairDueAt} onChange={(e) => setNextRepairDueAt(e.target.value)} />
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
