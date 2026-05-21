"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiPostNoContent, apiPutNoContent } from "@/lib/api-client";
import { SecondaryButton } from "@/components/ui";

type MaterialDispositionEditable = {
  condition: string;
  reason: string;
  chargeTo: number;
  supplierReturnId?: string | null;
  responsiblePerson?: string | null;
  status: number;
  isVoided: boolean;
};

export function ServiceJobMaterialDispositionActions({
  serviceJobId,
  dispositionId,
  disposition,
  disabled,
}: {
  serviceJobId: string;
  dispositionId: string;
  disposition: MaterialDispositionEditable;
  disabled?: boolean;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState<"edit" | "post" | "void" | null>(null);
  const [error, setError] = useState<string | null>(null);
  const isDraft = disposition.status === 0 && !disposition.isVoided;

  async function editDetails() {
    const condition = window.prompt("Condition", disposition.condition);
    if (condition === null) return;
    const reason = window.prompt("Reason", disposition.reason);
    if (reason === null) return;
    const chargeToInput = window.prompt("Charge to: 0 Customer, 1 Company, 2 Supplier, 3 Employee, 4 Warranty", String(disposition.chargeTo));
    if (chargeToInput === null) return;
    const chargeTo = Number(chargeToInput);
    if (!Number.isInteger(chargeTo) || chargeTo < 0 || chargeTo > 4) {
      setError("Charge to must be 0, 1, 2, 3, or 4.");
      return;
    }
    const responsiblePerson = window.prompt("Responsible person", disposition.responsiblePerson ?? "");
    if (responsiblePerson === null) return;
    const supplierReturnId = window.prompt("Supplier return ID", disposition.supplierReturnId ?? "");
    if (supplierReturnId === null) return;

    setError(null);
    setBusy("edit");
    try {
      await apiPutNoContent(`service/jobs/${serviceJobId}/material-dispositions/${dispositionId}`, {
        condition: condition.trim(),
        reason: reason.trim(),
        chargeTo,
        supplierReturnId: supplierReturnId.trim() || null,
        responsiblePerson: responsiblePerson.trim() || null,
      });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  async function voidDisposition() {
    const reason = window.prompt("Reason for voiding this material disposition?");
    if (reason === null) return;

    setError(null);
    setBusy("void");
    try {
      await apiPostNoContent(`service/jobs/${serviceJobId}/material-dispositions/${dispositionId}/void`, { reason: reason.trim() || null });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  async function postDisposition() {
    setError(null);
    setBusy("post");
    try {
      await apiPostNoContent(`service/jobs/${serviceJobId}/material-dispositions/${dispositionId}/post`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="space-y-1">
      <div className="flex flex-wrap gap-2">
        <SecondaryButton type="button" disabled={disabled || !isDraft || busy !== null} onClick={editDetails}>
          {busy === "edit" ? "Saving..." : "Edit Details"}
        </SecondaryButton>
        <SecondaryButton type="button" disabled={disabled || !isDraft || busy !== null} onClick={postDisposition}>
          {busy === "post" ? "Posting..." : "Post"}
        </SecondaryButton>
        <SecondaryButton type="button" disabled={disabled || disposition.isVoided || busy !== null} onClick={voidDisposition}>
          {busy === "void" ? "Voiding..." : "Void"}
        </SecondaryButton>
      </div>
      {error ? <div className="text-xs text-red-600 dark:text-red-300">{error}</div> : null}
    </div>
  );
}
