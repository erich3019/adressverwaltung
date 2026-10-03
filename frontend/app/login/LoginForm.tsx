'use client';

import { useState } from 'react';
import { signIn } from 'next-auth/react';
import { useRouter, useSearchParams } from 'next/navigation';

// ──────────────────────────────────────────────────────────────
// LoginForm: Client-Komponente (useSearchParams → Suspense nötig).
// Wird von der Server-Komponente page.tsx mit googleEnabled versorgt.
// ──────────────────────────────────────────────────────────────

interface LoginFormProps {
  /** Zeige Google-Schaltfläche nur, wenn GOOGLE_CLIENT_ID gesetzt ist. */
  googleEnabled: boolean;
}

export function LoginForm({ googleEnabled }: LoginFormProps) {
  const router       = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl  = searchParams.get('callbackUrl') ?? '/';

  const [email,    setEmail]    = useState('');
  const [passwort, setPasswort] = useState('');
  const [zeigePw,  setZeigePw]  = useState(false);
  const [laden,    setLaden]    = useState(false);
  const [fehler,   setFehler]   = useState<string | null>(null);

  async function handleCredentials(e: React.FormEvent) {
    e.preventDefault();
    setFehler(null);
    setLaden(true);

    const result = await signIn('credentials', {
      email,
      password:   passwort,
      redirect:   false,
      callbackUrl,
    });

    setLaden(false);

    if (result?.error) {
      setFehler('Ungültige E-Mail-Adresse oder falsches Passwort.');
    } else {
      router.push(callbackUrl);
      router.refresh();
    }
  }

  async function handleGoogle() {
    await signIn('google', { callbackUrl });
  }

  const inputClass =
    'w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none ' +
    'focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white';

  return (
    <div className="bg-white rounded-2xl shadow-lg w-full max-w-md p-8">
      <h1 className="text-2xl font-bold text-gray-800 mb-2">Anmelden</h1>
      <p className="text-gray-500 text-sm mb-6">
        Adressverwaltung – bitte melde dich an
      </p>

      {/* Fehlermeldung */}
      {fehler && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4">
          {fehler}
        </div>
      )}

      {/* Credentials-Login */}
      <form onSubmit={handleCredentials} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">E-Mail</label>
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
            className={inputClass}
            autoComplete="email"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Passwort</label>
          <div className="relative">
            <input
              type={zeigePw ? 'text' : 'password'}
              required
              value={passwort}
              onChange={(e) => setPasswort(e.target.value)}
              placeholder="••••••••"
              className={inputClass + ' pr-10'}
              autoComplete="current-password"
            />
            {/* Auge-Button: Passwort ein-/ausblenden */}
            <button
              type="button"
              onClick={() => setZeigePw((v) => !v)}
              className="absolute inset-y-0 right-0 flex items-center px-3 text-gray-400 hover:text-gray-700"
              aria-label={zeigePw ? 'Passwort verbergen' : 'Passwort anzeigen'}
            >
              {zeigePw ? (
                // Auge durchgestrichen (verbergen)
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M13.875 18.825A10.05 10.05 0 0112 19c-5 0-9-4-9-7a9.77 9.77 0 012.168-5.168M6.343 6.343A9.956 9.956 0 0112 5c5 0 9 4 9 7a9.956 9.956 0 01-1.343 2.657M15 12a3 3 0 11-6 0 3 3 0 016 0zM3 3l18 18" />
                </svg>
              ) : (
                // Auge offen (anzeigen)
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.477 0 8.268 2.943 9.542 7-1.274 4.057-5.065 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
              )}
            </button>
          </div>
        </div>
        <button
          type="submit"
          disabled={laden}
          className="w-full bg-blue-600 text-white py-2.5 rounded-lg hover:bg-blue-700 font-semibold transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {laden ? 'Wird angemeldet…' : 'Anmelden'}
        </button>
      </form>

      {/* Google-Login – nur anzeigen wenn OAuth konfiguriert */}
      {googleEnabled && (
        <>
          <div className="flex items-center gap-3 my-5">
            <div className="flex-1 border-t border-gray-200" />
            <span className="text-xs text-gray-400 uppercase tracking-wide">oder</span>
            <div className="flex-1 border-t border-gray-200" />
          </div>

          <button
            onClick={handleGoogle}
            className="w-full flex items-center justify-center gap-3 border border-gray-300 rounded-lg py-2.5 px-4 hover:bg-gray-50 transition-colors font-medium text-gray-700"
          >
            {/* Google-Icon (SVG) */}
            <svg className="w-5 h-5" viewBox="0 0 24 24" aria-hidden="true">
              <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
              <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
              <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" />
              <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" />
            </svg>
            Mit Google anmelden
          </button>
        </>
      )}
    </div>
  );
}
