"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type OperationDto = { id: string };
type ItemOption = { id: string; sku: string; name: string };

export function ServiceJobOperationAddForm({
  serviceJobId,
  items,
  nextSequence,
  disabled,
}: {
  serviceJobId: string;
  items: ItemOption[];
  nextSequence: number;
  disabled?: boolean;
}) {
  const router = useRouter();
  const [sequence, setSequence] = useState(String(nextSequence));
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [plannedItemId, setPlannedItemId] = useState("");
  const [plannedQuantity, setPlannedQuantity] = useState("");
  const [estimatedLaborHours, setEstimatedLaborHours] = useState("");
  const [requiredAt, setRequiredAt] = useState("");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      if (!name.trim()) {
        throw new Error("Operation name is required.");
      }

      await apiPost<OperationDto>(`service/jobs/${serviceJobId}/operations`, {
        sequence: Number(sequence) || nextSequence,
        name: name.trim(),
        description: description.trim() || null,
        plannedItemId: plannedItemId || null,
        plannedQuantity: Number(plannedQuantity) || 0,
        estimatedLaborHours: Number(estimatedLaborHours) || 0,
        requiredAt: requiredAt ? new Date(requiredAt).toISOString() : null,
        notes: notes.trim() || null,
      });

      setSequence(String(nextSequence + 10));
      setName("");
      setDescription("");
      setPlannedItemId("");
      setPlannedQuantity("");
      setEstimatedLaborHours("");
      setRequiredAt("");
      setNotes("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 lg:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Seq</label>
          <Input type="number" min="1" step="1" value={sequence} onChange={(event) => setSequence(event.target.value)} disabled={disabled || busy} />
        </div>
        <div className="lg:col-span-2">
          <label className="mb-1 block text-sm font-medium">Operation / subassembly</label>
          <Input value={name} onChange={(event) => setName(event.target.value)} disabled={disabled || busy} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Required by</label>
          <Input type="date" value={requiredAt} onChange={(event) => setRequiredAt(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      <div className="grid gap-3 lg:grid-cols-4">
        <div className="lg:col-span-2">
          <label className="mb-1 block text-sm font-medium">Planned part</label>
          <Select value={plannedItemId} onChange={(event) => setPlannedItemId(event.target.value)} disabled={disabled || busy}>
            <option value="">No planned part</option>
            {items.map((item) => (
              <option key={item.id} value={item.id}>
                {item.sku} - {item.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Planned qty</label>
          <Input type="number" min="0" step="0.01" value={plannedQuantity} onChange={(event) => setPlannedQuantity(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Labor hours</label>
          <Input type="number" min="0" step="0.25" value={estimatedLaborHours} onChange={(event) => setEstimatedLaborHours(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      <div className="grid gap-3 lg:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Description</label>
          <Textarea value={description} onChange={(event) => setDescription(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Notes</label>
          <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>

      {error ? <div className="text-sm text-red-700 dark:text-red-300">{error}</div> : null}
      <Button type="submit" disabled={disabled || busy}>{busy ? "Adding..." : "Add Operation"}</Button>
    </form>
  );
}
