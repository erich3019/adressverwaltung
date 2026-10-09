'use client';

import { useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { Adresse } from '@/types/adresse';
import { getAdressenSeite, loescheAdresse, SEITENGROESSE } from '@/lib/api';
import Seitenwahl from '@/components/Seitenwahl';
import ConfirmDialog from '@/components/ConfirmDialog';
import Ladeanzeige from '@/components/Ladeanzeige';

export default function AdressenListePage() {
  const [adressen, setAdressen] = useState<Adresse[]>([]);
  const [gesamt, setGesamt]     = useState(0);
  const [seite, setSeite]       = useState(1);
  const [ladevorgang, setLadevorgang] = useState(true);
  const [fehler, setFehler] = useState<string | null>(null);

  // F-03: State für Bestätigungsdialog statt nativem confirm()
  const [zuLoeschendId, setZuLoeschendId] = useState<number | null>(null);
  const [loeschFehler, setLoeschFehler]   = useState<string | null>(null);

  // F-06: useCallback – ladeDaten hat stabile Referenz, kein Rebuild bei jedem Render
  const ladeDaten = useCallback(async () => {
    try {
      setLadevorgang(true);
      setFehler(null);
      const daten = await getAdressenSeite(seite);
      setAdressen(daten.eintraege);
      setGesamt(daten.gesamt);

      // Letzte Zeile der letzten Seite gelöscht → eine Seite zurück
      if (daten.eintraege.length === 0 && seite > 1) setSeite(seite - 1);
    } catch (err) {
      setFehler(
        'Fehler beim Laden der Adressen. Läuft das Backend und ist nginx erreichbar?'
      );
      console.error(err);
    } finally {
      setLadevorgang(false);
    }
  }, [seite]);

  useEffect(() => {
    ladeDaten();
  }, [ladeDaten]);

  // F-03: Dialog öffnen statt nativem confirm()
  function loeschenBestaetigen(id: number) {
    setLoeschFehler(null);
    setZuLoeschendId(id);
  }

  // F-03: Bestätigt → löschen; alert() durch loeschFehler-State ersetzt
  async function handleLoeschenBestaetigt() {
    if (zuLoeschendId === null) return;
    const id = zuLoeschendId;
    setZuLoeschendId(null);

    try {
      await loescheAdresse(id);
      await ladeDaten();
    } catch (err) {
      setLoeschFehler('Fehler beim Löschen der Adresse. Bitte versuche es erneut.');
      console.error(err);
    }
  }

  if (ladevorgang) {
    return <Ladeanzeige text="Adressen werden geladen..." />;
  }

  return (
    <main className="max-w-5xl mx-auto p-6">
      {/* F-03: Bestätigungsdialog (ersetzt nativen confirm()) */}
      <ConfirmDialog
        offen={zuLoeschendId !== null}
        meldung="Adresse wirklich löschen?"
        onBestaetigen={handleLoeschenBestaetigt}
        onAbbrechen={() => setZuLoeschendId(null)}
      />

      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Alle Adressen</h2>
          <p className="text-gray-500 text-sm mt-1">
            {gesamt} {gesamt === 1 ? 'Eintrag' : 'Einträge'} gespeichert
          </p>
        </div>
        <Link
          href="/adressen/neu"
          className="bg-akzent-600 text-white px-5 py-2 rounded-lg hover:bg-akzent-700 font-medium transition-colors"
        >
          + Neue Adresse
        </Link>
      </div>

      {fehler && (
        <div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded-lg mb-6 flex items-start gap-3">
          <span className="text-red-500 text-xl">⚠</span>
          <div>
            <p className="font-medium">Verbindungsfehler</p>
            <p className="text-sm mt-1">{fehler}</p>
            <button
              onClick={ladeDaten}
              className="text-sm mt-2 underline hover:no-underline"
            >
              Erneut versuchen
            </button>
          </div>
        </div>
      )}

      {/* F-03: Lösch-Fehler inline anzeigen statt alert() */}
      {loeschFehler && (
        <div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded-lg mb-6 flex items-start gap-3">
          <span className="text-red-500 text-xl">⚠</span>
          <div>
            <p className="font-medium">Fehler</p>
            <p className="text-sm mt-1">{loeschFehler}</p>
          </div>
          <button
            onClick={() => setLoeschFehler(null)}
            className="ml-auto text-red-400 hover:text-red-600 text-lg leading-none"
          >
            ×
          </button>
        </div>
      )}

      <div className="bg-white rounded-xl shadow-sm overflow-hidden">
        {adressen.length === 0 && !fehler ? (
          <div className="text-center py-16 text-gray-400">
            <p className="text-4xl mb-3">📭</p>
            <p className="text-lg font-medium">Keine Adressen vorhanden</p>
            <p className="text-sm mt-1">Klicke auf «+ Neue Adresse», um zu beginnen.</p>
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-akzent-800 text-white">
              <tr>
                <th className="px-4 py-3 text-left font-semibold">Vorname</th>
                <th className="px-4 py-3 text-left font-semibold">Name</th>
                <th className="px-4 py-3 text-left font-semibold">Strasse</th>
                <th className="px-4 py-3 text-left font-semibold">Nr.</th>
                <th className="px-4 py-3 text-left font-semibold">PLZ</th>
                <th className="px-4 py-3 text-left font-semibold">Ort</th>
                <th className="px-4 py-3 text-left font-semibold">Aktionen</th>
              </tr>
            </thead>
            <tbody>
              {adressen.map((adresse, index) => (
                <tr
                  key={adresse.id}
                  className={`border-b last:border-b-0 hover:bg-akzent-50 transition-colors ${
                    index % 2 === 0 ? 'bg-white' : 'bg-gray-50'
                  }`}
                >
                  <td className="px-4 py-3 font-medium text-gray-800">{adresse.vorname}</td>
                  <td className="px-4 py-3 font-medium text-gray-800">{adresse.name}</td>
                  <td className="px-4 py-3 text-gray-600">{adresse.strasse}</td>
                  <td className="px-4 py-3 text-gray-600">{adresse.strassennummer}</td>
                  <td className="px-4 py-3 text-gray-600">{adresse.plz}</td>
                  <td className="px-4 py-3 text-gray-600">{adresse.ort}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-3">
                      <Link
                        href={`/adressen/${adresse.id}/bearbeiten`}
                        className="text-akzent-600 hover:text-akzent-800 hover:underline font-medium"
                      >
                        Bearbeiten
                      </Link>
                      <button
                        onClick={() => loeschenBestaetigen(adresse.id)}
                        className="text-red-500 hover:text-red-700 hover:underline font-medium"
                      >
                        Löschen
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <Seitenwahl
        seite={seite}
        seiten={Math.ceil(gesamt / SEITENGROESSE)}
        onWechsel={setSeite}
      />
    </main>
  );
}
