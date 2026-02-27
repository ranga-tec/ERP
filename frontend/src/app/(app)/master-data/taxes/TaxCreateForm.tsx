"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type TaxDto = {
  id: string;
  code: string;
  name: string;
  ratePercent: number;
  isInclusive: boolean;
  scope: number;
  description?: string | null;
  isActive: boolean;
};

const scopeOptions = [
  { value: 1, label: "Sales" },
  { value: 2, label: "Purchase" },
  { value: 3, label: "Both" },
];

export function TaxCreateForm() {
  const router = useRouter();
  const [code, setCode] = useState("VAT15");
  const [name, setName] = useState("VAT 15%");
  const [ratePercent, setRatePercent] = useState("15");
  const [isInclusive, setIsInclusive] = useState("false");
  const [scope, setScope] = useState("3");
  const [description, setDescription] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPost<TaxDto>("taxes", {
        code,
        name,
        ratePercent: Number(ratePercent),
        isInclusive: isInclusive === "true",
        scope: Number(scope),
        description: description.trim() || null,
      });

      setCode("");
      setName("");
      setRatePercent("0");
      setIsInclusive("false");
      setScope("3");
      setDescription("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(e) => setCode(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Rate %</label>
          <Input value={ratePercent} onChange={(e) => setRatePercent(e.target.value)} inputMode="decimal" required />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Scope</label>
          <Select value={scope} onChange={(e) => setScope(e.target.value)}>
            {scopeOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Inclusive</label>
          <Select value={isInclusive} onChange={(e) => setIsInclusive(e.target.value)}>
            <option value="false">No</option>
            <option value="true">Yes</option>
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Description</label>
          <Input value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Tax Code"}
      </Button>
    </form>
  );
}
