import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { NEUEDGE_TOKEN_COOKIE, neuedgeSecureCookies } from "@/lib/env";
import { isJwtExpired, sessionFromToken } from "@/lib/jwt";

export const runtime = "nodejs";

export async function GET() {
  const cookieStore = await cookies();
  const token = cookieStore.get(NEUEDGE_TOKEN_COOKIE)?.value;
  if (!token) {
    return NextResponse.json({ session: null });
  }

  if (isJwtExpired(token)) {
    const resp = NextResponse.json({ session: null });
    resp.cookies.set(NEUEDGE_TOKEN_COOKIE, "", {
      httpOnly: true,
      sameSite: "lax",
      secure: neuedgeSecureCookies(),
      path: "/",
      maxAge: 0,
    });
    return resp;
  }

  const session = sessionFromToken(token);
  return NextResponse.json({ session });
}
