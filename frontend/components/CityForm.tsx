'use client';

import { useState } from 'react';
import { CityCreate } from '@/types/city';

interface Props {
  initialWerte?: Partial<CityCreate>;
  onSubmit: (daten: CityCreate) => Promise<void>;
  submitLabel: string;
}

export default function CityForm({ initialWerte = {}, onSubmit, submitLabel }: Props) {
  const [laden, setLaden]   = useState(false);
  const [fehler, setFehler] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setFehler(null);
    setLaden(true);

    const form = e.currentTarget;
    const daten: CityCreate = {
      postalCode: (form.elements.namedItem('postalCode') as HTMLInputElement).value.trim(),
      cityName:   (form.elements.namedItem('cityName')   as HTMLInputElement).value.trim(),
    };

    try {
      await onSubmit(daten);
    } catch (err) {
      setFehler('Fehler beim Speichern. Bitte versuche es erneut.');
      console.error(err);
    } finally {
      setLaden(false);
    }
  }

  const inputClass =
    'w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white';
  const labelClass = 'block text-sm font-medium text-gray-700 mb-1';

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      {fehler && (
        <div className="bg-red-50 border border-red-200 text-red-700 p-3 rounded-lg text-sm">
          {fehler}
        </div>
      )}

      <div>
        <label className={labelClass}>
          PLZ <span className="text-red-500">*</span>
        </label>
        <input
          name="postalCode"
          required
          minLength={4}
          maxLength={10}
          defaultValue={initialWerte.postalCode}
          placeholder="z.B. 8001"
          className={inputClass}
        />
        <p className="text-xs text-gray-500 mt-1">4–10 Zeichen, eindeutig</p>
      </div>

      <div>
        <label className={labelClass}>
          Ortsname <span className="text-red-500">*</span>
        </label>
        <input
          name="cityName"
          required
          maxLength={100}
          defaultValue={initialWerte.cityName}
          placeholder="z.B. Zürich"
          className={inputClass}
        />
      </div>

      <div className="flex gap-3 pt-2">
        <button
          type="submit"
          disabled={laden}
          className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {laden ? 'Wird gespeichert…' : submitLabel}
        </button>
      </div>
    </form>
  );
}
