'use client';

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import BenutzerForm, { BenutzerEingabe } from '@/components/BenutzerForm';
import Fehlermeldung from '@/components/Fehlermeldung';
import { getBenutzer, aktualisiereBenutzer } from '@/lib/api';
import type { Benutzer } from '@/types/benutzer';

export default function BenutzerBearbeitenPage() {
  const router = useRouter();
  const params = useParams();
  const id = Number(params.id);

  const [benutzer, setBenutzer] = useState<Benutzer | null>(null);
  const [fehler,   setFehler]   = useState<string | null>(null);
  const [laden,    setLaden]    = useState(true);

  useEffect(() => {
    getBenutzer(id)
      .then((b) => setBenutzer(b))
      .catch(() => setFehler('Benutzer konnte nicht geladen werden.'))
      .finally(() => setLaden(false));
  }, [id]);

  async function handleSubmit(daten: BenutzerEingabe) {
    await aktualisiereBenutzer(id, {
      displayName: daten.displayName,
      role:        daten.role,
      // Ohne Eingabe bleibt das Passwort bestehen
      ...(daten.password ? { password: daten.password } : {}),
    });
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
        <h1 className="text-2xl font-bold text-gray-800 mb-6">Benutzer bearbeiten</h1>

        {laden && (
          <p className="text-gray-500 text-sm">Wird geladen…</p>
        )}

        <Fehlermeldung meldung={fehler} />

        {benutzer && (
          <BenutzerForm benutzer={benutzer} onSubmit={handleSubmit} submitLabel="Änderungen speichern" />
        )}
      </div>
    </main>
  );
}
