"use client";

import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { Button, Input } from "@/components/ui";

type CleanupResponse = { scope: string; message: string };
type CleanupAction = {
  key: string;
  label: string;
  path: string;
  description: string;
  impact: string;
};

const actions: CleanupAction[] = [
  {
    key: "po",
    label: "Clear PO",
    path: "admin/test-data/clear-purchase-orders",
    description: "Clears purchase orders and dependent GRNs/supplier invoices.",
    impact: "Also removes GRN stock movements and related AP entries.",
  },
  {
    key: "grn",
    label: "Clear GRN Tables",
    path: "admin/test-data/clear-goods-receipts",
    description: "Clears goods receipts and GRN-linked supplier invoices.",
    impact: "Also removes GRN stock movements and related AP entries.",
  },
  {
    key: "stock",
    label: "Zero Stock",
    path: "admin/test-data/zero-stock",
    description: "Deletes all inventory movement rows.",
    impact: "Stock becomes zero while source documents remain visible.",
  },
  {
    key: "service",
    label: "Clear Jobs / Service",
    path: "admin/test-data/clear-service",
    description: "Clears service contracts, jobs, daily sheets, staff/progress entries, IOUs, expenses, MRNs, material dispositions, QC, and handovers.",
    impact: "Also removes service stock movements and service petty-cash ledger entries. Keeps equipment units, technicians, and petty cash funds.",
  },
];

export function TestDataCleanupPanel() {
  const [confirmation, setConfirmation] = useState("");
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const confirmed = confirmation.trim().toUpperCase() === "CLEAR";

  async function runAction(action: CleanupAction) {
    if (!confirmed) {
      setError("Type CLEAR before running a cleanup action.");
      return;
    }

    if (!window.confirm(`Run "${action.label}"? This is intended only for test data cleanup.`)) {
      return;
    }

    setBusyKey(action.key);
    setMessage(null);
    setError(null);
    try {
      const result = await apiPost<CleanupResponse>(action.path, {});
      setMessage(result.message);
      setConfirmation("");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusyKey(null);
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-950 dark:border-amber-700/60 dark:bg-amber-950/30 dark:text-amber-100">
        These actions are destructive and are intended for test databases only. They do not delete master data such as items,
        warehouses, customers, suppliers, equipment units, technicians, or petty cash funds.
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Type CLEAR to enable buttons</label>
        <Input value={confirmation} onChange={(e) => setConfirmation(e.target.value)} placeholder="CLEAR" />
      </div>

      {message ? <div className="rounded-md border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-900">{message}</div> : null}
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900">{error}</div> : null}

      <div className="grid gap-3 md:grid-cols-2">
        {actions.map((action) => (
          <div key={action.key} className="rounded-lg border border-[var(--card-border)] bg-[var(--surface)] p-3">
            <div className="flex items-start justify-between gap-3">
              <div>
                <div className="text-sm font-semibold">{action.label}</div>
                <div className="mt-1 text-xs text-zinc-500">{action.description}</div>
                <div className="mt-2 text-xs text-amber-700 dark:text-amber-300">{action.impact}</div>
              </div>
              <Button type="button" onClick={() => runAction(action)} disabled={!confirmed || busyKey !== null}>
                {busyKey === action.key ? "Clearing..." : action.label}
              </Button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
