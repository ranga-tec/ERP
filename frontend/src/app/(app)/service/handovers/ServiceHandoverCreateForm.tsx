"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string; customerId: string; status: number };
type CustomerRef = { id: string; code: string; name: string };
type ServiceHandoverDto = { id: string; number: string };

export function ServiceHandoverCreateForm({
  serviceJobs,
  customers,
}: {
  serviceJobs: ServiceJobRef[];
  customers: CustomerRef[];
}) {
  const router = useRouter();
  const [serviceJobId, setServiceJobId] = useState("");
  const [itemsReturned, setItemsReturned] = useState("");
  const [postServiceWarrantyMonths, setPostServiceWarrantyMonths] = useState("");
  const [customerAcknowledgement, setCustomerAcknowledgement] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const sortedJobs = serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number));

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      let warrantyMonths: number | null = null;
      if (postServiceWarrantyMonths.trim()) {
        warrantyMonths = Number(postServiceWarrantyMonths);
        if (!Number.isInteger(warrantyMonths) || warrantyMonths < 0) {
          throw new Error("Post-service warranty months must be a whole number >= 0.");
        }
      }

      const handover = await apiPost<ServiceHandoverDto>("service/handovers", {
        serviceJobId,
        itemsReturned: itemsReturned.trim(),
        postServiceWarrantyMonths: warrantyMonths,
        customerAcknowledgement: customerAcknowledgement.trim() || null,
        notes: notes.trim() || null,
      });
      router.push(`/service/handovers/${handover.id}`);
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
          <label className="mb-1 block text-sm font-medium">Service job</label>
          <Select value={serviceJobId} onChange={(e) => setServiceJobId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {sortedJobs.map((j) => {
              const customer = customerById.get(j.customerId);
              return (
                <option key={j.id} value={j.id}>
                  {j.number}
                  {customer ? ` - ${customer.code}` : ""}
                </option>
              );
            })}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Post-service Warranty (months)</label>
          <Input
            value={postServiceWarrantyMonths}
            onChange={(e) => setPostServiceWarrantyMonths(e.target.value)}
            inputMode="numeric"
            placeholder="Optional"
          />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Items returned to customer</label>
        <Textarea
          value={itemsReturned}
          onChange={(e) => setItemsReturned(e.target.value)}
          placeholder="Device, charger, battery, accessories, replaced parts (if returned), etc."
          required
        />
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Customer acknowledgement (optional)</label>
          <Input
            value={customerAcknowledgement}
            onChange={(e) => setCustomerAcknowledgement(e.target.value)}
            placeholder="Received in good condition / Signature ref"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
          <Input value={notes} onChange={(e) => setNotes(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Service Handover"}
      </Button>
    </form>
  );
}
