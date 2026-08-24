"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiDeleteNoContent, apiPutNoContent } from "@/lib/api-client";
import { ItemInlineLink } from "@/components/InlineLink";
import { ItemLookupField } from "@/components/ItemLookupField";
import { Button, DecimalInput, Input, SecondaryButton, Select } from "@/components/ui";

type ItemRef = { id: string; sku: string; name: string; unitOfMeasure: string };
type ExpenseAccountRef = { id: string; code: string; name: string };
type ServiceExpenseClaimLineDto = {
  id: string;
  itemId?: string | null;
  expenseAccountId?: string | null;
  expenseAccountCode?: string | null;
  expenseAccountName?: string | null;
  description: string;
  quantity: number;
  unitCost: number;
  billableToCustomer: boolean;
  receiptReference?: string | null;
  missingReceipt: boolean;
  missingReceiptReason?: string | null;
  missingReceiptApprovedAt?: string | null;
  lineTotal: number;
};

export function ServiceExpenseClaimLineRow({
  claimId,
  line,
  items,
  expenseAccounts,
  canEdit,
}: {
  claimId: string;
  line: ServiceExpenseClaimLineDto;
  items: ItemRef[];
  expenseAccounts: ExpenseAccountRef[];
  canEdit: boolean;
}) {
  const router = useRouter();
  const [isEditing, setIsEditing] = useState(false);
  const [itemId, setItemId] = useState(line.itemId ?? "");
  const [expenseAccountId, setExpenseAccountId] = useState(line.expenseAccountId ?? "");
  const [description, setDescription] = useState(line.description);
  const [quantity, setQuantity] = useState(line.quantity.toString());
  const [unitCost, setUnitCost] = useState(line.unitCost.toString());
  const [billableToCustomer, setBillableToCustomer] = useState(line.billableToCustomer);
  const [receiptReference, setReceiptReference] = useState(line.receiptReference ?? "");
  const [missingReceipt, setMissingReceipt] = useState(line.missingReceipt);
  const [missingReceiptReason, setMissingReceiptReason] = useState(line.missingReceiptReason ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function beginEdit() {
    setError(null);
    setItemId(line.itemId ?? "");
    setExpenseAccountId(line.expenseAccountId ?? "");
    setDescription(line.description);
    setQuantity(line.quantity.toString());
    setUnitCost(line.unitCost.toString());
    setBillableToCustomer(line.billableToCustomer);
    setReceiptReference(line.receiptReference ?? "");
    setMissingReceipt(line.missingReceipt);
    setMissingReceiptReason(line.missingReceiptReason ?? "");
    setIsEditing(true);
  }

  async function saveEdit() {
    setError(null);
    setBusy(true);
    try {
      const parsedQuantity = Number(quantity);
      if (!Number.isFinite(parsedQuantity) || parsedQuantity <= 0) {
        throw new Error("Quantity must be positive.");
      }

      const parsedUnitCost = Number(unitCost);
      if (!Number.isFinite(parsedUnitCost) || parsedUnitCost < 0) {
        throw new Error("Unit cost must be 0 or greater.");
      }

      await apiPutNoContent(`service/expense-claims/${claimId}/lines/${line.id}`, {
        itemId: itemId || null,
        expenseAccountId,
        description: description.trim(),
        quantity: parsedQuantity,
        unitCost: parsedUnitCost,
        billableToCustomer,
        receiptReference: missingReceipt ? null : receiptReference.trim(),
        missingReceipt,
        missingReceiptReason: missingReceipt ? missingReceiptReason.trim() : null,
      });

      setIsEditing(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function deleteLine() {
    if (!window.confirm("Delete this expense line?")) return;

    setError(null);
    setBusy(true);
    try {
      await apiDeleteNoContent(`service/expense-claims/${claimId}/lines/${line.id}`);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setBusy(false);
    }
  }

  const itemById = new Map(items.map((item) => [item.id, item]));
  const currentItem = line.itemId ? itemById.get(line.itemId) : null;
  const previewTotal = Number(quantity) * Number(unitCost);

  return (
    <tr className="border-b border-zinc-100 align-top dark:border-zinc-900">
      <td className="py-2 pr-3">
        {isEditing ? (
          <ItemLookupField
            items={items}
            value={itemId}
            onChange={setItemId}
            emptyLabel="Ad-hoc / outside buy"
            className="min-w-52"
          />
        ) : currentItem ? (
          <ItemInlineLink itemId={currentItem.id}>
            {currentItem.sku} - {currentItem.name}
          </ItemInlineLink>
        ) : (
          <span className="text-zinc-500">Ad-hoc / outside buy</span>
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">-</td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <Input value={description} onChange={(event) => setDescription(event.target.value)} className="min-w-56" />
        ) : (
          line.description
        )}
      </td>
      <td className="py-2 pr-3 text-sm">
        {isEditing ? (
          <Select value={expenseAccountId} onChange={(event) => setExpenseAccountId(event.target.value)} className="min-w-52" required>
            <option value="">Select expense account...</option>
            {expenseAccounts.map((account) => <option key={account.id} value={account.id}>{account.code} - {account.name}</option>)}
          </Select>
        ) : line.expenseAccountCode ? `${line.expenseAccountCode}${line.expenseAccountName ? ` - ${line.expenseAccountName}` : ""}` : (
          <span className="text-amber-700 dark:text-amber-300">Unassigned</span>
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <DecimalInput value={quantity} onChange={(event) => setQuantity(event.target.value)} className="min-w-20" />
        ) : (
          `${line.quantity} ${items.find((item) => item.id === (line.itemId ?? ""))?.unitOfMeasure ?? ""}`
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <DecimalInput value={unitCost} onChange={(event) => setUnitCost(event.target.value)} className="min-w-24" />
        ) : (
          line.unitCost.toFixed(2)
        )}
      </td>
      <td className="py-2 pr-3">
        {isEditing ? (
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={billableToCustomer}
              onChange={(event) => setBillableToCustomer(event.target.checked)}
              className="h-4 w-4 rounded border-zinc-300"
            />
            Billable
          </label>
        ) : line.billableToCustomer ? (
          "Yes"
        ) : (
          "No"
        )}
      </td>
      <td className="py-2 pr-3 text-zinc-500">
        {isEditing ? (
          <div className="min-w-52 space-y-2">
            <Input value={receiptReference} onChange={(event) => setReceiptReference(event.target.value)} disabled={missingReceipt} required={!missingReceipt} placeholder="Receipt number" />
            <label className="flex items-center gap-2 text-xs"><input type="checkbox" checked={missingReceipt} onChange={(event) => setMissingReceipt(event.target.checked)} />Missing receipt</label>
            {missingReceipt ? <Input value={missingReceiptReason} onChange={(event) => setMissingReceiptReason(event.target.value)} required placeholder="Reason" /> : null}
          </div>
        ) : line.missingReceipt ? (
          <span className={line.missingReceiptApprovedAt ? "text-emerald-700 dark:text-emerald-300" : "text-amber-700 dark:text-amber-300"}>{line.missingReceiptApprovedAt ? "Exception approved" : "Exception waiting"}</span>
        ) : line.receiptReference ?? "Receipt required"
        }
      </td>
      <td className="py-2 pr-3">
        {isEditing && Number.isFinite(previewTotal) ? previewTotal.toFixed(2) : line.lineTotal.toFixed(2)}
      </td>
      {canEdit ? (
        <td className="py-2 pr-3">
          <div className="flex flex-wrap items-center gap-2">
            {isEditing ? (
              <>
                <Button type="button" className="px-2 py-1 text-xs" onClick={saveEdit} disabled={busy}>
                  {busy ? "Saving..." : "Save"}
                </Button>
                <SecondaryButton
                  type="button"
                  className="px-2 py-1 text-xs"
                  onClick={() => {
                    setError(null);
                    setIsEditing(false);
                  }}
                  disabled={busy}
                >
                  Cancel
                </SecondaryButton>
              </>
            ) : (
              <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={beginEdit} disabled={busy}>
                Edit
              </SecondaryButton>
            )}
            <SecondaryButton type="button" className="px-2 py-1 text-xs" onClick={() => void deleteLine()} disabled={busy}>
              Delete
            </SecondaryButton>
          </div>
          {error ? <div className="mt-2 text-xs text-red-700 dark:text-red-300">{error}</div> : null}
        </td>
      ) : null}
    </tr>
  );
}
