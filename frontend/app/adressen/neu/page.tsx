'use client';

import { useRouter } from 'next/navigation';
import Link from 'next/link';
import AdresseForm from '@/components/AdresseForm';
import { erstelleAdresse } from '@/lib/api';
import { AdresseCreate } from '@/types/adresse';

export default function NeueAdressePage() {
  const router = useRouter();

  async function handleSubmit(daten: AdresseCreate) {
    await erstelleAdresse(daten);
    router.push('/');
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

      <h2 className="text-2xl font-bold text-gray-800 mb-6">Neue Adresse erfassen</h2>

      <div className="bg-white rounded-xl shadow-sm p-6">
        <AdresseForm onSubmit={handleSubmit} submitLabel="Adresse speichern" />
      </div>
    </main>
  );
}
