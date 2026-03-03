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
        "rounded-2xl border border-[var(--card-border)] bg-[var(--card-bg)] p-4 shadow-[var(--shadow-card)] backdrop-blur-sm transition-all duration-200",
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
        "inline-flex items-center justify-center rounded-xl bg-[var(--accent)] px-3.5 py-2 text-sm font-semibold text-[var(--accent-contrast)] shadow-[var(--shadow-button)] transition-all duration-200 hover:-translate-y-px hover:bg-[var(--accent-hover)] hover:shadow-[var(--shadow-button)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] disabled:cursor-not-allowed disabled:opacity-55",
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
        "inline-flex items-center justify-center rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3.5 py-2 text-sm font-medium text-[var(--foreground)] shadow-[var(--shadow-control)] transition-all duration-200 hover:-translate-y-px hover:bg-[var(--surface-soft)] hover:shadow-[var(--shadow-soft)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] disabled:cursor-not-allowed disabled:opacity-55",
        className ?? "",
        isInlineAction
          ? "h-auto rounded-none border-0 bg-transparent p-0 text-xs font-semibold text-[var(--link)] underline underline-offset-2 shadow-none hover:bg-transparent hover:text-[var(--link-hover)]"
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
        "inline-flex items-center justify-center rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3.5 py-2 text-sm font-medium text-[var(--foreground)] shadow-[var(--shadow-control)] transition-all duration-200 hover:-translate-y-px hover:bg-[var(--surface-soft)] hover:shadow-[var(--shadow-soft)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] disabled:cursor-not-allowed disabled:opacity-55",
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
        "w-full rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80",
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
        "w-full min-h-24 rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80",
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
        "w-full rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]",
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
        "w-full border-separate border-spacing-0 text-sm",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}
