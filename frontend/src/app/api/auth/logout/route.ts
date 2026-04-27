import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { ISS_TOKEN_COOKIE, issSecureCookies } from "@/lib/env";

export const runtime = "nodejs";

export async function POST() {
  const cookieStore = await cookies();
  cookieStore.set(ISS_TOKEN_COOKIE, "", {
    httpOnly: true,
    sameSite: "lax",
    secure: issSecureCookies(),
    path: "/",
    maxAge: 0,
  });
  return NextResponse.json({ ok: true });
}
