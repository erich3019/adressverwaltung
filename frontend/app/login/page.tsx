// Server-Komponente: kein 'use client' → darf process.env lesen.
// Liest GOOGLE_CLIENT_ID serverseitig und gibt googleEnabled als Prop
// an die Client-Komponente LoginForm weiter.
import { Suspense } from 'react';
import { LoginForm } from './LoginForm';

// ──────────────────────────────────────────────────────────────
// LoginPage (Server Component)
// ──────────────────────────────────────────────────────────────
export default function LoginPage() {
  // Google OAuth nur anbieten, wenn echte Credentials konfiguriert sind
  const googleEnabled = !!(
    process.env.GOOGLE_CLIENT_ID?.trim() &&
    process.env.GOOGLE_CLIENT_SECRET?.trim()
  );

  return (
    <main className="min-h-screen flex items-center justify-center bg-gray-100">
      {/*
        Suspense-Boundary ist Pflicht, weil LoginForm useSearchParams() verwendet.
        Ohne Suspense wirft Next.js 14 einen Build-Fehler:
        "Missing Suspense boundary with useSearchParams"
      */}
      <Suspense
        fallback={
          <div className="bg-white rounded-2xl shadow-lg w-full max-w-md p-8 text-center text-gray-400">
            Wird geladen…
          </div>
        }
      >
        <LoginForm googleEnabled={googleEnabled} />
      </Suspense>
    </main>
  );
}
