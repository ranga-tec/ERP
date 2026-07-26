"use client";

import { useEffect, useState, type ReactNode } from "react";
import { Button, Input, SecondaryButton } from "@/components/ui";

type ConfirmActionDialogProps = {
  open: boolean;
  title: string;
  description: ReactNode;
  confirmWord: string;
  confirmLabel: string;
  busy?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
};

/**
 * Guards an irreversible action behind a typed word, so it cannot be triggered by a stray
 * click. Same idea as the COMPLETE confirmation on a service job.
 */
export function ConfirmActionDialog({ open, ...rest }: ConfirmActionDialogProps) {
  // the body only mounts while open, so the typed value resets on its own
  return open ? <ConfirmActionDialogBody {...rest} /> : null;
}

function ConfirmActionDialogBody({
  title,
  description,
  confirmWord,
  confirmLabel,
  busy = false,
  onCancel,
  onConfirm,
}: Omit<ConfirmActionDialogProps, "open">) {
  const [typed, setTyped] = useState("");

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && !busy) {
        onCancel();
      }
    }

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [busy, onCancel]);

  const matches = typed.trim().toUpperCase() === confirmWord.toUpperCase();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4">
      <div role="dialog" aria-modal="true" className="w-full max-w-md rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl">
        <div className="text-base font-semibold">{title}</div>
        <div className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
          {description}{" "}
          Type <span className="font-mono font-semibold text-zinc-900 dark:text-zinc-100">{confirmWord}</span> to continue.
        </div>
        <label className="mt-4 block text-sm font-medium">Confirmation</label>
        <Input className="mt-1" value={typed} onChange={(event) => setTyped(event.target.value)} autoFocus disabled={busy} />
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <SecondaryButton type="button" disabled={busy} onClick={onCancel}>
            Cancel
          </SecondaryButton>
          <Button type="button" disabled={busy || !matches} onClick={onConfirm}>
            {busy ? "Working..." : confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}
