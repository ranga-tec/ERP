"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Textarea } from "@/components/ui";

type ServiceEstimateDto = {
  id: string;
  validUntil?: string | null;
  terms?: string | null;
};

function toDateInput(value?: string | null): string {
  return value ? new Date(value).toISOString().slice(0, 10) : "";
}

export function ServiceEstimateEditForm({ estimate }: { estimate: ServiceEstimateDto }) {
  const router = useRouter();
  const [validUntil, setValidUntil] = useState(toDateInput(estimate.validUntil));
  const [terms, setTerms] = useState(estimate.terms ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPut(`service/estimates/${estimate.id}`, {
        validUntil: validUntil ? new Date(validUntil).toISOString() : null,
        terms: terms.trim() || null,
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
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Valid until</label>
          <Input type="date" value={validUntil} onChange={(event) => setValidUntil(event.target.value)} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Terms</label>
        <Textarea value={terms} onChange={(event) => setTerms(event.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving..." : "Save Estimate"}
      </Button>
    </form>
  );
}
