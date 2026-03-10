import { NextResponse } from "next/server";
import { issApiBaseUrl } from "@/lib/env";

type AuthCapabilities = {
  registrationAllowed: boolean;
  bootstrapRegistrationOnly: boolean;
  selfRegistrationEnabled: boolean;
  hasUsers: boolean;
};

export const runtime = "nodejs";

export async function GET() {
  const resp = await fetch(new URL("/api/auth/capabilities", issApiBaseUrl()), {
    cache: "no-store",
  });

  if (!resp.ok) {
    const text = await resp.text();
    return NextResponse.json(
      { error: "Auth status unavailable.", detail: text },
      { status: resp.status },
    );
  }

  const data = (await resp.json()) as Partial<AuthCapabilities>;
  if (
    typeof data.registrationAllowed !== "boolean" ||
    typeof data.bootstrapRegistrationOnly !== "boolean" ||
    typeof data.selfRegistrationEnabled !== "boolean" ||
    typeof data.hasUsers !== "boolean"
  ) {
    return NextResponse.json({ error: "Invalid auth status response." }, { status: 502 });
  }

  return NextResponse.json(data);
}
