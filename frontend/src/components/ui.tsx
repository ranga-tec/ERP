import type { ComponentProps, ReactNode } from "react";

function buttonLabelText(children: ReactNode): string {
  if (typeof children === "string" || typeof children === "number") {
    return String(children);
  }

  if (Array.isArray(children)) {
    return children.map((child) => buttonLabelText(child)).join(" ");
  }

  return "";
}

export function Card(props: ComponentProps<"div">) {
  const { className, ...rest } = props;
  return (
    <div
      className={[
        "rounded-lg border border-[var(--card-border)] bg-[var(--card-bg)] p-3 shadow-[var(--shadow-card)] transition-colors duration-150",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}

export function Button(props: ComponentProps<"button">) {
  const { className, ...rest } = props;
  return (
    <button
      className={[
        "inline-flex min-h-8 items-center justify-center rounded-md border border-transparent bg-[var(--accent)] px-3 py-1.5 text-[13px] font-semibold text-[var(--accent-contrast)] shadow-[var(--shadow-button)] transition-colors duration-150 hover:bg-[var(--accent-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] disabled:cursor-not-allowed disabled:opacity-55",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}

export function SecondaryButton(props: ComponentProps<"button">) {
  const { className, ...rest } = props;
  const label = buttonLabelText(rest.children).replace(/\s+/g, " ").trim().toLowerCase();
  const isInlineAction = label === "edit" || label === "delete";
  return (
    <button
      className={[
        "inline-flex min-h-8 items-center justify-center rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-3 py-1.5 text-[13px] font-medium text-[var(--foreground)] shadow-[var(--shadow-control)] transition-colors duration-150 hover:bg-[var(--surface-soft)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] disabled:cursor-not-allowed disabled:opacity-55",
        className ?? "",
        isInlineAction
          ? "h-auto min-h-0 rounded-none border-0 bg-transparent p-0 text-[12px] font-semibold text-[var(--link)] underline underline-offset-2 shadow-none hover:bg-transparent hover:text-[var(--link-hover)]"
          : "",
      ].join(" ")}
      {...rest}
    />
  );
}

export function SecondaryLink(props: ComponentProps<"a">) {
  const { className, ...rest } = props;
  return (
    <a
      className={[
        "inline-flex min-h-8 items-center justify-center rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-3 py-1.5 text-[13px] font-medium text-[var(--foreground)] shadow-[var(--shadow-control)] transition-colors duration-150 hover:bg-[var(--surface-soft)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] disabled:cursor-not-allowed disabled:opacity-55",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}

export function Input(props: ComponentProps<"input">) {
  const { className, ...rest } = props;
  return (
    <input
      className={[
        "w-full rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-1.5 text-[13px] text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}

export function Textarea(props: ComponentProps<"textarea">) {
  const { className, ...rest } = props;
  return (
    <textarea
      className={[
        "w-full min-h-20 rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-1.5 text-[13px] text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}

export function Select(props: ComponentProps<"select">) {
  const { className, ...rest } = props;
  return (
    <select
      className={[
        "w-full rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-1.5 text-[13px] text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}

export function Table(props: ComponentProps<"table">) {
  const { className, ...rest } = props;
  return (
    <table
      className={[
        "app-table w-full border-separate border-spacing-0 text-[13px]",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}
