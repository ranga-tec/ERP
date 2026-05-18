import Link from "next/link";
import { AuditTrailButton } from "@/components/AuditTrailButton";

const actionLinkClassName =
  "font-semibold text-[var(--link)] underline underline-offset-2 decoration-[color:var(--link)]/45 transition-colors hover:text-[var(--link-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]";

type ListViewEditActionsProps = {
  viewHref: string;
  editHref?: string;
  canEdit?: boolean;
  auditTableName?: string;
  auditRecordId?: string;
};

function buildDefaultEditHref(viewHref: string): string {
  return viewHref.includes("?") ? `${viewHref}&mode=edit` : `${viewHref}?mode=edit`;
}

export function ListViewEditActions({
  viewHref,
  editHref = buildDefaultEditHref(viewHref),
  canEdit = true,
  auditTableName,
  auditRecordId,
}: ListViewEditActionsProps) {
  return (
    <div className="flex flex-wrap items-center gap-3 text-xs">
      <Link className={actionLinkClassName} href={viewHref}>
        View
      </Link>
      {canEdit ? (
        <Link className={actionLinkClassName} href={editHref}>
          Edit
        </Link>
      ) : (
        <span className="text-zinc-400">Edit</span>
      )}
      {auditTableName && auditRecordId ? <AuditTrailButton tableName={auditTableName} recordId={auditRecordId} /> : null}
    </div>
  );
}
