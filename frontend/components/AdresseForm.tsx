'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { AdresseCreate } from '@/types/adresse';
import { City } from '@/types/city';
import { sucheStaedteNachPlz, PLZ_SUCHE_MIN_LAENGE } from '@/lib/api';
import Fehlermeldung from '@/components/Fehlermeldung';
import { inputClass, labelClass, primaryButtonClass } from '@/components/formStyles';

// Wartezeit nach der letzten Eingabe, bevor die PLZ-Suche startet
const PLZ_SUCHE_VERZOEGERUNG_MS = 300;

interface Props {
  initialWerte?: Partial<AdresseCreate>;
  onSubmit: (daten: AdresseCreate) => Promise<void>;
  submitLabel: string;
}

export default function AdresseForm({ initialWerte = {}, onSubmit, submitLabel }: Props) {
  const [speichert, setSpeichert] = useState(false);
  const [fehler, setFehler] = useState<string | null>(null);

  // F-02: Alle Felder als Controlled Inputs (einheitlich via useState)
  // Vorher: vorname/name/strasse/strassennummer = uncontrolled (defaultValue)
  //         plz/ort = controlled (value + onChange)
  const [vorname,        setVorname]        = useState(initialWerte.vorname        ?? '');
  const [name,           setName]           = useState(initialWerte.name           ?? '');
  const [strasse,        setStrasse]        = useState(initialWerte.strasse        ?? '');
  const [strassennummer, setStrassennummer] = useState(initialWerte.strassennummer ?? '');
  const [plz,            setPlz]            = useState(initialWerte.plz            ?? '');
  const [ort,            setOrt]            = useState(initialWerte.ort            ?? '');

  // PLZ-Wertehilfe: Vorschläge aus der Cities-API
  const [vorschlaege,   setVorschlaege]   = useState<City[]>([]);
  const [zeigeDropdown, setZeigeDropdown] = useState(false);
  // F-05: Tippfehler korrigiert: suchelaeuft → sucheLaeuft (camelCase)
  const [sucheLaeuft,   setSucheLaeuft]   = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Debounced PLZ-Suche: API erst nach einer kurzen Eingabepause aufrufen
  useEffect(() => {
    if (plz.length < PLZ_SUCHE_MIN_LAENGE) {
      setVorschlaege([]);
      setZeigeDropdown(false);
      return;
    }

    setSucheLaeuft(true);
    const timer = setTimeout(async () => {
      const treffer = await sucheStaedteNachPlz(plz);
      setVorschlaege(treffer);
      setZeigeDropdown(treffer.length > 0);
      setSucheLaeuft(false);
    }, PLZ_SUCHE_VERZOEGERUNG_MS);

    return () => clearTimeout(timer);
  }, [plz]);

  // Klick ausserhalb des Dropdowns → Dropdown schliessen
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setZeigeDropdown(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Vorschlag auswählen → PLZ + Ort setzen und Dropdown schliessen
  const waehleVorschlag = useCallback((city: City) => {
    setPlz(city.postalCode);
    setOrt(city.cityName);
    setZeigeDropdown(false);
  }, []);

  // F-02: handleSubmit liest direkt aus State (keine form.elements mehr nötig)
  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setFehler(null);
    setSpeichert(true);

    const daten: AdresseCreate = {
      vorname:        vorname.trim(),
      name:           name.trim(),
      strasse:        strasse.trim(),
      strassennummer: strassennummer.trim(),
      plz:            plz.trim(),
      ort:            ort.trim(),
    };

    try {
      await onSubmit(daten);
    } catch (err) {
      setFehler('Fehler beim Speichern. Bitte versuche es erneut.');
      console.error(err);
    } finally {
      setSpeichert(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <Fehlermeldung meldung={fehler} />

      {/* Vorname / Nachname */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className={labelClass}>
            Vorname <span className="text-red-500">*</span>
          </label>
          <input
            name="vorname"
            required
            value={vorname}
            onChange={(e) => setVorname(e.target.value)}
            placeholder="z.B. Anna"
            className={inputClass}
          />
        </div>
        <div>
          <label className={labelClass}>
            Nachname <span className="text-red-500">*</span>
          </label>
          <input
            name="name"
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="z.B. Meier"
            className={inputClass}
          />
        </div>
      </div>

      {/* Strasse / Nummer */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="md:col-span-2">
          <label className={labelClass}>
            Strasse <span className="text-red-500">*</span>
          </label>
          <input
            name="strasse"
            required
            value={strasse}
            onChange={(e) => setStrasse(e.target.value)}
            placeholder="z.B. Bahnhofstrasse"
            className={inputClass}
          />
        </div>
        <div>
          <label className={labelClass}>
            Nummer <span className="text-red-500">*</span>
          </label>
          <input
            name="strassennummer"
            required
            value={strassennummer}
            onChange={(e) => setStrassennummer(e.target.value)}
            placeholder="z.B. 12a"
            className={inputClass}
          />
        </div>
      </div>

      {/* PLZ (mit Wertehilfe) / Ort */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* PLZ-Feld mit Autocomplete-Dropdown */}
        <div className="relative" ref={dropdownRef}>
          <label className={labelClass}>
            PLZ <span className="text-red-500">*</span>
          </label>
          <input
            name="plz"
            required
            value={plz}
            onChange={(e) => setPlz(e.target.value)}
            onFocus={() => vorschlaege.length > 0 && setZeigeDropdown(true)}
            placeholder="z.B. 8001"
            className={inputClass}
            autoComplete="off"
          />

          {/* Lade-Indikator */}
          {sucheLaeuft && (
            <span className="absolute right-3 top-9 text-gray-400 text-xs animate-pulse">
              Suche…
            </span>
          )}

          {/* Dropdown-Vorschlagsliste */}
          {zeigeDropdown && (
            <ul className="absolute z-10 w-full bg-white border border-gray-300 rounded-lg shadow-lg mt-1 max-h-48 overflow-y-auto">
              {vorschlaege.map((city) => (
                <li
                  key={city.id}
                  onMouseDown={() => waehleVorschlag(city)}
                  className="px-3 py-2 hover:bg-akzent-50 cursor-pointer flex justify-between text-sm"
                >
                  <span className="font-mono font-medium text-gray-800">{city.postalCode}</span>
                  <span className="text-gray-600 ml-3">{city.cityName}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Ort-Feld (wird durch Auswahl automatisch befüllt) */}
        <div className="md:col-span-2">
          <label className={labelClass}>
            Ort <span className="text-red-500">*</span>
          </label>
          <input
            name="ort"
            required
            value={ort}
            onChange={(e) => setOrt(e.target.value)}
            placeholder="z.B. Zürich"
            className={inputClass}
          />
        </div>
      </div>

      <div className="flex gap-3 pt-2">
        <button type="submit" disabled={speichert} className={primaryButtonClass}>
          {speichert ? 'Wird gespeichert…' : submitLabel}
        </button>
      </div>
    </form>
  );
}
