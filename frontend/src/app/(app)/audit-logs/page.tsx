import { backendFetchJson } from "@/lib/backend.server";
import { AuditLogTable, type AuditLogDto } from "./AuditLogTable";

export default async function AuditLogsPage() {
  const logs = await backendFetchJson<AuditLogDto[]>("/audit-logs?take=200");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Audit Logs</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Recent data changes captured by the backend, shown as readable before/after field differences.
        </p>
      </div>

      <AuditLogTable logs={logs} />
    </div>
  );
}
