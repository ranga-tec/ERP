"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type CustomerRef = { id: string; code: string; name: string };
type InvoiceSourceRef = {
  id: string;
  number: string;
  customerId: string;
  dispatchedAt: string;
  lineCount: number;
};
type InvoiceDto = { id: string; number: string };
type CreateMode = "dispatch" | "directDispatch" | "manual";

export function InvoiceCreateForm({
  customers,
  dispatches,
  directDispatches,
}: {
  customers: CustomerRef[];
  dispatches: InvoiceSourceRef[];
  directDispatches: InvoiceSourceRef[];
}) {
  const router = useRouter();
  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );
  const customerById = useMemo(() => new Map(customers.map((customer) => [customer.id, customer])), [customers]);
  const sourceOptions = useMemo(
    () => ({
      dispatch: dispatches.slice().sort((a, b) => b.dispatchedAt.localeCompare(a.dispatchedAt)),
      directDispatch: directDispatches.slice().sort((a, b) => b.dispatchedAt.localeCompare(a.dispatchedAt)),
    }),
    [dispatches, directDispatches],
  );

  const [mode, setMode] = useState<CreateMode>("dispatch");
  const [customerId, setCustomerId] = useState("");
  const [sourceId, setSourceId] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedSource =
    mode === "dispatch"
      ? sourceOptions.dispatch.find((source) => source.id === sourceId)
      : mode === "directDispatch"
        ? sourceOptions.directDispatch.find((source) => source.id === sourceId)
        : null;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const payloadDueDate = dueDate ? new Date(dueDate).toISOString() : null;
      let invoice: InvoiceDto;

      if (mode === "dispatch") {
        if (!sourceId) {
          throw new Error("Select an AOD / dispatch.");
        }

        invoice = await apiPost<InvoiceDto>("sales/invoices/from-dispatch", {
          dispatchId: sourceId,
          dueDate: payloadDueDate,
        });
      } else if (mode === "directDispatch") {
        if (!sourceId) {
          throw new Error("Select a direct dispatch.");
        }

        invoice = await apiPost<InvoiceDto>("sales/invoices/from-direct-dispatch", {
          directDispatchId: sourceId,
          dueDate: payloadDueDate,
        });
      } else {
        if (!customerId) {
          throw new Error("Customer is required.");
        }

        invoice = await apiPost<InvoiceDto>("sales/invoices", {
          customerId,
          dueDate: payloadDueDate,
        });
      }

      router.push(`/sales/invoices/${invoice.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  const activeSources = mode === "dispatch" ? sourceOptions.dispatch : sourceOptions.directDispatch;

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium">Create from</label>
        <Select
          value={mode}
          onChange={(e) => {
            setMode(e.target.value as CreateMode);
            setSourceId("");
            setCustomerId("");
          }}
        >
          <option value="dispatch">AOD / Dispatch</option>
          <option value="directDispatch">Direct Dispatch</option>
          <option value="manual">Manual Invoice</option>
        </Select>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        {mode === "manual" ? (
          <div>
            <label className="mb-1 block text-sm font-medium">Customer</label>
            <Select value={customerId} onChange={(e) => setCustomerId(e.target.value)} required>
              <option value="" disabled>
                Select...
              </option>
              {customerOptions.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.code} - {c.name}
                </option>
              ))}
            </Select>
          </div>
        ) : (
          <div>
            <label className="mb-1 block text-sm font-medium">
              {mode === "dispatch" ? "AOD / Dispatch" : "Direct Dispatch"}
            </label>
            <Select value={sourceId} onChange={(e) => setSourceId(e.target.value)} required>
              <option value="" disabled>
                Select...
              </option>
              {activeSources.map((source) => {
                const customer = customerById.get(source.customerId);
                return (
                  <option key={source.id} value={source.id}>
                    {source.number} - {customer?.code ?? source.customerId} - {source.lineCount} line(s)
                  </option>
                );
              })}
            </Select>
          </div>
        )}
        <div>
          <label className="mb-1 block text-sm font-medium">Due date (optional)</label>
          <Input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
        </div>
      </div>

      {selectedSource ? (
        <div className="text-sm text-zinc-500">
          Customer: {customerById.get(selectedSource.customerId)?.code ?? selectedSource.customerId} - Lines will be copied into a draft invoice.
        </div>
      ) : null}

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Invoice"}
      </Button>
    </form>
  );
}
