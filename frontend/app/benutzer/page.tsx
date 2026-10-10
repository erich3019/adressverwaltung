'use client';

import { useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { Benutzer } from '@/types/benutzer';
import { ApiFehler, getBenutzerListe, loescheBenutzer, entsperreBenutzer } from '@/lib/api';
import { SPERRE_MINUTEN } from '@/lib/anmeldung';
import ConfirmDialog from '@/components/ConfirmDialog';
import Fehlermeldung from '@/components/Fehlermeldung';
import Ladeanzeige from '@/components/Ladeanzeige';

// Ende der Sperre als Uhrzeit; eine abgelaufene Sperre gilt als keine
function gesperrtBis(benutzer: Benutzer): string | null {
  if (!benutzer.lockedUntil) return null;

  const ende = new Date(benutzer.lockedUntil);
  if (ende.getTime() <= Date.now()) return null;

  return ende.toLocaleTimeString('de-CH', { hour: '2-digit', minute: '2-digit' });
}

export default function BenutzerListePage() {
  const [benutzer, setBenutzer]       = useState<Benutzer[]>([]);
  const [ladevorgang, setLadevorgang] = useState(true);
  const [fehler, setFehler]           = useState<string | null>(null);

  // Die Benutzerverwaltung ist der Rolle Admin vorbehalten. Ob der angemeldete
  // Benutzer sie hat, entscheidet das Backend (403) – wie bei den Einstellungen
  // nicht die Rolle in der Session, die vom Login stammt und veraltet sein kann.
  const [keinZugriff, setKeinZugriff] = useState(false);

  const [zuLoeschen, setZuLoeschen]       = useState<Benutzer | null>(null);
  const [aktionsFehler, setAktionsFehler] = useState<string | null>(null);

  const ladeDaten = useCallback(async () => {
    try {
      setLadevorgang(true);
      setFehler(null);
      setBenutzer(await getBenutzerListe());
    } catch (err) {
      if (err instanceof ApiFehler && err.status === 403) {
        setKeinZugriff(true);
      } else {
        setFehler('Fehler beim Laden der Benutzer. Bitte versuche es erneut.');
        console.error(err);
      }
    } finally {
      setLadevorgang(false);
    }
  }, []);

  useEffect(() => {
    ladeDaten();
  }, [ladeDaten]);

  // Führt eine Aktion aus und lädt die Liste neu; Meldungen des Backends direkt zeigen
  async function ausfuehren(aktion: () => Promise<void>, fehlertext: string) {
    setAktionsFehler(null);

    try {
      await aktion();
      await ladeDaten();
    } catch (err) {
      setAktionsFehler(err instanceof ApiFehler && err.meldung ? err.meldung : fehlertext);
      console.error(err);
    }
  }

  async function handleLoeschenBestaetigt() {
    if (!zuLoeschen) return;
    const id = zuLoeschen.id;
    setZuLoeschen(null);

    await ausfuehren(() => loescheBenutzer(id), 'Fehler beim Löschen des Benutzers. Bitte versuche es erneut.');
  }

  if (ladevorgang) {
    return <Ladeanzeige text="Benutzer werden geladen..." />;
  }

  if (keinZugriff) {
    return (
      <main className="max-w-5xl mx-auto p-6">
        <h2 className="text-2xl font-bold text-gray-800 mb-2">Benutzer</h2>
        <p className="text-sm text-gray-500">
          Nur Benutzer mit der Rolle «Admin» können die Benutzer verwalten.
        </p>
      </main>
    );
  }

  return (
    <main className="max-w-5xl mx-auto p-6">
      <ConfirmDialog
        offen={zuLoeschen !== null}
        meldung={`Benutzer «${zuLoeschen?.email ?? ''}» wirklich löschen?`}
        onBestaetigen={handleLoeschenBestaetigt}
        onAbbrechen={() => setZuLoeschen(null)}
      />

      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Benutzer</h2>
          <p className="text-gray-500 text-sm mt-1">
            {benutzer.length} Benutzer · Nach 3 fehlerhaften
            Anmeldungen ist ein Benutzer für {SPERRE_MINUTEN} Minuten gesperrt.
          </p>
        </div>
        <Link
          href="/benutzer/neu"
          className="bg-akzent-600 text-white px-5 py-2 rounded-lg hover:bg-akzent-700 font-medium transition-colors"
        >
          + Neuer Benutzer
        </Link>
      </div>

      <Fehlermeldung meldung={fehler} className="mb-6" />
      <Fehlermeldung meldung={aktionsFehler} className="mb-6" />

      <div className="bg-white rounded-xl shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-akzent-800 text-white">
            <tr>
              <th className="px-4 py-3 text-left font-semibold">Name</th>
              <th className="px-4 py-3 text-left font-semibold">E-Mail</th>
              <th className="px-4 py-3 text-left font-semibold">Rolle</th>
              <th className="px-4 py-3 text-left font-semibold">Status</th>
              <th className="px-4 py-3 text-left font-semibold">Aktionen</th>
            </tr>
          </thead>
          <tbody>
            {benutzer.map((eintrag, index) => {
              const bis = gesperrtBis(eintrag);

              return (
                <tr
                  key={eintrag.id}
                  className={`border-b last:border-b-0 hover:bg-akzent-50 transition-colors ${
                    index % 2 === 0 ? 'bg-white' : 'bg-gray-50'
                  }`}
                >
                  <td className="px-4 py-3 font-medium text-gray-800">
                    {eintrag.displayName}
                    {eintrag.isSelf && <span className="text-gray-400 font-normal"> (ich)</span>}
                  </td>
                  <td className="px-4 py-3 text-gray-600">{eintrag.email}</td>
                  <td className="px-4 py-3 text-gray-600">{eintrag.role}</td>
                  <td className="px-4 py-3">
                    {bis ? (
                      <span className="text-red-600 font-medium">Gesperrt bis {bis}</span>
                    ) : (
                      <span className="text-gray-600">Aktiv</span>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-3">
                      <Link
                        href={`/benutzer/${eintrag.id}/bearbeiten`}
                        className="text-akzent-600 hover:text-akzent-800 hover:underline font-medium"
                      >
                        Bearbeiten
                      </Link>
                      {bis && (
                        <button
                          onClick={() =>
                            ausfuehren(() => entsperreBenutzer(eintrag.id), 'Fehler beim Entsperren. Bitte versuche es erneut.')
                          }
                          className="text-akzent-600 hover:text-akzent-800 hover:underline font-medium"
                        >
                          Entsperren
                        </button>
                      )}
                      {!eintrag.isSelf && (
                        <button
                          onClick={() => {
                            setAktionsFehler(null);
                            setZuLoeschen(eintrag);
                          }}
                          className="text-red-500 hover:text-red-700 hover:underline font-medium"
                        >
                          Löschen
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </main>
  );
}
