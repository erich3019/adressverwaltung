// F-04: NextAuth Session-Typen erweitern (Module Augmentation).
// Ermöglicht typsicheren Zugriff auf session.user.id und session.accessToken.
//
// accessToken = JWT Bearer-Token des C#-Backends (8 h Gültigkeit).
// Das Frontend liest ihn aus der Session und sendet ihn bei jedem API-Aufruf mit.

import 'next-auth';

declare module 'next-auth' {
  // Erweiterung des User-Objekts (Rückgabe von authorize())
  interface User {
    accessToken?: string;
  }

  interface Session {
    user: {
      id: string;
      name?: string | null;
      email?: string | null;
      image?: string | null;
    };
    // Backend-JWT, der im Authorization-Header mitgesendet wird
    accessToken?: string;
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    id?: string;
    accessToken?: string;   // Backend-JWT im verschlüsselten NextAuth-Cookie
  }
}
