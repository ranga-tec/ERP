import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { NotificationRetryButton } from "./NotificationRetryButton";

type NotificationDto = {
  id: string;
  channel: number;
  recipient: string;
  subject?: string | null;
  body: string;
  status: number;
  attempts: number;
  nextAttemptAt: string;
  lastAttemptAt?: string | null;
  sentAt?: string | null;
  lastError?: string | null;
  referenceType?: string | null;
  referenceId?: string | null;
  createdAt: string;
};

const channelLabel: Record<number, string> = { 1: "Email", 2: "SMS" };
const statusLabel: Record<number, string> = {
  0: "Pending",
  1: "Processing",
  2: "Sent",
  3: "Failed",
};

export default async function AdminNotificationsPage() {
  const items = await backendFetchJson<NotificationDto[]>("/admin/notifications?take=200");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Admin · Notifications</h1>
        <p className="mt-1 text-sm text-zinc-500">Notification outbox (email/SMS) with retry controls.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Outbox</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Created</th>
                <th className="py-2 pr-3">Channel</th>
                <th className="py-2 pr-3">Recipient</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Attempts</th>
                <th className="py-2 pr-3">Next</th>
                <th className="py-2 pr-3">Reference</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.map((n) => (
                <tr key={n.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 text-zinc-500">{new Date(n.createdAt).toLocaleString()}</td>
                  <td className="py-2 pr-3">{channelLabel[n.channel] ?? n.channel}</td>
                  <td className="py-2 pr-3">
                    <div className="font-mono text-xs">{n.recipient}</div>
                    {n.subject ? <div className="text-xs text-zinc-500">{n.subject}</div> : null}
                  </td>
                  <td className="py-2 pr-3">
                    <div>{statusLabel[n.status] ?? n.status}</div>
                    {n.lastError ? (
                      <div className="mt-1 max-w-md whitespace-pre-wrap text-xs text-red-600 dark:text-red-400">
                        {n.lastError}
                      </div>
                    ) : null}
                  </td>
                  <td className="py-2 pr-3">{n.attempts}</td>
                  <td className="py-2 pr-3 text-zinc-500">
                    {new Date(n.nextAttemptAt).toLocaleString()}
                  </td>
                  <td className="py-2 pr-3 font-mono text-xs">
                    {n.referenceType ? `${n.referenceType}:${n.referenceId ?? ""}` : "—"}
                  </td>
                  <td className="py-2 pr-3">
                    <NotificationRetryButton id={n.id} />
                  </td>
                </tr>
              ))}
              {items.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={8}>
                    No notifications yet.
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

