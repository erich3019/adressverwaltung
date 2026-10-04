'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { loescheCity } from '@/lib/api';
import ConfirmDialog from '@/components/ConfirmDialog';

interface Props {
  id: number;
}

// F-03: ConfirmDialog und Fehler-State statt nativem confirm()/alert()
export default function DeleteButton({ id }: Props) {
  const router = useRouter();
  const [dialogOffen, setDialogOffen] = useState(false);
  const [fehler, setFehler]           = useState<string | null>(null);

  async function handleLoeschenBestaetigt() {
    setDialogOffen(false);

    try {
      await loescheCity(id);
      router.refresh();
    } catch (err) {
      setFehler('Löschen fehlgeschlagen');
      console.error(err);
    }
  }

  return (
    <>
      <ConfirmDialog
        offen={dialogOffen}
        meldung="Stadt wirklich löschen?"
        onBestaetigen={handleLoeschenBestaetigt}
        onAbbrechen={() => setDialogOffen(false)}
      />
      <button
        onClick={() => {
          setFehler(null);
          setDialogOffen(true);
        }}
        className="text-red-500 hover:text-red-700 font-medium"
      >
        Löschen
      </button>
      {fehler && (
        <span role="alert" className="block text-xs text-red-600 mt-1">
          {fehler}
        </span>
      )}
    </>
  );
}
