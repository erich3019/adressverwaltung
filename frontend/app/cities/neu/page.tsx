'use client';

import { useRouter } from 'next/navigation';
import Link from 'next/link';
import CityForm from '@/components/CityForm';
import { erstelleCity } from '@/lib/api';
import type { CityCreate } from '@/types/city';

export default function NeueStadtPage() {
  const router = useRouter();

  async function handleSubmit(daten: CityCreate) {
    await erstelleCity(daten);
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
        <h1 className="text-2xl font-bold text-gray-800 mb-6">Neue Stadt anlegen</h1>
        <CityForm onSubmit={handleSubmit} submitLabel="Stadt speichern" />
      </div>
    </main>
  );
}
