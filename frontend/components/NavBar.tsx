'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useSession, signOut } from 'next-auth/react';

export default function NavBar() {
  const { data: session } = useSession();
  const pathname = usePathname();

  // Auf der Login-Seite keine Navigation anzeigen
  if (pathname === '/login') return null;

  const linkClass = (href: string) =>
    `px-3 py-1 rounded-md text-sm font-medium transition-colors ${
      pathname.startsWith(href)
        ? 'bg-blue-900 text-white'
        : 'text-blue-100 hover:bg-blue-700'
    }`;

  return (
    <header className="bg-blue-800 text-white shadow">
      <div className="max-w-5xl mx-auto px-6 py-3 flex items-center justify-between">
        {/* Logo / Titel */}
        <div>
          <Link href="/" className="text-xl font-bold hover:text-blue-200 transition-colors">
            Adressverwaltung
          </Link>
          <p className="text-blue-300 text-xs">Next.js + OData + PostgreSQL</p>
        </div>

        {/* Navigation-Links */}
        <nav className="flex items-center gap-2">
          <Link href="/adressen" className={linkClass('/adressen')}>
            Adressen
          </Link>
          <Link href="/cities" className={linkClass('/cities')}>
            Städte
          </Link>
          <Link href="/einstellungen" className={linkClass('/einstellungen')}>
            Einstellungen
          </Link>

          {/* Trennstrich */}
          <div className="w-px h-5 bg-blue-600 mx-1" />

          {/* Benutzer-Info + Logout */}
          {session?.user && (
            <span className="text-blue-200 text-sm hidden sm:inline truncate max-w-[140px]">
              {session.user.email}
            </span>
          )}
          <button
            onClick={() => signOut({ callbackUrl: '/login' })}
            className="px-3 py-1 rounded-md text-sm font-medium text-blue-100 hover:bg-blue-700 transition-colors"
          >
            Abmelden
          </button>
        </nav>
      </div>
    </header>
  );
}
