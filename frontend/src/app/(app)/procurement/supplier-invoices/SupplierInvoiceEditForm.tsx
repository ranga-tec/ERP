"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPut } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type SupplierRef = { id: string; code: string; name: string };
type PurchaseOrderRef = { id: string; number: string; supplierId: string; total: number; status: number };
type GoodsReceiptRef = { id: string; number: string; purchaseOrderId: string; status: number };
type DirectPurchaseRef = { id: string; number: string; supplierId: string; grandTotal: number; status: number };

type SupplierInvoiceDto = {
  id: string;
  supplierId: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate?: string | null;
  purchaseOrderId?: string | null;
  goodsReceiptId?: string | null;
  directPurchaseId?: string | null;
  subtotal: number;
  discountAmount: number;
  taxAmount: number;
  freightAmount: number;
  roundingAmount: number;
  notes?: string | null;
};

function isoToDateInput(value?: string | null): string {
  if (!value) {
    return "";
  }

  return new Date(value).toISOString().slice(0, 10);
}

function dateToIso(dateText: string | null): string | null {
  if (!dateText) {
    return null;
  }

  return new Date(`${dateText}T00:00:00`).toISOString();
}

function initialLinkMode(invoice: SupplierInvoiceDto): "none" | "po-grn" | "direct-purchase" {
  if (invoice.directPurchaseId) {
    return "direct-purchase";
  }

  if (invoice.purchaseOrderId || invoice.goodsReceiptId) {
    return "po-grn";
  }

  return "none";
}

export function SupplierInvoiceEditForm({
  invoice,
  suppliers,
  purchaseOrders,
  goodsReceipts,
  directPurchases,
}: {
  invoice: SupplierInvoiceDto;
  suppliers: SupplierRef[];
  purchaseOrders: PurchaseOrderRef[];
  goodsReceipts: GoodsReceiptRef[];
  directPurchases: DirectPurchaseRef[];
}) {
  const router = useRouter();
  const [supplierId, setSupplierId] = useState(invoice.supplierId);
  const [invoiceNumber, setInvoiceNumber] = useState(invoice.invoiceNumber);
  const [invoiceDate, setInvoiceDate] = useState(() => isoToDateInput(invoice.invoiceDate));
  const [dueDate, setDueDate] = useState(() => isoToDateInput(invoice.dueDate));
  const [linkMode, setLinkMode] = useState<"none" | "po-grn" | "direct-purchase">(() => initialLinkMode(invoice));
  const [purchaseOrderId, setPurchaseOrderId] = useState(invoice.purchaseOrderId ?? "");
  const [goodsReceiptId, setGoodsReceiptId] = useState(invoice.goodsReceiptId ?? "");
  const [directPurchaseId, setDirectPurchaseId] = useState(invoice.directPurchaseId ?? "");
  const [subtotal, setSubtotal] = useState(invoice.subtotal.toString());
  const [discountAmount, setDiscountAmount] = useState(invoice.discountAmount.toString());
  const [taxAmount, setTaxAmount] = useState(invoice.taxAmount.toString());
  const [freightAmount, setFreightAmount] = useState(invoice.freightAmount.toString());
  const [roundingAmount, setRoundingAmount] = useState(invoice.roundingAmount.toString());
  const [notes, setNotes] = useState(invoice.notes ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const poById = new Map(purchaseOrders.map((po) => [po.id, po]));
  const sortedSuppliers = suppliers.slice().sort((a, b) => a.code.localeCompare(b.code));

  const filteredPos = purchaseOrders
    .filter((po) => !supplierId || po.supplierId === supplierId)
    .sort((a, b) => b.number.localeCompare(a.number));

  const filteredGrns = goodsReceipts
    .filter((grn) => {
      if (!supplierId) {
        return true;
      }

      const po = poById.get(grn.purchaseOrderId);
      return po?.supplierId === supplierId;
    })
    .sort((a, b) => b.number.localeCompare(a.number));

  const filteredDirectPurchases = directPurchases
    .filter((dp) => !supplierId || dp.supplierId === supplierId)
    .sort((a, b) => b.number.localeCompare(a.number));

  const grandTotal =
    (Number(subtotal) || 0) -
    (Number(discountAmount) || 0) +
    (Number(taxAmount) || 0) +
    (Number(freightAmount) || 0) +
    (Number(roundingAmount) || 0);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const values = {
        subtotal: Number(subtotal),
        discountAmount: Number(discountAmount),
        taxAmount: Number(taxAmount),
        freightAmount: Number(freightAmount),
        roundingAmount: Number(roundingAmount),
      };

      for (const [label, value] of Object.entries(values)) {
        if (Number.isNaN(value)) {
          throw new Error(`${label} must be a valid number.`);
        }
      }

      if (values.subtotal < 0 || values.discountAmount < 0 || values.taxAmount < 0 || values.freightAmount < 0) {
        throw new Error("Subtotal, discount, tax and freight cannot be negative.");
      }

      if (grandTotal <= 0) {
        throw new Error("Grand total must be positive.");
      }

      await apiPut(`procurement/supplier-invoices/${invoice.id}`, {
        supplierId,
        invoiceNumber: invoiceNumber.trim(),
        invoiceDate: dateToIso(invoiceDate),
        dueDate: dueDate ? dateToIso(dueDate) : null,
        purchaseOrderId: linkMode === "po-grn" && purchaseOrderId ? purchaseOrderId : null,
        goodsReceiptId: linkMode === "po-grn" && goodsReceiptId ? goodsReceiptId : null,
        directPurchaseId: linkMode === "direct-purchase" && directPurchaseId ? directPurchaseId : null,
        subtotal: values.subtotal,
        discountAmount: values.discountAmount,
        taxAmount: values.taxAmount,
        freightAmount: values.freightAmount,
        roundingAmount: values.roundingAmount,
        notes: notes.trim() || null,
      });

      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Supplier</label>
          <Select
            value={supplierId}
            onChange={(event) => {
              setSupplierId(event.target.value);
              setPurchaseOrderId("");
              setGoodsReceiptId("");
              setDirectPurchaseId("");
            }}
            required
          >
            <option value="" disabled>
              Select...
            </option>
            {sortedSuppliers.map((supplier) => (
              <option key={supplier.id} value={supplier.id}>
                {supplier.code} - {supplier.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Supplier Invoice No.</label>
          <Input value={invoiceNumber} onChange={(event) => setInvoiceNumber(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Link Mode</label>
          <Select
            value={linkMode}
            onChange={(event) => {
              const mode = event.target.value as "none" | "po-grn" | "direct-purchase";
              setLinkMode(mode);
              setPurchaseOrderId("");
              setGoodsReceiptId("");
              setDirectPurchaseId("");
            }}
          >
            <option value="none">No link</option>
            <option value="po-grn">PO / GRN</option>
            <option value="direct-purchase">Direct Purchase</option>
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Invoice Date</label>
          <Input type="date" value={invoiceDate} onChange={(event) => setInvoiceDate(event.target.value)} required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Due Date (optional)</label>
          <Input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
        </div>
      </div>

      {linkMode === "po-grn" ? (
        <div className="grid gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium">Purchase Order (optional)</label>
            <Select value={purchaseOrderId} onChange={(event) => setPurchaseOrderId(event.target.value)}>
              <option value="">None</option>
              {filteredPos.map((po) => (
                <option key={po.id} value={po.id}>
                  {po.number} (Total {po.total.toFixed(2)})
                </option>
              ))}
            </Select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Goods Receipt (optional)</label>
            <Select value={goodsReceiptId} onChange={(event) => setGoodsReceiptId(event.target.value)}>
              <option value="">None</option>
              {filteredGrns.map((grn) => (
                <option key={grn.id} value={grn.id}>
                  {grn.number} {poById.get(grn.purchaseOrderId) ? `(PO ${poById.get(grn.purchaseOrderId)?.number})` : ""}
                </option>
              ))}
            </Select>
          </div>
        </div>
      ) : null}

      {linkMode === "direct-purchase" ? (
        <div>
          <label className="mb-1 block text-sm font-medium">Direct Purchase</label>
          <Select value={directPurchaseId} onChange={(event) => setDirectPurchaseId(event.target.value)}>
            <option value="">None</option>
            {filteredDirectPurchases.map((dp) => (
              <option key={dp.id} value={dp.id}>
                {dp.number} (Total {dp.grandTotal.toFixed(2)})
              </option>
            ))}
          </Select>
        </div>
      ) : null}

      <div className="grid gap-3 sm:grid-cols-5">
        <div>
          <label className="mb-1 block text-sm font-medium">Subtotal</label>
          <Input value={subtotal} onChange={(event) => setSubtotal(event.target.value)} inputMode="decimal" required />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Discount</label>
          <Input value={discountAmount} onChange={(event) => setDiscountAmount(event.target.value)} inputMode="decimal" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Tax</label>
          <Input value={taxAmount} onChange={(event) => setTaxAmount(event.target.value)} inputMode="decimal" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Freight</label>
          <Input value={freightAmount} onChange={(event) => setFreightAmount(event.target.value)} inputMode="decimal" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Rounding (+/-)</label>
          <Input value={roundingAmount} onChange={(event) => setRoundingAmount(event.target.value)} inputMode="decimal" />
        </div>
      </div>

      <div className="rounded-md border border-zinc-200 p-3 text-sm dark:border-zinc-800">
        <div className="font-medium">Grand Total: {Number.isFinite(grandTotal) ? grandTotal.toFixed(2) : "Invalid"}</div>
        <div className="mt-1 text-xs text-zinc-500">
          Updating linked references revalidates supplier consistency and posting prerequisites for the selected source.
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Input value={notes} onChange={(event) => setNotes(event.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Saving..." : "Save Supplier Invoice"}
      </Button>
    </form>
  );
}
