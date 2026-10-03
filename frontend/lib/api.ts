import { getSession } from 'next-auth/react';
import { getServerSession } from 'next-auth';
import { authOptions } from './auth';
import { Adresse, AdresseCreate, AdresseUpdate } from '@/types/adresse';
import { City, CityCreate, CityUpdate } from '@/types/city';

// Server Components (typeof window === 'undefined'): INTERNAL_API_URL verwenden
//   → Docker-intern: http://backend:8080 (nginx nicht nötig)
// Client Components (Browser): leerer String → relative URLs (/odata/...)
//   → Browser schickt Request an denselben Host (nginx leitet weiter)
//   → funktioniert sowohl mit http://localhost:3000 als auch https://localhost
const BASE_URL  = typeof window === 'undefined'
  ? (process.env.INTERNAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000')
  : '';
const ODATA_URL = `${BASE_URL}/odata`;

// ============================================================
// Authentifizierter Fetch
// ============================================================
// Liest den JWT Bearer-Token aus der NextAuth-Session und fügt ihn
// automatisch als Authorization-Header bei jedem API-Aufruf hinzu.
//
// Wichtig: Next.js unterscheidet Server Components und Client Components.
//   - Server Components (z.B. cities/page.tsx): getSession() von next-auth/react
//     funktioniert NICHT (kein Browser). Stattdessen: getServerSession(authOptions).
//   - Client Components (z.B. app/page.tsx mit 'use client'): getSession() korrekt.
//
// typeof window === 'undefined' → Server-Umgebung
async function apiFetch(url: string, init: RequestInit = {}): Promise<Response> {
  let token: string | undefined;

  if (typeof window === 'undefined') {
    // Server Component: getServerSession aus next-auth (serverseitig)
    const session = await getServerSession(authOptions);
    token = session?.accessToken;
  } else {
    // Client Component: getSession aus next-auth/react (clientseitig)
    const session = await getSession();
    token = session?.accessToken;
  }

  const headers: Record<string, string> = {
    ...(init.headers as Record<string, string> | undefined ?? {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  return fetch(url, { ...init, headers });
}

// ============================================================
// Hilfsfunktionen: OData-Antworten normalisieren
// OData gibt Properties standardmässig in PascalCase zurück (Id, Vorname...).
// Die Normalisierung akzeptiert beide Schreibweisen sicher.
// ============================================================

// F-01: Gemeinsame Audit-Feld-Normalisierung extrahiert (DRY).
// Vorher war dieser Block 6× in normalizeAdresse und 6× in normalizeCity dupliziert.
function normalizeAuditFields(raw: Record<string, unknown>) {
  return {
    createDate: String(raw['createDate'] ?? raw['CreateDate'] ?? ''),
    createdBy:  String(raw['createdBy']  ?? raw['CreatedBy']  ?? ''),
    changeDate: (raw['changeDate'] ?? raw['ChangeDate'] ?? null) as string | null,
    changedBy:  (raw['changedBy']  ?? raw['ChangedBy']  ?? null) as string | null,
    dateFrom:   String(raw['dateFrom']   ?? raw['DateFrom']   ?? ''),
    dateTo:     (raw['dateTo']     ?? raw['DateTo']     ?? null) as string | null,
  };
}

function normalizeAdresse(raw: Record<string, unknown>): Adresse {
  return {
    id:             Number(raw['id']             ?? raw['Id']),
    vorname:        String(raw['vorname']         ?? raw['Vorname']         ?? ''),
    name:           String(raw['name']            ?? raw['Name']            ?? ''),
    strasse:        String(raw['strasse']         ?? raw['Strasse']         ?? ''),
    strassennummer: String(raw['strassennummer']  ?? raw['Strassennummer']  ?? ''),
    plz:            String(raw['plz']             ?? raw['Plz']             ?? ''),
    ort:            String(raw['ort']             ?? raw['Ort']             ?? ''),
    ...normalizeAuditFields(raw),
  };
}

function normalizeCity(raw: Record<string, unknown>): City {
  return {
    id:         Number(raw['id']         ?? raw['Id']),
    postalCode: String(raw['postalCode'] ?? raw['PostalCode'] ?? ''),
    cityName:   String(raw['cityName']   ?? raw['CityName']   ?? ''),
    ...normalizeAuditFields(raw),
  };
}

// ============================================================
// ADRESSEN
// ============================================================

// GET /odata/Adressen – Alle Adressen abrufen
export async function getAlleAdressen(): Promise<Adresse[]> {
  const response = await apiFetch(`${ODATA_URL}/Adressen`, { cache: 'no-store' });

  if (!response.ok) {
    throw new Error(`Fehler beim Abrufen der Adressen: ${response.statusText}`);
  }

  const data = await response.json() as { value: Record<string, unknown>[] };
  return data.value.map(normalizeAdresse);
}

// GET /odata/Adressen(id) – Einzelne Adresse abrufen
export async function getAdresse(id: number): Promise<Adresse> {
  const response = await apiFetch(`${ODATA_URL}/Adressen(${id})`, { cache: 'no-store' });

  if (!response.ok) {
    throw new Error(`Adresse ${id} nicht gefunden: ${response.statusText}`);
  }

  return normalizeAdresse(await response.json() as Record<string, unknown>);
}

// POST /odata/Adressen – Neue Adresse erstellen
export async function erstelleAdresse(adresse: AdresseCreate): Promise<Adresse> {
  const response = await apiFetch(`${ODATA_URL}/Adressen`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(adresse),
  });

  if (!response.ok) {
    throw new Error(`Fehler beim Erstellen: ${response.statusText}`);
  }

  return normalizeAdresse(await response.json() as Record<string, unknown>);
}

// PATCH /odata/Adressen(id) – Adresse aktualisieren
export async function aktualisiereAdresse(
  id: number,
  aenderungen: AdresseUpdate
): Promise<void> {
  const response = await apiFetch(`${ODATA_URL}/Adressen(${id})`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(aenderungen),
  });

  if (!response.ok) {
    throw new Error(`Fehler beim Aktualisieren: ${response.statusText}`);
  }
}

// DELETE /odata/Adressen(id) – Adresse löschen
export async function loescheAdresse(id: number): Promise<void> {
  const response = await apiFetch(`${ODATA_URL}/Adressen(${id})`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    throw new Error(`Fehler beim Löschen: ${response.statusText}`);
  }
}

// ============================================================
// CITIES (Städte)
// ============================================================

// GET /odata/Cities – Alle Städte abrufen
export async function getAlleCities(): Promise<City[]> {
  const response = await apiFetch(`${ODATA_URL}/Cities`, { cache: 'no-store' });

  if (!response.ok) {
    throw new Error(`Fehler beim Abrufen der Städte: ${response.statusText}`);
  }

  const data = await response.json() as { value: Record<string, unknown>[] };
  return data.value.map(normalizeCity);
}

// GET /odata/Cities?$filter=startswith(postalCode,'prefix')&$top=10
// Für die PLZ-Wertehilfe im Adressformular
export async function sucheStaedteNachPlz(plzPrefix: string): Promise<City[]> {
  if (!plzPrefix || plzPrefix.length < 2) return [];

  // OData-Filter: PLZ beginnt mit dem eingegebenen Wert
  const filter = encodeURIComponent(`startswith(postalCode,'${plzPrefix}')`);
  const response = await apiFetch(
    `${ODATA_URL}/Cities?$filter=${filter}&$top=10&$orderby=postalCode`,
    { cache: 'no-store' }
  );

  if (!response.ok) return [];

  const data = await response.json() as { value: Record<string, unknown>[] };
  return data.value.map(normalizeCity);
}

// GET /odata/Cities(id) – Einzelne Stadt abrufen
export async function getCity(id: number): Promise<City> {
  const response = await apiFetch(`${ODATA_URL}/Cities(${id})`, { cache: 'no-store' });

  if (!response.ok) {
    throw new Error(`Stadt ${id} nicht gefunden: ${response.statusText}`);
  }

  return normalizeCity(await response.json() as Record<string, unknown>);
}

// POST /odata/Cities – Neue Stadt erstellen
export async function erstelleCity(city: CityCreate): Promise<City> {
  const response = await apiFetch(`${ODATA_URL}/Cities`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(city),
  });

  if (!response.ok) {
    throw new Error(`Fehler beim Erstellen: ${response.statusText}`);
  }

  return normalizeCity(await response.json() as Record<string, unknown>);
}

// PATCH /odata/Cities(id) – Stadt aktualisieren
export async function aktualisiereCity(
  id: number,
  aenderungen: CityUpdate
): Promise<void> {
  const response = await apiFetch(`${ODATA_URL}/Cities(${id})`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(aenderungen),
  });

  if (!response.ok) {
    throw new Error(`Fehler beim Aktualisieren: ${response.statusText}`);
  }
}

// DELETE /odata/Cities(id) – Stadt löschen
export async function loescheCity(id: number): Promise<void> {
  const response = await apiFetch(`${ODATA_URL}/Cities(${id})`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    throw new Error(`Fehler beim Löschen: ${response.statusText}`);
  }
}

// ============================================================
// EINSTELLUNGEN
// ============================================================

export interface SettingsData {
  notificationEmail: string;
}

// GET /settings – Einstellungen lesen
export async function getSettings(): Promise<SettingsData> {
  const response = await apiFetch(`${BASE_URL}/settings`, { cache: 'no-store' });

  if (!response.ok) {
    throw new Error(`Fehler beim Lesen der Einstellungen: ${response.statusText}`);
  }

  return response.json() as Promise<SettingsData>;
}

// PUT /settings – Einstellungen speichern
export async function speichereSettings(data: SettingsData): Promise<SettingsData> {
  const response = await apiFetch(`${BASE_URL}/settings`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    throw new Error(`Fehler beim Speichern der Einstellungen: ${response.statusText}`);
  }

  return response.json() as Promise<SettingsData>;
}
