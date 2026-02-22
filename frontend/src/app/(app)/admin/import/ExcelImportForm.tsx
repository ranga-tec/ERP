"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Button, Input, SecondaryLink } from "@/components/ui";

type ImportResult = {
  brandsCreated: number;
  brandsUpdated: number;
  warehousesCreated: number;
  warehousesUpdated: number;
  suppliersCreated: number;
  suppliersUpdated: number;
  customersCreated: number;
  customersUpdated: number;
  itemsCreated: number;
  itemsUpdated: number;
  reorderSettingsCreated: number;
  reorderSettingsUpdated: number;
  equipmentUnitsCreated: number;
  equipmentUnitsUpdated: number;
};

function parseError(text: string): string {
  try {
    const data = JSON.parse(text) as { error?: string; errors?: string[] };
    if (data.errors?.length) return data.errors.join("\n");
    if (data.error) return data.error;
    return text;
  } catch {
    return text;
  }
}

export function ExcelImportForm() {
  const router = useRouter();
  const [file, setFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setResult(null);
    if (!file) {
      setError("Choose a .xlsx file first.");
      return;
    }

    setBusy(true);
    try {
      const form = new FormData();
      form.append("file", file);

      const resp = await fetch("/api/backend/admin/import/excel", {
        method: "POST",
        body: form,
      });

      if (!resp.ok) {
        const text = await resp.text();
        throw new Error(parseError(text) || `${resp.status} ${resp.statusText}`);
      }

      const data = (await resp.json()) as ImportResult;
      setResult(data);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <SecondaryLink href="/api/backend/admin/import/template">
          Download template (.xlsx)
        </SecondaryLink>
      </div>

      <form onSubmit={submit} className="space-y-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Excel file</label>
          <Input
            type="file"
            accept=".xlsx"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
          <div className="mt-1 text-xs text-zinc-500">
            Upload the filled template. Import is transactional (all-or-nothing).
          </div>
        </div>

        <Button type="submit" disabled={busy}>
          {busy ? "Importing..." : "Import"}
        </Button>
      </form>

      {error ? (
        <pre className="whitespace-pre-wrap rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </pre>
      ) : null}

      {result ? (
        <div className="rounded-md border border-zinc-200 bg-zinc-50 p-3 text-sm dark:border-zinc-800 dark:bg-zinc-900">
          <div className="font-semibold">Import complete</div>
          <div className="mt-2 grid gap-1 sm:grid-cols-2">
            <div>Brands: +{result.brandsCreated} / ~{result.brandsUpdated}</div>
            <div>
              Warehouses: +{result.warehousesCreated} / ~{result.warehousesUpdated}
            </div>
            <div>Suppliers: +{result.suppliersCreated} / ~{result.suppliersUpdated}</div>
            <div>Customers: +{result.customersCreated} / ~{result.customersUpdated}</div>
            <div>Items: +{result.itemsCreated} / ~{result.itemsUpdated}</div>
            <div>
              Reorder settings: +{result.reorderSettingsCreated} / ~
              {result.reorderSettingsUpdated}
            </div>
            <div>
              Equipment units: +{result.equipmentUnitsCreated} / ~
              {result.equipmentUnitsUpdated}
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}

