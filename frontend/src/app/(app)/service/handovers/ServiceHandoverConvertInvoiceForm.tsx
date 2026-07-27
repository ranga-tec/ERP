"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiGet, apiPost } from "@/lib/api-client";
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
  /** set when the line was pulled from an issued material, so it is not billed twice */
  materialRequisitionLineId?: string;
  sourceCost?: number;
};

type BillableMaterial = {
  materialRequisitionLineId: string;
  materialRequisitionNumber: string;
  itemId: string;
  itemSku: string;
  itemName: string;
  issuedQuantity: number;
  returnedQuantity: number;
  netQuantity: number;
  alreadyInvoicedQuantity: number;
  remainingQuantity: number;
  unitCost: number;
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

function money(value: number): string {
  return value.toFixed(2);
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

  const [materials, setMaterials] = useState<BillableMaterial[]>([]);
  const [materialsOpen, setMaterialsOpen] = useState(false);
  const [materialSearch, setMaterialSearch] = useState("");

  useEffect(() => {
    let ignore = false;
    apiGet<BillableMaterial[]>(`service/handovers/${handoverId}/billable-materials`)
      .then((rows) => {
        if (!ignore) setMaterials(rows);
      })
      .catch(() => {
        if (!ignore) setMaterials([]);
      });
    return () => {
      ignore = true;
    };
  }, [handoverId]);

  const pulledMaterialLineIds = useMemo(
    () => new Set(manualLines.map((line) => line.materialRequisitionLineId).filter(Boolean) as string[]),
    [manualLines],
  );

  const visibleMaterials = useMemo(() => {
    const q = materialSearch.trim().toLowerCase();
    if (!q) return materials;
    return materials.filter((m) =>
      `${m.itemSku} ${m.itemName} ${m.materialRequisitionNumber}`.toLowerCase().includes(q));
  }, [materials, materialSearch]);

  /** Push an issued material into the invoice line grid, where the selling price is set. */
  function addMaterialLine(material: BillableMaterial) {
    setManualLines((current) => [
      ...current,
      {
        ...newManualLine("item"),
        itemId: material.itemId,
        quantity: String(material.remainingQuantity > 0 ? material.remainingQuantity : material.netQuantity),
        unitPrice: "0",
        materialRequisitionLineId: material.materialRequisitionLineId,
        sourceCost: material.unitCost,
      },
    ]);
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
          materialRequisitionLineId: line.materialRequisitionLineId ?? null,
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
          {materials.length > 0 ? (
            <div className="rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)]">
              <button
                type="button"
                className="flex w-full items-center justify-between gap-3 px-3 py-2 text-left"
                onClick={() => setMaterialsOpen((open) => !open)}
                aria-expanded={materialsOpen}
              >
                <span className="text-sm font-semibold">
                  <span aria-hidden="true" className="mr-2 text-zinc-500">{materialsOpen ? "-" : "+"}</span>
                  Issued materials ({materials.length})
                </span>
                <span className="text-xs text-zinc-500">
                  Pull what was used on the job instead of looking it up on the MRN
                </span>
              </button>

              {materialsOpen ? (
                <div className="border-t border-[var(--card-border)] p-3">
                  <Input
                    className="mb-3 max-w-sm"
                    placeholder="Search item, SKU or MRN..."
                    value={materialSearch}
                    onChange={(e) => setMaterialSearch(e.target.value)}
                  />
                  <div className="overflow-auto rounded-md border border-zinc-200 dark:border-zinc-800">
                    <table className="w-full min-w-[820px] text-sm">
                      <thead>
                        <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                          <th className="px-3 py-2">Item</th>
                          <th className="px-3 py-2">MRN</th>
                          <th className="px-3 py-2 text-right">Issued</th>
                          <th className="px-3 py-2 text-right">Returned</th>
                          <th className="px-3 py-2 text-right">Invoiced</th>
                          <th className="px-3 py-2 text-right">Left to bill</th>
                          <th className="px-3 py-2 text-right">Unit cost</th>
                          <th className="px-3 py-2"></th>
                        </tr>
                      </thead>
                      <tbody>
                        {visibleMaterials.map((m) => {
                          const pulled = pulledMaterialLineIds.has(m.materialRequisitionLineId);
                          const fullyBilled = m.remainingQuantity <= 0;
                          return (
                            <tr key={m.materialRequisitionLineId} className="border-b border-zinc-100 dark:border-zinc-900">
                              <td className="px-3 py-2">
                                <div className="font-mono text-xs">{m.itemSku}</div>
                                <div className="text-xs text-zinc-500">{m.itemName}</div>
                              </td>
                              <td className="px-3 py-2 font-mono text-xs text-zinc-500">{m.materialRequisitionNumber}</td>
                              <td className="px-3 py-2 text-right">{m.issuedQuantity}</td>
                              <td className="px-3 py-2 text-right text-zinc-500">{m.returnedQuantity}</td>
                              <td className="px-3 py-2 text-right text-zinc-500">{m.alreadyInvoicedQuantity}</td>
                              <td className="px-3 py-2 text-right font-medium">{m.remainingQuantity}</td>
                              <td className="px-3 py-2 text-right text-zinc-500">{money(m.unitCost)}</td>
                              <td className="px-3 py-2 text-right">
                                {pulled ? (
                                  <span className="text-xs text-emerald-700 dark:text-emerald-300">Added</span>
                                ) : fullyBilled ? (
                                  <span className="text-xs text-zinc-400">Fully invoiced</span>
                                ) : (
                                  <Button
                                    type="button"
                                    className="px-2 py-1 text-xs"
                                    disabled={disabled || busy}
                                    onClick={() => addMaterialLine(m)}
                                  >
                                    + Add
                                  </Button>
                                )}
                              </td>
                            </tr>
                          );
                        })}
                        {visibleMaterials.length === 0 ? (
                          <tr>
                            <td className="px-3 py-4 text-sm text-zinc-500" colSpan={8}>No matching issued materials.</td>
                          </tr>
                        ) : null}
                      </tbody>
                    </table>
                  </div>
                  <div className="mt-2 text-xs text-zinc-500">
                    Adding a row drops it into the invoice lines below, where you set the selling price.
                    Cost is shown for reference only and is never used as the price.
                  </div>
                </div>
              ) : null}
            </div>
          ) : null}

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
                      {line.sourceCost !== undefined ? (
                        <div className="mt-1 whitespace-nowrap text-[11px] text-zinc-500">
                          cost {money(line.sourceCost)}
                          {Number(line.unitPrice) > 0 ? (
                            <span className={Number(line.unitPrice) >= line.sourceCost ? " text-emerald-700 dark:text-emerald-300" : " text-red-700 dark:text-red-300"}>
                              {Number(line.unitPrice) >= line.sourceCost ? " · margin " : " · below cost "}
                              {money(Number(line.unitPrice) - line.sourceCost)}
                            </span>
                          ) : null}
                        </div>
                      ) : null}
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
