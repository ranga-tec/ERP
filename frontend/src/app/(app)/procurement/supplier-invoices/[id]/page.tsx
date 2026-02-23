import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card, SecondaryLink } from "@/components/ui";
import { SupplierInvoiceActions } from "../SupplierInvoiceActions";

type SupplierInvoiceDto = {
  id: string;
  number: string;
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
  grandTotal: number;
  status: number;
  postedAt?: string | null;
  accountsPayableEntryId?: string | null;
  notes?: string | null;
};

type SupplierDto = { id: string; code: string; name: string };
type PurchaseOrderSummaryDto = { id: string; number: string; supplierId: string; total: number; status: number };
type GoodsReceiptSummaryDto = { id: string; number: string; purchaseOrderId: string; status: number };
type DirectPurchaseSummaryDto = { id: string; number: string; supplierId: string; grandTotal: number; status: number };

const statusLabel: Record<number, string> = {
  0: "Draft",
  1: "Posted",
  2: "Voided",
};

export default async function SupplierInvoiceDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [invoice, suppliers, purchaseOrders, goodsReceipts, directPurchases] = await Promise.all([
    backendFetchJson<SupplierInvoiceDto>(`/procurement/supplier-invoices/${id}`),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<PurchaseOrderSummaryDto[]>("/procurement/purchase-orders?take=200"),
    backendFetchJson<GoodsReceiptSummaryDto[]>("/procurement/goods-receipts?take=200"),
    backendFetchJson<DirectPurchaseSummaryDto[]>("/procurement/direct-purchases?take=200"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const poById = new Map(purchaseOrders.map((po) => [po.id, po]));
  const grnById = new Map(goodsReceipts.map((grn) => [grn.id, grn]));
  const dpById = new Map(directPurchases.map((dp) => [dp.id, dp]));

  const isDraft = invoice.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/procurement/supplier-invoices" className="hover:underline">
            Supplier Invoices
          </Link>{" "}
          / <span className="font-mono text-xs">{invoice.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Supplier Invoice {invoice.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Supplier: {supplierById.get(invoice.supplierId)?.code ?? invoice.supplierId}</div>
          <div>Supplier Inv No: {invoice.invoiceNumber}</div>
          <div>Status: {statusLabel[invoice.status] ?? invoice.status}</div>
          <div>Invoice Date: {new Date(invoice.invoiceDate).toLocaleDateString()}</div>
          {invoice.dueDate ? <div>Due Date: {new Date(invoice.dueDate).toLocaleDateString()}</div> : null}
        </div>
      </div>

      <Card>
        <div className="grid gap-2 text-sm sm:grid-cols-2">
          <div>PO: {invoice.purchaseOrderId ? poById.get(invoice.purchaseOrderId)?.number ?? invoice.purchaseOrderId : "-"}</div>
          <div>GRN: {invoice.goodsReceiptId ? grnById.get(invoice.goodsReceiptId)?.number ?? invoice.goodsReceiptId : "-"}</div>
          <div>Direct Purchase: {invoice.directPurchaseId ? dpById.get(invoice.directPurchaseId)?.number ?? invoice.directPurchaseId : "-"}</div>
          <div>AP Entry: {invoice.accountsPayableEntryId ?? "-"}</div>
          <div>Posted At: {invoice.postedAt ? new Date(invoice.postedAt).toLocaleString() : "-"}</div>
          <div>Notes: {invoice.notes ?? "-"}</div>
        </div>
        <div className="mt-4 grid gap-2 text-sm sm:grid-cols-3">
          <div>Subtotal: {invoice.subtotal.toFixed(2)}</div>
          <div>Discount: {invoice.discountAmount.toFixed(2)}</div>
          <div>Tax: {invoice.taxAmount.toFixed(2)}</div>
          <div>Freight: {invoice.freightAmount.toFixed(2)}</div>
          <div>Rounding: {invoice.roundingAmount.toFixed(2)}</div>
          <div className="font-semibold">Grand Total: {invoice.grandTotal.toFixed(2)}</div>
        </div>
      </Card>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/procurement/supplier-invoices/${invoice.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <SupplierInvoiceActions supplierInvoiceId={invoice.id} canPost={isDraft} />
      </Card>
    </div>
  );
}
