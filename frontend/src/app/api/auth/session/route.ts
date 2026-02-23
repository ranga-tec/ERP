import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { ISS_TOKEN_COOKIE } from "@/lib/env";
import { isJwtExpired, sessionFromToken } from "@/lib/jwt";

export const runtime = "nodejs";

export async function GET() {
  const cookieStore = await cookies();
  const token = cookieStore.get(ISS_TOKEN_COOKIE)?.value;
  if (!token) {
    return NextResponse.json({ session: null });
  }

  if (isJwtExpired(token)) {
    const resp = NextResponse.json({ session: null });
    resp.cookies.delete(ISS_TOKEN_COOKIE);
    return resp;
  }

  const session = sessionFromToken(token);
  return NextResponse.json({ session });
}
