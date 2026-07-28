"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";
import { CATEGORY_CUSTOM, CATEGORY_JOB_WISE, categoryOptions } from "./categories";

type ServiceJobRef = { id: string; number: string };

export function PettyCashRequestLineAddForm({
  requestId,
  serviceJobs,
  disabled = false,
}: {
  requestId: string;
  serviceJobs: ServiceJobRef[];
  disabled?: boolean;
}) {
  const router = useRouter();
  const [category, setCategory] = useState(String(CATEGORY_JOB_WISE));
  const [serviceJobId, setServiceJobId] = useState("");
  const [customCategoryName, setCustomCategoryName] = useState("");
  const [purpose, setPurpose] = useState("");
  const [requestedAmount, setRequestedAmount] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const categoryValue = Number(category);
  const isJobWise = categoryValue === CATEGORY_JOB_WISE;
  const isCustom = categoryValue === CATEGORY_CUSTOM;

  const sortedJobs = serviceJobs.slice().sort((a, b) => b.number.localeCompare(a.number));

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await apiPost(`finance/petty-cash-requests/${requestId}/lines`, {
        category: categoryValue,
        serviceJobId: isJobWise ? serviceJobId : null,
        customCategoryName: isCustom ? customCategoryName.trim() : null,
        purpose: purpose.trim(),
        requestedAmount: Number(requestedAmount),
      });
      setPurpose("");
      setRequestedAmount("");
      setCustomCategoryName("");
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
          <label className="mb-1 block text-sm font-medium">Category</label>
          <Select value={category} onChange={(e) => setCategory(e.target.value)} disabled={disabled}>
            {categoryOptions.map((option) => (
              <option key={option.value} value={option.value}>{option.label}</option>
            ))}
          </Select>
        </div>

        {isJobWise ? (
          <div>
            <label className="mb-1 block text-sm font-medium">Job order</label>
            <Select
              value={serviceJobId}
              onChange={(e) => setServiceJobId(e.target.value)}
              required
              disabled={disabled}
            >
              <option value="" disabled>Select...</option>
              {sortedJobs.map((job) => (
                <option key={job.id} value={job.id}>{job.number}</option>
              ))}
            </Select>
            <p className="mt-1 text-xs text-zinc-500">
              Naming the job is what lets this spend reach that job&apos;s cost later.
            </p>
          </div>
        ) : null}

        {isCustom ? (
          <div>
            <label className="mb-1 block text-sm font-medium">Category name</label>
            <Input
              value={customCategoryName}
              onChange={(e) => setCustomCategoryName(e.target.value)}
              placeholder="e.g. Site refreshments"
              required
              disabled={disabled}
            />
          </div>
        ) : null}
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium">Purpose / reason</label>
          <Input
            value={purpose}
            onChange={(e) => setPurpose(e.target.value)}
            placeholder="What the money is for"
            required
            disabled={disabled}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Amount requested</label>
          <Input
            value={requestedAmount}
            inputMode="decimal"
            onChange={(e) => setRequestedAmount(e.target.value)}
            placeholder="0.00"
            required
            disabled={disabled}
          />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy || disabled}>
        {busy ? "Adding..." : "Add Category Line"}
      </Button>
    </form>
  );
}
