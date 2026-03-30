"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Input, SecondaryButton, Select } from "@/components/ui";

type EstimateRef = {
  id: string;
  number: string;
  status: number;
  issuedAt: string;
  total: number;
};

type ItemRef = {
  id: string;
  sku: string;
  name: string;
};

type ConvertResponse = { salesInvoiceId: string };

export function ServiceHandoverConvertInvoiceForm({
  handoverId,
  estimates,
  items,
  disabled,
  existingSalesInvoiceId,
}: {
  handoverId: string;
  estimates: EstimateRef[];
  items: ItemRef[];
  disabled: boolean;
  existingSalesInvoiceId?: string | null;
}) {
  const router = useRouter();
  const [serviceEstimateId, setServiceEstimateId] = useState("");
  const [laborItemId, setLaborItemId] = useState("");
  const [expenseItemId, setExpenseItemId] = useState("");
  const [laborBillingSource, setLaborBillingSource] = useState("0");
  const [dueDate, setDueDate] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const approvedEstimates = useMemo(
    () =>
      estimates
        .filter((e) => e.status === 1)
        .slice()
        .sort((a, b) => b.issuedAt.localeCompare(a.issuedAt)),
    [estimates],
  );

  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );

  async function convert() {
    setError(null);
    setBusy(true);
    try {
      const result = await apiPost<ConvertResponse>(`service/handovers/${handoverId}/convert-to-sales-invoice`, {
        serviceEstimateId: serviceEstimateId || null,
        laborItemId: laborItemId || null,
        expenseItemId: expenseItemId || null,
        laborBillingSource: Number(laborBillingSource),
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
      });
      router.push(`/sales/invoices/${result.salesInvoiceId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  if (existingSalesInvoiceId) {
    return (
      <div className="text-sm text-zinc-500">
        Service invoice already created. Open it from the linked invoice section.
      </div>
    );
  }

  return (
    <div className="space-y-3 rounded-lg border border-zinc-200 p-3 dark:border-zinc-800">
      <div className="text-sm font-medium">Convert to Sales Invoice</div>

      <div className="grid gap-3 sm:grid-cols-5">
        <div>
          <label className="mb-1 block text-sm font-medium">Approved Estimate</label>
          <Select value={serviceEstimateId} onChange={(e) => setServiceEstimateId(e.target.value)} disabled={disabled || busy}>
            <option value="">Latest approved (auto)</option>
            {approvedEstimates.map((e) => (
              <option key={e.id} value={e.id}>
                {e.number} ({e.total.toFixed(2)})
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Labor Billing Source</label>
          <Select value={laborBillingSource} onChange={(e) => setLaborBillingSource(e.target.value)} disabled={disabled || busy}>
            <option value="0">Auto: approved timesheets first</option>
            <option value="1">Use estimate labor lines</option>
            <option value="2">Use approved timesheets only</option>
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Labor Item (if labor lines exist)</label>
          <Select value={laborItemId} onChange={(e) => setLaborItemId(e.target.value)} disabled={disabled || busy}>
            <option value="">Select labor/service item (optional)</option>
            {itemOptions.map((i) => (
              <option key={i.id} value={i.id}>
                {i.sku} - {i.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Expense Item (if needed)</label>
          <Select value={expenseItemId} onChange={(e) => setExpenseItemId(e.target.value)} disabled={disabled || busy}>
            <option value="">Use estimate item or select fallback</option>
            {itemOptions.map((i) => (
              <option key={i.id} value={i.id}>
                {i.sku} - {i.name}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Invoice Due Date (optional)</label>
          <Input
            type="datetime-local"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
            disabled={disabled || busy}
          />
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <SecondaryButton type="button" disabled={disabled || busy} onClick={convert}>
          {busy ? "Converting..." : "Create Sales Invoice Draft"}
        </SecondaryButton>
        <div className="text-xs text-zinc-500">
          Labor billing can use estimate labor or approved timesheets. Warranty or contract coverage can reduce covered part or labor lines to zero. Expense lines without an item still need an expense or misc service item.
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}
    </div>
  );
}
