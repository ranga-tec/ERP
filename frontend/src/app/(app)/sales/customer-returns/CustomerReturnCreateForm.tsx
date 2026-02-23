"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type CustomerRef = { id: string; code: string; name: string };
type WarehouseRef = { id: string; code: string; name: string };
type InvoiceRef = { id: string; number: string; customerId: string; total: number; status: number };
type DispatchRef = { id: string; number: string; salesOrderId: string; status: number };
type SalesOrderRef = { id: string; number: string; customerId: string };
type CustomerReturnDto = { id: string; number: string };

export function CustomerReturnCreateForm({
  customers,
  warehouses,
  invoices,
  dispatches,
  salesOrders,
}: {
  customers: CustomerRef[];
  warehouses: WarehouseRef[];
  invoices: InvoiceRef[];
  dispatches: DispatchRef[];
  salesOrders: SalesOrderRef[];
}) {
  const router = useRouter();
  const [customerId, setCustomerId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [salesInvoiceId, setSalesInvoiceId] = useState("");
  const [dispatchNoteId, setDispatchNoteId] = useState("");
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const customerOptions = customers.slice().sort((a, b) => a.code.localeCompare(b.code));
  const warehouseOptions = warehouses.slice().sort((a, b) => a.code.localeCompare(b.code));
  const orderById = new Map(salesOrders.map((o) => [o.id, o]));

  const invoiceOptions = invoices
    .filter((i) => !customerId || i.customerId === customerId)
    .sort((a, b) => b.number.localeCompare(a.number));

  const dispatchOptions = dispatches
    .filter((d) => {
      if (!customerId) return true;
      const order = orderById.get(d.salesOrderId);
      return order?.customerId === customerId;
    })
    .sort((a, b) => b.number.localeCompare(a.number));

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const cr = await apiPost<CustomerReturnDto>("sales/customer-returns", {
        customerId,
        warehouseId,
        salesInvoiceId: salesInvoiceId || null,
        dispatchNoteId: dispatchNoteId || null,
        reason: reason.trim() || null,
      });
      router.push(`/sales/customer-returns/${cr.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Customer</label>
          <Select
            value={customerId}
            onChange={(e) => {
              setCustomerId(e.target.value);
              setSalesInvoiceId("");
              setDispatchNoteId("");
            }}
            required
          >
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
        <div>
          <label className="mb-1 block text-sm font-medium">Warehouse (return to)</label>
          <Select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {warehouseOptions.map((w) => (
              <option key={w.id} value={w.id}>
                {w.code} - {w.name}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium">Reference Invoice (optional)</label>
          <Select value={salesInvoiceId} onChange={(e) => setSalesInvoiceId(e.target.value)}>
            <option value="">None</option>
            {invoiceOptions.map((i) => (
              <option key={i.id} value={i.id}>
                {i.number} (Total {i.total.toFixed(2)})
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Reference Dispatch (optional)</label>
          <Select value={dispatchNoteId} onChange={(e) => setDispatchNoteId(e.target.value)}>
            <option value="">None</option>
            {dispatchOptions.map((d) => (
              <option key={d.id} value={d.id}>
                {d.number}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Reason (optional)</label>
        <Input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Defect, wrong item, DOA..." />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Customer Return"}
      </Button>
    </form>
  );
}
