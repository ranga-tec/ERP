import Link from "next/link";
import { AppFormModal } from "@/components/AppFormModal";
import { AuditTrailButton } from "@/components/AuditTrailButton";

const actionLinkClassName =
  "font-semibold text-[var(--link)] underline underline-offset-2 decoration-[color:var(--link)]/45 transition-colors hover:text-[var(--link-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]";

type ListViewEditActionsProps = {
  viewHref: string;
  editHref?: string;
  canEdit?: boolean;
  editInModal?: boolean;
  editModalTitle?: string;
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
  editInModal = false,
  editModalTitle = "Edit Document",
  auditTableName,
  auditRecordId,
}: ListViewEditActionsProps) {
  return (
    <div className="flex flex-wrap items-center gap-3 text-xs">
      <Link className={actionLinkClassName} href={viewHref}>
        View
      </Link>
      {canEdit && editInModal ? (
        <AppFormModal title={editModalTitle} description="Edit this draft document without leaving the list." buttonLabel="Edit" variant="secondary" size="xl">
          <div className="h-[72vh] overflow-hidden rounded-md border border-[var(--card-border)]">
            <iframe title={editModalTitle} src={editHref} className="h-full w-full bg-[var(--page-bg)]" />
          </div>
        </AppFormModal>
      ) : canEdit ? (
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
