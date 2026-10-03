// middleware.ts – Route-Schutz via NextAuth
// Alle Routen ausser /login und API/Assets erfordern eine gültige Session.
export { default } from 'next-auth/middleware';

export const config = {
  matcher: [
    '/((?!api/auth|_next/static|_next/image|favicon\\.ico|login).*)',
  ],
};
