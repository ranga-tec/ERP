import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";

type AuditLogDto = {
  id: string;
  occurredAt: string;
  userId?: string | null;
  tableName: string;
  action: number;
  key: string;
  changesJson: string;
};

const actionLabel: Record<number, string> = {
  1: "Insert",
  2: "Update",
  3: "Delete",
};

export default async function AuditLogsPage() {
  const logs = await backendFetchJson<AuditLogDto[]>("/audit-logs?take=200");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Audit Logs</h1>
        <p className="mt-1 text-sm text-zinc-500">Recent data changes captured by the backend.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Recent</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">When</th>
                <th className="py-2 pr-3">User</th>
                <th className="py-2 pr-3">Table</th>
                <th className="py-2 pr-3">Action</th>
                <th className="py-2 pr-3">Key</th>
                <th className="py-2 pr-3">Changes</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((l) => (
                <tr key={l.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 text-zinc-500">{new Date(l.occurredAt).toLocaleString()}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">{l.userId ? l.userId.slice(0, 8) : "—"}</td>
                  <td className="py-2 pr-3">{l.tableName}</td>
                  <td className="py-2 pr-3">{actionLabel[l.action] ?? l.action}</td>
                  <td className="py-2 pr-3 font-mono text-xs text-zinc-600 dark:text-zinc-400">{l.key}</td>
                  <td className="py-2 pr-3">
                    <details>
                      <summary className="cursor-pointer text-sm text-zinc-700 hover:underline dark:text-zinc-200">View</summary>
                      <pre className="mt-2 max-w-xl overflow-auto rounded-md border border-zinc-200 bg-zinc-50 p-2 text-xs text-zinc-800 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-100">
                        {l.changesJson}
                      </pre>
                    </details>
                  </td>
                </tr>
              ))}
              {logs.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={6}>
                    No audit logs yet.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </Table>
        </div>
      </Card>
    </div>
  );
}

