import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, Table } from "@/components/ui";
import { SupplierInvoiceCreateForm } from "./SupplierInvoiceCreateForm";

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

export default async function SupplierInvoicesPage() {
  const [invoices, suppliers, purchaseOrders, goodsReceipts, directPurchases] = await Promise.all([
    backendFetchJson<SupplierInvoiceDto[]>("/procurement/supplier-invoices?take=100"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<PurchaseOrderSummaryDto[]>("/procurement/purchase-orders?take=200"),
    backendFetchJson<GoodsReceiptSummaryDto[]>("/procurement/goods-receipts?take=200"),
    backendFetchJson<DirectPurchaseSummaryDto[]>("/procurement/direct-purchases?take=200"),
  ]);

  const supplierById = new Map(suppliers.map((s) => [s.id, s]));
  const poById = new Map(purchaseOrders.map((po) => [po.id, po]));
  const grnById = new Map(goodsReceipts.map((grn) => [grn.id, grn]));
  const dpById = new Map(directPurchases.map((dp) => [dp.id, dp]));

  function sourceLabel(invoice: SupplierInvoiceDto) {
    if (invoice.directPurchaseId) {
      return (
        <TransactionLink referenceType="DPR" referenceId={invoice.directPurchaseId}>
          DP {dpById.get(invoice.directPurchaseId)?.number ?? invoice.directPurchaseId}
        </TransactionLink>
      );
    }

    if (invoice.goodsReceiptId) {
      return (
        <TransactionLink referenceType="GRN" referenceId={invoice.goodsReceiptId}>
          GRN {grnById.get(invoice.goodsReceiptId)?.number ?? invoice.goodsReceiptId}
        </TransactionLink>
      );
    }

    if (invoice.purchaseOrderId) {
      return (
        <TransactionLink referenceType="PO" referenceId={invoice.purchaseOrderId}>
          PO {poById.get(invoice.purchaseOrderId)?.number ?? invoice.purchaseOrderId}
        </TransactionLink>
      );
    }

    return "-";
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Supplier Invoices</h1>
        <p className="mt-1 text-sm text-zinc-500">AP bills linked to PO/GRN/direct purchase or created standalone.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <SupplierInvoiceCreateForm
          suppliers={suppliers}
          purchaseOrders={purchaseOrders}
          goodsReceipts={goodsReceipts}
          directPurchases={directPurchases}
        />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Doc No</th>
                <th className="py-2 pr-3">Supplier Inv No</th>
                <th className="py-2 pr-3">Supplier</th>
                <th className="py-2 pr-3">Date</th>
                <th className="py-2 pr-3">Source</th>
                <th className="py-2 pr-3">Total</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {invoices.map((i) => (
                <tr key={i.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/procurement/supplier-invoices/${i.id}`}>
                      {i.number}
                    </Link>
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs">{i.invoiceNumber}</td>
                  <td className="py-2 pr-3">{supplierById.get(i.supplierId)?.code ?? i.supplierId}</td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(i.invoiceDate).toLocaleDateString()}</td>
                  <td className="py-2 pr-3 text-zinc-500">{sourceLabel(i)}</td>
                  <td className="py-2 pr-3">{i.grandTotal.toFixed(2)}</td>
                  <td className="py-2 pr-3">{statusLabel[i.status] ?? i.status}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions
                      viewHref={`/procurement/supplier-invoices/${i.id}`}
                      canEdit={i.status === 0}
                    />
                  </td>
                </tr>
              ))}
              {invoices.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                    No supplier invoices yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
