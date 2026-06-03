"use client";

import type { ReactNode } from "react";
import { useEffect, useId, useState } from "react";
import { Button, SecondaryButton } from "@/components/ui";

type AppFormModalSize = "md" | "lg" | "xl";
type AppFormModalVariant = "primary" | "secondary";

const sizeClass: Record<AppFormModalSize, string> = {
  md: "max-w-2xl",
  lg: "max-w-4xl",
  xl: "max-w-6xl",
};

export function AppFormModal({
  title,
  description,
  buttonLabel,
  children,
  disabled = false,
  variant = "primary",
  size = "lg",
}: {
  title: string;
  description?: string;
  buttonLabel: string;
  children: ReactNode;
  disabled?: boolean;
  variant?: AppFormModalVariant;
  size?: AppFormModalSize;
}) {
  const [open, setOpen] = useState(false);
  const titleId = useId();
  const Trigger = variant === "primary" ? Button : SecondaryButton;

  useEffect(() => {
    if (!open) return;

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [open]);

  return (
    <>
      <Trigger type="button" disabled={disabled} onClick={() => setOpen(true)}>
        {buttonLabel}
      </Trigger>

      {open ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-zinc-950/55 p-4" onMouseDown={() => setOpen(false)}>
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby={titleId}
            className={[
              "max-h-[88vh] w-full overflow-auto rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-xl",
              sizeClass[size],
            ].join(" ")}
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="mb-4 flex items-start justify-between gap-3 border-b border-[var(--card-border)] pb-3">
              <div>
                <div id={titleId} className="text-base font-semibold">
                  {title}
                </div>
                {description ? <div className="mt-1 text-sm text-zinc-500">{description}</div> : null}
              </div>
              <SecondaryButton type="button" aria-label="Close dialog" onClick={() => setOpen(false)}>
                Close
              </SecondaryButton>
            </div>
            {children}
          </div>
        </div>
      ) : null}
    </>
  );
}
