import Link from 'next/link';
import { getCitiesSeite, SEITENGROESSE } from '@/lib/api';
import Seitenwahl from '@/components/Seitenwahl';
import { inputClass } from '@/components/formStyles';
import DeleteButton from './DeleteButton';
import Fehlermeldung from '@/components/Fehlermeldung';

export const dynamic = 'force-dynamic'; // Kein statisches Caching

interface Props {
  // ?seite=2&suche=80 – Blättern und Suchen laufen über die URL
  searchParams: Promise<{ seite?: string; suche?: string }>;
}

export default async function CitiesPage({ searchParams }: Props) {
  const parameter = await searchParams;
  const suche     = (parameter.suche ?? '').slice(0, 100);
  const seite     = Math.max(1, Number.parseInt(parameter.seite ?? '1', 10) || 1);

  let ladefehler: string | null = null;
  let cities: Awaited<ReturnType<typeof getCitiesSeite>>['eintraege'] = [];
  let gesamt = 0;

  try {
    const daten = await getCitiesSeite(seite, suche);
    cities = daten.eintraege;
    gesamt = daten.gesamt;
  } catch (err) {
    console.error(err);
    ladefehler = 'Städte konnten nicht geladen werden.';
  }

  return (
    <main className="max-w-5xl mx-auto px-6 py-10">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-800">Städte</h1>
          <p className="text-gray-500 mt-1">PLZ-Verzeichnis für die Adressverwaltung</p>
        </div>
        <Link
          href="/cities/neu"
          className="bg-blue-600 text-white px-5 py-2 rounded-lg hover:bg-blue-700 font-semibold transition-colors"
        >
          + Neue Stadt
        </Link>
      </div>

      {/* Suche: gewöhnliches GET-Formular, die Seite wird mit ?suche=… neu geladen */}
      <form action="/cities" className="flex gap-3 mb-6">
        <input
          type="search"
          name="suche"
          defaultValue={suche}
          maxLength={100}
          placeholder="PLZ oder Ortsname suchen"
          aria-label="PLZ oder Ortsname suchen"
          className={inputClass}
        />
        <button
          type="submit"
          className="bg-gray-700 text-white px-5 py-2 rounded-lg hover:bg-gray-800 font-semibold transition-colors"
        >
          Suchen
        </button>
      </form>

      <Fehlermeldung meldung={ladefehler} className="mb-6" />

      {!ladefehler && (
        <p className="text-gray-500 text-sm mb-3">
          {gesamt} {gesamt === 1 ? 'Eintrag' : 'Einträge'}{suche ? ` für «${suche}»` : ''}
        </p>
      )}

      {/* Keine Treffer bei einer Suche */}
      {cities.length === 0 && !ladefehler && suche && (
        <div className="bg-white rounded-2xl shadow-sm p-12 text-center text-gray-400 text-lg">
          Keine Städte gefunden.
        </div>
      )}

      {/* Leerer Zustand */}
      {cities.length === 0 && !ladefehler && !suche && (
        <div className="bg-white rounded-2xl shadow-sm p-12 text-center">
          <p className="text-gray-400 text-lg mb-4">Noch keine Städte erfasst.</p>
          <Link
            href="/cities/neu"
            className="inline-block bg-blue-600 text-white px-5 py-2 rounded-lg hover:bg-blue-700 font-semibold transition-colors"
          >
            Erste Stadt anlegen
          </Link>
        </div>
      )}

      {/* Tabelle */}
      {cities.length > 0 && (
        <div className="bg-white rounded-2xl shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200 text-left">
                <th className="px-6 py-3 font-semibold text-gray-600 w-32">PLZ</th>
                <th className="px-6 py-3 font-semibold text-gray-600">Ortsname</th>
                <th className="px-6 py-3 font-semibold text-gray-600 text-right w-52">
                  Aktionen
                </th>
              </tr>
            </thead>
            <tbody>
              {cities.map((city, i) => (
                <tr
                  key={city.id}
                  className={`border-b border-gray-100 hover:bg-gray-50 transition-colors ${
                    i % 2 !== 0 ? 'bg-gray-50/40' : ''
                  }`}
                >
                  <td className="px-6 py-3 font-mono font-medium text-gray-800">
                    {city.postalCode}
                  </td>
                  <td className="px-6 py-3 text-gray-700">{city.cityName}</td>
                  <td className="px-6 py-3 text-right space-x-3 whitespace-nowrap">
                    <Link
                      href={`/cities/${city.id}/bearbeiten`}
                      className="text-blue-600 hover:text-blue-800 font-medium"
                    >
                      Bearbeiten
                    </Link>
                    <DeleteButton id={city.id} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Seitenwahl
        seite={seite}
        seiten={Math.ceil(gesamt / SEITENGROESSE)}
        hrefFuer={(ziel) => {
          const query = new URLSearchParams({ seite: String(ziel) });
          if (suche) query.set('suche', suche);
          return `/cities?${query}`;
        }}
      />
    </main>
  );
}
