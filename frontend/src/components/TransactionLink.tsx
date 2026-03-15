import type { ReactNode } from "react";
import { MaybeInlineLink } from "@/components/InlineLink";
import { getReferenceHref } from "@/lib/reference-routing.server";

type TransactionLinkProps = {
  referenceType: string;
  referenceId?: string | null;
  children: ReactNode;
  className?: string;
  fallbackClassName?: string;
  monospace?: boolean;
  title?: string;
};

export async function TransactionLink({
  referenceType,
  referenceId,
  children,
  className,
  fallbackClassName,
  monospace = false,
  title,
}: TransactionLinkProps) {
  const href = await getReferenceHref(referenceType, referenceId);

  return (
    <MaybeInlineLink
      href={href}
      className={className}
      fallbackClassName={fallbackClassName}
      monospace={monospace}
      title={title}
    >
      {children}
    </MaybeInlineLink>
  );
}
