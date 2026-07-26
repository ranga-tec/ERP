const dateFormat: Intl.DateTimeFormatOptions = {
  year: "numeric",
  month: "short",
  day: "2-digit",
};

const timeFormat: Intl.DateTimeFormatOptions = {
  ...dateFormat,
  hour: "2-digit",
  minute: "2-digit",
};

/**
 * Shows an audit date in a list cell, with the responsible user tucked behind a
 * disclosure so the table keeps one column per timestamp instead of two.
 * Renders a plain date when there is no user to reveal.
 */
export function AuditStamp({ at, by }: { at?: string | null; by?: string | null }) {
  if (!at) {
    return <span className="text-zinc-400">-</span>;
  }

  const date = new Date(at);
  const shortDate = date.toLocaleDateString(undefined, dateFormat);
  const fullDate = date.toLocaleString(undefined, timeFormat);

  if (!by) {
    return (
      <span className="whitespace-nowrap text-zinc-500" title={fullDate}>
        {shortDate}
      </span>
    );
  }

  return (
    <details className="group">
      <summary
        className="cursor-pointer list-none whitespace-nowrap text-zinc-500 underline decoration-dotted underline-offset-2 hover:text-[var(--foreground)]"
        title={fullDate}
      >
        {shortDate}
        <span aria-hidden="true" className="ml-1 text-[10px] text-zinc-400 group-open:hidden">
          ▸
        </span>
        <span aria-hidden="true" className="ml-1 hidden text-[10px] text-zinc-400 group-open:inline">
          ▾
        </span>
      </summary>
      <div className="mt-1 text-[11px] leading-tight text-zinc-500">
        <div className="whitespace-nowrap">{fullDate}</div>
        <div className="whitespace-nowrap font-medium text-[var(--foreground)]">by {by}</div>
      </div>
    </details>
  );
}
