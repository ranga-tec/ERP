import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { NEUEDGE_TOKEN_COOKIE, neuedgeSecureCookies } from "@/lib/env";

export const runtime = "nodejs";

export async function POST() {
  const cookieStore = await cookies();
  cookieStore.set(NEUEDGE_TOKEN_COOKIE, "", {
    httpOnly: true,
    sameSite: "lax",
    secure: neuedgeSecureCookies(),
    path: "/",
    maxAge: 0,
  });
  return NextResponse.json({ ok: true });
}
