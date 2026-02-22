"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string };
type QualityCheckDto = { id: string };

export function QualityCheckCreateForm({ serviceJobs }: { serviceJobs: ServiceJobRef[] }) {
  const router = useRouter();
  const jobOptions = useMemo(
    () => serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number)),
    [serviceJobs],
  );

  const [serviceJobId, setServiceJobId] = useState("");
  const [passed, setPassed] = useState("true");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const qc = await apiPost<QualityCheckDto>("service/quality-checks", {
        serviceJobId,
        passed: passed === "true",
        notes: notes.trim() || null,
      });
      router.push(`/service/quality-checks/${qc.id}`);
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
            {jobOptions.map((j) => (
              <option key={j.id} value={j.id}>
                {j.number}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Result</label>
          <Select value={passed} onChange={(e) => setPassed(e.target.value)} required>
            <option value="true">Passed</option>
            <option value="false">Failed</option>
          </Select>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Textarea value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Add Quality Check"}
      </Button>
    </form>
  );
}

