"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select } from "@/components/ui";

type CustomerRef = { id: string; code: string; name: string };
type SupplierRef = { id: string; code: string; name: string };
type PaymentDto = { id: string; referenceNumber: string };

const directionLabel: Record<string, string> = { "1": "Incoming", "2": "Outgoing" };
const counterpartyLabel: Record<string, string> = { "1": "Customer", "2": "Supplier" };

export function PaymentCreateForm({
  customers,
  suppliers,
}: {
  customers: CustomerRef[];
  suppliers: SupplierRef[];
}) {
  const router = useRouter();

  const customerOptions = useMemo(
    () => customers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [customers],
  );
  const supplierOptions = useMemo(
    () => suppliers.slice().sort((a, b) => a.code.localeCompare(b.code)),
    [suppliers],
  );

  const [direction, setDirection] = useState("1");
  const [counterpartyType, setCounterpartyType] = useState("1");
  const [counterpartyId, setCounterpartyId] = useState("");
  const [amount, setAmount] = useState("0");
  const [notes, setNotes] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const counterpartyOptions = counterpartyType === "1" ? customerOptions : supplierOptions;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const amt = Number(amount);
      if (Number.isNaN(amt) || amt <= 0) {
        throw new Error("Amount must be positive.");
      }

      const payment = await apiPost<PaymentDto>("finance/payments", {
        direction: Number(direction),
        counterpartyType: Number(counterpartyType),
        counterpartyId,
        amount: amt,
        notes: notes.trim() || null,
      });

      router.push(`/finance/payments/${payment.id}`);
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
          <label className="mb-1 block text-sm font-medium">Direction</label>
          <Select value={direction} onChange={(e) => setDirection(e.target.value)} required>
            <option value="1">{directionLabel["1"]}</option>
            <option value="2">{directionLabel["2"]}</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Counterparty type</label>
          <Select
            value={counterpartyType}
            onChange={(e) => {
              setCounterpartyType(e.target.value);
              setCounterpartyId("");
            }}
            required
          >
            <option value="1">{counterpartyLabel["1"]}</option>
            <option value="2">{counterpartyLabel["2"]}</option>
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium">Counterparty</label>
          <Select value={counterpartyId} onChange={(e) => setCounterpartyId(e.target.value)} required>
            <option value="" disabled>
              Select...
            </option>
            {counterpartyOptions.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} — {c.name}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Amount</label>
          <Input value={amount} onChange={(e) => setAmount(e.target.value)} inputMode="decimal" required />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium">Notes (optional)</label>
        <Input value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      <Button type="submit" disabled={busy}>
        {busy ? "Creating..." : "Create Payment"}
      </Button>
    </form>
  );
}

