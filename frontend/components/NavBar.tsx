'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useSession, signOut } from 'next-auth/react';

export default function NavBar() {
  const { data: session } = useSession();
  const pathname = usePathname();

  // Auf der Login-Seite keine Navigation anzeigen
  if (pathname === '/login') return null;

  const linkClass = (aktiv: boolean) =>
    `px-3 py-1 rounded-md text-sm font-medium transition-colors ${
      aktiv ? 'bg-akzent-900 text-white' : 'text-akzent-100 hover:bg-akzent-700'
    }`;

  // Die Adressliste liegt auf «/», Erfassen und Bearbeiten unter «/adressen/…»
  const adressenAktiv = pathname === '/' || pathname.startsWith('/adressen');

  return (
    <header className="bg-akzent-800 text-white shadow-sm">
      <div className="max-w-5xl mx-auto px-6 py-3 flex items-center justify-between">
        {/* Logo / Titel */}
        <div>
          <Link href="/" className="text-xl font-bold hover:text-akzent-200 transition-colors">
            Adressverwaltung
          </Link>
          <p className="text-akzent-300 text-xs">Next.js + OData + PostgreSQL</p>
        </div>

        {/* Navigation-Links */}
        <nav className="flex items-center gap-2">
          <Link href="/" className={linkClass(adressenAktiv)}>
            Adressen
          </Link>
          <Link href="/cities" className={linkClass(pathname.startsWith('/cities'))}>
            Städte
          </Link>
          <Link href="/einstellungen" className={linkClass(pathname.startsWith('/einstellungen'))}>
            Einstellungen
          </Link>
          <Link href="/benutzer" className={linkClass(pathname.startsWith('/benutzer'))}>
            Benutzer
          </Link>

          {/* Trennstrich */}
          <div className="w-px h-5 bg-akzent-600 mx-1" />

          {/* Benutzer-Info + Logout */}
          {session?.user && (
            <span className="text-akzent-200 text-sm hidden sm:inline truncate max-w-[140px]">
              {session.user.email}
            </span>
          )}
          <button
            onClick={() => signOut({ callbackUrl: '/login' })}
            className="px-3 py-1 rounded-md text-sm font-medium text-akzent-100 hover:bg-akzent-700 transition-colors"
          >
            Abmelden
          </button>
        </nav>
      </div>
    </header>
  );
}
