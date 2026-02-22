import { backendFetchJson } from "@/lib/backend.server";
import { Card, Table } from "@/components/ui";
import { UserCreateForm } from "./UserCreateForm";
import { UserRowActions } from "./UserRowActions";

type UserDto = {
  id: string;
  email: string;
  displayName?: string | null;
  isLocked: boolean;
  lockoutEnd?: string | null;
  roles: string[];
};

export default async function AdminUsersPage() {
  const users = await backendFetchJson<UserDto[]>("/admin/users?take=200");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Admin · Users</h1>
        <p className="mt-1 text-sm text-zinc-500">Create users, assign roles, and manage access.</p>
      </div>

      <Card>
        <div className="mb-3 text-sm font-semibold">Create</div>
        <UserCreateForm />
      </Card>

      <Card>
        <div className="mb-3 text-sm font-semibold">List</div>
        <div className="overflow-auto">
          <Table>
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800">
                <th className="py-2 pr-3">Email</th>
                <th className="py-2 pr-3">Name</th>
                <th className="py-2 pr-3">Roles</th>
                <th className="py-2 pr-3">Status</th>
                <th className="py-2 pr-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id} className="border-b border-zinc-100 align-top dark:border-zinc-900">
                  <td className="py-2 pr-3 font-mono text-xs">{u.email}</td>
                  <td className="py-2 pr-3">{u.displayName ?? "—"}</td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap gap-1">
                      {(u.roles?.length ? u.roles : ["(none)"]).map((r) => (
                        <span
                          key={r}
                          className="rounded-md border border-zinc-200 bg-white px-2 py-0.5 text-xs dark:border-zinc-800 dark:bg-zinc-950"
                        >
                          {r}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="py-2 pr-3 text-sm">
                    {u.isLocked ? (
                      <div>
                        <div className="font-medium text-red-700 dark:text-red-300">Disabled</div>
                        {u.lockoutEnd ? (
                          <div className="text-xs text-zinc-500">
                            Until {new Date(u.lockoutEnd).toLocaleString()}
                          </div>
                        ) : null}
                      </div>
                    ) : (
                      <div className="font-medium text-emerald-700 dark:text-emerald-300">Active</div>
                    )}
                  </td>
                  <td className="py-2 pr-3">
                    <UserRowActions userId={u.id} initialRoles={u.roles ?? []} isLocked={u.isLocked} />
                  </td>
                </tr>
              ))}
              {users.length === 0 ? (
                <tr>
                  <td className="py-6 text-sm text-zinc-500" colSpan={5}>
                    No users found.
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

