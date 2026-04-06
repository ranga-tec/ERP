"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Textarea } from "@/components/ui";

type ServiceHandoverDto = {
  id: string;
  itemsReturned: string;
  postServiceWarrantyMonths?: number | null;
  customerAcknowledgement?: string | null;
  notes?: string | null;
};

export function ServiceHandoverEditForm({ handover }: { handover: ServiceHandoverDto }) {
  const router = useRouter();
  const [itemsReturned, setItemsReturned] = useState(handover.itemsReturned);
  const [postServiceWarrantyMonths, setPostServiceWarrantyMonths] = useState(
    handover.postServiceWarrantyMonths?.toString() ?? "",
  );
  const [customerAcknowledgement, setCustomerAcknowledgement] = useState(handover.customerAcknowledgement ?? "");
  const [notes, setNotes] = useState(handover.notes ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
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

      await apiPut(`service/handovers/${handover.id}`, {
        itemsReturned: itemsReturned.trim(),
        postServiceWarrantyMonths: warrantyMonths,
        customerAcknowledgement: customerAcknowledgement.trim() || null,
        notes: notes.trim() || null,
      });

      router.push(`/service/handovers/${handover.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Items returned to customer</label>
        <Textarea
          value={itemsReturned}
          onChange={(event) => setItemsReturned(event.target.value)}
          placeholder="Device, charger, battery, accessories, replaced parts (if returned), etc."
          required
        />
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Post-service Warranty (months)</label>
          <Input
            value={postServiceWarrantyMonths}
            onChange={(event) => setPostServiceWarrantyMonths(event.target.value)}
            inputMode="numeric"
            placeholder="Optional"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Customer acknowledgement (optional)</label>
          <Input
            value={customerAcknowledgement}
            onChange={(event) => setCustomerAcknowledgement(event.target.value)}
            placeholder="Received in good condition / Signature ref"
          />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving..." : "Save Handover"}
      </Button>
    </form>
  );
}
