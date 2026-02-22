import type { ComponentProps } from "react";

export function Card(props: ComponentProps<"div">) {
  const { className, ...rest } = props;
  return (
    <div
      className={[
        "rounded-xl border border-zinc-200 bg-white p-4 shadow-sm dark:border-zinc-800 dark:bg-zinc-950",
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
        "rounded-md bg-zinc-900 px-3 py-2 text-sm font-semibold text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-white",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}

export function SecondaryButton(props: ComponentProps<"button">) {
  const { className, ...rest } = props;
  return (
    <button
      className={[
        "rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm font-medium text-zinc-900 hover:bg-zinc-50 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100 dark:hover:bg-zinc-800",
        className ?? "",
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
        "inline-block rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm font-medium text-zinc-900 hover:bg-zinc-50 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100 dark:hover:bg-zinc-800",
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
        "w-full rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-zinc-900/10 dark:border-zinc-700 dark:bg-zinc-900 dark:focus:ring-zinc-100/10",
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
        "w-full min-h-24 rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-zinc-900/10 dark:border-zinc-700 dark:bg-zinc-900 dark:focus:ring-zinc-100/10",
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
        "w-full rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-zinc-900/10 dark:border-zinc-700 dark:bg-zinc-900 dark:focus:ring-zinc-100/10",
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
        "w-full border-collapse text-sm",
        className ?? "",
      ].join(" ")}
      {...rest}
    />
  );
}
