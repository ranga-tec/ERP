import Link from "next/link";
import { backendFetchJson } from "@/lib/backend.server";
import { Card } from "@/components/ui";
import { NotificationMarkAllReadButton, NotificationMarkReadButton } from "./NotificationActions";

type UserNotificationDto = {
  id: string;
  title: string;
  message: string;
  href?: string | null;
  createdAt: string;
  readAt?: string | null;
  referenceType?: string | null;
  referenceId?: string | null;
};

export default async function NotificationsPage() {
  const notifications = await backendFetchJson<UserNotificationDto[]>("/notifications?take=100");
  const unreadCount = notifications.filter((notification) => !notification.readAt).length;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Notifications</h1>
          <p className="mt-1 text-sm text-zinc-500">Requests, approvals, and workflow updates assigned to you.</p>
        </div>
        {unreadCount > 0 ? <NotificationMarkAllReadButton /> : null}
      </div>

      <Card>
        <div className="space-y-3">
          {notifications.map((notification) => (
            <div
              key={notification.id}
              className={[
                "rounded-md border p-3",
                notification.readAt
                  ? "border-[var(--card-border)] bg-[var(--card-bg)]"
                  : "border-blue-200 bg-blue-50/70 dark:border-blue-900/40 dark:bg-blue-950/20",
              ].join(" ")}
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <div className="text-sm font-semibold">{notification.title}</div>
                  <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-300">{notification.message}</div>
                  <div className="mt-2 text-xs text-zinc-500">
                    {new Date(notification.createdAt).toLocaleString()}
                    {notification.readAt ? ` / Read ${new Date(notification.readAt).toLocaleString()}` : ""}
                  </div>
                </div>
                <div className="flex flex-wrap gap-2">
                  {notification.href ? (
                    <Link className="text-sm font-semibold text-[var(--link)] underline underline-offset-2" href={notification.href}>
                      Open
                    </Link>
                  ) : null}
                  {!notification.readAt ? <NotificationMarkReadButton id={notification.id} /> : null}
                </div>
              </div>
            </div>
          ))}

          {notifications.length === 0 ? (
            <div className="py-8 text-center text-sm text-zinc-500">No notifications yet.</div>
          ) : null}
        </div>
      </Card>
    </div>
  );
}
