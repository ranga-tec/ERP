import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { CreditNoteCreateForm } from "../credit-notes/CreditNoteCreateForm";
import { CreditNoteListTable, type CreditNoteListDto } from "../credit-notes/CreditNoteListTable";

type SupplierDto = { id: string; code: string; name: string };
type CurrentPermissionsDto = { permissions: string[] };

export default async function ApCreditNotesPage() {
  const [notes, suppliers, currentPermissions] = await Promise.all([
    backendFetchJson<CreditNoteListDto[]>("/finance/credit-notes?counterpartyType=2"),
    backendFetchJson<SupplierDto[]>("/suppliers"),
    backendFetchJson<CurrentPermissionsDto>("/me/permissions"),
  ]);

  const canCreate = new Set(currentPermissions.permissions).has("Finance.CreditNote.Create");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">A/P Credit Notes</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Supplier credit notes reduce accounts payable balances and can be based on supplier returns.
        </p>
      </div>

      {canCreate ? (
        <Card>
          <div className="mb-3 text-sm font-semibold">Create A/P Credit Note</div>
          <CreditNoteCreateForm
            customers={[]}
            suppliers={suppliers}
            fixedCounterpartyType="2"
            submitLabel="Create A/P Credit Note"
          />
        </Card>
      ) : null}

      <Card>
        <div className="mb-3 text-sm font-semibold">A/P Credit Notes</div>
        <CreditNoteListTable notes={notes} counterparties={suppliers} emptyText="No A/P credit notes yet." />
      </Card>
    </div>
  );
}
