'use client';

import { useState } from 'react';
import { Benutzer, ROLLEN, Rolle } from '@/types/benutzer';
import { ApiFehler } from '@/lib/api';
import Fehlermeldung from '@/components/Fehlermeldung';
import { inputClass, labelClass, primaryButtonClass } from '@/components/formStyles';

/** Eingaben des Formulars; password ist beim Bearbeiten leer, wenn es bleiben soll. */
export interface BenutzerEingabe {
  email: string;
  displayName: string;
  role: Rolle;
  password: string;
}

interface Props {
  /** Vorhandener Benutzer beim Bearbeiten; ohne Angabe wird ein neuer erfasst. */
  benutzer?: Benutzer;
  onSubmit: (daten: BenutzerEingabe) => Promise<void>;
  submitLabel: string;
}

const PASSWORT_MIN_LAENGE = 8;

export default function BenutzerForm({ benutzer, onSubmit, submitLabel }: Props) {
  const neu = !benutzer;

  const [speichert, setSpeichert] = useState(false);
  const [fehler, setFehler]       = useState<string | null>(null);

  const [email,       setEmail]       = useState(benutzer?.email ?? '');
  const [displayName, setDisplayName] = useState(benutzer?.displayName ?? '');
  const [role,        setRole]        = useState<Rolle>(benutzer?.role ?? 'User');
  const [password,    setPassword]    = useState('');

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setFehler(null);
    setSpeichert(true);

    try {
      await onSubmit({ email: email.trim(), displayName: displayName.trim(), role, password });
    } catch (err) {
      // Meldungen des Backends (z.B. «E-Mail-Adresse gibt es bereits») direkt zeigen
      setFehler(
        err instanceof ApiFehler && err.meldung
          ? err.meldung
          : 'Fehler beim Speichern. Bitte versuche es erneut.'
      );
      console.error(err);
    } finally {
      setSpeichert(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <Fehlermeldung meldung={fehler} />

      <div>
        <label htmlFor="email" className={labelClass}>
          E-Mail {neu && <span className="text-red-500">*</span>}
        </label>
        <input
          id="email"
          type="email"
          required
          maxLength={256}
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="z.B. anna.meier@example.com"
          autoComplete="off"
          className={inputClass}
          disabled={!neu}
        />
        {!neu && (
          <p className="text-xs text-gray-500 mt-1">Die E-Mail-Adresse lässt sich nicht ändern.</p>
        )}
      </div>

      <div>
        <label htmlFor="displayName" className={labelClass}>
          Anzeigename <span className="text-red-500">*</span>
        </label>
        <input
          id="displayName"
          required
          maxLength={100}
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          placeholder="z.B. Anna Meier"
          className={inputClass}
        />
      </div>

      <div>
        <label htmlFor="role" className={labelClass}>
          Rolle <span className="text-red-500">*</span>
        </label>
        <select
          id="role"
          value={role}
          onChange={(e) => setRole(e.target.value as Rolle)}
          className={inputClass}
          disabled={benutzer?.isSelf}
        >
          {ROLLEN.map((r) => (
            <option key={r} value={r}>
              {r}
            </option>
          ))}
        </select>
        <p className="text-xs text-gray-500 mt-1">
          {benutzer?.isSelf
            ? 'Die eigene Rolle lässt sich nicht ändern.'
            : 'User pflegt Adressen und Städte; Admin verwaltet zusätzlich Benutzer und Einstellungen.'}
        </p>
      </div>

      <div>
        <label htmlFor="password" className={labelClass}>
          {neu ? 'Passwort' : 'Neues Passwort'} {neu && <span className="text-red-500">*</span>}
        </label>
        <input
          id="password"
          type="password"
          required={neu}
          minLength={PASSWORT_MIN_LAENGE}
          maxLength={128}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="new-password"
          className={inputClass}
        />
        <p className="text-xs text-gray-500 mt-1">
          Mindestens {PASSWORT_MIN_LAENGE} Zeichen.
          {neu && ' Der neue Benutzer erhält eine E-Mail an seine Adresse – ohne Passwort; gib es ihm auf anderem Weg weiter.'}
          {!neu && ' Leer lassen, um das Passwort zu behalten. Ein neues Passwort meldet den Benutzer überall ab.'}
        </p>
      </div>

      <div className="flex gap-3 pt-2">
        <button type="submit" disabled={speichert} className={primaryButtonClass}>
          {speichert ? 'Wird gespeichert…' : submitLabel}
        </button>
      </div>
    </form>
  );
}
