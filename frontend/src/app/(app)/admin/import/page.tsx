import { Card } from "@/components/ui";
import { ExcelImportForm } from "./ExcelImportForm";

export default function AdminImportPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Admin · Import</h1>
        <p className="mt-1 text-sm text-zinc-500">Import master data from Excel templates.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Excel import</div>
        <ExcelImportForm />
      </Card>
    </div>
  );
}

