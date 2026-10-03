import type { NextAuthOptions } from 'next-auth';
import CredentialsProvider from 'next-auth/providers/credentials';
import GoogleProvider from 'next-auth/providers/google';

// Für server-seitige Aufrufe (Docker: interner Service-Name)
const INTERNAL_API = process.env.INTERNAL_API_URL
  ?? process.env.NEXT_PUBLIC_API_URL
  ?? 'http://localhost:5000';

// Google OAuth nur aktivieren, wenn echte Credentials gesetzt sind
const googleClientId     = process.env.GOOGLE_CLIENT_ID     ?? '';
const googleClientSecret = process.env.GOOGLE_CLIENT_SECRET ?? '';
const googleEnabled      = googleClientId.length > 0 && googleClientSecret.length > 0;

export const authOptions: NextAuthOptions = {
  providers: [
    // ----------------------------------------------------------------
    // Credentials Provider: E-Mail + Passwort via Backend /auth/login
    // ----------------------------------------------------------------
    CredentialsProvider({
      name: 'E-Mail & Passwort',
      credentials: {
        email:    { label: 'E-Mail',   type: 'email',    placeholder: 'you@example.com' },
        password: { label: 'Passwort', type: 'password' },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials?.password) return null;

        try {
          const response = await fetch(`${INTERNAL_API}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              email:    credentials.email,
              password: credentials.password,
            }),
          });

          if (!response.ok) return null;

          // Backend gibt { id, name, email, token } zurück (LoginResponse DTO)
          const user = await response.json() as {
            id:    string;
            name:  string;
            email: string;
            token: string;   // JWT Bearer-Token
          };

          return {
            id:          String(user.id),
            email:       user.email,
            name:        user.name,
            accessToken: user.token,   // wird in jwt-Callback weitergegeben
          };
        } catch {
          // Netzwerk- oder Parse-Fehler → Login verweigern
          return null;
        }
      },
    }),

    // ----------------------------------------------------------------
    // Google Provider (OAuth 2.0) – nur wenn Credentials konfiguriert
    // ----------------------------------------------------------------
    ...(googleEnabled
      ? [GoogleProvider({ clientId: googleClientId, clientSecret: googleClientSecret })]
      : []),
  ],

  // Eigene Login-Seite statt NextAuth-Default
  pages: {
    signIn: '/login',
  },

  // JWT-Session (kein Datenbank-Adapter nötig)
  session: {
    strategy: 'jwt',
    maxAge:   8 * 60 * 60, // 8 Stunden
  },

  callbacks: {
    // ID und Backend-JWT aus User-Objekt in den NextAuth-Token übernehmen.
    // Dieser Callback wird beim Login (user vorhanden) und bei jedem Request ausgeführt.
    async jwt({ token, user }) {
      if (user) {
        token.id          = user.id;
        token.accessToken = user.accessToken;   // Backend-JWT im verschlüsselten Cookie
      }
      return token;
    },
    // NextAuth-Token in Session-Objekt umwandeln, das das Frontend lesen kann.
    // F-04: Typsicherer Zugriff dank Module Augmentation in types/next-auth.d.ts
    async session({ session, token }) {
      if (token?.id && session.user) {
        session.user.id = token.id;
      }
      // Backend-JWT für API-Aufrufe in der Session verfügbar machen
      session.accessToken = token.accessToken;
      return session;
    },
  },

  secret: process.env.NEXTAUTH_SECRET,
};
