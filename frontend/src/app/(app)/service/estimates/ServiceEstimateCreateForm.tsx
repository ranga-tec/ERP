"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type ServiceJobRef = { id: string; number: string; customerId: string; status: number };
type CustomerRef = { id: string; code: string; name: string };
type ServiceEstimateDto = { id: string; number: string };

export function ServiceEstimateCreateForm({
  serviceJobs,
  customers,
}: {
  serviceJobs: ServiceJobRef[];
  customers: CustomerRef[];
}) {
  const router = useRouter();
  const [serviceJobId, setServiceJobId] = useState("");
  const [validUntil, setValidUntil] = useState("");
  const [terms, setTerms] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const customerById = new Map(customers.map((c) => [c.id, c]));
  const jobOptions = serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number));

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const estimate = await apiPost<ServiceEstimateDto>("service/estimates", {
        serviceJobId,
        validUntil: validUntil ? new Date(validUntil).toISOString() : null,
        terms: terms.trim() || null,
      });
      router.push(`/service/estimates/${estimate.id}`);
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
            {jobOptions.map((j) => {
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
          <label className="mb-1 block text-sm font-medium">Valid till (optional)</label>
          <Input type="datetime-local" value={validUntil} onChange={(e) => setValidUntil(e.target.value)} />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Terms (optional)</label>
        <Textarea
          value={terms}
          onChange={(e) => setTerms(e.target.value)}
          placeholder="Warranty, exclusions, turnaround, advance payment, etc."
        />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Service Estimate"}
      </Button>
    </form>
  );
}
