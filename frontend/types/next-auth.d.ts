// F-04: NextAuth Session-Typen erweitern (Module Augmentation).
// Ermöglicht typsicheren Zugriff auf session.user.id und session.accessToken.
//
// accessToken = JWT Bearer-Token des C#-Backends (8 h Gültigkeit).
// Er liegt im verschlüsselten NextAuth-Cookie und bleibt auf dem Server: Im Browser
// ist session.accessToken immer undefined, API-Aufrufe laufen dort über /api/backend.

import 'next-auth';

declare module 'next-auth' {
  // Erweiterung des User-Objekts (Rückgabe von authorize())
  interface User {
    accessToken?: string;
    role?: string;
  }

  interface Session {
    user: {
      id: string;
      name?: string | null;
      email?: string | null;
      image?: string | null;
      /** "Admin" oder "User" – nur für die Anzeige; massgebend ist die Prüfung im Backend. */
      role?: string;
    };
    // Backend-JWT – nur serverseitig gesetzt (getServerSession mit serverAuthOptions)
    accessToken?: string;
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    id?: string;
    role?: string;
    accessToken?: string;   // Backend-JWT im verschlüsselten NextAuth-Cookie
  }
}
