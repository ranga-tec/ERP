import { NextResponse, type NextRequest } from "next/server";
import { ISS_TOKEN_COOKIE } from "@/lib/env";
import { isJwtExpired } from "@/lib/jwt";

const PUBLIC_FILE = /\.(.*)$/;

export function proxy(req: NextRequest) {
  const { pathname } = req.nextUrl;

  if (
    pathname.startsWith("/login") ||
    pathname.startsWith("/api/auth") ||
    pathname.startsWith("/_next") ||
    pathname === "/favicon.ico" ||
    PUBLIC_FILE.test(pathname)
  ) {
    return NextResponse.next();
  }

  const token = req.cookies.get(ISS_TOKEN_COOKIE)?.value;
  if (!token || isJwtExpired(token)) {
    const url = req.nextUrl.clone();
    url.pathname = "/login";
    url.searchParams.set("next", pathname);
    const response = NextResponse.redirect(url);
    if (token) {
      response.cookies.delete(ISS_TOKEN_COOKIE);
    }
    return response;
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
