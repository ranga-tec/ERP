"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api-client";
import { Button, Input, Select, Textarea } from "@/components/ui";

type MaterialLineRef = {
  materialRequisitionLineId: string;
  materialRequisitionNumber: string;
  itemSku: string;
  itemName: string;
  quantity: number;
};
type DispositionDto = { id: string };
type DailySheetRef = { id: string; number: string; status: number };

export function ServiceJobMaterialDispositionAddForm({
  serviceJobId,
  materialLines,
  dailySheets = [],
  disabled,
}: {
  serviceJobId: string;
  materialLines: MaterialLineRef[];
  dailySheets?: DailySheetRef[];
  disabled?: boolean;
}) {
  const router = useRouter();
  const uniqueLines = useMemo(() => {
    const map = new Map<string, MaterialLineRef>();
    for (const line of materialLines) {
      if (line.materialRequisitionLineId && line.materialRequisitionLineId !== "00000000-0000-0000-0000-000000000000") {
        map.set(line.materialRequisitionLineId, line);
      }
    }
    return [...map.values()];
  }, [materialLines]);
  const [dailySheetId, setDailySheetId] = useState("");
  const [lineId, setLineId] = useState("");
  const [kind, setKind] = useState("0");
  const [quantity, setQuantity] = useState("1");
  const [condition, setCondition] = useState("");
  const [reason, setReason] = useState("");
  const [chargeTo, setChargeTo] = useState("0");
  const [supplierReturnId, setSupplierReturnId] = useState("");
  const [responsiblePerson, setResponsiblePerson] = useState("");
  const [serials, setSerials] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const parsedQuantity = Number(quantity);
      if (!lineId) throw new Error("Material line is required.");
      if (!Number.isFinite(parsedQuantity) || parsedQuantity <= 0) throw new Error("Quantity must be positive.");
      if (!reason.trim()) throw new Error("Reason is required.");

      await apiPost<DispositionDto>(`service/jobs/${serviceJobId}/material-dispositions`, {
        materialRequisitionLineId: lineId,
        kind: Number(kind),
        quantity: parsedQuantity,
        condition: condition.trim() || null,
        reason: reason.trim(),
        chargeTo: Number(chargeTo),
        supplierReturnId: supplierReturnId.trim() || null,
        responsiblePerson: responsiblePerson.trim() || null,
        serials: serials.split(/\r?\n|,/).map((value) => value.trim()).filter(Boolean),
        serviceJobDailySheetId: dailySheetId || null,
      });

      setDailySheetId("");
      setLineId("");
      setKind("0");
      setQuantity("1");
      setCondition("");
      setReason("");
      setChargeTo("0");
      setSupplierReturnId("");
      setResponsiblePerson("");
      setSerials("");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="grid gap-3 lg:grid-cols-2 xl:grid-cols-5">
        <div>
          <label className="mb-1 block text-sm font-medium">Daily sheet</label>
          <Select value={dailySheetId} onChange={(event) => setDailySheetId(event.target.value)} disabled={disabled || busy}>
            <option value="">Unlinked</option>
            {dailySheets.filter((sheet) => sheet.status !== 2).map((sheet) => <option key={sheet.id} value={sheet.id}>{sheet.number}</option>)}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Issued material line</label>
          <Select value={lineId} onChange={(event) => setLineId(event.target.value)} disabled={disabled || busy} required>
            <option value="" disabled>Select...</option>
            {uniqueLines.map((line) => (
              <option key={line.materialRequisitionLineId} value={line.materialRequisitionLineId}>
                {line.materialRequisitionNumber} - {line.itemSku} - {line.itemName} ({line.quantity})
              </option>
            ))}
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Disposition</label>
          <Select value={kind} onChange={(event) => setKind(event.target.value)} disabled={disabled || busy}>
            <option value="0">Used</option>
            <option value="1">Unused returned</option>
            <option value="2">Incorrect returned</option>
            <option value="3">Damaged</option>
            <option value="4">Rejected / supplier return</option>
          </Select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Quantity</label>
          <Input value={quantity} onChange={(event) => setQuantity(event.target.value)} inputMode="decimal" disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Charge to</label>
          <Select value={chargeTo} onChange={(event) => setChargeTo(event.target.value)} disabled={disabled || busy}>
            <option value="0">Customer</option>
            <option value="1">Company</option>
            <option value="2">Supplier</option>
            <option value="3">Employee</option>
            <option value="4">Warranty</option>
          </Select>
        </div>
      </div>
      <div className="grid gap-3 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium">Condition</label>
          <Input value={condition} onChange={(event) => setCondition(event.target.value)} disabled={disabled || busy} placeholder="Good, damaged, rejected..." />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Supplier return ID</label>
          <Input value={supplierReturnId} onChange={(event) => setSupplierReturnId(event.target.value)} disabled={disabled || busy} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">Responsible person</label>
          <Input value={responsiblePerson} onChange={(event) => setResponsiblePerson(event.target.value)} disabled={disabled || busy} />
        </div>
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium">Reason</label>
        <Textarea value={reason} onChange={(event) => setReason(event.target.value)} disabled={disabled || busy} required />
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium">Serials</label>
        <Textarea value={serials} onChange={(event) => setSerials(event.target.value)} disabled={disabled || busy} placeholder="One serial per line or comma separated" />
      </div>
      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">{error}</div>
      ) : null}
      <Button type="submit" disabled={disabled || busy || uniqueLines.length === 0}>{busy ? "Saving..." : "Add Material Disposition"}</Button>
    </form>
  );
}
