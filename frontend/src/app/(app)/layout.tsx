import { cookies } from "next/headers";
import { AppShell } from "@/components/AppShell";
import { NEUEDGE_TOKEN_COOKIE } from "@/lib/env";
import { sessionFromToken } from "@/lib/jwt";

export default async function AppLayout({ children }: { children: React.ReactNode }) {
  const cookieStore = await cookies();
  const token = cookieStore.get(NEUEDGE_TOKEN_COOKIE)?.value;
  const session = token ? sessionFromToken(token) : null;

  return (
    <AppShell
      email={session?.email ?? "Signed in"}
      roles={session?.roles ?? []}
    >
      {children}
    </AppShell>
  );
}
