import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { ItemInlineLink } from "@/components/InlineLink";
import { TransactionLink } from "@/components/TransactionLink";
import { Card, SecondaryLink } from "@/components/ui";
import { CustomerReturnActions } from "../CustomerReturnActions";
import { CustomerReturnLineAddForm } from "../CustomerReturnLineAddForm";
import { CustomerReturnLinesEditor } from "../CustomerReturnLinesEditor";
import { DocumentCollaborationPanel } from "@/components/DocumentCollaborationPanel";
import { StockAvailabilityExplorer } from "@/components/StockAvailabilityExplorer";

type CustomerReturnDto = {
  id: string;
  number: string;
  customerId: string;
  warehouseId: string;
  returnDate: string;
  status: number;
  salesInvoiceId?: string | null;
  dispatchNoteId?: string | null;
  reason?: string | null;
  lines: { id: string; itemId: string; quantity: number; unitPrice: number; batchNumber?: string | null; serials: string[] }[];
};

type CustomerDto = { id: string; code: string; name: string };
type WarehouseDto = { id: string; code: string; name: string };
type ItemDto = { id: string; sku: string; name: string; trackingType: number; defaultUnitCost: number };
type InvoiceSummaryDto = { id: string; number: string; customerId: string; total: number; status: number };
type DispatchSummaryDto = { id: string; number: string; salesOrderId: string; status: number };

const statusLabel: Record<number, string> = { 0: "Draft", 1: "Posted", 2: "Voided" };

export default async function CustomerReturnDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  const [customerReturn, customers, warehouses, items, invoices, dispatches] = await Promise.all([
    backendFetchJson<CustomerReturnDto>(`/sales/customer-returns/${id}`),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<WarehouseDto[]>("/warehouses"),
    backendFetchJson<ItemDto[]>("/items"),
    backendFetchJson<InvoiceSummaryDto[]>("/sales/invoices?take=200"),
    backendFetchJson<DispatchSummaryDto[]>("/sales/dispatches?take=200"),
  ]);

  const customerById = new Map(customers.map((customer) => [customer.id, customer]));
  const warehouseById = new Map(warehouses.map((warehouse) => [warehouse.id, warehouse]));
  const invoiceById = new Map(invoices.map((invoice) => [invoice.id, invoice]));
  const dispatchById = new Map(dispatches.map((dispatch) => [dispatch.id, dispatch]));
  const isDraft = customerReturn.status === 0;

  return (
    <div className="space-y-6">
      <div>
        <div className="text-sm text-zinc-500">
          <Link href="/sales/customer-returns" className="hover:underline">
            Customer Returns
          </Link>{" "}
          / <span className="font-mono text-xs">{customerReturn.number}</span>
        </div>
        <h1 className="mt-1 text-2xl font-semibold">Customer Return {customerReturn.number}</h1>
        <div className="mt-2 flex flex-wrap gap-3 text-sm text-zinc-600 dark:text-zinc-400">
          <div>Customer: {customerById.get(customerReturn.customerId)?.code ?? customerReturn.customerId}</div>
          <div>Warehouse: {warehouseById.get(customerReturn.warehouseId)?.code ?? customerReturn.warehouseId}</div>
          <div>Status: {statusLabel[customerReturn.status] ?? customerReturn.status}</div>
          <div>Date: {new Date(customerReturn.returnDate).toLocaleString()}</div>
        </div>
        <div className="mt-2 text-sm text-zinc-500">
          Invoice:{" "}
          {customerReturn.salesInvoiceId ? (
            <TransactionLink referenceType="INV" referenceId={customerReturn.salesInvoiceId}>
              {invoiceById.get(customerReturn.salesInvoiceId)?.number ?? customerReturn.salesInvoiceId}
            </TransactionLink>
          ) : (
            "-"
          )}{" "}
          - Dispatch:{" "}
          {customerReturn.dispatchNoteId ? (
            <TransactionLink referenceType="DN" referenceId={customerReturn.dispatchNoteId}>
              {dispatchById.get(customerReturn.dispatchNoteId)?.number ?? customerReturn.dispatchNoteId}
            </TransactionLink>
          ) : (
            "-"
          )}
        </div>
        {customerReturn.reason ? <div className="mt-2 text-sm text-zinc-500">Reason: {customerReturn.reason}</div> : null}
      </div>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="text-sm font-semibold">Actions</div>
          <SecondaryLink
            href={`/api/backend/sales/customer-returns/${customerReturn.id}/pdf`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Download PDF
          </SecondaryLink>
        </div>
        <CustomerReturnActions customerReturnId={customerReturn.id} canPost={isDraft && customerReturn.lines.length > 0} />
      </Card>

      {isDraft ? (
        <>
          <Card>
          <div className="mb-3 text-sm font-semibold">Add line</div>
          <CustomerReturnLineAddForm
            customerReturnId={customerReturn.id}
            items={items}
            warehouses={warehouses}
            warehouseId={customerReturn.warehouseId}
          />
        </Card>

          <Card>
            <div className="mb-3 text-sm font-semibold">Stock visibility</div>
            <StockAvailabilityExplorer warehouses={warehouses} items={items} initialWarehouseId={customerReturn.warehouseId} />
          </Card>
        </>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">Lines</div>
        <CustomerReturnLinesEditor
          customerReturnId={customerReturn.id}
          warehouseId={customerReturn.warehouseId}
          warehouses={warehouses}
          lines={customerReturn.lines}
          itemLabelById={new Map(
            items.map((item) => [
              item.id,
              <ItemInlineLink key={item.id} itemId={item.id}>
                {`${item.sku} - ${item.name}`}
              </ItemInlineLink>,
            ]),
          )}
          itemSearchLabelById={new Map(items.map((item) => [item.id, `${item.sku} ${item.name}`.toLowerCase()]))}
          canEdit={isDraft}
        />
      </Card>

      <DocumentCollaborationPanel referenceType="CRTN" referenceId={id} />
    </div>
  );
}
