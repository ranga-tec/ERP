"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string };
type WorkOrderDto = { id: string };

export function WorkOrderCreateForm({ serviceJobs }: { serviceJobs: ServiceJobRef[] }) {
  const router = useRouter();
  const jobOptions = useMemo(
    () => serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number)),
    [serviceJobs],
  );

  const [serviceJobId, setServiceJobId] = useState("");
  const [description, setDescription] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const wo = await apiPost<WorkOrderDto>("service/work-orders", {
        serviceJobId,
        description: description.trim(),
        assignedToUserId: null,
      });
      router.push(`/service/work-orders/${wo.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Service job</label>
        <Select value={serviceJobId} onChange={(e) => setServiceJobId(e.target.value)} required>
          <option value="" disabled>
            Select...
          </option>
          {jobOptions.map((j) => (
            <option key={j.id} value={j.id}>
              {j.number}
            </option>
          ))}
        </Select>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Description</label>
        <Textarea value={description} onChange={(e) => setDescription(e.target.value)} required />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Work Order"}
      </Button>
    </form>
  );
}

