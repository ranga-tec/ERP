"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input } from "@/components/ui";

type ReferenceFormDto = {
  id: string;
  code: string;
  name: string;
  module: string;
  routeTemplate?: string | null;
  isActive: boolean;
};

export function ReferenceFormCreateForm() {
  const router = useRouter();
  const [code, setCode] = useState("PO");
  const [name, setName] = useState("Purchase Order");
  const [module, setModule] = useState("Procurement");
  const [routeTemplate, setRouteTemplate] = useState("/procurement/purchase-orders/{id}");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await apiPost<ReferenceFormDto>("reference-forms", {
        code,
        name,
        module,
        routeTemplate: routeTemplate.trim() || null,
      });

      setCode("");
      setName("");
      setModule("");
      setRouteTemplate("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium">Code</label>
          <Input value={code} onChange={(e) => setCode(e.target.value)} required />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Name</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Module</label>
          <Input value={module} onChange={(e) => setModule(e.target.value)} required />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Route Template</label>
          <Input value={routeTemplate} onChange={(e) => setRouteTemplate(e.target.value)} />
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Reference Form"}
      </Button>
    </form>
  );
}
