'use client';

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import AdresseForm from '@/components/AdresseForm';
import Fehlermeldung from '@/components/Fehlermeldung';
import Ladeanzeige from '@/components/Ladeanzeige';
import { getAdresse, aktualisiereAdresse } from '@/lib/api';
import { Adresse, AdresseCreate } from '@/types/adresse';

export default function AdresseBearbeitenPage() {
  const router = useRouter();
  const params = useParams();
  const id = Number(params.id);

  const [adresse, setAdresse] = useState<Adresse | null>(null);
  const [ladevorgang, setLadevorgang] = useState(true);
  const [fehler, setFehler]           = useState<string | null>(null);

  // F-03: Fehler-State statt alert() – gleich wie auf der Seite «Stadt bearbeiten»
  useEffect(() => {
    getAdresse(id)
      .then(setAdresse)
      .catch(() => setFehler('Adresse konnte nicht geladen werden.'))
      .finally(() => setLadevorgang(false));
  }, [id]);

  async function handleSubmit(daten: AdresseCreate) {
    await aktualisiereAdresse(id, daten);
    router.push('/');
  }

  if (ladevorgang) {
    return <Ladeanzeige text="Adresse wird geladen..." />;
  }

  return (
    <main className="max-w-2xl mx-auto p-6">
      <div className="flex items-center gap-4 mb-6">
        <Link
          href="/"
          className="text-gray-500 hover:text-gray-700 flex items-center gap-1 text-sm font-medium"
        >
          ← Zurück zur Liste
        </Link>
      </div>

      <h2 className="text-2xl font-bold text-gray-800 mb-2">Adresse bearbeiten</h2>
      {adresse && (
        <p className="text-gray-500 text-sm mb-6">
          {adresse.vorname} {adresse.name} – ID #{adresse.id}
        </p>
      )}

      <div className="bg-white rounded-xl shadow p-6">
        <Fehlermeldung meldung={fehler} />
        {adresse && (
          <AdresseForm
            initialWerte={adresse}
            onSubmit={handleSubmit}
            submitLabel="Änderungen speichern"
          />
        )}
      </div>
    </main>
  );
}
