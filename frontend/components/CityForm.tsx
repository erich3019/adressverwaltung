'use client';

import { useState } from 'react';
import { CityCreate } from '@/types/city';
import Fehlermeldung from '@/components/Fehlermeldung';
import { inputClass, labelClass, primaryButtonClass } from '@/components/formStyles';

interface Props {
  initialWerte?: Partial<CityCreate>;
  onSubmit: (daten: CityCreate) => Promise<void>;
  submitLabel: string;
}

export default function CityForm({ initialWerte = {}, onSubmit, submitLabel }: Props) {
  const [speichert, setSpeichert] = useState(false);
  const [fehler, setFehler]       = useState<string | null>(null);

  // F-02: Controlled Inputs wie in AdresseForm (vorher form.elements mit Type-Casts)
  const [postalCode, setPostalCode] = useState(initialWerte.postalCode ?? '');
  const [cityName,   setCityName]   = useState(initialWerte.cityName   ?? '');

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setFehler(null);
    setSpeichert(true);

    try {
      await onSubmit({ postalCode: postalCode.trim(), cityName: cityName.trim() });
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

      <div>
        <label className={labelClass}>
          PLZ <span className="text-red-500">*</span>
        </label>
        <input
          name="postalCode"
          required
          minLength={4}
          maxLength={10}
          value={postalCode}
          onChange={(e) => setPostalCode(e.target.value)}
          placeholder="z.B. 8001"
          className={inputClass}
        />
        <p className="text-xs text-gray-500 mt-1">4–10 Zeichen</p>
      </div>

      <div>
        <label className={labelClass}>
          Ortsname <span className="text-red-500">*</span>
        </label>
        <input
          name="cityName"
          required
          maxLength={100}
          value={cityName}
          onChange={(e) => setCityName(e.target.value)}
          placeholder="z.B. Zürich"
          className={inputClass}
        />
      </div>

      <div className="flex gap-3 pt-2">
        <button type="submit" disabled={speichert} className={primaryButtonClass}>
          {speichert ? 'Wird gespeichert…' : submitLabel}
        </button>
      </div>
    </form>
  );
}
