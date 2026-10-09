'use client';

import { useEffect, useState } from 'react';
import { useSession } from 'next-auth/react';
import { getSettings, speichereSettings } from '@/lib/api';
import Fehlermeldung from '@/components/Fehlermeldung';
import { inputClass, labelClass, primaryButtonClass } from '@/components/formStyles';

export default function EinstellungenPage() {
  const [email,      setEmail]      = useState('');
  const [laden,      setLaden]      = useState(true);
  const [speichern,  setSpeichern]  = useState(false);
  const [fehler,     setFehler]     = useState<string | null>(null);
  const [erfolg,     setErfolg]     = useState(false);

  // Ändern dürfen nur Administratoren. Das Backend prüft die Rolle selbst;
  // hier wird das Formular lediglich passend dazu gesperrt.
  const { data: session } = useSession();
  const istAdmin = session?.user?.role === 'Admin';

  // Einstellungen beim ersten Rendern laden
  useEffect(() => {
    getSettings()
      .then((s) => setEmail(s.notificationEmail ?? ''))
      .catch(() => setFehler('Einstellungen konnten nicht geladen werden.'))
      .finally(() => setLaden(false));
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFehler(null);
    setErfolg(false);
    setSpeichern(true);

    try {
      await speichereSettings({ notificationEmail: email.trim() });
      setErfolg(true);
    } catch {
      setFehler('Fehler beim Speichern. Bitte versuche es erneut.');
    } finally {
      setSpeichern(false);
    }
  }

  return (
    <main className="max-w-2xl mx-auto px-6 py-10">
      <h1 className="text-3xl font-bold text-gray-800 mb-2">Einstellungen</h1>
      <p className="text-gray-500 mb-8">Konfiguration der Benachrichtigungen</p>

      <div className="bg-white rounded-2xl shadow-sm p-8">
        <h2 className="text-lg font-semibold text-gray-700 mb-4">E-Mail-Benachrichtigungen</h2>
        <p className="text-sm text-gray-500 mb-6">
          Bei jeder neu erfassten Adresse wird eine Benachrichtigung an diese E-Mail-Adresse gesendet.
          Leer lassen, um keine E-Mails zu senden.
        </p>

        {laden && (
          <p className="text-gray-400 text-sm animate-pulse">Wird geladen…</p>
        )}

        {!laden && (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className={labelClass}>
                Benachrichtigungs-E-Mail
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="z.B. admin@example.com"
                className={inputClass}
                disabled={!istAdmin}
              />
              <p className="text-xs text-gray-400 mt-1">
                Leer lassen = keine E-Mail-Benachrichtigungen
              </p>
            </div>

            {!istAdmin && (
              <p className="text-sm text-gray-500">
                Nur Benutzer mit der Rolle «Admin» können die Einstellungen ändern.
              </p>
            )}

            <Fehlermeldung meldung={fehler} />

            {erfolg && (
              <div className="bg-green-50 border border-green-200 text-green-700 p-3 rounded-lg text-sm">
                ✓ Einstellungen wurden gespeichert.
              </div>
            )}

            <div className="pt-2">
              <button type="submit" disabled={speichern || !istAdmin} className={primaryButtonClass}>
                {speichern ? 'Wird gespeichert…' : 'Speichern'}
              </button>
            </div>
          </form>
        )}
      </div>
    </main>
  );
}
