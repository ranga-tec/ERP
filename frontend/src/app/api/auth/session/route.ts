import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { ISS_TOKEN_COOKIE } from "@/lib/env";
import { sessionFromToken } from "@/lib/jwt";

export const runtime = "nodejs";

export async function GET() {
  const cookieStore = await cookies();
  const token = cookieStore.get(ISS_TOKEN_COOKIE)?.value;
  if (!token) {
    return NextResponse.json({ session: null });
  }

  const session = sessionFromToken(token);
  return NextResponse.json({ session });
}
