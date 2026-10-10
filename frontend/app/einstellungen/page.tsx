'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { getSettings, speichereSettings } from '@/lib/api';
import { AKZENTFARBEN, Akzentfarbe, STANDARDFARBE } from '@/lib/farben';
import Fehlermeldung from '@/components/Fehlermeldung';
import { inputClass, labelClass, primaryButtonClass } from '@/components/formStyles';

export default function EinstellungenPage() {
  const router = useRouter();
  const [email,      setEmail]      = useState('');
  const [farbe,      setFarbe]      = useState<Akzentfarbe>(STANDARDFARBE);
  const [laden,      setLaden]      = useState(true);
  const [speichern,  setSpeichern]  = useState(false);
  const [fehler,     setFehler]     = useState<string | null>(null);

  // Ändern dürfen nur Administratoren. Ob der angemeldete Benutzer einer ist, meldet
  // das Backend mit den Einstellungen (canEdit) – es kennt die aktuelle Rolle aus der
  // Datenbank. Die Rolle in der Session stammt vom Login und kann veraltet sein.
  const [darfAendern, setDarfAendern] = useState(false);

  // Einstellungen beim ersten Rendern laden
  useEffect(() => {
    getSettings()
      .then((s) => {
        setEmail(s.notificationEmail);
        setFarbe(s.accentColor);
        setDarfAendern(s.canEdit);
      })
      .catch(() => setFehler('Einstellungen konnten nicht geladen werden.'))
      .finally(() => setLaden(false));
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFehler(null);
    setSpeichern(true);

    try {
      await speichereSettings({ notificationEmail: email.trim(), accentColor: farbe });
      // Zurück zur Adressliste. Das Layout liest die Farbe auf dem Server:
      // neu laden, damit sie überall gilt
      router.push('/');
      router.refresh();
    } catch {
      setFehler('Fehler beim Speichern. Bitte versuche es erneut.');
    } finally {
      setSpeichern(false);
    }
  }

  return (
    <main className="max-w-2xl mx-auto px-6 py-10">
      <h1 className="text-3xl font-bold text-gray-800 mb-2">Einstellungen</h1>
      <p className="text-gray-500 mb-8">Benachrichtigungen und Darstellung</p>

      {laden && (
        <p className="text-gray-400 text-sm animate-pulse">Wird geladen…</p>
      )}

      {!laden && (
        <form onSubmit={handleSubmit} className="space-y-6">
          <section className="bg-white rounded-2xl shadow-sm p-8">
            <h2 className="text-lg font-semibold text-gray-700 mb-4">E-Mail-Benachrichtigungen</h2>
            <p className="text-sm text-gray-500 mb-6">
              Bei jeder neu erfassten Adresse und bei jedem gelöschten Benutzer wird eine Benachrichtigung
              an diese E-Mail-Adresse gesendet. Leer lassen, um diese Benachrichtigungen auszuschalten.
            </p>

            <label htmlFor="benachrichtigung" className={labelClass}>
              Benachrichtigungs-E-Mail
            </label>
            <input
              id="benachrichtigung"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="z.B. admin@example.com"
              maxLength={256}
              className={inputClass}
              disabled={!darfAendern}
            />
            <p className="text-xs text-gray-400 mt-1">
              Leer lassen = keine E-Mail-Benachrichtigungen
            </p>
          </section>

          <section className="bg-white rounded-2xl shadow-sm p-8">
            <h2 className="text-lg font-semibold text-gray-700 mb-4">Darstellung</h2>
            <p className="text-sm text-gray-500 mb-6">
              Die Farbe gilt für alle Benutzer: Kopfzeile, Schaltflächen und Links.
            </p>

            <fieldset disabled={!darfAendern}>
              <legend className={labelClass}>Farbe</legend>
              <div className="flex flex-wrap gap-3 mt-2">
                {AKZENTFARBEN.map((eintrag) => (
                  <label
                    key={eintrag.wert}
                    // data-farbe schaltet die Palette für dieses Element um (globals.css),
                    // so zeigt jedes Feld seine eigene Farbe
                    data-farbe={eintrag.wert}
                    className={`flex items-center gap-2 border rounded-lg px-3 py-2 text-sm transition-colors ${
                      farbe === eintrag.wert
                        ? 'border-akzent-600 ring-2 ring-akzent-500 text-gray-900'
                        : 'border-gray-300 text-gray-600'
                    } ${darfAendern ? 'cursor-pointer hover:border-akzent-600' : 'opacity-60'}`}
                  >
                    <input
                      type="radio"
                      name="farbe"
                      value={eintrag.wert}
                      checked={farbe === eintrag.wert}
                      onChange={() => setFarbe(eintrag.wert)}
                      className="sr-only"
                    />
                    <span aria-hidden="true" className="w-5 h-5 rounded-full bg-akzent-600" />
                    {eintrag.name}
                  </label>
                ))}
              </div>
            </fieldset>
          </section>

          {!darfAendern && (
            <p className="text-sm text-gray-500">
              Nur Benutzer mit der Rolle «Admin» können die Einstellungen ändern.
            </p>
          )}

          <Fehlermeldung meldung={fehler} />

          <button type="submit" disabled={speichern || !darfAendern} className={primaryButtonClass}>
            {speichern ? 'Wird gespeichert…' : 'Speichern'}
          </button>
        </form>
      )}
    </main>
  );
}
