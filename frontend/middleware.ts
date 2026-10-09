// middleware.ts – Seitenschutz und Content-Security-Policy
//
// 1. Alle Routen ausser /login und den NextAuth-Routen erfordern eine Session mit
//    Backend-Token. Ohne ihn liesse sich keine einzige API-Abfrage ausführen.
// 2. Jede Seite erhält eine CSP mit einer Nonce, die nur für diese eine Antwort gilt.
//    Skripte laufen nur mit dieser Nonce; eingeschleuster Code hat sie nicht.
import { NextRequest, NextResponse } from 'next/server';
import { getToken } from 'next-auth/jwt';

function contentSecurityPolicy(nonce: string): string {
  // next dev lädt Code über eval() nach; im ausgelieferten Build ist das nicht nötig
  const dev = process.env.NODE_ENV !== 'production' ? " 'unsafe-eval'" : '';

  return [
    "default-src 'self'",
    `script-src 'self' 'nonce-${nonce}' 'strict-dynamic'${dev}`,
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data:",
    "font-src 'self'",
    "connect-src 'self'",
    "frame-ancestors 'self'",
    "base-uri 'self'",
    "form-action 'self'",
    "object-src 'none'",
  ].join('; ');
}

export async function middleware(req: NextRequest) {
  const { pathname, search } = req.nextUrl;
  const istLogin = pathname === '/login';

  if (!istLogin) {
    const token = await getToken({ req });

    if (!token?.accessToken) {
      // API-Aufrufe beantwortet der Proxy selbst mit 401; Seiten leiten zum Login um
      if (pathname.startsWith('/api/')) return NextResponse.next();

      const login = new URL('/login', req.url);
      if (pathname !== '/') login.searchParams.set('callbackUrl', pathname + search);
      return NextResponse.redirect(login);
    }
  }

  const nonce = btoa(crypto.randomUUID());
  const csp   = contentSecurityPolicy(nonce);

  // Next.js liest die Nonce aus dem CSP-Header der Anfrage und setzt sie in seine Skripte ein
  const requestHeaders = new Headers(req.headers);
  requestHeaders.set('x-nonce', nonce);
  requestHeaders.set('Content-Security-Policy', csp);

  const response = NextResponse.next({ request: { headers: requestHeaders } });
  response.headers.set('Content-Security-Policy', csp);
  return response;
}

export const config = {
  matcher: [
    '/((?!api/auth|_next/static|_next/image|favicon\\.ico).*)',
  ],
};
