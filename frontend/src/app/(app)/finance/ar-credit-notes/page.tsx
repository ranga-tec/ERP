import { backendFetchJson } from "@/lib/backend.server";
import { AppFormModal } from "@/components/AppFormModal";
import { Card } from "@/components/ui";
import { CreditNoteCreateForm } from "../credit-notes/CreditNoteCreateForm";
import { CreditNoteListTable, type CreditNoteListDto } from "../credit-notes/CreditNoteListTable";

type CustomerDto = { id: string; code: string; name: string };
type CurrentPermissionsDto = { permissions: string[] };

export default async function ArCreditNotesPage() {
  const [notes, customers, currentPermissions] = await Promise.all([
    backendFetchJson<CreditNoteListDto[]>("/finance/credit-notes?counterpartyType=1"),
    backendFetchJson<CustomerDto[]>("/customers"),
    backendFetchJson<CurrentPermissionsDto>("/me/permissions"),
  ]);

  const canCreate = new Set(currentPermissions.permissions).has("Finance.CreditNote.Create");

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">A/R Credit Notes</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Customer credit notes reduce accounts receivable balances and can be based on customer returns.
          </p>
        </div>
        {canCreate ? (
          <AppFormModal title="Create A/R Credit Note" description="Create a customer credit note and allocate it against AR." buttonLabel="+ New A/R Credit Note" size="xl">
            <CreditNoteCreateForm
              customers={customers}
              suppliers={[]}
              fixedCounterpartyType="1"
              submitLabel="Create A/R Credit Note"
            />
          </AppFormModal>
        ) : null}
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">A/R Credit Notes</div>
        <CreditNoteListTable notes={notes} counterparties={customers} emptyText="No A/R credit notes yet." />
      </Card>
    </div>
  );
}
