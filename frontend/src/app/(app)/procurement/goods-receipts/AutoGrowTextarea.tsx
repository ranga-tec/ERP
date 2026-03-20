"use client";

import { useEffect, useRef, type ComponentProps } from "react";

export function AutoGrowTextarea(props: ComponentProps<"textarea">) {
  const { className, onInput, rows, value, ...rest } = props;
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  useEffect(() => {
    const element = textareaRef.current;
    if (!element) {
      return;
    }

    element.style.height = "0px";
    element.style.height = `${element.scrollHeight}px`;
  }, [value]);

  return (
    <textarea
      {...rest}
      ref={textareaRef}
      value={value}
      rows={rows ?? 1}
      onInput={(event) => {
        const element = event.currentTarget;
        element.style.height = "0px";
        element.style.height = `${element.scrollHeight}px`;
        onInput?.(event);
      }}
      className={[
        "w-full resize-none overflow-hidden rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80",
        className ?? "",
      ].join(" ")}
    />
  );
}
