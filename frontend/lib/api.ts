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
// Hilfsfunktionen: Aufruf mit einheitlicher Fehlerbehandlung
// F-07: Vorher wiederholte jede API-Funktion fetch, Statusprüfung und throw.
// ============================================================

type RawEntity = Record<string, unknown>;

// Führt den Aufruf aus und wirft einen Fehler, wenn das Backend keinen Erfolg meldet.
async function apiRequest(url: string, fehlertext: string, init: RequestInit = {}): Promise<Response> {
  const response = await apiFetch(url, { cache: 'no-store', ...init });

  if (!response.ok) {
    throw new Error(`${fehlertext}: ${response.statusText}`);
  }

  return response;
}

// Request-Optionen für Aufrufe mit JSON-Body (POST, PATCH, PUT)
function jsonRequest(method: 'POST' | 'PATCH' | 'PUT', daten: unknown): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(daten),
  };
}

// OData-Collection ({ value: [...] }) lesen und jede Zeile normalisieren
async function leseListe<T>(response: Response, normalize: (raw: RawEntity) => T): Promise<T[]> {
  const data = await response.json() as { value: RawEntity[] };
  return data.value.map(normalize);
}

// ============================================================
// Hilfsfunktionen: OData-Antworten normalisieren
// Das Backend liefert camelCase (EnableLowerCamelCase). PascalCase (Id, Vorname...)
// wird als Rückfall weiterhin akzeptiert.
// ============================================================

// F-01: Gemeinsame Audit-Feld-Normalisierung extrahiert (DRY).
// Vorher war dieser Block 6× in normalizeAdresse und 6× in normalizeCity dupliziert.
function normalizeAuditFields(raw: RawEntity) {
  return {
    createDate: String(raw['createDate'] ?? raw['CreateDate'] ?? ''),
    createdBy:  String(raw['createdBy']  ?? raw['CreatedBy']  ?? ''),
    changeDate: (raw['changeDate'] ?? raw['ChangeDate'] ?? null) as string | null,
    changedBy:  (raw['changedBy']  ?? raw['ChangedBy']  ?? null) as string | null,
    dateFrom:   String(raw['dateFrom']   ?? raw['DateFrom']   ?? ''),
    dateTo:     (raw['dateTo']     ?? raw['DateTo']     ?? null) as string | null,
  };
}

function normalizeAdresse(raw: RawEntity): Adresse {
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

function normalizeCity(raw: RawEntity): City {
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
  const response = await apiRequest(`${ODATA_URL}/Adressen`, 'Fehler beim Abrufen der Adressen');
  return leseListe(response, normalizeAdresse);
}

// GET /odata/Adressen(id) – Einzelne Adresse abrufen
export async function getAdresse(id: number): Promise<Adresse> {
  const response = await apiRequest(`${ODATA_URL}/Adressen(${id})`, `Adresse ${id} nicht gefunden`);
  return normalizeAdresse(await response.json() as RawEntity);
}

// POST /odata/Adressen – Neue Adresse erstellen
export async function erstelleAdresse(adresse: AdresseCreate): Promise<Adresse> {
  const response = await apiRequest(
    `${ODATA_URL}/Adressen`,
    'Fehler beim Erstellen',
    jsonRequest('POST', adresse)
  );
  return normalizeAdresse(await response.json() as RawEntity);
}

// PATCH /odata/Adressen(id) – Adresse aktualisieren
export async function aktualisiereAdresse(id: number, aenderungen: AdresseUpdate): Promise<void> {
  await apiRequest(
    `${ODATA_URL}/Adressen(${id})`,
    'Fehler beim Aktualisieren',
    jsonRequest('PATCH', aenderungen)
  );
}

// DELETE /odata/Adressen(id) – Adresse löschen
export async function loescheAdresse(id: number): Promise<void> {
  await apiRequest(`${ODATA_URL}/Adressen(${id})`, 'Fehler beim Löschen', { method: 'DELETE' });
}

// ============================================================
// CITIES (Städte)
// ============================================================

// Ab dieser Eingabelänge schlägt die PLZ-Wertehilfe Städte vor
export const PLZ_SUCHE_MIN_LAENGE = 2;
const PLZ_SUCHE_MAX_TREFFER = 10;

// GET /odata/Cities – Alle Städte abrufen
export async function getAlleCities(): Promise<City[]> {
  const response = await apiRequest(`${ODATA_URL}/Cities`, 'Fehler beim Abrufen der Städte');
  return leseListe(response, normalizeCity);
}

// GET /odata/Cities?$filter=startswith(postalCode,'prefix')&$top=10
// Für die PLZ-Wertehilfe im Adressformular. Liefert bei Fehlern eine leere Liste,
// damit die Eingabe im Formular nicht blockiert wird.
export async function sucheStaedteNachPlz(plzPrefix: string): Promise<City[]> {
  if (!plzPrefix || plzPrefix.length < PLZ_SUCHE_MIN_LAENGE) return [];

  // Hochkomma verdoppeln, damit die Eingabe das OData-Stringliteral nicht verlassen kann
  const plzLiteral = plzPrefix.replace(/'/g, "''");
  const filter = encodeURIComponent(`startswith(postalCode,'${plzLiteral}')`);
  const response = await apiFetch(
    `${ODATA_URL}/Cities?$filter=${filter}&$top=${PLZ_SUCHE_MAX_TREFFER}&$orderby=postalCode`,
    { cache: 'no-store' }
  );

  if (!response.ok) return [];

  return leseListe(response, normalizeCity);
}

// GET /odata/Cities(id) – Einzelne Stadt abrufen
export async function getCity(id: number): Promise<City> {
  const response = await apiRequest(`${ODATA_URL}/Cities(${id})`, `Stadt ${id} nicht gefunden`);
  return normalizeCity(await response.json() as RawEntity);
}

// POST /odata/Cities – Neue Stadt erstellen
export async function erstelleCity(city: CityCreate): Promise<City> {
  const response = await apiRequest(
    `${ODATA_URL}/Cities`,
    'Fehler beim Erstellen',
    jsonRequest('POST', city)
  );
  return normalizeCity(await response.json() as RawEntity);
}

// PATCH /odata/Cities(id) – Stadt aktualisieren
export async function aktualisiereCity(id: number, aenderungen: CityUpdate): Promise<void> {
  await apiRequest(
    `${ODATA_URL}/Cities(${id})`,
    'Fehler beim Aktualisieren',
    jsonRequest('PATCH', aenderungen)
  );
}

// DELETE /odata/Cities(id) – Stadt löschen
export async function loescheCity(id: number): Promise<void> {
  await apiRequest(`${ODATA_URL}/Cities(${id})`, 'Fehler beim Löschen', { method: 'DELETE' });
}

// ============================================================
// EINSTELLUNGEN
// ============================================================

export interface SettingsData {
  notificationEmail: string;
}

// GET /settings – Einstellungen lesen
export async function getSettings(): Promise<SettingsData> {
  const response = await apiRequest(`${BASE_URL}/settings`, 'Fehler beim Lesen der Einstellungen');
  return response.json() as Promise<SettingsData>;
}

// PUT /settings – Einstellungen speichern
export async function speichereSettings(data: SettingsData): Promise<SettingsData> {
  const response = await apiRequest(
    `${BASE_URL}/settings`,
    'Fehler beim Speichern der Einstellungen',
    jsonRequest('PUT', data)
  );
  return response.json() as Promise<SettingsData>;
}
