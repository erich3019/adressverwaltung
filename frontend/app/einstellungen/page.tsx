'use client';

import { useEffect, useState } from 'react';
import { getSettings, speichereSettings, type SettingsData } from '@/lib/api';

export default function EinstellungenPage() {
  const [email,      setEmail]      = useState('');
  const [laden,      setLaden]      = useState(true);
  const [speichern,  setSpeichern]  = useState(false);
  const [fehler,     setFehler]     = useState<string | null>(null);
  const [erfolg,     setErfolg]     = useState(false);

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
      const data: SettingsData = { notificationEmail: email.trim() };
      await speichereSettings(data);
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

      <div className="bg-white rounded-2xl shadow p-8">
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
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Benachrichtigungs-E-Mail
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="z.B. admin@example.com"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white"
              />
              <p className="text-xs text-gray-400 mt-1">
                Leer lassen = keine E-Mail-Benachrichtigungen
              </p>
            </div>

            {fehler && (
              <div className="bg-red-50 border border-red-200 text-red-700 p-3 rounded-lg text-sm">
                {fehler}
              </div>
            )}

            {erfolg && (
              <div className="bg-green-50 border border-green-200 text-green-700 p-3 rounded-lg text-sm">
                ✓ Einstellungen wurden gespeichert.
              </div>
            )}

            <div className="pt-2">
              <button
                type="submit"
                disabled={speichern}
                className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 font-semibold transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {speichern ? 'Wird gespeichert…' : 'Speichern'}
              </button>
            </div>
          </form>
        )}
      </div>
    </main>
  );
}
