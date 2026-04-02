import Link from "next/link";

const actionLinkClassName =
  "font-semibold text-[var(--link)] underline underline-offset-2 decoration-[color:var(--link)]/45 transition-colors hover:text-[var(--link-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)]";

type ListViewEditActionsProps = {
  viewHref: string;
  editHref?: string;
  canEdit?: boolean;
};

export function ListViewEditActions({
  viewHref,
  editHref = viewHref,
  canEdit = true,
}: ListViewEditActionsProps) {
  return (
    <div className="flex flex-wrap gap-3 text-xs">
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
    </div>
  );
}
