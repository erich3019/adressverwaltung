// middleware.ts – Route-Schutz via NextAuth
// Alle Routen ausser /login und API/Assets erfordern eine gültige Session.
import { withAuth } from 'next-auth/middleware';

// Eine Session zählt nur mit Backend-Token: Ohne ihn liesse sich keine einzige
// API-Abfrage ausführen, die Seiten sollen dann auch nicht erreichbar sein.
export default withAuth({
  callbacks: {
    authorized: ({ token }) => Boolean(token?.accessToken),
  },
});

export const config = {
  matcher: [
    '/((?!api/auth|_next/static|_next/image|favicon\\.ico|login).*)',
  ],
};
