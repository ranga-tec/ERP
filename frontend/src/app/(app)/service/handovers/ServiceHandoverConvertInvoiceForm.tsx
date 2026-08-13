"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Input, SecondaryButton, Select } from "@/components/ui";
import {
  ServiceJobBillingBuilder,
  type BillingItemRef,
  type BillingTaxRef,
} from "./ServiceJobBillingBuilder";

type EstimateRef = {
  id: string;
  number: string;
  status: number;
  issuedAt: string;
  total: number;
};

type ConvertResponse = { salesInvoiceId: string };
type InvoiceMode = "direct" | "estimate";

export function ServiceHandoverConvertInvoiceForm({
  handoverId,
  estimates,
  items,
  taxes,
  disabled,
  existingSalesInvoiceId,
  redirectToSalesInvoice = true,
}: {
  handoverId: string;
  estimates: EstimateRef[];
  items: BillingItemRef[];
  taxes: BillingTaxRef[];
  disabled: boolean;
  existingSalesInvoiceId?: string | null;
  redirectToSalesInvoice?: boolean;
}) {
  const router = useRouter();
  const [invoiceMode, setInvoiceMode] = useState<InvoiceMode>("direct");
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
  const serviceItemOptions = useMemo(
    () => itemOptions.filter((item) => item.type === 3 && item.isActive !== false),
    [itemOptions],
  );

  async function convertFromEstimate() {
    setError(null);
    setBusy(true);
    try {
      const result = await apiPost<ConvertResponse>(`service/handovers/${handoverId}/convert-to-sales-invoice`, {
        serviceEstimateId: serviceEstimateId || null,
        laborItemId: laborItemId || null,
        expenseItemId: expenseItemId || null,
        laborBillingSource: Number(laborBillingSource),
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
        manualLines: [],
      });
      if (redirectToSalesInvoice) {
        router.push(`/sales/invoices/${result.salesInvoiceId}`);
        return;
      }
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-3 rounded-lg border border-zinc-200 p-3 dark:border-zinc-800">
      {existingSalesInvoiceId ? (
        <div className="rounded-md border border-blue-200 bg-blue-50 p-2 text-sm text-blue-900 dark:border-blue-900/40 dark:bg-blue-950/40 dark:text-blue-100">
          A draft invoice already exists. Any newly approved billable labour or petty-cash expenses selected below will be added to that draft without duplicating existing lines.
        </div>
      ) : null}
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Invoice Source</label>
          <Select value={invoiceMode} onChange={(e) => setInvoiceMode(e.target.value as InvoiceMode)} disabled={disabled || busy}>
            <option value="direct">Bill what the job actually used - no quotation needed</option>
            <option value="estimate">Bill an approved quotation</option>
          </Select>
        </div>
      </div>

      {invoiceMode === "direct" ? (
        <ServiceJobBillingBuilder
          handoverId={handoverId}
          items={items}
          taxes={taxes}
          disabled={disabled}
          redirectToSalesInvoice={redirectToSalesInvoice}
        />
      ) : (
        <div className="space-y-3">
          <div className="text-xs text-zinc-500">
            The invoice copies the approved quotation line for line. Use this only when the customer
            signed off a quote and expects to be charged that amount rather than what the job used.
          </div>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <div>
              <label className="mb-1 block text-sm font-medium">Approved Estimate</label>
              <Select value={serviceEstimateId} onChange={(e) => setServiceEstimateId(e.target.value)} disabled={disabled || busy}>
                <option value="">Latest approved quotation (auto)</option>
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
              <label className="mb-1 block text-sm font-medium">Labor Item Override (optional)</label>
              <Select value={laborItemId} onChange={(e) => setLaborItemId(e.target.value)} disabled={disabled || busy}>
                <option value="">Automatic service item</option>
                {serviceItemOptions.map((i) => (
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
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Invoice Due Date (optional)</label>
            <Input
              className="max-w-xs"
              type="datetime-local"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              disabled={disabled || busy}
            />
          </div>

          <SecondaryButton type="button" disabled={disabled || busy} onClick={convertFromEstimate}>
            {busy ? "Converting..." : "Create Sales Invoice Draft"}
          </SecondaryButton>

          {error ? (
            <div className="rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
              {error}
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
}
