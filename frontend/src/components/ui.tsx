import type { ChangeEventHandler, ComponentProps, ReactNode } from "react";
export { Select } from "./SearchableSelect";

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

export function sanitizeDecimalInput(value: string, decimalScale = 4) {
  const scale = Math.max(0, Math.trunc(decimalScale));
  const cleaned = value.replace(/,/g, "").replace(/[^\d.]/g, "");
  const decimalIndex = cleaned.indexOf(".");

  if (decimalIndex < 0 || scale === 0) {
    return cleaned.replace(/\./g, "");
  }

  const integerPart = cleaned.slice(0, decimalIndex) || "0";
  const decimalPart = cleaned.slice(decimalIndex + 1).replace(/\./g, "").slice(0, scale);
  return `${integerPart}.${decimalPart}`;
}

type DecimalInputProps = Omit<ComponentProps<"input">, "type" | "inputMode" | "onChange"> & {
  decimalScale?: number;
  onChange?: ChangeEventHandler<HTMLInputElement>;
};

/**
 * Non-negative decimal input used for quantities, hours, rates, percentages, and money.
 * It stays a text control so exponent notation and signs cannot bypass the character filter.
 */
export function DecimalInput({ decimalScale = 4, onChange, ...rest }: DecimalInputProps) {
  return (
    <Input
      {...rest}
      type="text"
      inputMode="decimal"
      pattern={`[0-9]*([.][0-9]{0,${Math.max(0, Math.trunc(decimalScale))}})?`}
      onChange={(event) => {
        event.currentTarget.value = sanitizeDecimalInput(event.currentTarget.value, decimalScale);
        onChange?.(event);
      }}
    />
  );
}

type IntegerInputProps = Omit<ComponentProps<"input">, "type" | "inputMode" | "onChange"> & {
  onChange?: ChangeEventHandler<HTMLInputElement>;
};

/** Non-negative whole-number input with the same paste and typing protection as DecimalInput. */
export function IntegerInput({ onChange, ...rest }: IntegerInputProps) {
  return (
    <Input
      {...rest}
      type="text"
      inputMode="numeric"
      pattern="[0-9]*"
      onChange={(event) => {
        event.currentTarget.value = event.currentTarget.value.replace(/\D/g, "");
        onChange?.(event);
      }}
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
