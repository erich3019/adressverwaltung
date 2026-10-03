'use client';

import { useRouter } from 'next/navigation';
import { loescheCity } from '@/lib/api';

interface Props {
  id: number;
}

export default function DeleteButton({ id }: Props) {
  const router = useRouter();

  async function handleDelete() {
    if (!confirm('Stadt wirklich löschen?')) return;
    try {
      await loescheCity(id);
      router.refresh();
    } catch {
      alert('Fehler beim Löschen. Bitte erneut versuchen.');
    }
  }

  return (
    <button
      onClick={handleDelete}
      className="text-red-500 hover:text-red-700 font-medium"
    >
      Löschen
    </button>
  );
}
