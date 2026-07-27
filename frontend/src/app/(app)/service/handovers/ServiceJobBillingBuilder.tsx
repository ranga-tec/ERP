"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiGet, apiPost } from "@/lib/api-client";
import { Button, Input, SecondaryButton, Select } from "@/components/ui";

export type BillingItemRef = {
  id: string;
  sku: string;
  name: string;
  defaultUnitCost?: number;
  defaultUnitPrice?: number;
  unitOfMeasure: string;
};

export type BillingTaxRef = { id: string; code: string; name: string; ratePercent: number; isActive: boolean };

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

type BillableLabour = {
  timeEntryId: string;
  workDate: string;
  technicianName: string;
  workDescription: string;
  hoursWorked: number;
  costRate: number;
  labourCost: number;
  billableHours: number;
  billingRate: number;
  taxPercent: number;
  suggestedTotal: number;
};

type BillableExpense = {
  expenseClaimLineId: string;
  expenseClaimNumber: string;
  expenseDate: string;
  description: string;
  quantity: number;
  unitCost: number;
  lineTotal: number;
};

type BillableCharges = {
  serviceJobId: string;
  jobNumber: string;
  entitlementCoverage: number;
  entitlementSummary?: string | null;
  partsCoveredByEntitlement: boolean;
  labourCoveredByEntitlement: boolean;
  materials: BillableMaterial[];
  labour: BillableLabour[];
  expenses: BillableExpense[];
};

/** Skip = 0, Itemised = 1, RolledUp = 2 — mirrors ServiceChargeBillingMode on the server. */
type BillingMode = 0 | 1 | 2;

type ChargeRow = {
  selected: boolean;
  quantity: string;
  unitPrice: string;
  discountPercent: string;
  taxPercent: string;
};

type OtherLine = {
  key: string;
  itemId: string;
  description: string;
  quantity: string;
  unitPrice: string;
  discountPercent: string;
  taxPercent: string;
};

type BuildResponse = { salesInvoiceId: string };

function money(value: number): string {
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function num(value: string): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function rowTotal(row: ChargeRow): number {
  return num(row.quantity) * num(row.unitPrice) * (1 - num(row.discountPercent) / 100);
}

function rowTax(row: ChargeRow): number {
  return rowTotal(row) * (num(row.taxPercent) / 100);
}

function newOtherLine(): OtherLine {
  return {
    key: crypto.randomUUID(),
    itemId: "",
    description: "",
    quantity: "1",
    unitPrice: "0",
    discountPercent: "0",
    taxPercent: "0",
  };
}

/**
 * The one place a service job turns into a customer invoice. Every charge the job accumulated -
 * parts issued, labour approved, expenses recharged - is listed with its cost and a suggested
 * price, and nothing reaches the invoice unless it is ticked here.
 */
export function ServiceJobBillingBuilder({
  handoverId,
  items,
  taxes,
  disabled,
  redirectToSalesInvoice = true,
}: {
  handoverId: string;
  items: BillingItemRef[];
  taxes: BillingTaxRef[];
  disabled: boolean;
  redirectToSalesInvoice?: boolean;
}) {
  const router = useRouter();
  const [charges, setCharges] = useState<BillableCharges | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [materialRows, setMaterialRows] = useState<Record<string, ChargeRow>>({});
  const [labourRows, setLabourRows] = useState<Record<string, ChargeRow>>({});
  const [expenseRows, setExpenseRows] = useState<Record<string, ChargeRow>>({});

  const [labourMode, setLabourMode] = useState<BillingMode>(2);
  const [expenseMode, setExpenseMode] = useState<BillingMode>(2);
  const [labourItemId, setLabourItemId] = useState("");
  const [expenseItemId, setExpenseItemId] = useState("");

  const [otherLines, setOtherLines] = useState<OtherLine[]>([]);
  const [discountKind, setDiscountKind] = useState<"percent" | "amount">("percent");
  const [discountValue, setDiscountValue] = useState("0");
  const [dueDate, setDueDate] = useState("");

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const itemOptions = useMemo(
    () => items.slice().sort((a, b) => a.sku.localeCompare(b.sku)),
    [items],
  );
  const taxOptions = useMemo(
    () => taxes.filter((t) => t.isActive).slice().sort((a, b) => a.code.localeCompare(b.code)),
    [taxes],
  );

  useEffect(() => {
    let ignore = false;
    apiGet<BillableCharges>(`service/handovers/${handoverId}/billable-charges`)
      .then((data) => {
        if (ignore) return;
        setCharges(data);
        setMaterialRows(
          Object.fromEntries(
            data.materials
              .filter((m) => m.remainingQuantity > 0)
              .map((m) => [
                m.materialRequisitionLineId,
                {
                  selected: true,
                  quantity: String(m.remainingQuantity),
                  // the item's list price, never the cost — cost is shown alongside for margin
                  unitPrice: String(items.find((i) => i.id === m.itemId)?.defaultUnitPrice ?? 0),
                  discountPercent: "0",
                  taxPercent: "0",
                } satisfies ChargeRow,
              ]),
          ),
        );
        setLabourRows(
          Object.fromEntries(
            data.labour.map((l) => [
              l.timeEntryId,
              {
                selected: true,
                quantity: String(l.billableHours),
                unitPrice: String(l.billingRate),
                discountPercent: "0",
                taxPercent: String(l.taxPercent),
              } satisfies ChargeRow,
            ]),
          ),
        );
        setExpenseRows(
          Object.fromEntries(
            data.expenses.map((e) => [
              e.expenseClaimLineId,
              {
                selected: true,
                quantity: String(e.quantity),
                unitPrice: String(e.unitCost),
                discountPercent: "0",
                taxPercent: "0",
              } satisfies ChargeRow,
            ]),
          ),
        );
      })
      .catch((err) => {
        if (!ignore) setLoadError(err instanceof Error ? err.message : String(err));
      });
    return () => {
      ignore = true;
    };
  }, [handoverId, items]);

  const selectedMaterials = useMemo(
    () => (charges?.materials ?? []).filter((m) => materialRows[m.materialRequisitionLineId]?.selected),
    [charges, materialRows],
  );
  const selectedLabour = useMemo(
    () => (charges?.labour ?? []).filter((l) => labourRows[l.timeEntryId]?.selected),
    [charges, labourRows],
  );
  const selectedExpenses = useMemo(
    () => (charges?.expenses ?? []).filter((e) => expenseRows[e.expenseClaimLineId]?.selected),
    [charges, expenseRows],
  );

  const totals = useMemo(() => {
    let gross = 0;
    let tax = 0;
    let cost = 0;

    for (const m of selectedMaterials) {
      const row = materialRows[m.materialRequisitionLineId];
      gross += rowTotal(row);
      tax += rowTax(row);
      cost += num(row.quantity) * m.unitCost;
    }
    if (labourMode !== 0) {
      for (const l of selectedLabour) {
        const row = labourRows[l.timeEntryId];
        gross += rowTotal(row);
        tax += rowTax(row);
        cost += l.labourCost;
      }
    }
    if (expenseMode !== 0) {
      for (const e of selectedExpenses) {
        const row = expenseRows[e.expenseClaimLineId];
        gross += rowTotal(row);
        tax += rowTax(row);
        cost += e.lineTotal;
      }
    }
    for (const line of otherLines) {
      if (!line.itemId) continue;
      const row: ChargeRow = { selected: true, ...line };
      gross += rowTotal(row);
      tax += rowTax(row);
    }

    const discount =
      discountKind === "percent"
        ? Math.round(gross * (num(discountValue) / 100) * 100) / 100
        : Math.min(num(discountValue), gross);
    // tax follows the discounted amount, matching the server's proration
    const taxAfterDiscount = gross > 0 ? tax * ((gross - discount) / gross) : 0;

    return {
      gross,
      discount,
      net: gross - discount,
      tax: taxAfterDiscount,
      total: gross - discount + taxAfterDiscount,
      cost,
      margin: gross - discount - cost,
    };
  }, [
    selectedMaterials,
    materialRows,
    selectedLabour,
    labourRows,
    labourMode,
    selectedExpenses,
    expenseRows,
    expenseMode,
    otherLines,
    discountKind,
    discountValue,
  ]);

  function patchRow(
    setter: React.Dispatch<React.SetStateAction<Record<string, ChargeRow>>>,
    key: string,
    patch: Partial<ChargeRow>,
  ) {
    setter((current) => ({ ...current, [key]: { ...current[key], ...patch } }));
  }

  function setAllSelected(
    setter: React.Dispatch<React.SetStateAction<Record<string, ChargeRow>>>,
    selected: boolean,
  ) {
    setter((current) =>
      Object.fromEntries(Object.entries(current).map(([key, row]) => [key, { ...row, selected }])),
    );
  }

  async function build() {
    setError(null);
    setBusy(true);
    try {
      const labourCharges =
        labourMode === 0
          ? []
          : selectedLabour.map((l) => {
              const row = labourRows[l.timeEntryId];
              return {
                timeEntryId: l.timeEntryId,
                quantity: num(row.quantity),
                unitPrice: num(row.unitPrice),
                discountPercent: num(row.discountPercent),
                taxPercent: num(row.taxPercent),
              };
            });

      const expenseCharges =
        expenseMode === 0
          ? []
          : selectedExpenses.map((e) => {
              const row = expenseRows[e.expenseClaimLineId];
              return {
                expenseClaimLineId: e.expenseClaimLineId,
                quantity: num(row.quantity),
                unitPrice: num(row.unitPrice),
                discountPercent: num(row.discountPercent),
                taxPercent: num(row.taxPercent),
              };
            });

      if (labourCharges.some((c) => c.quantity <= 0)) {
        throw new Error("Labour quantities must be greater than zero. Untick a line instead of setting it to zero.");
      }
      if (expenseCharges.some((c) => c.quantity <= 0)) {
        throw new Error("Expense quantities must be greater than zero. Untick a line instead of setting it to zero.");
      }
      if (labourCharges.length > 0 && !labourItemId) {
        throw new Error("Choose the service item that labour is billed against.");
      }
      if (expenseCharges.length > 0 && !expenseItemId) {
        throw new Error("Choose the item that recharged expenses are billed against.");
      }

      const materialLines = selectedMaterials.map((m) => {
        const row = materialRows[m.materialRequisitionLineId];
        if (num(row.quantity) <= 0) {
          throw new Error(`Quantity for ${m.itemSku} must be greater than zero. Untick it instead.`);
        }
        return {
          itemId: m.itemId,
          quantity: num(row.quantity),
          unitPrice: num(row.unitPrice),
          discountPercent: num(row.discountPercent),
          taxPercent: num(row.taxPercent),
          materialRequisitionLineId: m.materialRequisitionLineId,
          description: null,
        };
      });

      const extras = otherLines
        .filter((line) => line.itemId)
        .map((line) => {
          if (num(line.quantity) <= 0) {
            throw new Error("Additional charge quantities must be greater than zero.");
          }
          return {
            itemId: line.itemId,
            quantity: num(line.quantity),
            unitPrice: num(line.unitPrice),
            discountPercent: num(line.discountPercent),
            taxPercent: num(line.taxPercent),
            materialRequisitionLineId: null,
            description: line.description.trim() || null,
          };
        });

      if (materialLines.length === 0 && labourCharges.length === 0 && expenseCharges.length === 0 && extras.length === 0) {
        throw new Error("Select at least one charge to bill.");
      }

      const result = await apiPost<BuildResponse>(`service/handovers/${handoverId}/build-invoice`, {
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
        headerDiscountPercent: discountKind === "percent" ? num(discountValue) : 0,
        headerDiscountAmount: discountKind === "amount" ? num(discountValue) : 0,
        materialLines,
        labourMode: labourCharges.length === 0 ? 0 : labourMode,
        labourItemId: labourItemId || null,
        labourCharges,
        expenseMode: expenseCharges.length === 0 ? 0 : expenseMode,
        expenseItemId: expenseItemId || null,
        expenseCharges,
        otherLines: extras,
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

  if (loadError) {
    return (
      <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
        Could not load the job&rsquo;s charges: {loadError}
      </div>
    );
  }

  if (!charges) {
    return <div className="text-sm text-zinc-500">Loading the job&rsquo;s charges...</div>;
  }

  const nothingToBill =
    charges.materials.length === 0 && charges.labour.length === 0 && charges.expenses.length === 0;

  return (
    <div className="space-y-4">
      <div>
        <div className="text-sm font-medium">Build the customer invoice</div>
        <div className="mt-1 text-xs text-zinc-500">
          Everything {charges.jobNumber} has charged so far is listed below with its cost. Tick what
          the customer pays for and set the selling price. Nothing here is billed twice &mdash; anything
          already invoiced is left out.
        </div>
      </div>

      {charges.partsCoveredByEntitlement || charges.labourCoveredByEntitlement ? (
        <div className="rounded-md border border-amber-300 bg-amber-50 p-3 text-xs text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/40 dark:text-amber-100">
          <div className="font-semibold">Under warranty or contract cover</div>
          <div className="mt-1">
            {charges.entitlementSummary ?? "This job is covered."}{" "}
            {charges.partsCoveredByEntitlement ? "Parts" : ""}
            {charges.partsCoveredByEntitlement && charges.labourCoveredByEntitlement ? " and " : ""}
            {charges.labourCoveredByEntitlement ? "Labour" : ""} will be forced to 0.00 on the
            invoice regardless of the prices set here.
          </div>
        </div>
      ) : null}

      {nothingToBill ? (
        <div className="rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)] p-3 text-sm text-zinc-500">
          This job has no uninvoiced charges. Add an additional charge below if you still need to
          raise an invoice.
        </div>
      ) : null}

      <ChargeSection
        title="Parts issued"
        count={charges.materials.length}
        selectedCount={selectedMaterials.length}
        onSelectAll={(v) => setAllSelected(setMaterialRows, v)}
      >
        <table className="w-full min-w-[880px] text-sm">
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="px-2 py-2 w-8"></th>
              <th className="px-2 py-2">Item</th>
              <th className="px-2 py-2">MRN</th>
              <th className="px-2 py-2 text-right">Left to bill</th>
              <th className="px-2 py-2 text-right">Unit cost</th>
              <th className="px-2 py-2">Qty</th>
              <th className="px-2 py-2">Unit price</th>
              <th className="px-2 py-2">Disc %</th>
              <th className="px-2 py-2">Tax %</th>
              <th className="px-2 py-2 text-right">Total</th>
            </tr>
          </thead>
          <tbody>
            {charges.materials.map((m) => {
              const row = materialRows[m.materialRequisitionLineId];
              if (!row) {
                return (
                  <tr key={m.materialRequisitionLineId} className="border-b border-zinc-100 text-zinc-400 dark:border-zinc-900">
                    <td className="px-2 py-2"></td>
                    <td className="px-2 py-2">
                      <div className="font-mono text-xs">{m.itemSku}</div>
                      <div className="text-xs">{m.itemName}</div>
                    </td>
                    <td className="px-2 py-2 font-mono text-xs">{m.materialRequisitionNumber}</td>
                    <td className="px-2 py-2 text-right" colSpan={7}>
                      Fully invoiced
                    </td>
                  </tr>
                );
              }
              const price = num(row.unitPrice);
              return (
                <tr key={m.materialRequisitionLineId} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="px-2 py-2">
                    <input
                      type="checkbox"
                      checked={row.selected}
                      disabled={disabled || busy}
                      onChange={(e) => patchRow(setMaterialRows, m.materialRequisitionLineId, { selected: e.target.checked })}
                    />
                  </td>
                  <td className="px-2 py-2">
                    <div className="font-mono text-xs">{m.itemSku}</div>
                    <div className="text-xs text-zinc-500">{m.itemName}</div>
                  </td>
                  <td className="px-2 py-2 font-mono text-xs text-zinc-500">{m.materialRequisitionNumber}</td>
                  <td className="px-2 py-2 text-right">{m.remainingQuantity} {items.find((item) => item.id === m.itemId)?.unitOfMeasure ?? ""}</td>
                  <td className="px-2 py-2 text-right text-zinc-500">{money(m.unitCost)}</td>
                  <td className="px-2 py-2"><NumCell value={row.quantity} onChange={(v) => patchRow(setMaterialRows, m.materialRequisitionLineId, { quantity: v })} disabled={disabled || busy} /><div className="mt-1 text-[11px] text-zinc-500">{items.find((item) => item.id === m.itemId)?.unitOfMeasure ?? ""}</div></td>
                  <td className="px-2 py-2">
                    <NumCell value={row.unitPrice} onChange={(v) => patchRow(setMaterialRows, m.materialRequisitionLineId, { unitPrice: v })} disabled={disabled || busy} />
                    {price > 0 ? (
                      <div className={price >= m.unitCost ? "mt-1 text-[11px] text-emerald-700 dark:text-emerald-300" : "mt-1 text-[11px] text-red-700 dark:text-red-300"}>
                        {price >= m.unitCost ? "margin " : "below cost "}
                        {money(price - m.unitCost)}
                      </div>
                    ) : (
                      <div className="mt-1 text-[11px] text-amber-700 dark:text-amber-300">no price set</div>
                    )}
                  </td>
                  <td className="px-2 py-2"><NumCell value={row.discountPercent} onChange={(v) => patchRow(setMaterialRows, m.materialRequisitionLineId, { discountPercent: v })} disabled={disabled || busy} /></td>
                  <td className="px-2 py-2"><NumCell value={row.taxPercent} onChange={(v) => patchRow(setMaterialRows, m.materialRequisitionLineId, { taxPercent: v })} disabled={disabled || busy} /></td>
                  <td className="px-2 py-2 text-right font-medium">{money(rowTotal(row))}</td>
                </tr>
              );
            })}
            {charges.materials.length === 0 ? (
              <tr><td className="px-2 py-4 text-sm text-zinc-500" colSpan={10}>No materials were issued to this job.</td></tr>
            ) : null}
          </tbody>
        </table>
      </ChargeSection>

      <ChargeSection
        title="Labour"
        count={charges.labour.length}
        selectedCount={selectedLabour.length}
        onSelectAll={(v) => setAllSelected(setLabourRows, v)}
        controls={
          <ModeControls
            mode={labourMode}
            onMode={setLabourMode}
            itemId={labourItemId}
            onItemId={setLabourItemId}
            itemOptions={itemOptions}
            itemLabel="Bill labour against"
            disabled={disabled || busy}
          />
        }
      >
        <table className="w-full min-w-[880px] text-sm">
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="px-2 py-2 w-8"></th>
              <th className="px-2 py-2">Work</th>
              <th className="px-2 py-2 text-right">Hrs worked</th>
              <th className="px-2 py-2 text-right">Labour cost</th>
              <th className="px-2 py-2">Bill hrs</th>
              <th className="px-2 py-2">Rate</th>
              <th className="px-2 py-2">Disc %</th>
              <th className="px-2 py-2">Tax %</th>
              <th className="px-2 py-2 text-right">Total</th>
            </tr>
          </thead>
          <tbody>
            {charges.labour.map((l) => {
              const row = labourRows[l.timeEntryId];
              if (!row) return null;
              const hoursMismatch = num(row.quantity) < l.hoursWorked;
              return (
                <tr key={l.timeEntryId} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="px-2 py-2">
                    <input
                      type="checkbox"
                      checked={row.selected}
                      disabled={disabled || busy || labourMode === 0}
                      onChange={(e) => patchRow(setLabourRows, l.timeEntryId, { selected: e.target.checked })}
                    />
                  </td>
                  <td className="px-2 py-2">
                    <div>{l.workDescription}</div>
                    <div className="text-xs text-zinc-500">
                      {l.technicianName} &middot; {new Date(l.workDate).toLocaleDateString()}
                    </div>
                  </td>
                    <td className="px-2 py-2 text-right">{l.hoursWorked.toFixed(2)} hrs</td>
                  <td className="px-2 py-2 text-right text-zinc-500">{money(l.labourCost)}</td>
                  <td className="px-2 py-2">
                    <NumCell value={row.quantity} onChange={(v) => patchRow(setLabourRows, l.timeEntryId, { quantity: v })} disabled={disabled || busy || labourMode === 0} />
                    {hoursMismatch ? (
                      <button
                        type="button"
                        className="mt-1 text-[11px] text-amber-700 underline dark:text-amber-300"
                        disabled={disabled || busy || labourMode === 0}
                        onClick={() => patchRow(setLabourRows, l.timeEntryId, { quantity: String(l.hoursWorked) })}
                      >
                        bill all {l.hoursWorked.toFixed(2)} hrs
                      </button>
                    ) : null}
                  </td>
                  <td className="px-2 py-2"><NumCell value={row.unitPrice} onChange={(v) => patchRow(setLabourRows, l.timeEntryId, { unitPrice: v })} disabled={disabled || busy || labourMode === 0} /></td>
                  <td className="px-2 py-2"><NumCell value={row.discountPercent} onChange={(v) => patchRow(setLabourRows, l.timeEntryId, { discountPercent: v })} disabled={disabled || busy || labourMode === 0} /></td>
                  <td className="px-2 py-2"><NumCell value={row.taxPercent} onChange={(v) => patchRow(setLabourRows, l.timeEntryId, { taxPercent: v })} disabled={disabled || busy || labourMode === 0} /></td>
                  <td className="px-2 py-2 text-right font-medium">{money(rowTotal(row))}</td>
                </tr>
              );
            })}
            {charges.labour.length === 0 ? (
              <tr><td className="px-2 py-4 text-sm text-zinc-500" colSpan={9}>No approved billable labour is waiting to be invoiced.</td></tr>
            ) : null}
          </tbody>
        </table>
      </ChargeSection>

      <ChargeSection
        title="Expenses to recharge"
        count={charges.expenses.length}
        selectedCount={selectedExpenses.length}
        onSelectAll={(v) => setAllSelected(setExpenseRows, v)}
        controls={
          <ModeControls
            mode={expenseMode}
            onMode={setExpenseMode}
            itemId={expenseItemId}
            onItemId={setExpenseItemId}
            itemOptions={itemOptions}
            itemLabel="Bill expenses against"
            disabled={disabled || busy}
          />
        }
      >
        <table className="w-full min-w-[820px] text-sm">
          <thead>
            <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
              <th className="px-2 py-2 w-8"></th>
              <th className="px-2 py-2">Expense</th>
              <th className="px-2 py-2 text-right">Claimed</th>
              <th className="px-2 py-2">Qty</th>
              <th className="px-2 py-2">Recharge at</th>
              <th className="px-2 py-2">Disc %</th>
              <th className="px-2 py-2">Tax %</th>
              <th className="px-2 py-2 text-right">Total</th>
            </tr>
          </thead>
          <tbody>
            {charges.expenses.map((e) => {
              const row = expenseRows[e.expenseClaimLineId];
              if (!row) return null;
              return (
                <tr key={e.expenseClaimLineId} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="px-2 py-2">
                    <input
                      type="checkbox"
                      checked={row.selected}
                      disabled={disabled || busy || expenseMode === 0}
                      onChange={(ev) => patchRow(setExpenseRows, e.expenseClaimLineId, { selected: ev.target.checked })}
                    />
                  </td>
                  <td className="px-2 py-2">
                    <div>{e.description}</div>
                    <div className="text-xs text-zinc-500">
                      {e.expenseClaimNumber} &middot; {new Date(e.expenseDate).toLocaleDateString()}
                    </div>
                  </td>
                  <td className="px-2 py-2 text-right text-zinc-500">{money(e.lineTotal)}</td>
                    <td className="px-2 py-2"><NumCell value={row.quantity} onChange={(v) => patchRow(setExpenseRows, e.expenseClaimLineId, { quantity: v })} disabled={disabled || busy || expenseMode === 0} /></td>
                  <td className="px-2 py-2"><NumCell value={row.unitPrice} onChange={(v) => patchRow(setExpenseRows, e.expenseClaimLineId, { unitPrice: v })} disabled={disabled || busy || expenseMode === 0} /></td>
                  <td className="px-2 py-2"><NumCell value={row.discountPercent} onChange={(v) => patchRow(setExpenseRows, e.expenseClaimLineId, { discountPercent: v })} disabled={disabled || busy || expenseMode === 0} /></td>
                  <td className="px-2 py-2"><NumCell value={row.taxPercent} onChange={(v) => patchRow(setExpenseRows, e.expenseClaimLineId, { taxPercent: v })} disabled={disabled || busy || expenseMode === 0} /></td>
                  <td className="px-2 py-2 text-right font-medium">{money(rowTotal(row))}</td>
                </tr>
              );
            })}
            {charges.expenses.length === 0 ? (
              <tr><td className="px-2 py-4 text-sm text-zinc-500" colSpan={8}>No approved billable expenses are waiting to be recharged.</td></tr>
            ) : null}
          </tbody>
        </table>
      </ChargeSection>

      <div className="rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)] p-3">
        <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Additional charges</div>
          <div className="text-xs text-zinc-500">
            Sundries, call-out fees, or anything the job did not record as a charge
          </div>
        </div>
        {otherLines.length > 0 ? (
          <div className="mb-2 overflow-auto rounded-md border border-zinc-200 dark:border-zinc-800">
            <table className="w-full min-w-[820px] text-sm">
              <thead>
                <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                  <th className="px-2 py-2">Item</th>
                  <th className="px-2 py-2">Description</th>
                  <th className="px-2 py-2">Qty</th>
                  <th className="px-2 py-2">Unit price</th>
                  <th className="px-2 py-2">Disc %</th>
                  <th className="px-2 py-2">Tax</th>
                  <th className="px-2 py-2"></th>
                </tr>
              </thead>
              <tbody>
                {otherLines.map((line) => (
                  <tr key={line.key} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                    <td className="px-2 py-2 min-w-[220px]">
                      <Select
                        value={line.itemId}
                        onChange={(e) => {
                          const itemId = e.target.value;
                          const item = itemOptions.find((candidate) => candidate.id === itemId);
                          setOtherLines((current) =>
                            current.map((row) =>
                              row.key === line.key
                                ? {
                                    ...row,
                                    itemId,
                                    // seed from the selling price, not the cost
                                    unitPrice: row.unitPrice === "0" ? String(item?.defaultUnitPrice ?? 0) : row.unitPrice,
                                  }
                                : row,
                            ),
                          );
                        }}
                        disabled={disabled || busy}
                      >
                        <option value="">Search item by SKU or name...</option>
                        {itemOptions.map((item) => (
                          <option key={item.id} value={item.id}>
                            {item.sku} - {item.name}
                          </option>
                        ))}
                      </Select>
                    </td>
                    <td className="px-2 py-2">
                      <Input
                        value={line.description}
                        placeholder="Shown on the invoice"
                        onChange={(e) => setOtherLines((c) => c.map((r) => (r.key === line.key ? { ...r, description: e.target.value } : r)))}
                        disabled={disabled || busy}
                      />
                    </td>
                    <td className="px-2 py-2"><NumCell value={line.quantity} onChange={(v) => setOtherLines((c) => c.map((r) => (r.key === line.key ? { ...r, quantity: v } : r)))} disabled={disabled || busy} /></td>
                    <td className="px-2 py-2"><NumCell value={line.unitPrice} onChange={(v) => setOtherLines((c) => c.map((r) => (r.key === line.key ? { ...r, unitPrice: v } : r)))} disabled={disabled || busy} /></td>
                    <td className="px-2 py-2"><NumCell value={line.discountPercent} onChange={(v) => setOtherLines((c) => c.map((r) => (r.key === line.key ? { ...r, discountPercent: v } : r)))} disabled={disabled || busy} /></td>
                    <td className="px-2 py-2">
                      <Select
                        value={line.taxPercent}
                        onChange={(e) => setOtherLines((c) => c.map((r) => (r.key === line.key ? { ...r, taxPercent: e.target.value } : r)))}
                        disabled={disabled || busy}
                      >
                        <option value="0">No tax</option>
                        {taxOptions.map((tax) => (
                          <option key={tax.id} value={String(tax.ratePercent)}>
                            {tax.code} ({tax.ratePercent}%)
                          </option>
                        ))}
                      </Select>
                    </td>
                    <td className="px-2 py-2">
                      <SecondaryButton
                        type="button"
                        className="px-2 py-1 text-xs"
                        onClick={() => setOtherLines((c) => c.filter((r) => r.key !== line.key))}
                        disabled={disabled || busy}
                      >
                        Remove
                      </SecondaryButton>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
        <SecondaryButton
          type="button"
          className="px-3 py-1.5 text-xs"
          onClick={() => setOtherLines((current) => [...current, newOtherLine()])}
          disabled={disabled || busy}
        >
          + Add charge
        </SecondaryButton>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="rounded-md border border-[var(--card-border)] p-3">
          <div className="mb-3 text-sm font-semibold">Invoice settings</div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-xs font-medium text-zinc-500">Due date (optional)</label>
              <Input type="datetime-local" value={dueDate} onChange={(e) => setDueDate(e.target.value)} disabled={disabled || busy} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-zinc-500">Discount on the whole invoice</label>
              <div className="flex gap-2">
                <Select
                  className="w-28"
                  value={discountKind}
                  onChange={(e) => setDiscountKind(e.target.value as "percent" | "amount")}
                  disabled={disabled || busy}
                >
                  <option value="percent">%</option>
                  <option value="amount">Amount</option>
                </Select>
                <Input value={discountValue} onChange={(e) => setDiscountValue(e.target.value)} inputMode="decimal" disabled={disabled || busy} />
              </div>
              <div className="mt-1 text-[11px] text-zinc-500">
                Spread across the lines so tax stays right. Per-line discount is separate.
              </div>
            </div>
          </div>
        </div>

        <div className="rounded-md border border-[var(--card-border)] bg-[var(--surface-soft)] p-3">
          <div className="mb-3 text-sm font-semibold">Invoice total</div>
          <dl className="space-y-1 text-sm">
            <Row label="Lines subtotal" value={money(totals.gross)} />
            {totals.discount > 0 ? <Row label="Invoice discount" value={`-${money(totals.discount)}`} /> : null}
            {totals.discount > 0 ? <Row label="Net subtotal" value={money(totals.net)} /> : null}
            <Row label="Tax" value={money(totals.tax)} />
            <Row label="Total" value={money(totals.total)} strong />
            <div className="!mt-3 border-t border-[var(--card-border)] pt-2">
              <Row label="Job cost of what is billed" value={money(totals.cost)} muted />
              <Row
                label="Margin"
                value={money(totals.margin)}
                tone={totals.margin >= 0 ? "good" : "bad"}
              />
            </div>
          </dl>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <Button type="button" disabled={disabled || busy} onClick={build}>
          {busy ? "Creating..." : "Create Sales Invoice Draft"}
        </Button>
        <div className="text-xs text-zinc-500">
          The draft can still be re-priced before you post it, but charges are linked from here.
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

function Row({
  label,
  value,
  strong,
  muted,
  tone,
}: {
  label: string;
  value: string;
  strong?: boolean;
  muted?: boolean;
  tone?: "good" | "bad";
}) {
  const toneClass =
    tone === "good"
      ? "text-emerald-700 dark:text-emerald-300"
      : tone === "bad"
        ? "text-red-700 dark:text-red-300"
        : "";
  return (
    <div className={`flex items-baseline justify-between gap-4 ${muted ? "text-zinc-500" : ""}`}>
      <dt className={strong ? "font-semibold" : ""}>{label}</dt>
      <dd className={`tabular-nums ${strong ? "text-base font-semibold" : ""} ${toneClass}`}>{value}</dd>
    </div>
  );
}

function NumCell({
  value,
  onChange,
  disabled,
}: {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <Input
      className="w-24"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      inputMode="decimal"
      disabled={disabled}
    />
  );
}

function ModeControls({
  mode,
  onMode,
  itemId,
  onItemId,
  itemOptions,
  itemLabel,
  disabled,
}: {
  mode: BillingMode;
  onMode: (mode: BillingMode) => void;
  itemId: string;
  onItemId: (id: string) => void;
  itemOptions: BillingItemRef[];
  itemLabel: string;
  disabled: boolean;
}) {
  return (
    <div className="flex flex-wrap items-end gap-3">
      <div>
        <label className="mb-1 block text-[11px] font-medium uppercase tracking-wide text-zinc-500">On the invoice</label>
        <Select className="w-44" value={String(mode)} onChange={(e) => onMode(Number(e.target.value) as BillingMode)} disabled={disabled}>
          <option value="2">One combined line</option>
          <option value="1">Itemised</option>
          <option value="0">Do not bill</option>
        </Select>
      </div>
      {mode !== 0 ? (
        <div className="min-w-[240px]">
          <label className="mb-1 block text-[11px] font-medium uppercase tracking-wide text-zinc-500">{itemLabel}</label>
          <Select value={itemId} onChange={(e) => onItemId(e.target.value)} disabled={disabled}>
            <option value="">Select item...</option>
            {itemOptions.map((item) => (
              <option key={item.id} value={item.id}>
                {item.sku} - {item.name}
              </option>
            ))}
          </Select>
        </div>
      ) : null}
    </div>
  );
}

function ChargeSection({
  title,
  count,
  selectedCount,
  onSelectAll,
  controls,
  children,
}: {
  title: string;
  count: number;
  selectedCount: number;
  onSelectAll: (selected: boolean) => void;
  controls?: React.ReactNode;
  children: React.ReactNode;
}) {
  const [open, setOpen] = useState(true);

  return (
    <div className="rounded-md border border-[var(--card-border)]">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--card-border)] px-3 py-2">
        <button type="button" className="text-left" onClick={() => setOpen((v) => !v)} aria-expanded={open}>
          <span className="text-sm font-semibold">
            <span aria-hidden="true" className="mr-2 text-zinc-500">{open ? "-" : "+"}</span>
            {title}
          </span>
          <span className="ml-2 text-xs text-zinc-500">
            {selectedCount} of {count} selected
          </span>
        </button>
        <div className="flex flex-wrap items-end gap-3">
          {controls}
          {count > 0 ? (
            <div className="flex gap-2 text-xs">
              <button type="button" className="text-[var(--link)] hover:underline" onClick={() => onSelectAll(true)}>
                Select all
              </button>
              <button type="button" className="text-[var(--link)] hover:underline" onClick={() => onSelectAll(false)}>
                Clear
              </button>
            </div>
          ) : null}
        </div>
      </div>
      {open ? <div className="overflow-auto p-3">{children}</div> : null}
    </div>
  );
}
