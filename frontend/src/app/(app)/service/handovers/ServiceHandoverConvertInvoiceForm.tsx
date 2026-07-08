"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

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
  defaultUnitCost?: number;
};
type TaxRef = { id: string; code: string; name: string; ratePercent: number; isActive: boolean };

type ConvertResponse = { salesInvoiceId: string };
type InvoiceMode = "direct" | "estimate";
type ManualLineKind = "labor" | "item" | "sundries";
type ManualLineDraft = {
  key: string;
  kind: ManualLineKind;
  itemId: string;
  quantity: string;
  unitPrice: string;
  discountPercent: string;
  taxCodeId: string;
  taxPercent: string;
};

function newManualLine(kind: ManualLineKind = "item"): ManualLineDraft {
  return {
    key: crypto.randomUUID(),
    kind,
    itemId: "",
    quantity: "1",
    unitPrice: "0",
    discountPercent: "0",
    taxCodeId: "",
    taxPercent: "0",
  };
}

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
  items: ItemRef[];
  taxes: TaxRef[];
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
  const [manualLines, setManualLines] = useState<ManualLineDraft[]>([
    newManualLine("labor"),
    newManualLine("item"),
    newManualLine("sundries"),
  ]);
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
  const taxOptions = useMemo(
    () => taxes.filter((t) => t.isActive).slice().sort((a, b) => a.code.localeCompare(b.code)),
    [taxes],
  );

  function updateManualLine(key: string, patch: Partial<ManualLineDraft>) {
    setManualLines((current) =>
      current.map((line) => (line.key === key ? { ...line, ...patch } : line)),
    );
  }

  function parseManualLines() {
    return manualLines
      .filter((line) => line.itemId)
      .map((line) => {
        const quantity = Number(line.quantity);
        const unitPrice = Number(line.unitPrice);
        const discountPercent = Number(line.discountPercent);
        const taxPercent = Number(line.taxPercent);

        if (Number.isNaN(quantity) || quantity <= 0) {
          throw new Error("Manual invoice quantities must be positive.");
        }
        if (Number.isNaN(unitPrice) || unitPrice < 0) {
          throw new Error("Manual invoice unit prices must be 0 or greater.");
        }
        if (Number.isNaN(discountPercent) || discountPercent < 0) {
          throw new Error("Manual invoice discounts must be 0 or greater.");
        }
        if (Number.isNaN(taxPercent) || taxPercent < 0) {
          throw new Error("Manual invoice tax must be 0 or greater.");
        }

        return {
          itemId: line.itemId,
          quantity,
          unitPrice,
          discountPercent,
          taxPercent,
        };
      });
  }

  async function convert() {
    setError(null);
    setBusy(true);
    try {
      const manualInvoiceLines = invoiceMode === "direct" ? parseManualLines() : [];
      if (invoiceMode === "direct" && manualInvoiceLines.length === 0) {
        throw new Error("Add at least one direct invoice line.");
      }

      const result = await apiPost<ConvertResponse>(`service/handovers/${handoverId}/convert-to-sales-invoice`, {
        serviceEstimateId: invoiceMode === "estimate" ? serviceEstimateId || null : null,
        laborItemId: invoiceMode === "estimate" ? laborItemId || null : null,
        expenseItemId: invoiceMode === "estimate" ? expenseItemId || null : null,
        laborBillingSource: invoiceMode === "estimate" ? Number(laborBillingSource) : 1,
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
        manualLines: manualInvoiceLines,
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

  if (existingSalesInvoiceId) {
    return (
      <div className="text-sm text-zinc-500">
        Service invoice already created. Open it from the linked invoice section.
      </div>
    );
  }

  return (
    <div className="space-y-3 rounded-lg border border-zinc-200 p-3 dark:border-zinc-800">
      <div>
        <div className="text-sm font-medium">Create Sales Invoice Draft</div>
        <div className="mt-1 text-xs text-zinc-500">
          Direct job invoicing does not require a quotation. Use estimate billing only when the client approved a quotation.
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Invoice Source</label>
          <Select value={invoiceMode} onChange={(e) => setInvoiceMode(e.target.value as InvoiceMode)} disabled={disabled || busy}>
            <option value="direct">Direct job invoice - no quotation required</option>
            <option value="estimate">Use approved quotation / timesheets</option>
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

      {invoiceMode === "estimate" ? (
        <div className="grid gap-3 sm:grid-cols-4">
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
        </div>
      ) : (
        <div className="space-y-3">
          <div className="overflow-auto rounded-md border border-zinc-200 dark:border-zinc-800">
            <table className="w-full min-w-[900px] text-sm">
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="px-3 py-2">Line Type</th>
                  <th className="px-3 py-2">Item</th>
                  <th className="px-3 py-2">Qty</th>
                  <th className="px-3 py-2">Unit Price</th>
                  <th className="px-3 py-2">Discount %</th>
                  <th className="px-3 py-2">Tax</th>
                  <th className="px-3 py-2">Actions</th>
                </tr>
              </thead>
              <tbody>
                {manualLines.map((line) => (
                  <tr key={line.key} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                    <td className="px-3 py-2">
                      <Select value={line.kind} onChange={(e) => updateManualLine(line.key, { kind: e.target.value as ManualLineKind })} disabled={disabled || busy}>
                        <option value="labor">Labour / work done</option>
                        <option value="item">Additional item</option>
                        <option value="sundries">Sundries / grease / lubricants</option>
                      </Select>
                    </td>
                    <td className="px-3 py-2">
                      <Select
                        value={line.itemId}
                        onChange={(e) => {
                          const itemId = e.target.value;
                          const item = itemOptions.find((candidate) => candidate.id === itemId);
                          updateManualLine(line.key, {
                            itemId,
                            unitPrice: item && line.unitPrice === "0" ? String(item.defaultUnitCost ?? 0) : line.unitPrice,
                          });
                        }}
                        disabled={disabled || busy}
                      >
                        <option value="">Select item...</option>
                        {itemOptions.map((item) => (
                          <option key={item.id} value={item.id}>
                            {item.sku} - {item.name}
                          </option>
                        ))}
                      </Select>
                    </td>
                    <td className="px-3 py-2">
                      <Input value={line.quantity} onChange={(e) => updateManualLine(line.key, { quantity: e.target.value })} inputMode="decimal" disabled={disabled || busy} />
                    </td>
                    <td className="px-3 py-2">
                      <Input value={line.unitPrice} onChange={(e) => updateManualLine(line.key, { unitPrice: e.target.value })} inputMode="decimal" disabled={disabled || busy} />
                    </td>
                    <td className="px-3 py-2">
                      <Input value={line.discountPercent} onChange={(e) => updateManualLine(line.key, { discountPercent: e.target.value })} inputMode="decimal" disabled={disabled || busy} />
                    </td>
                    <td className="px-3 py-2">
                      <div className="grid gap-2">
                        <Select
                          value={line.taxCodeId}
                          onChange={(e) => {
                            const taxCodeId = e.target.value;
                            const tax = taxOptions.find((candidate) => candidate.id === taxCodeId);
                            updateManualLine(line.key, {
                              taxCodeId,
                              taxPercent: tax ? String(tax.ratePercent) : line.taxPercent,
                            });
                          }}
                          disabled={disabled || busy}
                        >
                          <option value="">No tax code</option>
                          {taxOptions.map((tax) => (
                            <option key={tax.id} value={tax.id}>
                              {tax.code} - {tax.name}
                            </option>
                          ))}
                        </Select>
                        <Input value={line.taxPercent} onChange={(e) => updateManualLine(line.key, { taxPercent: e.target.value })} inputMode="decimal" disabled={disabled || busy} />
                      </div>
                    </td>
                    <td className="px-3 py-2">
                      <SecondaryButton
                        type="button"
                        className="px-2 py-1 text-xs"
                        onClick={() => setManualLines((current) => current.filter((candidate) => candidate.key !== line.key))}
                        disabled={disabled || busy || manualLines.length === 1}
                      >
                        Remove
                      </SecondaryButton>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button type="button" className="px-3 py-1.5 text-xs" onClick={() => setManualLines((current) => [...current, newManualLine("labor")])} disabled={disabled || busy}>
              Add labour
            </Button>
            <Button type="button" className="px-3 py-1.5 text-xs" onClick={() => setManualLines((current) => [...current, newManualLine("item")])} disabled={disabled || busy}>
              Add item
            </Button>
            <Button type="button" className="px-3 py-1.5 text-xs" onClick={() => setManualLines((current) => [...current, newManualLine("sundries")])} disabled={disabled || busy}>
              Add sundries
            </Button>
          </div>
        </div>
      )}

      <div className="flex flex-wrap items-center gap-2">
        <SecondaryButton type="button" disabled={disabled || busy} onClick={convert}>
          {busy ? "Converting..." : "Create Sales Invoice Draft"}
        </SecondaryButton>
        <div className="text-xs text-zinc-500">
          Direct job invoices do not require an approved quotation. Use a service item for labour and a sundries item/category for grease, lubricants, and consumables.
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
