'use client';

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import CityForm from '@/components/CityForm';
import Fehlermeldung from '@/components/Fehlermeldung';
import { getCity, aktualisiereCity } from '@/lib/api';
import type { City, CityCreate } from '@/types/city';

export default function StadtBearbeitenPage() {
  const router = useRouter();
  const params = useParams();
  const id = Number(params.id);

  const [city,    setCity]    = useState<City | null>(null);
  const [fehler,  setFehler]  = useState<string | null>(null);
  const [laden,   setLaden]   = useState(true);

  useEffect(() => {
    getCity(id)
      .then((c) => setCity(c))
      .catch(() => setFehler('Stadt konnte nicht geladen werden.'))
      .finally(() => setLaden(false));
  }, [id]);

  async function handleSubmit(daten: CityCreate) {
    await aktualisiereCity(id, daten);
    router.push('/cities');
    router.refresh();
  }

  return (
    <main className="max-w-2xl mx-auto px-6 py-10">
      <div className="mb-6">
        <Link href="/cities" className="text-blue-600 hover:text-blue-800 text-sm font-medium">
          ← Zurück zur Übersicht
        </Link>
      </div>

      <div className="bg-white rounded-2xl shadow p-8">
        <h1 className="text-2xl font-bold text-gray-800 mb-6">Stadt bearbeiten</h1>

        {laden && (
          <p className="text-gray-500 text-sm">Wird geladen…</p>
        )}

        <Fehlermeldung meldung={fehler} />

        {city && (
          <CityForm
            initialWerte={{ postalCode: city.postalCode, cityName: city.cityName }}
            onSubmit={handleSubmit}
            submitLabel="Änderungen speichern"
          />
        )}
      </div>
    </main>
  );
}
