"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Select, Textarea } from "@/components/ui";

type QualityCheckEditable = {
  id: string;
  passed: boolean;
  notes?: string | null;
};

export function QualityCheckEditForm({ qualityCheck }: { qualityCheck: QualityCheckEditable }) {
  const router = useRouter();
  const [passed, setPassed] = useState(qualityCheck.passed ? "true" : "false");
  const [notes, setNotes] = useState(qualityCheck.notes ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPut(`service/quality-checks/${qualityCheck.id}`, {
        passed: passed === "true",
        notes: notes.trim() || null,
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
      <div>
        <label className="mb-1 block text-sm font-medium">Result</label>
        <Select value={passed} onChange={(e) => setPassed(e.target.value)} required>
          <option value="true">Passed</option>
          <option value="false">Failed</option>
        </Select>
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
        {busy ? "Saving..." : "Save Inspection / QC"}
      </Button>
    </form>
  );
}
