import Link from "next/link";
import { Card } from "@/components/ui";

export default function PettyCashFundNotFound() {
  return (
    <Card>
      <h1 className="text-xl font-semibold">Petty cash fund not found</h1>
      <p className="mt-2 text-sm text-zinc-500">
        This fund no longer exists, or the saved link points to an older local database record.
      </p>
      <Link className="mt-4 inline-block text-sm font-medium underline" href="/finance/petty-cash">
        Return to Petty Cash Funds
      </Link>
    </Card>
  );
}
