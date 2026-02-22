import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { ISS_TOKEN_COOKIE, issApiBaseUrl } from "@/lib/env";

type LoginRequest = { email: string; password: string };
type AuthResponse = { token: string; userId: string; email?: string; roles?: string[] };

export const runtime = "nodejs";

export async function POST(req: Request) {
  const body = (await req.json()) as Partial<LoginRequest>;
  const email = typeof body.email === "string" ? body.email : "";
  const password = typeof body.password === "string" ? body.password : "";

  const resp = await fetch(new URL("/api/auth/login", issApiBaseUrl()), {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!resp.ok) {
    return NextResponse.json(
      { error: "Login failed." },
      { status: resp.status },
    );
  }

  const data = (await resp.json()) as Partial<AuthResponse>;
  if (typeof data.token !== "string" || data.token.length === 0) {
    return NextResponse.json({ error: "Invalid auth response." }, { status: 502 });
  }

  const cookieStore = await cookies();
  cookieStore.set(ISS_TOKEN_COOKIE, data.token, {
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    path: "/",
    maxAge: 60 * 60 * 8,
  });

  return NextResponse.json({
    userId: data.userId,
    email: data.email,
    roles: data.roles ?? [],
  });
}
