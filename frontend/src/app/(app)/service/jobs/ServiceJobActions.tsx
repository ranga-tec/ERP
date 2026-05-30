"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiPostNoContent } from "@/lib/api-client";
import { Button, Input, SecondaryButton } from "@/components/ui";

const COMPLETE_CONFIRMATION = "COMPLETE";

export function ServiceJobActions({
  jobId,
  canStart,
  canComplete,
  canClose,
  canReopen,
  compact = false,
}: {
  jobId: string;
  canStart: boolean;
  canComplete: boolean;
  canClose: boolean;
  canReopen: boolean;
  compact?: boolean;
}) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmCompleteOpen, setConfirmCompleteOpen] = useState(false);
  const [completeConfirmation, setCompleteConfirmation] = useState("");

  async function act(path: string) {
    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`service/jobs/${jobId}/${path}`, {});
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function reopen() {
    const reason = window.prompt("Reason for reopening this closed job?");
    if (reason === null) return;

    setError(null);
    setBusy(true);
    try {
      await apiPostNoContent(`service/jobs/${jobId}/reopen`, { reason: reason.trim() || null });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  }

  async function completeJob() {
    await act("complete");
    setConfirmCompleteOpen(false);
    setCompleteConfirmation("");
  }

  return (
    <div className="space-y-2">
      <div className={compact ? "flex flex-wrap justify-end gap-1.5" : "flex flex-wrap gap-2"}>
        <SecondaryButton className={compact ? "min-h-7 px-2 py-1 text-xs" : undefined} type="button" disabled={!canStart || busy} onClick={() => act("start")}>
          Start
        </SecondaryButton>
        <SecondaryButton className={compact ? "min-h-7 px-2 py-1 text-xs" : undefined} type="button" disabled={!canComplete || busy} onClick={() => setConfirmCompleteOpen(true)}>
          Complete
        </SecondaryButton>
        <SecondaryButton className={compact ? "min-h-7 px-2 py-1 text-xs" : undefined} type="button" disabled={!canClose || busy} onClick={() => act("close")}>
          Close
        </SecondaryButton>
        <SecondaryButton className={compact ? "min-h-7 px-2 py-1 text-xs" : undefined} type="button" disabled={!canReopen || busy} onClick={reopen}>
          Reopen
        </SecondaryButton>
        <SecondaryButton className={compact ? "min-h-7 px-2 py-1 text-xs" : undefined} type="button" disabled={busy} onClick={() => act("refresh-entitlement")}>
          Refresh Entitlement
        </SecondaryButton>
      </div>
      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-2 text-sm text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
          {error}
        </div>
      ) : null}

      {confirmCompleteOpen ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4">
          <div className="w-full max-w-md rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl">
            <div className="text-base font-semibold">Complete Service Job</div>
            <div className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
              Completing the job moves it to work-completed status and changes what users can still add to the job. Type{" "}
              <span className="font-mono font-semibold text-zinc-900 dark:text-zinc-100">{COMPLETE_CONFIRMATION}</span> to continue.
            </div>
            <label className="mt-4 block text-sm font-medium">Confirmation</label>
            <Input
              className="mt-1"
              value={completeConfirmation}
              onChange={(event) => setCompleteConfirmation(event.target.value)}
              autoFocus
              disabled={busy}
            />
            <div className="mt-4 flex flex-wrap justify-end gap-2">
              <SecondaryButton
                type="button"
                disabled={busy}
                onClick={() => {
                  setConfirmCompleteOpen(false);
                  setCompleteConfirmation("");
                }}
              >
                Cancel
              </SecondaryButton>
              <Button
                type="button"
                disabled={busy || completeConfirmation.trim() !== COMPLETE_CONFIRMATION}
                onClick={completeJob}
              >
                {busy ? "Completing..." : "Complete Job"}
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
