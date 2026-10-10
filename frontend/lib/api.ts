import { signOut } from 'next-auth/react';
import { getServerSession } from 'next-auth';
import { serverAuthOptions } from './auth';
import { API_HEADER } from './apiHeader';
import { Adresse, AdresseCreate, AdresseUpdate } from '@/types/adresse';
import { City, CityCreate, CityUpdate } from '@/types/city';
import { Benutzer, BenutzerCreate, BenutzerUpdate, alsRolle } from '@/types/benutzer';
import { Akzentfarbe, alsAkzentfarbe, STANDARDFARBE } from './farben';

// Server Components (typeof window === 'undefined'): direkt zum Backend
//   → Docker-intern: http://backend:8080, der Bearer-Token kommt aus der Session
// Client Components (Browser): über den Proxy /api/backend der eigenen App
//   → der Proxy hängt den Token an; der Browser bekommt ihn nie zu sehen
const IM_BROWSER = typeof window !== 'undefined';
const BASE_URL   = IM_BROWSER
  ? '/api/backend'
  : (process.env.INTERNAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000');
const ODATA_URL  = `${BASE_URL}/odata`;

// ============================================================
// Authentifizierter Fetch
// ============================================================
// Wichtig: Next.js unterscheidet Server Components und Client Components.
//   - Server Components (z.B. cities/page.tsx): Der Backend-JWT steht in der
//     Session von getServerSession(serverAuthOptions) und wird als
//     Authorization-Header mitgesendet.
//   - Client Components (z.B. app/page.tsx mit 'use client'): Der Aufruf geht mit
//     dem Session-Cookie an /api/backend; ein Token ist im Browser nicht vorhanden.
async function apiFetch(url: string, init: RequestInit = {}): Promise<Response> {
  const headers: Record<string, string> = {
    ...(init.headers as Record<string, string> | undefined ?? {}),
  };

  if (IM_BROWSER) {
    // Vom Proxy verlangter Header (Schutz vor Cross-Site-Request-Forgery)
    headers[API_HEADER] = '1';
  } else {
    const session = await getServerSession(serverAuthOptions);
    if (session?.accessToken) headers.Authorization = `Bearer ${session.accessToken}`;
  }

  const response = await fetch(url, { ...init, headers });

  // 401 im Browser: Die Session ist abgelaufen oder das Token wurde widerrufen
  // (z.B. Abmeldung auf einem anderen Gerät) → zurück zum Login
  if (IM_BROWSER && response.status === 401) {
    await signOut({ callbackUrl: '/login' });
  }

  return response;
}

// ============================================================
// Hilfsfunktionen: Aufruf mit einheitlicher Fehlerbehandlung
// F-07: Vorher wiederholte jede API-Funktion fetch, Statusprüfung und throw.
// ============================================================

type RawEntity = Record<string, unknown>;

// Fehler eines API-Aufrufs mit dem HTTP-Status und – falls das Backend eine mitgibt –
// der Meldung aus der Antwort ({ message: "…" }), die sich dem Benutzer zeigen lässt.
export class ApiFehler extends Error {
  constructor(
    text: string,
    public readonly status: number,
    public readonly meldung: string | null
  ) {
    super(text);
    this.name = 'ApiFehler';
  }
}

async function leseMeldung(response: Response): Promise<string | null> {
  try {
    const data = await response.json() as { message?: unknown };
    return typeof data.message === 'string' ? data.message : null;
  } catch {
    return null;
  }
}

// Führt den Aufruf aus und wirft einen ApiFehler, wenn das Backend keinen Erfolg meldet.
async function apiRequest(url: string, fehlertext: string, init: RequestInit = {}): Promise<Response> {
  const response = await apiFetch(url, { cache: 'no-store', ...init });

  if (!response.ok) {
    throw new ApiFehler(
      `${fehlertext}: ${response.statusText}`,
      response.status,
      await leseMeldung(response)
    );
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

// Eine Seite einer Liste samt Gesamtzahl aller Treffer
export interface Seite<T> {
  eintraege: T[];
  gesamt: number;
}

// Zeilen pro Seite in den Listen. Das Backend liefert höchstens 100 Zeilen pro Antwort.
export const SEITENGROESSE = 25;

// Hochkomma verdoppeln, damit eine Eingabe das OData-Stringliteral nicht verlassen kann
function odataText(wert: string): string {
  return wert.replace(/'/g, "''");
}

// Liest eine Seite: $top/$skip blättern, $count=true liefert die Gesamtzahl mit
async function leseSeite<T>(
  url: string,
  seite: number,
  fehlertext: string,
  normalize: (raw: RawEntity) => T
): Promise<Seite<T>> {
  const trenner = url.includes('?') ? '&' : '?';
  const skip    = (Math.max(1, seite) - 1) * SEITENGROESSE;
  const response = await apiRequest(
    `${url}${trenner}$top=${SEITENGROESSE}&$skip=${skip}&$count=true`,
    fehlertext
  );
  const data = await response.json() as { value: RawEntity[]; '@odata.count'?: number };

  return {
    eintraege: data.value.map(normalize),
    gesamt:    data['@odata.count'] ?? data.value.length,
  };
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

// GET /odata/Adressen?$orderby=…&$top=…&$skip=…&$count=true – Eine Seite der Adressen
export async function getAdressenSeite(seite: number): Promise<Seite<Adresse>> {
  return leseSeite(
    `${ODATA_URL}/Adressen?$orderby=name,vorname,id`,
    seite,
    'Fehler beim Abrufen der Adressen',
    normalizeAdresse
  );
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

// GET /odata/Cities?$filter=…&$orderby=…&$top=…&$skip=…&$count=true – Eine Seite der Städte.
// suche filtert nach dem Anfang der PLZ oder einem Teil des Ortsnamens.
export async function getCitiesSeite(seite: number, suche = ''): Promise<Seite<City>> {
  const text   = odataText(suche.trim());
  const filter = text
    ? `&$filter=${encodeURIComponent(
        `startswith(postalCode,'${text}') or contains(tolower(cityName),'${text.toLowerCase()}')`
      )}`
    : '';

  return leseSeite(
    `${ODATA_URL}/Cities?$orderby=postalCode,cityName,id${filter}`,
    seite,
    'Fehler beim Abrufen der Städte',
    normalizeCity
  );
}

// GET /odata/Cities?$filter=startswith(postalCode,'prefix')&$top=10
// Für die PLZ-Wertehilfe im Adressformular. Liefert bei Fehlern eine leere Liste,
// damit die Eingabe im Formular nicht blockiert wird.
export async function sucheStaedteNachPlz(plzPrefix: string): Promise<City[]> {
  if (!plzPrefix || plzPrefix.length < PLZ_SUCHE_MIN_LAENGE) return [];

  const filter = encodeURIComponent(`startswith(postalCode,'${odataText(plzPrefix)}')`);
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
  accentColor: Akzentfarbe;
  // Ob der angemeldete Benutzer die Einstellungen ändern darf (Rolle Admin).
  // Das Backend liest die Rolle bei jeder Anfrage aus der Datenbank.
  canEdit: boolean;
}

/** Was PUT /settings entgegennimmt. */
export type SettingsUpdate = Pick<SettingsData, 'notificationEmail' | 'accentColor'>;

function normalizeSettings(raw: RawEntity): SettingsData {
  return {
    notificationEmail: String(raw['notificationEmail'] ?? ''),
    accentColor:       alsAkzentfarbe(raw['accentColor']),
    canEdit:           raw['canEdit'] === true,
  };
}

// GET /settings – Einstellungen lesen
export async function getSettings(): Promise<SettingsData> {
  const response = await apiRequest(`${BASE_URL}/settings`, 'Fehler beim Lesen der Einstellungen');
  return normalizeSettings(await response.json() as RawEntity);
}

// Akzentfarbe für das Layout. Ohne Anmeldung (Login-Seite) oder bei einem Fehler
// gilt die Standardfarbe, damit die Seite in jedem Fall erscheint.
export async function getAkzentfarbe(): Promise<Akzentfarbe> {
  try {
    const response = await apiFetch(`${BASE_URL}/settings`, { cache: 'no-store' });
    if (!response.ok) return STANDARDFARBE;
    return normalizeSettings(await response.json() as RawEntity).accentColor;
  } catch {
    return STANDARDFARBE;
  }
}

// PUT /settings – Einstellungen speichern
export async function speichereSettings(data: SettingsUpdate): Promise<SettingsData> {
  const response = await apiRequest(
    `${BASE_URL}/settings`,
    'Fehler beim Speichern der Einstellungen',
    jsonRequest('PUT', data)
  );
  return normalizeSettings(await response.json() as RawEntity);
}

// ============================================================
// BENUTZER (nur Rolle Admin; andere erhalten 403)
// ============================================================

const USERS_URL = `${BASE_URL}/users`;

function normalizeBenutzer(raw: RawEntity): Benutzer {
  return {
    id:          Number(raw['id']),
    email:       String(raw['email']       ?? ''),
    displayName: String(raw['displayName'] ?? ''),
    role:        alsRolle(raw['role']),
    lockedUntil: (raw['lockedUntil'] ?? null) as string | null,
    isSelf:      raw['isSelf'] === true,
  };
}

// GET /users – Alle Benutzer
export async function getBenutzerListe(): Promise<Benutzer[]> {
  const response = await apiRequest(USERS_URL, 'Fehler beim Abrufen der Benutzer');
  return (await response.json() as RawEntity[]).map(normalizeBenutzer);
}

// GET /users/id – Einzelnen Benutzer abrufen
export async function getBenutzer(id: number): Promise<Benutzer> {
  const response = await apiRequest(`${USERS_URL}/${id}`, `Benutzer ${id} nicht gefunden`);
  return normalizeBenutzer(await response.json() as RawEntity);
}

// POST /users – Neuen Benutzer anlegen
export async function erstelleBenutzer(benutzer: BenutzerCreate): Promise<Benutzer> {
  const response = await apiRequest(USERS_URL, 'Fehler beim Erstellen', jsonRequest('POST', benutzer));
  return normalizeBenutzer(await response.json() as RawEntity);
}

// PUT /users/id – Anzeigename, Rolle und optional Passwort ändern
export async function aktualisiereBenutzer(id: number, aenderungen: BenutzerUpdate): Promise<void> {
  await apiRequest(`${USERS_URL}/${id}`, 'Fehler beim Aktualisieren', jsonRequest('PUT', aenderungen));
}

// DELETE /users/id – Benutzer löschen
export async function loescheBenutzer(id: number): Promise<void> {
  await apiRequest(`${USERS_URL}/${id}`, 'Fehler beim Löschen', { method: 'DELETE' });
}

// POST /users/id/unlock – Anmeldesperre nach Fehlversuchen aufheben
export async function entsperreBenutzer(id: number): Promise<void> {
  await apiRequest(`${USERS_URL}/${id}/unlock`, 'Fehler beim Entsperren', { method: 'POST' });
}
