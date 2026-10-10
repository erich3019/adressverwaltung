import type { NextAuthOptions, Session } from 'next-auth';
import CredentialsProvider from 'next-auth/providers/credentials';
import GoogleProvider from 'next-auth/providers/google';
import { ANMELDUNG_GESPERRT } from './anmeldung';

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

          // 429: Die Adresse ist nach zu vielen Fehlversuchen gesperrt. Ein Fehler aus
          // authorize() erreicht das Login-Formular als result.error.
          if (response.status === 429) throw new Error(ANMELDUNG_GESPERRT);

          if (!response.ok) return null;

          // Backend gibt { id, name, email, role, token } zurück (LoginResponse DTO)
          const user = await response.json() as {
            id:    string;
            name:  string;
            email: string;
            role:  string;   // "Admin" oder "User"
            token: string;   // JWT Bearer-Token
          };

          return {
            id:          String(user.id),
            email:       user.email,
            name:        user.name,
            role:        user.role,
            accessToken: user.token,   // wird in jwt-Callback weitergegeben
          };
        } catch (err) {
          if (err instanceof Error && err.message === ANMELDUNG_GESPERRT) throw err;

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
    // Nur Anmeldungen zulassen, für die das Backend ein Token ausgestellt hat.
    // Google bestätigt lediglich, dass es das Konto gibt – ohne diese Prüfung
    // erhielte jedes beliebige Google-Konto eine Session.
    async signIn({ user }) {
      return user.accessToken ? true : '/login?error=AccessDenied';
    },
    // ID und Backend-JWT aus User-Objekt in den NextAuth-Token übernehmen.
    // Dieser Callback wird beim Login (user vorhanden) und bei jedem Request ausgeführt.
    async jwt({ token, user }) {
      if (user) {
        token.id          = user.id;
        token.role        = user.role;
        token.accessToken = user.accessToken;   // Backend-JWT im verschlüsselten Cookie
      }
      return token;
    },
    // NextAuth-Token in Session-Objekt umwandeln, das das Frontend lesen kann.
    // F-04: Typsicherer Zugriff dank Module Augmentation in types/next-auth.d.ts
    // Diese Session geht über /api/auth/session an den Browser – der Backend-JWT
    // gehört deshalb bewusst NICHT hinein (siehe serverAuthOptions).
    async session({ session, token }) {
      if (token?.id && session.user) {
        session.user.id   = token.id;
        session.user.role = token.role;
      }
      return session;
    },
  },

  events: {
    // Abmeldung auch dem Backend melden: Es widerruft dann die ausgestellten Tokens.
    // Schlägt der Aufruf fehl, läuft das Token spätestens nach 8 Stunden ab.
    async signOut({ token }) {
      if (!token?.accessToken) return;

      try {
        await fetch(`${INTERNAL_API}/auth/logout`, {
          method:  'POST',
          headers: { Authorization: `Bearer ${token.accessToken}` },
        });
      } catch (err) {
        console.error('Abmeldung am Backend fehlgeschlagen', err);
      }
    },
  },

  secret: process.env.NEXTAUTH_SECRET,
};

// Variante für getServerSession() in Server Components: Hier bleibt die Session auf
// dem Server, deshalb darf sie den Backend-JWT für API-Aufrufe enthalten.
// Nicht im NextAuth-Handler (app/api/auth) verwenden.
export const serverAuthOptions: NextAuthOptions = {
  ...authOptions,
  callbacks: {
    ...authOptions.callbacks,
    async session(params) {
      const session = await authOptions.callbacks!.session!(params) as Session;
      session.accessToken = params.token.accessToken;
      return session;
    },
  },
};
