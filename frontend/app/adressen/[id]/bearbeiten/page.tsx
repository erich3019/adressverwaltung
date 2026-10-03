'use client';

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import AdresseForm from '@/components/AdresseForm';
import { getAdresse, aktualisiereAdresse } from '@/lib/api';
import { Adresse, AdresseCreate } from '@/types/adresse';

export default function AdresseBearbeitenPage() {
  const router = useRouter();
  const params = useParams();
  const id = Number(params.id);

  const [adresse, setAdresse] = useState<Adresse | null>(null);
  const [ladevorgang, setLadevorgang] = useState(true);

  useEffect(() => {
    async function laden() {
      try {
        const daten = await getAdresse(id);
        setAdresse(daten);
      } catch {
        alert('Adresse nicht gefunden.');
        router.push('/');
      } finally {
        setLadevorgang(false);
      }
    }
    laden();
  }, [id, router]);

  async function handleSubmit(daten: AdresseCreate) {
    await aktualisiereAdresse(id, daten);
    router.push('/');
  }

  if (ladevorgang) {
    return (
      <div className="flex justify-center items-center min-h-64">
        <div className="text-center">
          <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto mb-3"></div>
          <p className="text-gray-500">Adresse wird geladen...</p>
        </div>
      </div>
    );
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
