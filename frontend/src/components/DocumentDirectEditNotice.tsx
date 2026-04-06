import { Card, SecondaryLink } from "@/components/ui";

export function DocumentDirectEditNotice({ addLineHref }: { addLineHref: string }) {
  return (
    <Card>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="max-w-3xl text-sm text-zinc-500">
          Existing lines are already open in edit mode. The separate add-line form is hidden here so it does not look like a
          blank saved row.
        </div>
        <SecondaryLink href={addLineHref}>Switch to Add Line</SecondaryLink>
      </div>
    </Card>
  );
}
