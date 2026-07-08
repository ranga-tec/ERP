import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { ListViewEditActions } from "@/components/ListViewEditActions";
import { SearchableRow, SearchableTable } from "@/components/SearchableTable";
import { Card } from "@/components/ui";
import { PaymentCreateForm } from "./PaymentCreateForm";

type PaymentDto = {
  id: string;
  referenceNumber: string;
  direction: number;
  counterpartyType: number;
  counterpartyId: string;
  paymentTypeId?: string | null;
  paymentTypeCode?: string | null;
  paymentTypeName?: string | null;
  currencyCode: string;
  exchangeRate: number;
  amount: number;
  baseAmount: number;
  paidAt: string;
  notes?: string | null;
};

type CustomerDto = { id: string; code: string; name: string };
type SupplierDto = { id: string; code: string; name: string };
type PaymentTypeDto = { id: string; code: string; name: string; isActive: boolean };
type CurrencyDto = { id: string; code: string; name: string; isBase: boolean; isActive: boolean };
type CurrentPermissionsDto = { permissions: string[] };

const directionLabel: Record<number, string> = { 1: "Incoming", 2: "Outgoing" };
const counterpartyLabel: Record<number, string> = { 1: "Customer", 2: "Supplier" };

export default async function PaymentsPage() {
  const [payments, customers, suppliers, paymentTypes, currencies, currentPermissions] = await Promise.all([
    backendFetchJson<PaymentDto[]>("/finance/payments?take=100"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<PaymentTypeDto[]>("/payment-types"),
    backendFetchJson<CurrencyDto[]>("/currencies"),
    backendFetchJson<CurrentPermissionsDto>("/me/permissions"),
  ]);

  const permissions = new Set(currentPermissions.permissions);
  const canCreate = permissions.has("Finance.Payment.Create");
  const customerById = new Map(customers.map((c) => [c.id, c]));
  const supplierById = new Map(suppliers.map((s) => [s.id, s]));

  function counterpartyCode(type: number, id: string): string {
    if (type === 1) return customerById.get(id)?.code ?? id;
    if (type === 2) return supplierById.get(id)?.code ?? id;
    return id;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Payment Receipts</h1>
          <p className="mt-1 text-sm text-zinc-500">Record incoming/outgoing payments and allocate to AR/AP.</p>
        </div>
        {canCreate ? (
          <AppFormModal title="Create Payment Receipt" description="Record an incoming or outgoing payment and allocate it to open AR/AP entries." buttonLabel="+ New Payment" size="xl">
            <PaymentCreateForm
              customers={customers}
              suppliers={suppliers}
              paymentTypes={paymentTypes}
              currencies={currencies}
            />
          </AppFormModal>
        ) : null}
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <SearchableTable
          placeholder="Search payments..."
          emptyMessage="No payments yet."
          emptyColSpan={10}
          headers={
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Reference</th>
                <th className="py-2 pr-3">Direction</th>
                <th className="py-2 pr-3">Counterparty</th>
                <th className="py-2 pr-3">Payment Type</th>
                <th className="py-2 pr-3">Currency</th>
                <th className="py-2 pr-3">Paid</th>
                <th className="py-2 pr-3">Amount</th>
                <th className="py-2 pr-3">Base Amount</th>
                <th className="py-2 pr-3">Notes</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
          }
        >
          {payments.map((p) => (
            <SearchableRow
              key={p.id}
              searchText={`${p.referenceNumber} ${directionLabel[p.direction] ?? p.direction} ${counterpartyCode(p.counterpartyType, p.counterpartyId)} ${p.paymentTypeCode ?? ""} ${p.paymentTypeName ?? ""} ${p.currencyCode} ${p.amount} ${p.baseAmount} ${p.notes ?? ""}`}
            >
                <tr key={p.id} className="border-b border-zinc-100 dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">
                    <Link className="hover:underline" href={`/finance/payments/${p.id}`}>
                      {p.referenceNumber}
                    </Link>
                  </td>
                  <td className="py-2 pr-3">{directionLabel[p.direction] ?? p.direction}</td>
                  <td className="py-2 pr-3">
                    {counterpartyLabel[p.counterpartyType] ?? p.counterpartyType}:{" "}
                    {counterpartyCode(p.counterpartyType, p.counterpartyId)}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {p.paymentTypeCode ? `${p.paymentTypeCode}${p.paymentTypeName ? ` - ${p.paymentTypeName}` : ""}` : "-"}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {p.currencyCode} @ {p.exchangeRate}
                  </td>
                  <td className="py-2 pr-3 text-zinc-500">{new Date(p.paidAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{p.amount}</td>
                  <td className="py-2 pr-3">{p.baseAmount}</td>
                  <td className="py-2 pr-3 text-zinc-500">{p.notes ?? "-"}</td>
                  <td className="py-2 pr-3">
                    <ListViewEditActions viewHref={`/finance/payments/${p.id}`} canEdit={false} />
                  </td>
                </tr>
            </SearchableRow>
          ))}
        </SearchableTable>
      </Card>
    </div>
  );
}
