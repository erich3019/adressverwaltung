'use client';

import { useRouter } from 'next/navigation';
import Link from 'next/link';
import BenutzerForm, { BenutzerEingabe } from '@/components/BenutzerForm';
import { erstelleBenutzer } from '@/lib/api';

export default function NeuerBenutzerPage() {
  const router = useRouter();

  async function handleSubmit(daten: BenutzerEingabe) {
    await erstelleBenutzer(daten);
    router.push('/benutzer');
  }

  return (
    <main className="max-w-2xl mx-auto px-6 py-10">
      <div className="mb-6">
        <Link href="/benutzer" className="text-akzent-600 hover:text-akzent-800 text-sm font-medium">
          ← Zurück zur Übersicht
        </Link>
      </div>

      <div className="bg-white rounded-2xl shadow-sm p-8">
        <h1 className="text-2xl font-bold text-gray-800 mb-6">Neuen Benutzer anlegen</h1>
        <BenutzerForm onSubmit={handleSubmit} submitLabel="Benutzer speichern" />
      </div>
    </main>
  );
}
