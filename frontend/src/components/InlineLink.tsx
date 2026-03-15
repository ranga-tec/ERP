import Link from "next/link";
import type { ReactNode } from "react";
import { buildItemHref } from "@/lib/item-routing";

const inlineLinkClassName = "text-[var(--link)] underline underline-offset-2 decoration-[color:var(--link)]/45 transition-colors hover:text-[var(--link-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]";
const inlineReferenceLinkClassName = `font-mono text-xs ${inlineLinkClassName}`;

function defaultLinkClassName(monospace: boolean): string {
  return monospace ? inlineReferenceLinkClassName : inlineLinkClassName;
}

function defaultFallbackClassName(monospace: boolean): string {
  return monospace ? "font-mono text-xs text-zinc-500" : "text-zinc-500";
}

type MaybeInlineLinkProps = {
  href?: string | null;
  children: ReactNode;
  className?: string;
  fallbackClassName?: string;
  monospace?: boolean;
  title?: string;
};

export function MaybeInlineLink({
  href,
  children,
  className,
  fallbackClassName,
  monospace = false,
  title,
}: MaybeInlineLinkProps) {
  if (!href) {
    return (
      <span className={fallbackClassName ?? className ?? defaultFallbackClassName(monospace)} title={title}>
        {children}
      </span>
    );
  }

  return (
    <Link
      href={href}
      className={className ?? defaultLinkClassName(monospace)}
      title={title}
    >
      {children}
    </Link>
  );
}

type ItemInlineLinkProps = Omit<MaybeInlineLinkProps, "href"> & {
  itemId?: string | null;
};

export function ItemInlineLink({ itemId, ...props }: ItemInlineLinkProps) {
  return <MaybeInlineLink href={itemId ? buildItemHref(itemId) : null} {...props} />;
}
